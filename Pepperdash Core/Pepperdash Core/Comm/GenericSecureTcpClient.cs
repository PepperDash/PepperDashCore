using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using PepperDash.Core;

namespace PepperDash.Core
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

        public bool RequiresPresharedKey { get; set; }

        public ushort uRequiresPresharedKey
        {
            set
            {
                if (value == 1)
                    RequiresPresharedKey = true;
                else
                    RequiresPresharedKey = false;
            }
        }

        /// <summary>
        /// SharedKey is sent for varification to the server. Shared key can be any text (255 char limit in SIMPL+ Module), but must match the Shared Key on the Server module
        /// </summary>
        private string _SharedKey;
        public string SharedKey 
        {
            get
            {
                return _SharedKey;
            }
            set
            {
                if (Client.ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED)
                    Client.DisconnectFromServer();
                _SharedKey = value;
            }
        }

        private bool WaitingForSharedKeyResponse { get; set; } 

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
                Debug.Console(1, Debug.ErrorLogLevel.Warning, "GenericSecureTcpClient '{0}': Invalid port", Key);
                return;
            }
            if (string.IsNullOrEmpty(SharedKey) && RequiresPresharedKey)
            {
                Debug.Console(1, Debug.ErrorLogLevel.Warning, "GenericSecureTcpClient '{0}': No Shared Key set", Key);
                return;
            }
            if (Client != null)
                Client.Dispose();
            Client = new SecureTCPClient(Hostname, Port, BufferSize);
            Client.SocketStatusChange += Client_SocketStatusChange;
            try
            {
                DisconnectCalledByUser = false;
                if(RequiresPresharedKey)
                    WaitingForSharedKeyResponse = true;
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
            RetryTimer = new CTimer(ConnectToServerCallback, AutoReconnectIntervalMs);
		}

		void Receive(SecureTCPClient client, int numBytes)
		{
			if (numBytes > 0)
			{
				var bytes = client.IncomingDataBuffer.Take(numBytes).ToArray();
                var str = Encoding.GetEncoding(28591).GetString(bytes, 0, bytes.Length);
                if (WaitingForSharedKeyResponse && RequiresPresharedKey)
                {
                    if (str != (SharedKey + "\n"))
                    {
                        WaitingForSharedKeyResponse = false;
                        Client.DisconnectFromServer();
                        CrestronConsole.PrintLine("Client {0} was disconnected from server because the server did not respond with a matching shared key after connection", Key);
                        ErrorLog.Error("Client {0} was disconnected from server because the server did not respond with a matching shared key after connection", Key);
                        return;
                    }
                    else
                    {
                        WaitingForSharedKeyResponse = false;
                        CrestronConsole.PrintLine("Client {0} successfully connected to the server and received the Shared Key. Ready for communication", Key);
                    }
                }
                else
                {
                    var bytesHandler = BytesReceived;
                    if (bytesHandler != null)
                        bytesHandler(this, new GenericCommMethodReceiveBytesArgs(bytes));
                    var textHandler = TextReceived;
                    if (textHandler != null)
                    {

                        textHandler(this, new GenericCommMethodReceiveTextArgs(str));
                    }
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
			Client.SendData(bytes, bytes.Length);
		}

        public void SendBytes(byte[] bytes)
        {
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
                    SendText(SharedKey + "\n");
					DisconnectCalledByUser = false;
					break;
			}

			var handler = ConnectionChange;
			if (handler != null)
				ConnectionChange(this, new GenericSocketStatusChageEventArgs(this));
		}
	}
}