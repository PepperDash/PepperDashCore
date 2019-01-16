using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace PepperDash.Core
{
    public class GenericUdpServer : Device, IBasicCommunication
    {
        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<GenericCommMethodReceiveBytesArgs> BytesReceived;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler<GenericCommMethodReceiveTextArgs> TextReceived;

        /// <summary>
        /// 
        /// </summary>
        //public event GenericSocketStatusChangeEventDelegate SocketStatusChange;
        public event EventHandler<GenericSocketStatusChageEventArgs> ConnectionChange;

        public SocketStatus ClientStatus
        {
            get
            {
                return Server.ServerStatus;
            }
        }

        /// <summary>
        /// Address of server
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// Port on server
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Another damn S+ helper because S+ seems to treat large port nums as signed ints
        /// which screws up things
        /// </summary>
        public ushort UPort
        {
            get { return Convert.ToUInt16(Port); }
            set { Port = Convert.ToInt32(value); }
        }

        /// <summary>
        /// Indicates that the UDP Server is enabled
        /// </summary>
        public bool IsConnected
        {
            get;
            private set;
        }

        /// <summary>
        /// Defaults to 2000
        /// </summary>
        public int BufferSize { get; set; }

        public UDPServer Server { get; private set; }

        public GenericUdpServer(string key, string address, int port, int buffefSize)
            : base(key)
        {
            Hostname = address;
            Port = port;
            BufferSize = buffefSize;

            CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(CrestronEnvironment_ProgramStatusEventHandler);
            CrestronEnvironment.EthernetEventHandler += new EthernetEventHandler(CrestronEnvironment_EthernetEventHandler);
        }

        void CrestronEnvironment_EthernetEventHandler(EthernetEventArgs ethernetEventArgs)
        {
            // Re-enable the server if the link comes back up and the status should be connected
            if (ethernetEventArgs.EthernetEventType == eEthernetEventType.LinkUp
                && IsConnected)
            {
                Connect();
            }
        }

        void CrestronEnvironment_ProgramStatusEventHandler(eProgramStatusEventType programEventType)
        {
            if (programEventType == eProgramStatusEventType.Stopping)
            {
                Debug.Console(1, this, "Program stopping. Disabling Server");
                Disconnect();
            }
        }

        /// <summary>
        /// Enables the UDP Server
        /// </summary>
        public void Connect()
        {
            if (Server == null)
            {
                Server = new UDPServer();

            }

            if (string.IsNullOrEmpty(Hostname))
            {
                Debug.Console(1, Debug.ErrorLogLevel.Warning, "GenericUdpServer '{0}': No address set", Key);
                return;
            }
            if (Port < 1 || Port > 65535)
            {
                {
                    Debug.Console(1, Debug.ErrorLogLevel.Warning, "GenericUdpServer '{0}': Invalid port", Key);
                    return;
                }
            }

            var status = Server.EnableUDPServer(Hostname, Port);

            Debug.Console(2, this, "SocketErrorCode: {0}", status);
            if (status == SocketErrorCodes.SOCKET_OK)
                IsConnected = true;

            // Start receiving data
            Server.ReceiveDataAsync(Receive);
        }

        /// <summary>
        /// Disabled the UDP Server
        /// </summary>
        public void Disconnect()
        {
            if(Server != null)
                Server.DisableUDPServer();

            IsConnected = false;
        }


        /// <summary>
        /// Recursive method to receive data
        /// </summary>
        /// <param name="server"></param>
        /// <param name="numBytes"></param>
        void Receive(UDPServer server, int numBytes)
        {
            Debug.Console(2, this, "Received {0} bytes", numBytes);

            if (numBytes > 0)
            {
                var bytes = server.IncomingDataBuffer.Take(numBytes).ToArray();

                Debug.Console(2, this, "Bytes: {0}", bytes.ToString());
                var bytesHandler = BytesReceived;
                if (bytesHandler != null)
                    bytesHandler(this, new GenericCommMethodReceiveBytesArgs(bytes));
                else
                    Debug.Console(2, this, "bytesHandler is null");
                var textHandler = TextReceived;
                if (textHandler != null)
                {
                    var str = Encoding.GetEncoding(28591).GetString(bytes, 0, bytes.Length);
                    Debug.Console(2, this, "RX: {0}", str);
                    textHandler(this, new GenericCommMethodReceiveTextArgs(str));
                }
                else
                    Debug.Console(2, this, "textHandler is null");
            }
            server.ReceiveDataAsync(Receive);          
        }

        /// <summary>
        /// General send method
        /// </summary>
        /// <param name="text"></param>
        public void SendText(string text)
        {
            var bytes = Encoding.GetEncoding(28591).GetBytes(text);

            if (IsConnected && Server != null)
            {
                Debug.Console(2, this, "TX: {0}", text);
                Server.SendData(bytes, bytes.Length);
            }
        }

        public void SendBytes(byte[] bytes)
        {
            //if (Debug.Level == 2)
            //    Debug.Console(2, this, "Sending {0} bytes: '{1}'", bytes.Length, ComTextHelper.GetEscapedText(bytes));
            if (IsConnected && Server != null)
                Server.SendData(bytes, bytes.Length);
        }



    }

    public class UdpServerPropertiesConfig
    {
        [JsonProperty(Required = Required.Always)]
        public string Address { get; set; }

        [JsonProperty(Required = Required.Always)]
        public int Port { get; set; }

        /// <summary>
        /// Defaults to 32768
        /// </summary>
        public int BufferSize { get; set; }

        public UdpServerPropertiesConfig()
        {
            BufferSize = 32768;
        }
    }
}