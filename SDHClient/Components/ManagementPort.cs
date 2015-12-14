﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NetworkNode.Ports;

namespace Client
{
    public class ClientManagementPort
    {
        private LocalSocektBuilder builder;
        private Socket menagementPort;
        private ClientManagementCenter center;

        public ClientManagementPort(int port)
        {
            builder = LocalSocektBuilder.Instance;
            menagementPort = builder.getTcpSocket(port);
        }

        public void StartListening(ClientManagementCenter center)
        {
            byte[] bytes = new Byte[100000];
            this.center = center;
            try
            {

                menagementPort.Listen(1);

                // Start listening for connections.
                while (true)
                {
                    Console.WriteLine("CLIENT: waiting for a connection wtih management...");
                    Socket handler = menagementPort.Accept();
                    string data = null;

                    // An incoming connection needs to be processed.
                    
                    bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        
                    

                    string response = center.PerformManagementAction(data);

                    // Echo the data back to the client.
                    byte[] msg = Encoding.ASCII.GetBytes(response);

                    handler.Send(msg);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


    }
}
