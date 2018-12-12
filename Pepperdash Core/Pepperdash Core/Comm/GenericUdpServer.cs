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
    public class GenericUdpServer : Device
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
        public bool IsEnabled
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
        }

        void CrestronEnvironment_ProgramStatusEventHandler(eProgramStatusEventType programEventType)
        {
            if (programEventType == eProgramStatusEventType.Stopping)
            {
                Debug.Console(1, this, "Program stopping. Disabling Server");
                Disable();
            }
        }

        /// <summary>
        /// Enables the UDP Server
        /// </summary>
        public void Enable()
        {
            if (Server == null)
            {
                Server = new UDPServer();

                // Start receiving data
                Server.ReceiveDataAsync(Receive);
            }

            if (Server.EnableUDPServer() == SocketErrorCodes.SOCKET_OK)
                IsEnabled = true;
        }

        /// <summary>
        /// Disabled the UDP Server
        /// </summary>
        public void Disable()
        {
            Server.DisableUDPServer();

            IsEnabled = false;
        }


        /// <summary>
        /// Recursive method to receive data
        /// </summary>
        /// <param name="server"></param>
        /// <param name="numBytes"></param>
        void Receive(UDPServer server, int numBytes)
        {
            if (numBytes > 0)
            {
                var bytes = server.IncomingDataBuffer.Take(numBytes).ToArray();
                var bytesHandler = BytesReceived;
                if (bytesHandler != null)
                    bytesHandler(this, new GenericCommMethodReceiveBytesArgs(bytes));
                var textHandler = TextReceived;
                if (textHandler != null)
                {
                    var str = Encoding.GetEncoding(28591).GetString(bytes, 0, bytes.Length);
                    textHandler(this, new GenericCommMethodReceiveTextArgs(str));
                }
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

            if (IsEnabled && Server != null)
                Server.SendData(bytes, bytes.Length);
        }

        public void SendBytes(byte[] bytes)
        {
            //if (Debug.Level == 2)
            //    Debug.Console(2, this, "Sending {0} bytes: '{1}'", bytes.Length, ComTextHelper.GetEscapedText(bytes));
            if (IsEnabled && Server != null)
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