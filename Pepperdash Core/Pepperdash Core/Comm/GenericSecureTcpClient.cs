using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using PepperDash.Core;

namespace SecureTCP.SecureTCP.Client
{
    public class GenericSecureTcpClient : Device, ISocketStatus, IAutoReconnect
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
        /// Defaults to 2000
        /// </summary>
        public int BufferSize { get; set; }

        public SecureTCPClient Client;

        /// <summary>
		/// 
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
		/// 
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
		/// 
		/// </summary>
		public string ClientStatusText { get { return ClientStatus.ToString(); } }

        /// <summary>
        /// 
        /// </summary>
        public string ConnectionFailure { get { return ClientStatus.ToString(); } }

        /// <summary>
        /// 
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

        public GenericSecureTcpClient(string key, string address, int port, int bufferSize)
			: base(key)
		{
            Hostname = address;
            Port = port;
            BufferSize = bufferSize;
			AutoReconnectIntervalMs = 5000;
		}

        public GenericSecureTcpClient()
			: base("Uninitialized SecureTcpClient")
		{
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
                if (Client != null)
                {
                    Debug.Console(1, this, "Program stopping. Closing connection");
                    Client.DisconnectFromServer();
                    Client.Dispose();
                }
            }
        }

        public override bool Deactivate()
		{
            if(Client != null)
    			Client.SocketStatusChange -= this.Client_SocketStatusChange;
			return true;
		}

		public void Connect()
		{
            if (IsConnected) 
                return;

            if (string.IsNullOrEmpty(Hostname))
            {
                Debug.Console(1, Debug.ErrorLogLevel.Warning, "GenericSecureTcpClient '{0}': No address set", Key);
                return;
            }
            if (Port < 1 || Port > 65535)
            {
                {
                    Debug.Console(1, Debug.ErrorLogLevel.Warning, "GenericSecureTcpClient '{0}': Invalid port", Key);
                    return;
                }
            }
            
            Client = new SecureTCPClient(Hostname, Port, BufferSize);
            Client.SocketStatusChange += Client_SocketStatusChange;
            try
            {
                DisconnectCalledByUser = false;
                SocketErrorCodes error = Client.ConnectToServer();
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine("Secure Client could not connect. Error: {0}", ex.Message);  
            }
		}

		public void Disconnect()
        {
            DisconnectCalledByUser = true;
			Client.DisconnectFromServer();
		}

		void ConnectToServerCallback(object o)
		{
            Client.ConnectToServer();
            if (Client.ClientStatus != SocketStatus.SOCKET_STATUS_CONNECTED)
                WaitAndTryReconnect();
        }

		void WaitAndTryReconnect()
		{
			Client.DisconnectFromServer();
			Debug.Console(2, "Attempting reconnect, status={0}", Client.ClientStatus);
            RetryTimer = new CTimer(ConnectToServerCallback, 1000);
		}

		void Receive(SecureTCPClient client, int numBytes)
		{
			if (numBytes > 0)
			{
				var bytes = client.IncomingDataBuffer.Take(numBytes).ToArray();
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
			Client.ReceiveDataAsync(Receive);
		}

		/// <summary>
		/// General send method
		/// </summary>
		public void SendText(string text)
		{
			var bytes = Encoding.GetEncoding(28591).GetBytes(text);
			// Check debug level before processing byte array
			//if (Debug.Level == 2)
			//    Debug.Console(2, this, "Sending {0} bytes: '{1}'", bytes.Length, ComTextHelper.GetEscapedText(bytes));
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

		public void SendBytes(byte[] bytes)
		{
			//if (Debug.Level == 2)
			//    Debug.Console(2, this, "Sending {0} bytes: '{1}'", bytes.Length, ComTextHelper.GetEscapedText(bytes));
			Client.SendData(bytes, bytes.Length);
		}


		void Client_SocketStatusChange(SecureTCPClient client, SocketStatus clientSocketStatus)
		{
			Debug.Console(2, this, "Socket status change {0} ({1})", clientSocketStatus, ClientStatusText);
			if (client.ClientStatus != SocketStatus.SOCKET_STATUS_CONNECTED && !DisconnectCalledByUser)
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
		}
	}
}