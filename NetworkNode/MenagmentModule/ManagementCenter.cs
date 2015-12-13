﻿
using NetworkNode.SDHFrame;
using NetworkNode.HPC;
using NetworkNode.Ports;
using NetworkNode.TTF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkNode.MenagmentModule
{
 
    public class ManagementCenter
    {
        ManagementPort managementPort;
        NetworkNode node;

        public ManagementCenter(ManagementPort managementPort, NetworkNode node)
        {
            this.node = node;
            this.managementPort = managementPort;
        }

        public string PerformManagementAction(string request)
        {
            string[] departedRequest = request.Split('|');
            int argLength = departedRequest.Length - 1;
            string requestType = departedRequest[0];
            List<List<string>> arguments = new List<List<string>>();

            for (int i = 1; i < departedRequest.Length; i++)
            {
                arguments.Add(new List<string>(departedRequest[i].Split('#')));
            }

            String response = "ERROR";
            switch (requestType)
            {
                case "disable-node":
                    {
                        response = disableNode();
                        break;
                    }
                case "shutdown-interface":
                    {
                        response = shutdownInterface(arguments);
                        break;
                    }
                case "sub-conection-HPC":
                    {
                        response = addForwardingRecord(arguments);
                        break;
                    }
                case "get-connection-list":
                    {
                        response = getConnectionList();
                        break;
                    }
                case "get-ports":
                    {
                        response = getPortList();
                        break;
                    }
                case "identify":
                    {
                        response = identify();
                        break;
                    }

            }

            return response;
        }

        private string identify()
        {
            return node.Id.ToString(); ;
        }

        private string disableNode()
        {
            return node.DisableNode() ? "OK" : "ERROR";
        }

        private string shutdownInterface(List<List<string>> testPort)
        {

            int port = int.Parse(testPort[0][0]);
            return node.ShudownInterface(port) ? "OK" : "ERROR";
        }

        private string getPortList()
        {
            List<List<int>> inOutPorts = node.GetPorts();
            StringBuilder builder = new StringBuilder();
            int mainIndex = 0;
            foreach (List<int> ports in inOutPorts)
            {
                int index = 0;
                foreach (int port in ports)
                {

                    builder.Append(port);
                    if (index < ports.Count - 1)
                    {
                        builder.Append("#");
                    } 
                }

                if (mainIndex < inOutPorts.Count - 1)
                {
                    builder.Append("|");
                }
            }
            return builder.ToString(); ;
        }
        private string getConnectionList()
        {
            List<ForwardingRecord> records = node.GetForwardingRecords();
            StringBuilder builder = new StringBuilder();
            int index = 0;
            foreach (ForwardingRecord record in records)
            {
                builder.Append(record.InputPort);
                builder.Append("#");
                builder.Append(record.OutputPort);
                builder.Append("#");
                builder.Append(record.VcNumberIn);
                builder.Append("#");
                builder.Append(record.VcNumberOut);
                builder.Append("#");
                builder.Append(record.ContainerLevel.ToString());
                builder.Append("#");
                builder.Append(record.Stm.ToString());
                if (index < records.Count-1)
                {
                    builder.Append("|");
                }
                
                index++;
            }

            return builder.ToString();
        }

        private string addForwardingRecord(List<List<string>> literalRecords)
        {
            List<ForwardingRecord> records = new List<ForwardingRecord>();
            foreach (List<string> literalRecord in literalRecords)
            {
                int inPort = int.Parse(literalRecord[0]);
                int outPort = int.Parse(literalRecord[1]);
                int inContainer= int.Parse(literalRecord[2]);
                int outContainer = int.Parse(literalRecord[3]);
                VirtualContainerLevel level = VirtualContainerLevelExt.GetContainer(literalRecord[4]);
                StmLevel stm = StmLevelExt.GetContainer(literalRecord[5]);
                records.Add(new ForwardingRecord(inPort, outPort, stm,level,inContainer,outContainer));
            }
            ExecutionResult result = node.AddForwardingRecords(records);

            return result.Result ? "OK" : "ERROR " + result.Msg;
        }

    }
}
