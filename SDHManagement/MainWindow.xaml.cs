using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SDHManagement2.FileUtils;
using SDHManagement2.Models;
using SDHManagement2.SocketUtils;
using Microsoft.Win32;

namespace SDHManagement2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        #region variables
        private SocketHandler socketHandler;
        private List<string> availableModules;
        private List<string> availableConteners;
        private string[] actionList = 
            {"disable-node",
            "close-connection",
            "sub-connection-HPC",
            "get-connection-list",
            "get-ports",
            "load-config"
        };
        // Krosownica
        private string[] actionaNameList =
            {"Dezaktywuj krosownice",
            "Usuń istniejące połączenie",
            "Dodaj nowe połączenie",
            "Pobierz listę istniejących połączeń",
            "Pobierz listę dostępnych portów",
            "Wczytaj plik konfiguracyjny"
        };
        private string[] clientAction =
            {"resource-location",
            "get-resource-list",
            "delete-resource",
            "load-config"
            };
        private string[] clientNameAction =
            {"Przydział zasobów",
            "Pobierz listę przyznanych zasobów",
            "Odbierz przyznane zasoby",
            "Wczytaj plik konfiguracyjny"
            };

        private string[] selection =
            {
             "Krosownica",
             "Klient"
            };
        public List<Router> routerList { get; set; }
        public List<Router> clientList { get; set; }
        private bool initialised = false;
        #endregion
        public MainWindow()
        {
            InitializeComponent();
            selectionBox.IsEnabled = false;
            actionBox.IsEnabled = false;
            nodeBox.IsEnabled = false;
            Load_All.IsEnabled = false;
            console.Items.Add(DateTime.Now.ToString("HH:mm:ss tt") + ": Konsola zarządzania");
            selectionBox.ItemsSource = selection.ToList();
            actionBox.ItemsSource = actionaNameList.ToList();
            button2.IsEnabled = false;
            addNewButton.IsEnabled = false;

        }
        public void Dispose()
        {
            // Area.Dispose(); 
        }
        private void button_Click(object sender, RoutedEventArgs e)
        {
            string nodeName;
            string action;

            nodeName = nodeBox.Text;
            if (selectionBox.SelectedIndex == 0)
            {
                action = actionList[actionBox.SelectedIndex];
            }
            else
            {
                action = clientAction[actionBox.SelectedIndex];
            }

            socketHandler.commandHandle(action, nodeName);

        }
        public void appendConsole(string text, string name, string command)
        {
            if (command == null || name == null)
            {
                console.Items.Add(DateTime.Now.ToString("HH:mm:ss tt") + ": " + text);

            }

            else if (command.Equals("get-ports|"))
            {
                string tmp = stringToPortArray(text);

                console.Items.Add(DateTime.Now.ToString("HH:mm:ss tt")
                                    + ":\n"
                                    + name
                                    + ": Dostępne porty"
                                    + "\n" + tmp);

            }
            else if (command.Equals("get-connection-list|"))
            {
                string[] tmp = stringToConnectionArray(text);


                console.Items.Add(DateTime.Now.ToString("HH:mm:ss tt")
                                    + ":\n"
                                    + name
                                    + " Istniejące połączenia");
                foreach (string con in tmp)
                {
                    console.Items.Add(con);
                }
            }
            else if (command.Equals("get-resource-list|"))
            {
                string[] temp = clientStringToConnectionArray(text);

                console.Items.Add(DateTime.Now.ToString("HH:mm:ss tt")
                                    + ":\n"
                                    + name
                                    + " Istniejące połączenia");
                foreach (string conn in temp)
                {
                    console.Items.Add(conn);
                }
            }
            else
            {
                console.Items.Add(DateTime.Now.ToString("HH:mm:ss tt") + ": " + name + ": " + text);
            }

            console.SelectedIndex = console.Items.Count - 1;
            console.ScrollIntoView(console.SelectedItem);
        }
        private string stringToPortArray(String port_string)
        {
            string[] temp = port_string.Split('|');
            string[] temp2 = new string[temp.Length];
            for (int i = 0; i < temp.Length; i++)
            {
                string[] tmp = temp[i].Split('#');
                temp2[i] = string.Join(":", tmp);
            }
            return string.Join(", ", temp2);

        }
        private string[] stringToConnectionArray(String con_string)
        {
            if (con_string.Equals(""))
            {
                return new string[1] { "" };
            }
            string[] temp_connections = con_string.Split('|');
            string[] connections = new string[temp_connections.Length];

            for (int i = 0; i < temp_connections.Length; i++)
            {
                string[] tmp = temp_connections[i].Split('#');
                int position = i + 1;
                connections[i] = "Polaczanie " + position + ". kontener: " + tmp[2] + "\n" +
                 "Numer 1. portu: " + tmp[0] + ", HP: " + tmp[4] + ", LP: " + tmp[3] +
                 "\nNumer 2.  portu: " + tmp[1] + ", HP: " + tmp[6] + ", LP: " + tmp[5];
            }
            return connections;
        }
        private string[] clientStringToConnectionArray(String constring)
        {
            string[] temp_connections = constring.Split('|');
            string[] connections = new string[temp_connections.Length];

            for (int i = 0; i < temp_connections.Length; i++)
            {
                //port#stm#vclevel#hPath#lPath|port#stm#vclevel#hPath#lPath|.
                string[] tmp = temp_connections[i].Split('#');
                int position = i + 1;
                connections[i] = "Polaczanie " + position + ".\n" +
                    "Numer portu: " + tmp[0] + ". STM na porcie: " + tmp[1] + ". Kontener: " + tmp[2] + ".\n" +
                    "HigherPath: " + tmp[3] + ". LowerPath: " + tmp[4];

            }
            return connections;
        }
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Returnable returnable = ConfigReader.readPortsFromConfig("managementConfigFile.xml");

            List<int> portList = returnable.portList;
            clientList = new List<Router>();
            availableConteners = returnable.contenerList;
            availableModules = returnable.moduleList;

            routerList = new List<Router>();


            if (portList == null)
            {
                appendConsole("Błąd odczytu pliku konfiguracyjnego, spróbuj ponownie.", null, null);
                return;
            }
            appendConsole("Obsługiwany moduł w sieci: " + availableModules[0], null, null);
            appendConsole("Obsługiwane kontenery:", null, null);
            foreach (string s in availableConteners)
            {
                appendConsole(s, null, null);
            }

            socketHandler = new SocketHandler(portList, this, availableModules, availableConteners);
            actionBox.IsEnabled = true;
            nodeBox.IsEnabled = true;
            selectionBox.IsEnabled = true;
            button1.IsEnabled = false;
            button2.IsEnabled = true;
            addNewButton.IsEnabled = true;
            initialised = true;
            Load_All.IsEnabled = true;


        }
        private void button2_Click(object sender, RoutedEventArgs e)
        {
            socketHandler.refresh();
        }
        private void addNewButton_Click(object sender, RoutedEventArgs e)
        {

            Dictionary<int, string> commandDictionary = ConfigReader.readCommandsFromConfig("managementCommandsFile.xml");

            foreach (string s in socketHandler.nodelist)
            {
                try
                {
                    string command = commandDictionary[socketHandler.GetRouter(s).port];
                    socketHandler.sendCommand(s, command, true);
                }
                catch (Exception ex)
                {
                    appendConsole("Experymentalny ficzer coś nie działa :(", null, null);
                }
            }

            foreach (string s in socketHandler.clientNameList)
            {
                try
                {
                    string command = commandDictionary[socketHandler.GetRouter(s).port];
                    socketHandler.sendCommand(s, command, true);
                }
                catch (Exception ex)
                {
                    appendConsole("Experymentalny ficzer coś nie działa :(", null, null);
                }
            }


            return;

        }
        private void nodeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void selectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (!initialised)
            {
                nodeBox.ItemsSource = new List<string>();
                return;

            }
            switch (selectionBox.SelectedItem.ToString())
            {
                case "Klient":
                    nodeBox.ItemsSource = socketHandler.clientNameList;

                    actionBox.ItemsSource = clientNameAction.ToList();
                    break;
                case "Krosownica":

                    nodeBox.ItemsSource = socketHandler.nodelist;
                    actionBox.ItemsSource = actionaNameList.ToList();
                    break;
                default:
                    break;
            }
        }
        private void actionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Load_All_Click(object sender, RoutedEventArgs e)
        {
            String[] lines = null;
            try
            {
                //var y = Path.GetFullPath("..\\..\\..\\config\\");
                OpenFileDialog openFileDialog = new OpenFileDialog();
                string defaultDirectoryPath = Directory.GetCurrentDirectory();
                string path_def = new DirectoryInfo(((((new DirectoryInfo(defaultDirectoryPath).Parent).Parent).Parent).Parent).FullName + "\\Configs").ToString();
                openFileDialog.InitialDirectory = path_def;
                openFileDialog.Filter = "ini files (*.ini)|*.ini|All files (*.*)|*.*";
                //openFileDialog.FilterIndex = 2;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == true)
                {
                    lines = File.ReadAllLines(openFileDialog.FileName);
                }

                //var lines = File.ReadAllLines ("..\\..\\..\\config\\list.ini");

                foreach (string s in lines)
                {
                    string type = s.Split('|')[0]; //node = N, client = C
                    string nodeName = s.Split('|')[1];

                    string path = s.Split('|')[2];
                    string fullpath = Path.GetFullPath("..\\..\\..\\Configs"+path);
                    socketHandler.LoadConfig(nodeName, fullpath );
                    //nodeName = nodeBox.Text;
                    //if (selectionBox.SelectedIndex == 0)
                    //{
                    //     action = actionList[actionBox.SelectedIndex];
                    // }
                    // else
                    // {
                    //  action = clientAction[actionBox.SelectedIndex];
                    // }

                    //socketHandler.commandHandle(action, nodeName);
                }
            }
            catch (Exception) { }
        }
    }
}
