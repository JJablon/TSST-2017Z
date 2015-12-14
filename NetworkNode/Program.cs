using NetworkNode.SDHFrame;
using NetworkNode.HPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkNode
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            Frame frame = new Frame(StmLevel.STM256);
            frame.Msoh = new Header("test", "test2", "test");
            frame.Rsoh = new Header("test", "test2", "test");

            frame.SetVirtualContainer(VirtualContainerLevel.VC32, 0, new VirtualContainer(VirtualContainerLevel.VC32));
            frame.SetVirtualContainer(VirtualContainerLevel.VC21, 10, new VirtualContainer(VirtualContainerLevel.VC21));


            FrameBuilder fmb = new SDHFrame.FrameBuilder();
            string var = fmb.BuildLiteral(frame);
            */
            
            
            if (args.Length == 0)
            {
                Console.WriteLine("Input parameter missing");
            }

			String id = args[0];

//         	NetworkNodeSetupProcess setUpProcess = new NetworkNodeSetupProcess("..\\..\\..\\Configs\\nodeConfig" + id + ".xml");


            NetworkNode node = setUpProcess.startNodeProcess();
            ForwardingRecord record = new ForwardingRecord(4000, 6000, StmLevel.STM1, VirtualContainerLevel.VC32, 0, 1);
            List<ForwardingRecord> records = new List<ForwardingRecord>();
            records.Add(record);
            node.AddForwardingRecords(records);
            Console.WriteLine("Start emulation");
            startReadingCommands(node);
            //node.emulateManagement("sub-conection-HPC|1002-2003#|");


        }

        private static void startReadingCommands(NetworkNode node) {
            while (true) 
            {
                Console.WriteLine("Oczekiwanie na polecenie");
                Console.WriteLine("R - kana� rozm�wny rsoh");
                Console.WriteLine("M - kana� rozm�wny msoh");

                string command = Console.ReadLine();
                
                if (command.Equals("R"))
                {
                    string args = takeArgs();
                    node.AddRsohContent(args);

                } 
                else if (command.Equals("M"))
                {
                    string args = takeArgs();
                    node.AddMsohContent(args);
                }
                else
                {
                    Console.WriteLine("Nie odnaleziono polecenia");
                    continue;
                }
            }
        }

        private static string takeArgs()
        {
            Console.WriteLine("Wprowad� dane");
            return Console.ReadLine();
        }
    }

}