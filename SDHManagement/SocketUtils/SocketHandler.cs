using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using SDHManagement2.Models;
using SDHManagement2.AdditionalWindows;
using SDHManagement2.FileUtils;
using Microsoft.Win32;
using System.IO;

namespace SDHManagement2.SocketUtils
{
    public class SocketHandler
    {
        #region variables
        private MainWindow mainWindow;
        private List<Router> endPointsList;
        private List<Router> rList;
        private List<Router> cList;
        private List<string> availableConteners;
        private List<string> availableModules;
        public List<string> nodelist { get; set; }
        public List<string> clientNameList { get; set; }
        #endregion
        public SocketHandler(List<int> portList, MainWindow main, List<string> modules, List<string> conteners)
        {
            rList = new List<Router>();
            cList = new List<Router>();
            availableConteners = conteners;
            availableModules = modules;
            mainWindow = main;
            nodelist = new List<string>();
            clientNameList = new List<string>();
            endPointsList = new List<Router>();

            foreach(int port_ in portList)
            {
                Router temp = new Router { port = port_, connected = false };
                endPointsList.Add(temp);
            }

            discoverPorts(endPointsList);

        }
        private void discoverPorts(List<Router> routerList)
        {
            foreach(Router r in routerList)
            {
                r.socket = new RouterSocket(r.port);
                r.socket.TurnOn();
                r.identifier = identify(r);

                if(r.identifier ==null){
                    continue;
                }
                if (r.nodetype.Equals("router"))
                {
                    rList.Add(r);
                    nodelist.Add(r.identifier);
                    mainWindow.appendConsole("Połączono z krosownicą: " + r.identifier + " na porcie: " + r.port, null, null);
                }
                else if (r.nodetype.Equals("client"))
                {
                    cList.Add(r);
                    clientNameList.Add(r.identifier);
                    mainWindow.appendConsole("Połączono z klientem: " + r.identifier + " na porcie: " + r.port, null, null);

                }

            }

        }
        private string identify(Router r)
        {
            RouterSocket targetSocket = r.socket;
            byte[] bytes = new byte[100000];
            string response = null;
            Socket conversationSocket = null;
            try {
                
                targetSocket.InitConversation();
                conversationSocket = targetSocket.GetSocket();
            }
            catch (Exception e)
            {
                mainWindow.appendConsole("Nie można było nawiązać połączania na porcie "+r.port+" ,błąd: "+e.Message, null, null);
            }
            try
            {
                byte[] msg = Encoding.ASCII.GetBytes("identify|");

                conversationSocket.Send(msg);


                bytes = new byte[1024];
                int bytesrec = 0;

                if (conversationSocket.Poll(1000000, SelectMode.SelectRead))
                {
                    bytesrec = conversationSocket.Receive(bytes); // This call will not block
                }
                else
                {
                    mainWindow.appendConsole("Nie można było nawiązać połączenia na porcie " + r.socket.port + ". Spróbuj ponownie", null, null);

                }
                response += Encoding.ASCII.GetString(bytes, 0, bytesrec);

                r.connected = true;

                string[] responseArray = response.Split('|');

                if (responseArray[0].Equals("router"))
                {
                    string routerName = responseArray[1];
                    r.nodetype = "router";
                    return routerName;
                }
                else if (responseArray[0].Equals("client"))
                {
                    string clientName = responseArray[1];
                    r.nodetype = "client";
                    return clientName;
                }
                else
                {
                    mainWindow.appendConsole("Endpoint " + r.port + " podał nieprawidłową odpowiedź. Spróbuj ponownie", null, null);
                    return null;
                }
            }
            catch (Exception e)
            {
                mainWindow.appendConsole("Błąd podczas identyfikacji: " + e.Message,null,null);
                return null;
            }
            
          }
        public Router GetRouter(String name)
        {
            if (rList == null)
            {
                return null;
            }

            foreach (var router in rList)
            {
                if (router.identifier.Equals(name))
                {
                    return router;
                }

            }
            foreach (var client in cList)
            {
                if (client.identifier.Equals(name))
                {
                    return client;
                }
            }
            return null;
        }
        public RouterSocket getClientSocket(String name)
        {
            if (cList == null)
            {
                return null;
            }

            foreach (var client in cList)
            {
                if (client.identifier.Equals(name))
                {
                    return client.socket;
                }

            }
            return null;
        }
        public String sendCommand(String name, String command,bool append)
        {
            Router  targetRouter = GetRouter(name);
            RouterSocket targetSocket = null;
            byte [] bytes = new byte[100000];
            string response=null;
            try {
                targetSocket = targetRouter.socket; 
                targetSocket.InitConversation();
            }catch(Exception e)
            {
                rList.Remove(targetRouter);
                cList.Remove(targetRouter);
                nodelist.Remove(targetRouter.identifier);
                clientNameList.Remove(targetRouter.identifier);
                mainWindow.nodeBox.ItemsSource = nodelist;
                int selected = mainWindow.selectionBox.SelectedIndex;
                mainWindow.selectionBox.SelectedIndex = (selected == 1 ? 0 : 1);
                targetRouter.connected = false;
                mainWindow.appendConsole(string.Format("{0} nie odpowiada. Błąd {1}.Spróbuj ponownie po odświeżeniu.", name,e.Message), null, null);
                return null;
            }
            Socket conversationSocket = targetSocket.GetSocket();
            
            try
            {
                byte[] msg = Encoding.ASCII.GetBytes(command);

                conversationSocket.Send(msg);
                
                //while (true)
                {

                    bytes=new byte[1024];
                    int bytesRec = conversationSocket.Receive(bytes);
                    response += Encoding.ASCII.GetString(bytes, 0, bytesRec);

                    if (append)
                    {
                        mainWindow.appendConsole(response, name, command);
                    }
                }
                
            }
            catch (Exception e)
            {
                mainWindow.appendConsole(string.Format("Polecenie nie mogło być zrealizowane przez błąd {0}. Spróbuj ponownie",e.Message),null,null);
            }
            return response;
        }
        public string addConfig (string node, string path, char mode)
        {
            LoadConfig(node,path);
            return "DONE";


        }
        public string commandHandle(string command, string node)
        {
            if (node.Length < 1)
            {
                mainWindow.appendConsole("Nie wybrano docelowego punktu końcowego", null, null);
                return null;
            }
            switch (command)
            {
                // do klienta
                case "resource-location":
                    string portresponse = sendCommand(node, "get-ports|", false);
                    string con_response = sendCommand(node, "get-resource-list|", false);
                    ResourceRelocationWindow relocationwindow = new ResourceRelocationWindow(this, node,portresponse,con_response,availableModules,availableConteners);
                    relocationwindow.ShowDialog();
                    return "DONE";
                // do klienta
                case "get-resource-list":
                    return sendCommand(node, command + "|", true);
                // do klienta
                case "delete-resource":
                    string conresponse = sendCommand(node, "get-resource-list|", false);
                    DropConnectionWindow deleteWindow = new DropConnectionWindow(this, conresponse, node, mainWindow, "client");
                    deleteWindow.ShowDialog();
                    return "DONE";
                // do krosownicy
                case "disable-node":
                    return sendCommand(node, command+"|",true);
                // do krosownicy
                case "close-connection":
                    string connresponse = sendCommand(node, "get-connection-list|",false);
                    DropConnectionWindow dropWindow = new DropConnectionWindow(this,connresponse,node,mainWindow,"router");
                    dropWindow.ShowDialog();
                    return "DONE";
                // do krosownicy
                case "sub-connection-HPC":
                    string port_response = sendCommand(node, "get-ports|",false);
                    string connectionresponse = sendCommand(node, "get-connection-list|",false);
                    AddConnectionWindow window = new AddConnectionWindow(this,port_response,connectionresponse,node,availableModules,availableConteners);
                    window.ShowDialog();
                    return "DONE";
                // do krosownicy
                case "get-connection-list":
                    return sendCommand(node, command + "|",true);
                // do klienta i krosownicy
                case "get-ports":
                    return sendCommand(node, command + "|",true);
                case "load-config":
                    LoadConfig(node);
                    return "DONE";
                default:
                    mainWindow.appendConsole("Stało się niemożliwe",null,null);
                    return "ERROR";

            }
        }
        public void LoadConfig(String nodeName)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            string defaultDirectoryPath = Directory.GetCurrentDirectory();
            string path = new DirectoryInfo(((((new DirectoryInfo(defaultDirectoryPath).Parent).Parent).Parent).Parent).FullName + "\\Configs").ToString();
            openFileDialog.InitialDirectory = path;
            openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog.FilterIndex = 2;
            openFileDialog.RestoreDirectory = true;

            try
            {

                string result = "";
                if (openFileDialog.ShowDialog() == true)
                    result = File.ReadAllText(openFileDialog.FileName);
                this.sendCommand(nodeName, result, true);
            }
            catch (Exception ex)
            {

            }
        }
        public void LoadConfig(String nodeName, String path)
        {
            //OpenFileDialog openFileDialog = new OpenFileDialog();
            //string defaultDirectoryPath = Directory.GetCurrentDirectory();
            //string path = new DirectoryInfo(((((new DirectoryInfo(defaultDirectoryPath).Parent).Parent).Parent).Parent).FullName + "\\Configs").ToString();
            //openFileDialog.InitialDirectory = path;
            //openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            //openFileDialog.FilterIndex = 2;
            //openFileDialog.RestoreDirectory = true;

            try
            {

                string result = "";
              //  if (openFileDialog.ShowDialog() == true)
                    result = File.ReadAllText(path);
                this.sendCommand(nodeName, result, true);
            }
            catch (Exception ex)
            {

            }
        }
        public void refresh()
        {

            foreach (var router in endPointsList)
            {
                if (!router.connected)
                { 
                    mainWindow.appendConsole(string.Format("Próba połączenia na porcie {0}",router.port),null,null);

                try
                    {
                        router.socket = new RouterSocket(router.port);
                        router.socket.TurnOn();
                        string name = identify(router);

                        if (name == null)
                        {
                            continue;
                        }
                        router.identifier = name;
                        router.connected = true;

                        
                        if (router.nodetype.Equals("router"))
                        {
                            rList.Add(router);
                            nodelist.Add(router.identifier);
                            mainWindow.appendConsole("Połączono z krosownicą: " + router.identifier + " na porcie: " + router.port, null, null);

                        }
                        else if (router.nodetype.Equals("client"))
                        {
                            cList.Add(router);
                            clientNameList.Add(router.identifier);
                            mainWindow.appendConsole("Połączono z klientem: " + router.identifier + " na porcie: " + router.port, null, null);

                        }

                }
                catch (Exception e)
                {
                    mainWindow.appendConsole(string.Format("Nie można było połączyć się z {0} z powodu błędu: {1}",router.identifier,e.Message),null,null);
                }
            }
        }
       
        }
    }
}
