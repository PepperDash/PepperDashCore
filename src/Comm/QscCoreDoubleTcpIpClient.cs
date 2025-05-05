using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core
{
    /// <summary>
    /// Allows for two simultaneous TCP clients to connect to a redundant pair of QSC Core DSPs and manages 
    /// </summary>
    public class QscCoreDoubleTcpIpClient : IKeyed
    {
        /// <summary>
        /// Key to uniquely identify the instance of the class
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Fires when a bool value changes to notify the S+ module
        /// </summary>
        public event EventHandler<BoolChangeEventArgs> BoolChange;
        /// <summary>
        /// Fires when a ushort value changes to notify the S+ module
        /// </summary>
        public event EventHandler<UshrtChangeEventArgs> UshortChange;
        /// <summary>
        /// Fires when a string value changes to notify the S+ module
        /// </summary>
        public event EventHandler<StringChangeEventArgs> StringChange;

        /// <summary>
        /// The client for the master DSP unit
        /// </summary>
        public GenericTcpIpClient MasterClient { get; private set; }
        /// <summary>
        /// The client for the slave DSP unit
        /// </summary>
        public GenericTcpIpClient SlaveClient { get; private set; }

        string Username;
        string Password;
        string LineEnding;

        CommunicationGather MasterGather;
        CommunicationGather SlaveGather;

        bool IsPolling;
        int PollingIntervalSeconds;
        CTimer PollTimer;

        bool SlaveIsActive;

        /// <summary>
        /// Default constuctor for S+
        /// </summary>
        public QscCoreDoubleTcpIpClient()
        {
            MasterClient = new GenericTcpIpClient("temp-master");
            MasterClient.AutoReconnect = true;
            MasterClient.AutoReconnectIntervalMs = 2000;
            SlaveClient = new GenericTcpIpClient("temp-slave");
            SlaveClient.AutoReconnect = true;
            SlaveClient.AutoReconnectIntervalMs = 2000;

        }

        /// <summary>
        /// Connects to both DSP units
        /// </summary>
        /// <param name="key"></param>
        /// <param name="masterAddress"></param>
        /// <param name="masterPort"></param>
        /// <param name="slaveAddress"></param>
        /// <param name="slavePort"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="pollingIntervalSeconds"></param>
        /// <param name="lineEnding"></param>
        public void Connect(string key, string masterAddress, int masterPort,
            string slaveAddress, int slavePort, string username, string password,
            int pollingIntervalSeconds, string lineEnding)
        {
            Key = key;

            PollingIntervalSeconds = pollingIntervalSeconds;
            Username = username;
            Password = password;
            LineEnding = lineEnding;

            MasterClient.Initialize(key + "-master");
            SlaveClient.Initialize(key + "-slave");

            MasterClient.Hostname = masterAddress;
            MasterClient.Port = masterPort;

            if (MasterClient != null)
            {
                MasterClient.Disconnect();
            }

            if (SlaveClient != null)
            {
                SlaveClient.Disconnect();
            }

            if (MasterGather == null)
            {
                MasterGather = new CommunicationGather(MasterClient, lineEnding);
                MasterGather.IncludeDelimiter = true;
            }

            MasterGather.LineReceived -= MasterGather_LineReceived;
            MasterGather.LineReceived += new EventHandler<GenericCommMethodReceiveTextArgs>(MasterGather_LineReceived);

            MasterClient.ConnectionChange -= MasterClient_SocketStatusChange;
            MasterClient.ConnectionChange += MasterClient_SocketStatusChange;

            SlaveClient.Hostname = slaveAddress;
            SlaveClient.Port = slavePort;

            if (SlaveGather == null)
            {
                SlaveGather = new CommunicationGather(SlaveClient, lineEnding);
                SlaveGather.IncludeDelimiter = true;
            }

            SlaveGather.LineReceived -= MasterGather_LineReceived;
            SlaveGather.LineReceived += new EventHandler<GenericCommMethodReceiveTextArgs>(SlaveGather_LineReceived);

            SlaveClient.ConnectionChange -= SlaveClient_SocketStatusChange;
            SlaveClient.ConnectionChange += SlaveClient_SocketStatusChange;

            MasterClient.Connect();
            SlaveClient.Connect();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Disconnect()
        {
            if (MasterClient != null)
            {
                MasterGather.LineReceived -= MasterGather_LineReceived;
                MasterClient.Disconnect();
            }
            if (SlaveClient != null)
            {
                SlaveGather.LineReceived -= SlaveGather_LineReceived;
                SlaveClient.Disconnect();
            }
            if (PollTimer != null)
            {
                IsPolling = false;

                PollTimer.Stop();
                PollTimer = null;
            }
        }

        /// <summary>
        /// Does not include line feed
        /// </summary>
        public void SendText(string s)
        {
            if (SlaveIsActive)
            {
                if (SlaveClient != null)
                {
                    Debug.Console(2, this, "Sending to Slave: {0}", s);
                    SlaveClient.SendText(s);
                }
            }
            else
            {
                if (MasterClient != null)
                {
                    Debug.Console(2, this, "Sending to Master: {0}", s);
                    MasterClient.SendText(s);
                }
            }
        }

        void MasterClient_SocketStatusChange(object sender, GenericSocketStatusChageEventArgs args)
        {
            OnUshortChange((ushort)args.Client.ClientStatus, MasterClientStatusId);

            if (args.Client.IsConnected)
            {
                MasterGather.LineReceived += MasterGather_LineReceived;

                StartPolling();
            }
            else
                MasterGather.LineReceived -= MasterGather_LineReceived;
        }

        void SlaveClient_SocketStatusChange(object sender, GenericSocketStatusChageEventArgs args)
        {
            OnUshortChange((ushort)args.Client.ClientStatus, SlaveClientStatusId);

            if (args.Client.IsConnected)
            {
                SlaveGather.LineReceived += SlaveGather_LineReceived;

                StartPolling();
            }
            else
                SlaveGather.LineReceived -= SlaveGather_LineReceived;

        }


        void MasterGather_LineReceived(object sender, GenericCommMethodReceiveTextArgs e)
        {
            if (e.Text.Contains("login_required"))
            {
                MasterClient.SendText(string.Format("login {0} {1} \x0d\x0a", Username, Password));
            }
            else if (e.Text.Contains("login_success"))
            {
                // START THE POLLING, YO!
            }
            else if (e.Text.StartsWith("sr"))
            {
                // example response  "sr "MyDesign" "NIEC2bxnVZ6a" 1 1"

                var split = e.Text.Trim().Split(' ');
                if (split[split.Length - 1] == "1")
                {
                    SlaveIsActive = false;
                    OnBoolChange(false, SlaveIsActiveId);
                    OnBoolChange(true, MasterIsActiveId);
                }
            }
            if (!SlaveIsActive)
                OnStringChange(e.Text, LineReceivedId);
        }

        void SlaveGather_LineReceived(object sender, GenericCommMethodReceiveTextArgs e)
        {
            if (e.Text.Contains("login_required"))
            {
                SlaveClient.SendText(string.Format("login {0} {1} \x0d\x0a", Username, Password));
            }
            else if (e.Text.Contains("login_success"))
            {
                // START THE POLLING, YO!
            }
            else if (e.Text.StartsWith("sr"))
            {
                var split = e.Text.Trim().Split(' ');
                if (split[split.Length - 1] == "1")
                {
                    SlaveIsActive = true;
                    OnBoolChange(true, SlaveIsActiveId);
                    OnBoolChange(false, MasterIsActiveId);
                }
            }
            if (SlaveIsActive)
                OnStringChange(e.Text, LineReceivedId);
        }

        void StartPolling()
        {
            if (!IsPolling)
            {
                IsPolling = true;

                Poll();
                if (PollTimer != null)
                    PollTimer.Stop();

                PollTimer = new CTimer(o => Poll(), null, PollingIntervalSeconds * 1000, PollingIntervalSeconds * 1000);
            }
        }

        void Poll()
        {
            if (MasterClient != null && MasterClient.IsConnected)
            {
                Debug.Console(2, this, "Polling Master.");
                MasterClient.SendText("sg\x0d\x0a");

            }
            if (SlaveClient != null && SlaveClient.IsConnected)
            {
                Debug.Console(2, this, "Polling Slave.");
                SlaveClient.SendText("sg\x0d\x0a");
            }
        }



        // login NAME PIN ---> login_success, login_failed

        // status get
        // sg --> sr DESIGN_NAME DESIGN_ID IS_PRIMARY IS_ACTIVE

        // CRLF

        void OnBoolChange(bool state, ushort type)
        {
            var handler = BoolChange;
            if (handler != null)
                handler(this, new BoolChangeEventArgs(state, type));
        }

        void OnUshortChange(ushort state, ushort type)
        {
            var handler = UshortChange;
            if (handler != null)
                handler(this, new UshrtChangeEventArgs(state, type));
        }

        void OnStringChange(string value, ushort type)
        {
            var handler = StringChange;
            if (handler != null)
                handler(this, new StringChangeEventArgs(value, type));
        }

        /// <summary>
        /// 
        /// </summary>
        public const ushort MasterIsActiveId = 3;
        /// <summary>
        /// 
        /// </summary>
        public const ushort SlaveIsActiveId = 4;
        /// <summary>
        /// 
        /// </summary>
        public const ushort MainModuleInitiailzeId = 5;

        /// <summary>
        /// 
        /// </summary>
        public const ushort MasterClientStatusId = 101;
        /// <summary>
        /// 
        /// </summary>
        public const ushort SlaveClientStatusId = 102;

        /// <summary>
        /// 
        /// </summary>
        public const ushort LineReceivedId = 201;
    }
}