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
        /// This event will fire when a message is dequeued that includes the source IP and Port info if needed to determine the source of the received data.
        /// </summary>
		public event EventHandler<GenericUdpReceiveTextExtraArgs> DataRecievedExtra;

        /// <summary>
        /// Queue to temporarily store received messages with the source IP and Port info
        /// </summary>
		private CrestronQueue<GenericUdpReceiveTextExtraArgs> MessageQueue;

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


		CCriticalSection DequeueLock;
        /// <summary>
        /// Address of server
        /// </summary>
        public string Hostname { get; set; }

		/// <summary>
		/// IP Address of the sender of the last recieved message 
		/// </summary>


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

			DequeueLock = new CCriticalSection();
			MessageQueue = new CrestronQueue<GenericUdpReceiveTextExtraArgs>();

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
				var sourceIp = Server.IPAddressLastMessageReceivedFrom;
				var sourcePort = Server.IPPortLastMessageReceivedFrom;
                var bytes = server.IncomingDataBuffer.Take(numBytes).ToArray();
				var str = Encoding.GetEncoding(28591).GetString(bytes, 0, bytes.Length);
				MessageQueue.TryToEnqueue(new GenericUdpReceiveTextExtraArgs(str, sourceIp, sourcePort, bytes));

				Debug.Console(2, this, "Bytes: {0}", bytes.ToString());
                var bytesHandler = BytesReceived;
                if (bytesHandler != null)
                    bytesHandler(this, new GenericCommMethodReceiveBytesArgs(bytes));
                else
                    Debug.Console(2, this, "bytesHandler is null");
                var textHandler = TextReceived;
                if (textHandler != null)
                {
                   
                    Debug.Console(2, this, "RX: {0}", str);
                    textHandler(this, new GenericCommMethodReceiveTextArgs(str));
                }
                else
                    Debug.Console(2, this, "textHandler is null");
            }
            server.ReceiveDataAsync(Receive);

            //  Attempt to enter the CCritical Secion and if we can, start the dequeue thread 
            var gotLock = DequeueLock.TryEnter();
            if (gotLock)
                CrestronInvoke.BeginInvoke((o) => DequeueEvent());
        }

        /// <summary>
        /// This method gets spooled up in its own thread an protected by a CCriticalSection to prevent multiple threads from running concurrently.
        /// It will dequeue items as they are enqueued automatically.
        /// </summary>
		void DequeueEvent()
		{
			try
			{
				while (true)
				{
					// Pull from Queue and fire an event. Block indefinitely until an item can be removed, similar to a Gather.
					var message = MessageQueue.Dequeue();
					var dataRecivedExtra = DataRecievedExtra;
					if (dataRecivedExtra != null)
					{
						dataRecivedExtra(this, message);
					}
				}
			}
			catch (Exception e)
			{
				Debug.Console(0, "GenericUdpServer DequeueEvent error: {0}\r", e);
			}
			// Make sure to leave the CCritical section in case an exception above stops this thread, or we won't be able to restart it.
			if (DequeueLock != null)
			{
				DequeueLock.Leave();
			}
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

	public class GenericUdpReceiveTextExtraArgs : EventArgs
	{
		public string Text { get; private set; }
		public string IpAddress { get; private set; }
		public int	Port { get; private set; }
		public byte[] Bytes { get; private set; }

		public GenericUdpReceiveTextExtraArgs(string text, string ipAddress, int port, byte[] bytes)
		{
			Text = text;
			IpAddress = ipAddress;
			Port = port;
			Bytes = bytes;
		}

		/// <summary>
		/// Stupid S+ Constructor
		/// </summary>
		public GenericUdpReceiveTextExtraArgs() { }
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