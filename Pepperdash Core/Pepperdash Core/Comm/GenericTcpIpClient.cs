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
    /// <summary>
    /// A class to handle basic TCP/IP communications with a server
    /// </summary>
	public class GenericTcpIpClient : Device, ISocketStatusWithStreamDebugging, IAutoReconnect
    {
        private const string SplusKey = "Uninitialized TcpIpClient";
        /// <summary>
        /// Object to enable stream debugging
        /// </summary>
        public CommunicationStreamDebugging StreamDebugging { get; private set; }

		/// <summary>
		/// Fires when data is received from the server and returns it as a Byte array
		/// </summary>
		public event EventHandler<GenericCommMethodReceiveBytesArgs> BytesReceived;

		/// <summary>
		/// Fires when data is received from the server and returns it as text
		/// </summary>
		public event EventHandler<GenericCommMethodReceiveTextArgs> TextReceived;

		/// <summary>
		/// 
		/// </summary>
		//public event GenericSocketStatusChangeEventDelegate SocketStatusChange;
		public event EventHandler<GenericSocketStatusChageEventArgs> ConnectionChange;


		private string _Hostname { get; set;} 
        /// <summary>
        /// Address of server
        /// </summary>
        public string Hostname {
			get
			{
				return _Hostname;
			}

			set
			{
				_Hostname = value;
				if (Client != null)
				{

					Client.AddressClientConnectedTo = _Hostname;
				}
			}
		}

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
        /// Defaults to 2000
        /// </summary>
        public int BufferSize { get; set; }

		/// <summary>
		/// The actual client class
		/// </summary>
		public TCPClient Client { get; private set; }

		/// <summary>
		/// True if connected to the server
		/// </summary>
		public bool IsConnected 
        { 
            get { return Client != null && Client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED; } 
        }
        
        /// <summary>
        /// S+ helper for IsConnected
        /// </summary>
        public ushort UIsConnected
        {
            get { return (ushort)(IsConnected ? 1 : 0); }
        }

		/// <summary>
		/// Status of the socket
		/// </summary>
		public SocketStatus ClientStatus 
        { 
            get 
            {
                if (Client == null)
                    return SocketStatus.SOCKET_STATUS_NO_CONNECT;
                return Client.ClientStatus; 
            } 
        }

        /// <summary>
        /// Contains the familiar Simpl analog status values. This drives the ConnectionChange event
        /// and IsConnected with be true when this == 2.
        /// </summary>
        public ushort UStatus
        {
            get { return (ushort)ClientStatus; }
        }

		/// <summary>
        /// Status of the socket
		/// </summary>
		public string ClientStatusText { get { return ClientStatus.ToString(); } }

		/// <summary>
		/// Ushort representation of client status
		/// </summary>
        [Obsolete]
		public ushort UClientStatus { get { return (ushort)ClientStatus; } }

		/// <summary>
		/// Connection failure reason
		/// </summary>
		public string ConnectionFailure { get { return ClientStatus.ToString(); } }

		/// <summary>
		/// If true, enables AutoConnect
		/// </summary>
		public bool AutoReconnect { get; set; }

        /// <summary>
        /// S+ helper for AutoReconnect
        /// </summary>
        public ushort UAutoReconnect
        {
            get { return (ushort)(AutoReconnect ? 1 : 0); }
            set { AutoReconnect = value == 1; }
        }
		/// <summary>
		/// Milliseconds to wait before attempting to reconnect. Defaults to 5000
		/// </summary>
		public int AutoReconnectIntervalMs { get; set; }

		/// <summary>
		/// Set only when the disconnect method is called.
		/// </summary>
		bool DisconnectCalledByUser;

		/// <summary>
		/// 
		/// </summary>
		public bool Connected
		{
			get { return Client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED; }
		}

		CTimer RetryTimer;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">unique string to differentiate between instances</param>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="bufferSize"></param>
		public GenericTcpIpClient(string key, string address, int port, int bufferSize)
			: base(key)
		{
            StreamDebugging = new CommunicationStreamDebugging(key);
            Hostname = address;
            Port = port;
            BufferSize = bufferSize;
			AutoReconnectIntervalMs = 5000;

            CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(CrestronEnvironment_ProgramStatusEventHandler);
		}

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key"></param>
        public GenericTcpIpClient(string key)
            : base(key)
        {
            StreamDebugging = new CommunicationStreamDebugging(key);
            CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(CrestronEnvironment_ProgramStatusEventHandler);
            AutoReconnectIntervalMs = 5000;
            BufferSize = 2000;
        }

        /// <summary>
        /// Default constructor for S+
        /// </summary>
        public GenericTcpIpClient()
			: base(SplusKey)
		{
            StreamDebugging = new CommunicationStreamDebugging(SplusKey);
			CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(CrestronEnvironment_ProgramStatusEventHandler);
			AutoReconnectIntervalMs = 5000;
            BufferSize = 2000;
		}

        /// <summary>
        /// Just to help S+ set the key
        /// </summary>
        public void Initialize(string key)
        {
            Key = key;
        }

        /// <summary>
        /// Handles closing this up when the program shuts down
        /// </summary>
        void CrestronEnvironment_ProgramStatusEventHandler(eProgramStatusEventType programEventType)
        {
            if (programEventType == eProgramStatusEventType.Stopping)
            {
                Debug.Console(1, this, "Program stopping. Closing connection");
				Disconnect();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
		public override bool Deactivate()
		{
            if (Client != null)
            {
                Client.SocketStatusChange -= this.Client_SocketStatusChange;
                DisconnectClient();
            }
			return true;
		}

        /// <summary>
        /// Attempts to connect to the server
        /// </summary>
		public void Connect()
		{
            if (IsConnected)
                DisconnectClient();

            if (string.IsNullOrEmpty(Hostname))
            {
                Debug.Console(1, Debug.ErrorLogLevel.Warning, "GenericTcpIpClient '{0}': No address set", Key);
                return;
            }
            if (Port < 1 || Port > 65535)
            {
                {
                    Debug.Console(1, Debug.ErrorLogLevel.Warning, "GenericTcpIpClient '{0}': Invalid port", Key);
                    return;
                }
            }

            if (Client == null)
            {
                Client = new TCPClient(Hostname, Port, BufferSize);
                Client.SocketStatusChange -= Client_SocketStatusChange;
                Client.SocketStatusChange += Client_SocketStatusChange;
            }
			DisconnectCalledByUser = false;
			
			Client.ConnectToServerAsync(ConnectToServerCallback); // (null);
		}

        /// <summary>
        /// Attempts to disconnect the client
        /// </summary>
		public void Disconnect()
		{
            DisconnectCalledByUser = true;

            // Stop trying reconnects, if we are
            if (RetryTimer != null)
            {
                RetryTimer.Stop();
                RetryTimer = null;
            }

            if (Client != null)
            {
                DisconnectClient();
                Client = null;
                Debug.Console(1, this, "Disconnected");
            }
		}

        /// <summary>
        /// Does the actual disconnect business
        /// </summary>
        public void DisconnectClient()
        {
            if (Client != null)
            {
                Debug.Console(1, this, "Disconnecting client");
                if(IsConnected)
                    Client.DisconnectFromServer();
            }
        }

        /// <summary>
        /// Callback method for connection attempt
        /// </summary>
        /// <param name="c"></param>
		void ConnectToServerCallback(TCPClient c)
		{
			Debug.Console(1, this, "Server connection result: {0}", c.ClientStatus);
			if (c.ClientStatus != SocketStatus.SOCKET_STATUS_CONNECTED && AutoReconnect)
				WaitAndTryReconnect();
		}

        /// <summary>
        /// Disconnects, waits and attemtps to connect again
        /// </summary>
		void WaitAndTryReconnect()
		{
            DisconnectClient();
            
            if (Client != null)
            {
                Debug.Console(1, this, "Attempting reconnect, status={0}", Client.ClientStatus);

                if (!DisconnectCalledByUser)
                    RetryTimer = new CTimer(o => 
                    {
                        if (Client == null)
                        {
                            return;
                        }
                        
                        Client.ConnectToServerAsync(ConnectToServerCallback); 
                    }, AutoReconnectIntervalMs);
            }

		}

        /// <summary>
        /// Recieves incoming data
        /// </summary>
        /// <param name="client"></param>
        /// <param name="numBytes"></param>
		void Receive(TCPClient client, int numBytes)
		{
            if (client != null)
            {
                if (numBytes > 0)
                {
                    var bytes = client.IncomingDataBuffer.Take(numBytes).ToArray();
                    var bytesHandler = BytesReceived;
                    if (bytesHandler != null)
                    {
                        if (StreamDebugging.RxStreamDebuggingIsEnabled)
                        {
                            Debug.Console(0, this, "Received {1} bytes: '{0}'", ComTextHelper.GetEscapedText(bytes), bytes.Length);
                        }
                        bytesHandler(this, new GenericCommMethodReceiveBytesArgs(bytes));
                    }
                    var textHandler = TextReceived;
                    if (textHandler != null)
                    {
                        var str = Encoding.GetEncoding(28591).GetString(bytes, 0, bytes.Length);

                        if (StreamDebugging.RxStreamDebuggingIsEnabled)
                            Debug.Console(0, this, "Received {1} characters of text: '{0}'", ComTextHelper.GetDebugText(str), str.Length);

                        textHandler(this, new GenericCommMethodReceiveTextArgs(str));

                    }

                    
                }

                client.ReceiveDataAsync(Receive);
            }
		}

		/// <summary>
		/// General send method
		/// </summary>
		public void SendText(string text)
		{
			var bytes = Encoding.GetEncoding(28591).GetBytes(text);
			// Check debug level before processing byte array
            if (StreamDebugging.TxStreamDebuggingIsEnabled)
                Debug.Console(0, this, "Sending {0} characters of text: '{1}'", text.Length, ComTextHelper.GetDebugText(text));
            if(Client != null)
			    Client.SendData(bytes, bytes.Length);


		}

		/// <summary>
		/// This is useful from console and...?
		/// </summary>
		public void SendEscapedText(string text)
		{
			var unescapedText = Regex.Replace(text, @"\\x([0-9a-fA-F][0-9a-fA-F])", s =>
				{
					var hex = s.Groups[1].Value;
					return ((char)Convert.ToByte(hex, 16)).ToString();
				});
			SendText(unescapedText);
		}

        /// <summary>
        /// Sends Bytes to the server
        /// </summary>
        /// <param name="bytes"></param>
		public void SendBytes(byte[] bytes)
		{
            if (StreamDebugging.TxStreamDebuggingIsEnabled)
                Debug.Console(0, this, "Sending {0} bytes: '{1}'", bytes.Length, ComTextHelper.GetEscapedText(bytes));
            if(Client != null)
			    Client.SendData(bytes, bytes.Length);
		}

        /// <summary>
        /// Socket Status Change Handler
        /// </summary>
        /// <param name="client"></param>
        /// <param name="clientSocketStatus"></param>
		void Client_SocketStatusChange(TCPClient client, SocketStatus clientSocketStatus)
		{
			Debug.Console(1, this, "Socket status change {0} ({1})", clientSocketStatus, ClientStatusText);
			if (client.ClientStatus != SocketStatus.SOCKET_STATUS_CONNECTED && !DisconnectCalledByUser && AutoReconnect)
				WaitAndTryReconnect();

			// Probably doesn't need to be a switch since all other cases were eliminated
			switch (clientSocketStatus)
			{
				case SocketStatus.SOCKET_STATUS_CONNECTED:
					Client.ReceiveDataAsync(Receive);
					DisconnectCalledByUser = false;
					break;
			}

			var handler = ConnectionChange;
			if (handler != null)
				ConnectionChange(this, new GenericSocketStatusChageEventArgs(this));

			// Relay the event
			//var handler = SocketStatusChange;
			//if (handler != null)
			//    SocketStatusChange(this);
		}
	}

    /// <summary>
    /// Configuration properties for TCP/SSH Connections
    /// </summary>
	public class TcpSshPropertiesConfig
	{
        /// <summary>
        /// Address to connect to
        /// </summary>
		[JsonProperty(Required = Required.Always)]
		public string Address { get; set; }
		
        /// <summary>
        /// Port to connect to
        /// </summary>
		[JsonProperty(Required = Required.Always)]
		public int Port { get; set; }
		
        /// <summary>
        /// Username credential
        /// </summary>
		public string Username { get; set; }
        /// <summary>
        /// Passord credential
        /// </summary>
		public string Password { get; set; }

		/// <summary>
		/// Defaults to 32768
		/// </summary>
		public int BufferSize { get; set; }

		/// <summary>
		/// Defaults to true
		/// </summary>
		public bool AutoReconnect { get; set; }

		/// <summary>
		/// Defaults to 5000ms
		/// </summary>
		public int AutoReconnectIntervalMs { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
		public TcpSshPropertiesConfig()
		{
			BufferSize = 32768;
			AutoReconnect = true;
			AutoReconnectIntervalMs = 5000;
            Username = "";
            Password = "";
		}

	}

}