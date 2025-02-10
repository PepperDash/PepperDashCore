using System;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using Crestron.SimplSharp.Ssh;
using Crestron.SimplSharp.Ssh.Common;

namespace PepperDash.Core
{
	/// <summary>
	/// 
	/// </summary>
    public class GenericSshClient : Device, ISocketStatusWithStreamDebugging, IAutoReconnect
	{
	    private const string SPlusKey = "Uninitialized SshClient";
        /// <summary>
        /// Object to enable stream debugging
        /// </summary>
        public CommunicationStreamDebugging StreamDebugging { get; private set; }

		/// <summary>
		/// Event that fires when data is received.  Delivers args with byte array
		/// </summary>
		public event EventHandler<GenericCommMethodReceiveBytesArgs> BytesReceived;

		/// <summary>
		/// Event that fires when data is received.  Delivered as text.
		/// </summary>
		public event EventHandler<GenericCommMethodReceiveTextArgs> TextReceived;

		/// <summary>
		/// Event when the connection status changes.
		/// </summary>
		public event EventHandler<GenericSocketStatusChageEventArgs> ConnectionChange;

        ///// <summary>
        ///// 
        ///// </summary>
        //public event GenericSocketStatusChangeEventDelegate SocketStatusChange;

		/// <summary>
		/// Address of server
		/// </summary>
		public string Hostname { get; set; }

		/// <summary>
		/// Port on server
		/// </summary>
		public int Port { get; set; }

		/// <summary>
		/// Username for server
		/// </summary>
		public string Username { get; set; }

		/// <summary>
		/// And... Password for server.  That was worth documenting!
		/// </summary>
		public string Password { get; set; }

		/// <summary>
		/// True when the server is connected - when status == 2.
		/// </summary>
		public bool IsConnected 
		{ 
			// returns false if no client or not connected
            get { return Client != null && ClientStatus == SocketStatus.SOCKET_STATUS_CONNECTED; }
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
			get { return _ClientStatus; }
			private set
			{
				if (_ClientStatus == value)
					return;
				_ClientStatus = value;
				OnConnectionChange();
			}
		}
		SocketStatus _ClientStatus;

		/// <summary>
		/// Contains the familiar Simpl analog status values. This drives the ConnectionChange event
		/// and IsConnected with be true when this == 2.
		/// </summary>
		public ushort UStatus 
		{
			get { return (ushort)_ClientStatus; }	
		}

		/// <summary>
		/// Determines whether client will attempt reconnection on failure. Default is true
		/// </summary>
		public bool AutoReconnect { get; set; }

		/// <summary>
		/// Will be set and unset by connect and disconnect only
		/// </summary>
		public bool ConnectEnabled { get; private set; }

		/// <summary>
		/// S+ helper for AutoReconnect
		/// </summary>
		public ushort UAutoReconnect
		{
			get { return (ushort)(AutoReconnect ? 1 : 0); }
			set { AutoReconnect = value == 1; }
		}

		/// <summary>
		/// Millisecond value, determines the timeout period in between reconnect attempts.
		/// Set to 5000 by default
		/// </summary>
		public int AutoReconnectIntervalMs { get; set; }

		SshClient Client;

		ShellStream TheStream;

		CTimer ReconnectTimer;

        //Lock object to prevent simulatneous connect/disconnect operations
        private CCriticalSection connectLock = new CCriticalSection();

        private bool DisconnectLogged = false;

		/// <summary>
		/// Typical constructor.
		/// </summary>
		public GenericSshClient(string key, string hostname, int port, string username, string password) :
			base(key)
		{
            StreamDebugging = new CommunicationStreamDebugging(key);
			CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(CrestronEnvironment_ProgramStatusEventHandler);
			Key = key;
			Hostname = hostname;
			Port = port;
			Username = username;
			Password = password; 
			AutoReconnectIntervalMs = 5000;

            ReconnectTimer = new CTimer(o =>
	            {
                    if (ConnectEnabled)
                    {
                        Connect();
                    }
	            }, Timeout.Infinite);
		}

		/// <summary>
		/// S+ Constructor - Must set all properties before calling Connect
		/// </summary>
		public GenericSshClient()
			: base(SPlusKey)
		{
			CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(CrestronEnvironment_ProgramStatusEventHandler);
			AutoReconnectIntervalMs = 5000;

            ReconnectTimer = new CTimer(o =>
            {
                if (ConnectEnabled)
                {
                    Connect();
                }
            }, Timeout.Infinite);
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
                    Disconnect();
				}
			}
		}

		/// <summary>
		/// Connect to the server, using the provided properties.
		/// </summary>
		public void Connect()
        {
            // Don't go unless everything is here
            if (string.IsNullOrEmpty(Hostname) || Port < 1 || Port > 65535
                || Username == null || Password == null)
            {
                Debug.Console(0, this, Debug.ErrorLogLevel.Error, "Connect failed.  Check hostname, port, username and password are set or not null");
                return;
            }

            ConnectEnabled = true;

            try
            {
                connectLock.Enter();
                if (IsConnected)
                {
                    Debug.Console(1, this, "Connection already connected.  Exiting Connect()");
                }
                else
                {
                    Debug.Console(1, this, "Attempting connect");

                    // Cancel reconnect if running.
					if (ReconnectTimer != null)
					{
						ReconnectTimer.Stop();
					}

                    // Cleanup the old client if it already exists
                    if (Client != null)
                    {
                        Debug.Console(1, this, "Cleaning up disconnected client");
                        KillClient(SocketStatus.SOCKET_STATUS_BROKEN_LOCALLY);
                    }

                    // This handles both password and keyboard-interactive (like on OS-X, 'nixes)
                    KeyboardInteractiveAuthenticationMethod kauth = new KeyboardInteractiveAuthenticationMethod(Username);
                    kauth.AuthenticationPrompt += new EventHandler<AuthenticationPromptEventArgs>(kauth_AuthenticationPrompt);
                    PasswordAuthenticationMethod pauth = new PasswordAuthenticationMethod(Username, Password);

                    Debug.Console(1, this, "Creating new SshClient");
                    ConnectionInfo connectionInfo = new ConnectionInfo(Hostname, Port, Username, pauth, kauth);
                    Client = new SshClient(connectionInfo);
                    Client.ErrorOccurred += Client_ErrorOccurred;

                    //Attempt to connect
                    ClientStatus = SocketStatus.SOCKET_STATUS_WAITING;
                    try
                    {
                        Client.Connect();
                        TheStream = Client.CreateShellStream("PDTShell", 100, 80, 100, 200, 65534);
                        if (TheStream.DataAvailable)
                        {
                            // empty the buffer if there is data
                            string str = TheStream.Read();
                        }
                        TheStream.DataReceived += Stream_DataReceived;
                        Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Connected");
                        ClientStatus = SocketStatus.SOCKET_STATUS_CONNECTED;
                        DisconnectLogged = false;
                    }
                    catch (SshConnectionException e)
                    {
                        var ie = e.InnerException; // The details are inside!!
                        var errorLogLevel = DisconnectLogged == true ? Debug.ErrorLogLevel.None : Debug.ErrorLogLevel.Error;

                        if (ie is SocketException)
                            Debug.Console(1, this, errorLogLevel, "'{0}' CONNECTION failure: Cannot reach host, ({1})", Key, ie.Message);
                        else if (ie is System.Net.Sockets.SocketException)
                            Debug.Console(1, this, errorLogLevel, "'{0}' Connection failure: Cannot reach host '{1}' on port {2}, ({3})",
                                Key, Hostname, Port, ie.GetType());
                        else if (ie is SshAuthenticationException)
                        {
                            Debug.Console(1, this, errorLogLevel, "Authentication failure for username '{0}', ({1})",
                                Username, ie.Message);
                        }
                        else
                            Debug.Console(1, this, errorLogLevel, "Error on connect:\r({0})", ie.Message);

                        DisconnectLogged = true;
                        KillClient(SocketStatus.SOCKET_STATUS_CONNECT_FAILED);
                        if (AutoReconnect)
                        {
                            Debug.Console(1, this, "Checking autoreconnect: {0}, {1}ms", AutoReconnect, AutoReconnectIntervalMs);
                            ReconnectTimer.Reset(AutoReconnectIntervalMs);
                        }
                    }
                    catch (Exception e)
                    {
                        var errorLogLevel = DisconnectLogged == true ? Debug.ErrorLogLevel.None : Debug.ErrorLogLevel.Error;
                        Debug.Console(1, this, errorLogLevel, "Unhandled exception on connect:\r({0})", e.Message);
                        DisconnectLogged = true;
                        KillClient(SocketStatus.SOCKET_STATUS_CONNECT_FAILED);
                        if (AutoReconnect)
                        {
                            Debug.Console(1, this, "Checking autoreconnect: {0}, {1}ms", AutoReconnect, AutoReconnectIntervalMs);
                            ReconnectTimer.Reset(AutoReconnectIntervalMs);
                        }
                    }
                }
            }
            finally
            {
                connectLock.Leave();
            }
        }

		/// <summary>
		/// Disconnect the clients and put away it's resources.
		/// </summary>
		public void Disconnect()
		{
			ConnectEnabled = false;
			// Stop trying reconnects, if we are
			if (ReconnectTimer != null)
			{
				ReconnectTimer.Stop();
				// ReconnectTimer = null;
			}

            KillClient(SocketStatus.SOCKET_STATUS_BROKEN_LOCALLY);
		}

        /// <summary>
        /// Kills the stream, cleans up the client and sets it to null
        /// </summary>
        private void KillClient(SocketStatus status)
        {
            KillStream();

            try
            {
                if (Client != null)
                {
                    Client.ErrorOccurred -= Client_ErrorOccurred;
                    Client.Disconnect();
                    Client.Dispose();
                    Client = null;
                    ClientStatus = status;
                    Debug.Console(1, this, "Disconnected");
                }
            }
            catch (Exception ex)
            {
                Debug.Console(0, this, Debug.ErrorLogLevel.Notice, "Exception in Kill Client:{0}", ex);
            }
        }

		/// <summary>
		/// Anything to do with reestablishing connection on failures
		/// </summary>
		void HandleConnectionFailure()
		{
            KillClient(SocketStatus.SOCKET_STATUS_CONNECT_FAILED);

            Debug.Console(1, this, "Client nulled due to connection failure. AutoReconnect: {0}, ConnectEnabled: {1}", AutoReconnect, ConnectEnabled);
		    if (AutoReconnect && ConnectEnabled)
		    {
		        Debug.Console(1, this, "Checking autoreconnect: {0}, {1}ms", AutoReconnect, AutoReconnectIntervalMs);
		        if (ReconnectTimer == null)
		        {
		            ReconnectTimer = new CTimer(o =>
		            {
		                Connect();
		            }, AutoReconnectIntervalMs);
		            Debug.Console(1, this, "Attempting connection in {0} seconds",
		                (float) (AutoReconnectIntervalMs/1000));
		        }
		        else
		        {
		            Debug.Console(1, this, "{0} second reconnect cycle running",
		                (float) (AutoReconnectIntervalMs/1000));
		        }
		    }
		}

        /// <summary>
        /// Kills the stream
        /// </summary>
		void KillStream()
		{
            try
            {
                if (TheStream != null)
                {
                    TheStream.DataReceived -= Stream_DataReceived;
                    TheStream.Close();
                    TheStream.Dispose();
                    TheStream = null;
                    Debug.Console(1, this, "Disconnected stream");
                }
            }
            catch (Exception ex)
            {
                Debug.Console(0, this, Debug.ErrorLogLevel.Notice, "Exception in Kill Stream:{0}", ex);
            }
		}

		/// <summary>
		/// Handles the keyboard interactive authentication, should it be required.
		/// </summary>
		void kauth_AuthenticationPrompt(object sender, AuthenticationPromptEventArgs e)
		{
			foreach (AuthenticationPrompt prompt in e.Prompts)
				if (prompt.Request.IndexOf("Password:", StringComparison.InvariantCultureIgnoreCase) != -1)
					prompt.Response = Password;
		}
	
		/// <summary>
		/// Handler for data receive on ShellStream.  Passes data across to queue for line parsing.
		/// </summary>
		void Stream_DataReceived(object sender, Crestron.SimplSharp.Ssh.Common.ShellDataEventArgs e)
		{
            if (((ShellStream)sender).Length <= 0L)
            {
                return;
            }
            var response = ((ShellStream)sender).Read(); 
 
			var bytesHandler = BytesReceived;
            
		    if (bytesHandler != null)
		    {
                var bytes = Encoding.UTF8.GetBytes(response);
		        if (StreamDebugging.RxStreamDebuggingIsEnabled)
		        {
		            Debug.Console(0, this, "Received {1} bytes: '{0}'", ComTextHelper.GetEscapedText(bytes), bytes.Length);
		        }
                bytesHandler(this, new GenericCommMethodReceiveBytesArgs(bytes));
		    }
				
			var textHandler = TextReceived;
			if (textHandler != null)
			{
                if (StreamDebugging.RxStreamDebuggingIsEnabled)
                    Debug.Console(0, this, "Received: '{0}'", ComTextHelper.GetDebugText(response));

                textHandler(this, new GenericCommMethodReceiveTextArgs(response));
            }
			
		}


		/// <summary>
		/// Error event handler for client events - disconnect, etc.  Will forward those events via ConnectionChange
		/// event
		/// </summary>
		void Client_ErrorOccurred(object sender, Crestron.SimplSharp.Ssh.Common.ExceptionEventArgs e)
		{
            CrestronInvoke.BeginInvoke(o =>
            {
                if (e.Exception is SshConnectionException || e.Exception is System.Net.Sockets.SocketException)
                    Debug.Console(1, this, Debug.ErrorLogLevel.Error, "Disconnected by remote");
                else
                    Debug.Console(1, this, Debug.ErrorLogLevel.Error, "Unhandled SSH client error: {0}", e.Exception);

                try
                {
                    connectLock.Enter();
                    KillClient(SocketStatus.SOCKET_STATUS_BROKEN_REMOTELY);
                }
                finally
                {
                    connectLock.Leave();
                }
                if (AutoReconnect && ConnectEnabled)
                {
                    Debug.Console(1, this, "Checking autoreconnect: {0}, {1}ms", AutoReconnect, AutoReconnectIntervalMs);
                    ReconnectTimer.Reset(AutoReconnectIntervalMs);
                }
            });
		}

		/// <summary>
		/// Helper for ConnectionChange event
		/// </summary>
		void OnConnectionChange()
		{
			if (ConnectionChange != null)
				ConnectionChange(this, new GenericSocketStatusChageEventArgs(this));
		}

		#region IBasicCommunication Members

		/// <summary>
		/// Sends text to the server
		/// </summary>
		/// <param name="text"></param>
		public void SendText(string text)
		{
            try
            {
                if (Client != null && TheStream != null && IsConnected)
                {
                    if (StreamDebugging.TxStreamDebuggingIsEnabled)
                        Debug.Console(0,
                                      this,
                                      "Sending {0} characters of text: '{1}'",
                                      text.Length,
                                      ComTextHelper.GetDebugText(text));

                    TheStream.Write(text);
                    TheStream.Flush();
                }
                else
                {
                    Debug.Console(1, this, "Client is null or disconnected.  Cannot Send Text");
                }
            }
            catch (ObjectDisposedException ex)
            {
                Debug.Console(0, this, Debug.ErrorLogLevel.Notice, "Exception: {0}", ex.Message);
                Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Stack Trace: {0}", ex.StackTrace);

                KillClient(SocketStatus.SOCKET_STATUS_CONNECT_FAILED);
                ReconnectTimer.Reset();
            }
            catch (Crestron.SimplSharp.CrestronIO.IOException ex)
            {
                Debug.Console(0, this, Debug.ErrorLogLevel.Error, "Crestron.SimplSharp.CrestronIO.IOException: {0} -\n{1}\n", ex.Message, ex);
                Debug.Console(1, this, Debug.ErrorLogLevel.Error, "Crestron.SimplSharp.CrestronIO.IOException Stack Trace: {0}", ex.StackTrace);
                Debug.Console(1, this, Debug.ErrorLogLevel.Error, "Crestron.SimplSharp.CrestronIO.IOException Stream write failed");
                KillClient(SocketStatus.SOCKET_STATUS_CONNECT_FAILED);
                ReconnectTimer.Reset();

            }
            catch (Exception ex)
            {
                Debug.Console(0, this, Debug.ErrorLogLevel.Error, "Exception: {0} -\n{1}\n", ex.Message, ex);
                Debug.Console(1, this, Debug.ErrorLogLevel.Error, "Stack Trace: {0}", ex.StackTrace);
                Debug.Console(1, this, Debug.ErrorLogLevel.Error, "Stream write failed");

            }
		}

        /// <summary>
        /// Sends Bytes to the server
        /// </summary>
        /// <param name="bytes"></param>
		public void SendBytes(byte[] bytes)
		{
			try
			{
                if (Client != null && TheStream != null && IsConnected)
                {
                    if (StreamDebugging.TxStreamDebuggingIsEnabled)
                        Debug.Console(0, this, "Sending {0} bytes: '{1}'", bytes.Length, ComTextHelper.GetEscapedText(bytes));

                    TheStream.Write(bytes, 0, bytes.Length);
                    TheStream.Flush();
                }
                else
                {
                    Debug.Console(1, this, "Client is null or disconnected.  Cannot Send Bytes");
                }
            }
            catch (ObjectDisposedException ex)
            {
                Debug.Console(0, this, Debug.ErrorLogLevel.Notice, "Exception: {0}", ex.Message);
                Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Stack Trace: {0}", ex.StackTrace);

                KillClient(SocketStatus.SOCKET_STATUS_CONNECT_FAILED);
                ReconnectTimer.Reset();
            }
            catch (Exception ex)
            {
                Debug.Console(0, this, Debug.ErrorLogLevel.Notice, "Exception: {0}", ex.Message);
                Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Stack Trace: {0}", ex.StackTrace);

				Debug.Console(1, this, Debug.ErrorLogLevel.Error, "Stream write failed");
			}
		}

		#endregion
	}

	//*****************************************************************************************************
	//*****************************************************************************************************
	/// <summary>
	/// Fired when connection changes
	/// </summary>
	public class SshConnectionChangeEventArgs : EventArgs
	{
        /// <summary>
        /// Connection State
        /// </summary>
		public bool IsConnected { get; private set; }

        /// <summary>
        /// Connection Status represented as a ushort
        /// </summary>
		public ushort UIsConnected { get { return (ushort)(Client.IsConnected ? 1 : 0); } }

        /// <summary>
        /// The client
        /// </summary>
		public GenericSshClient Client { get; private set; }

        /// <summary>
        /// Socket Status as represented by
        /// </summary>
		public ushort Status { get { return Client.UStatus; } }

		/// <summary>
        ///  S+ Constructor
		/// </summary>
		public SshConnectionChangeEventArgs() { }

        /// <summary>
        /// EventArgs class
        /// </summary>
        /// <param name="isConnected">Connection State</param>
        /// <param name="client">The Client</param>
		public SshConnectionChangeEventArgs(bool isConnected, GenericSshClient client)
		{
			IsConnected = isConnected;
			Client = client;
		}
	}
}
