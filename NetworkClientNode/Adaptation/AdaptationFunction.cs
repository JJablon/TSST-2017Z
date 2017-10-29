using NetworkNode.SDHFrame;
using NetworkNode.TTF;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;

namespace NetworkClientNode.Adaptation
{
    public class ClientData : EventArgs 
    {
        public Dictionary<StreamData,string> StreamsData {get;set;}
        public ClientData (Dictionary<StreamData,string> streamsData) 
        {
            StreamsData = streamsData;
        }
        public string ToString()
        {
            string returnString = string.Empty;

            foreach (var item in StreamsData)
            {
                returnString += item.Value + "\n";
            }
            return returnString;
        }
    }
    public delegate void HandleClientData(ClientData data);
    public class AdaptationFunction
    {
        private TransportTerminalFunction Ttf;
        private List<StreamData> Streams;
        private Dictionary<int, IFrame> OutputCredentials;
        private FrameBuilder Builder;

        public event HandleClientData HandleClientData;
        public AdaptationFunction(TransportTerminalFunction ttf)
        {
            Ttf = ttf;
            Builder = new FrameBuilder();
            Ttf.HandleInputFrame += new HandleInputFrame(GetDataFromFrame);

            this.Streams = new List<StreamData>();

            Dictionary<int, StmLevel> portsLevels = Ttf.GetPorts();
            OutputCredentials = new Dictionary<int, IFrame>();
            
            foreach (int portNumber in portsLevels.Keys)
            {
                OutputCredentials.Add(portNumber, new Frame(portsLevels[portNumber]));
            }

        }

        public void GetDataFromFrame(object sender, InputFrameArgs args)
        {
            int inputPort = args.PortNumber;
            List<StreamData> streamForPort = new List<StreamData>();
            Dictionary<StreamData, string> streamsData = new Dictionary<StreamData, string>();
            foreach (StreamData stream in Streams)
            {
                if (stream.Port == inputPort)
                {
                    string content = null;

                    VirtualContainer vc = (VirtualContainer)args.Frame.GetVirtualContainer(stream.VcLevel, stream.HigherPath, stream.LowerPath);
                    if (vc != null)
                    {
                        Container conteriner = vc.Content.Count > 0 ? vc.Content[0] as Container : null;
                        content = "";


                        if (conteriner != null)
                        {
                            content = conteriner.Content;
                            if (content == null || content.Equals("")
                                || Regex.Matches(content,"\\S{1,}").Count ==0
                                )
                            {
                                continue;
                            }
                        }
                        else
                        {
                           // content = "Error: Container does not transport client data";
                        }
                    }
                    else
                    {
                        //content = "Error: Container does not transport client data";
                    }

                    streamsData.Add(stream, content);
                }
            }

            if (HandleClientData != null)
            {
                HandleClientData(new ClientData(streamsData));
            }
        }
        public void SentData(Dictionary<StreamData,string> dataToSent)
        {
            Dictionary<int, IFrame> outputData = new Dictionary<int, IFrame>();
            foreach (StreamData stream in dataToSent.Keys)
            {
                if (!Streams.Contains(stream))
                {
                    //TODO można rzucać wyjątek
                    continue;
                }

                if (!outputData.ContainsKey(stream.Port))
                {
                    outputData.Add(stream.Port, new Frame(stream.Stm));
                }

                //TODO ewentualne dzielenie danych
                Container content = new Container(dataToSent[stream]);
                VirtualContainer vc = new VirtualContainer(stream.VcLevel, content);
                outputData[stream.Port].SetVirtualContainer(stream.VcLevel, stream.HigherPath, stream.LowerPath, vc);
            }
            
            Ttf.PassDataToInterfaces(outputData);        
        }

        public ExecutionResult AddStreamData(List<StreamData> records)
        {
            int index = 0;
            foreach (StreamData record in records)
            {

                if (!CheckStreamData(record))
                {
                    return new ExecutionResult(false, "Error at record " + index);
                }
                index++;
            }

            Streams.AddRange(records);

            return new ExecutionResult(true, null);
        }

        private bool CheckStreamData(StreamData record)
        {
            VirtualContainer vc = new VirtualContainer(record.VcLevel);
            return OutputCredentials[record.Port].SetVirtualContainer(record.VcLevel, record.HigherPath, record.LowerPath, vc);
            
        }

        private bool ClearCredentials(StreamData record)
        {
            Frame outputCredential = (Frame)OutputCredentials[record.Port];
            return outputCredential.ClearVirtualContainer(record.VcLevel, record.HigherPath, record.LowerPath);
        }


        public Dictionary<int, StmLevel> GetPorts()
        {
            return Ttf.GetPorts();
        }

        public List<StreamData> GetStreamData()
        {
            return Streams;
        }

        public bool RemoveStreamData(StreamData record)
        {
            if (Streams.Contains(record))
            {
                Streams.Remove(record);
                ClearCredentials(record);
                return true;
            }
            return false;
        }
    }
}
