using System;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Ssh;
using Crestron.SimplSharp.Ssh.Common;

namespace PepperDash.Core
{
	public class ConnectionChangeEventArgs : EventArgs
	{
		public bool IsConnected { get; private set; }

		public ushort UIsConnected { get { return (ushort)(Client.IsConnected ? 1 : 0); } }

		public GenericSshClient Client { get; private set; }
		public ushort Status { get { return Client.UStatus; } }

		// S+ Constructor
		public ConnectionChangeEventArgs() { }

		public ConnectionChangeEventArgs(bool isConnected, GenericSshClient client)
		{
			IsConnected = isConnected;
			Client = client;
		}
	}

	//*****************************************************************************************************
	//*****************************************************************************************************

	public class GenericSshClient : Device, IBasicCommunication, IAutoReconnect
	{
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
		public event EventHandler<ConnectionChangeEventArgs> ConnectionChange;


		public string Hostname { get; set; }
		/// <summary>
		/// Port on server
		/// </summary>
		public int Port { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }

		public bool IsConnected 
		{ 
			// returns false if no client or not connected
			get { return UStatus == 2; }
		}
		/// <summary>
		/// Contains the familiar Simpl analog status values 
		/// </summary>
		public ushort UStatus 
		{
			get { return _UStatus; }
			private set
			{
				if (_UStatus == value)
					return;
				_UStatus = value;
				OnConnectionChange();
			}
		
		}
		ushort _UStatus;

		/// <summary>
		/// Determines whether client will attempt reconnection on failure. Default is true
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
		/// Millisecond value, determines the timeout period in between reconnect attempts.
		/// Set to 5000 by default
		/// </summary>
		public int AutoReconnectIntervalMs { get; set; }

		SshClient Client;
		ShellStream TheStream;
		CTimer ReconnectTimer;

		/// <summary>
		/// Typical constructor.
		/// </summary>
		public GenericSshClient(string key, string hostname, int port, string username, string password) :
			base(key)
		{
			CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(CrestronEnvironment_ProgramStatusEventHandler);
			Key = key;
			Hostname = hostname;
			Port = port;
			Username = username;
			Password = password; 
			AutoReconnectIntervalMs = 5000;
		}

		/// <summary>
		/// S+ Constructor - Must set all properties before calling Connect
		/// </summary>
		public GenericSshClient()
			: base("Uninitialized SshClient")
		{
			CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(CrestronEnvironment_ProgramStatusEventHandler);
			AutoReconnectIntervalMs = 5000;
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
					Debug.Console(2, this, "Program stopping. Closing connection");
					Client.Disconnect();
					Client.Dispose();
				}
			}
		}

		/// <summary>
		/// Connect to the server, using the provided properties.
		/// </summary>
		public void Connect()
		{
			Debug.Console(1, this, "attempting connect, IsConnected={0}", Client != null ? Client.IsConnected : false);
			
			// Cancel reconnect if running.
			if (ReconnectTimer != null)
			{
				ReconnectTimer.Stop();
				ReconnectTimer = null;
			}

			// Don't try to connect if already
			if (IsConnected)
				return;
			
			// Don't go unless everything is here
			if (string.IsNullOrEmpty(Hostname) || Port < 1 || Port > 65535
				|| Username == null || Password == null)
			{
				Debug.Console(0, this, "Connect failed.  Check hostname, port, username and password are set or not null");
				return;
			}

			//You can do it!
			UStatus = 1;
			//IsConnected = false;

			// This handles both password and keyboard-interactive (like on OS-X, 'nixes)
			KeyboardInteractiveAuthenticationMethod kauth = new KeyboardInteractiveAuthenticationMethod(Username);
			kauth.AuthenticationPrompt += new EventHandler<AuthenticationPromptEventArgs>(kauth_AuthenticationPrompt);
			PasswordAuthenticationMethod pauth = new PasswordAuthenticationMethod(Username, Password);
			ConnectionInfo connectionInfo = new ConnectionInfo(Hostname, Port, Username, pauth, kauth);

			// always spin up new client in case parameters have changed
			// **** MAY WANT TO CHANGE THIS BECAUSE OF SOCKET LEAKS ****
			if (Client != null)
			{
				Client.Disconnect();
				Client = null;
			}
			Client = new SshClient(connectionInfo);
			
			Client.ErrorOccurred += Client_ErrorOccurred;
			try
			{
				Client.Connect();
				if (Client.IsConnected)
				{
					Client.KeepAliveInterval = TimeSpan.FromSeconds(2);
					Client.SendKeepAlive();
					TheStream = Client.CreateShellStream("PDTShell", 100, 80, 100, 200, 65534);
					TheStream.DataReceived += Stream_DataReceived;
					Debug.Console(1, this, "Connected");
					UStatus = 2;
					//IsConnected = true;
				}
				return;
			}
			catch (SshConnectionException e)
			{
				var ie = e.InnerException; // The details are inside!!
				if (ie is SocketException)
					Debug.Console(0, this, "'{0}' CONNECTION failure: Cannot reach host, ({1})", Key, ie.GetType());
				else if (ie is System.Net.Sockets.SocketException)
					Debug.Console(0, this, "'{0}' Connection failure: Cannot reach host '{1}' on port {2}, ({3})",
						Key, Hostname, Port, ie.GetType());
				else if (ie is SshAuthenticationException)
				{
					Debug.Console(0, this, "Authentication failure for username '{0}', ({1})",
						Username, ie.GetType());
				}
				else
					Debug.Console(0, this, "Error on connect:\r({0})", e);
			}
			catch (Exception e)
			{
				Debug.Console(0, this, "Unhandled exception on connect:\r({0})", e);
			}
			

			// Sucess will not make it this far
			UStatus = 3;
			//IsConnected = false;
			HandleConnectionFailure();
		}

		/// <summary>
		/// Disconnect the clients and put away it's resources.
		/// </summary>
		public void Disconnect()
		{
			// Stop trying reconnects, if we are
			if (ReconnectTimer != null)
			{
				ReconnectTimer.Stop();
				ReconnectTimer = null;
			}
			DiscoAndCleanup();
			UStatus = 5;
			//IsConnected = false;
		}

		/// <summary>
		/// 
		/// </summary>
		void DiscoAndCleanup()
		{
			if (Client != null)
			{
				Client.ErrorOccurred -= Client_ErrorOccurred;
				TheStream.DataReceived -= Stream_DataReceived;
				Debug.Console(2, this, "Cleaning up disconnected client");
				Client.Disconnect();
				Client.Dispose();
				Client = null;
			}
		}

		/// <summary>
		/// Anything to do with reestablishing connection on failures
		/// </summary>
		void HandleConnectionFailure()
		{
			DiscoAndCleanup();

			Debug.Console(2, this, "Checking autoreconnect: {0}, {1}ms", 
				AutoReconnect, AutoReconnectIntervalMs);
			if (AutoReconnect)
			{
				if (ReconnectTimer == null)// || !ReconnectTimerRunning)
				{
					ReconnectTimer = new CTimer(o =>
					{
						Connect();
						ReconnectTimer = null;
					}, AutoReconnectIntervalMs);
					Debug.Console(1, this, "Attempting connection in {0} seconds",
						(float)(AutoReconnectIntervalMs / 1000));
				}
				else
				{
					Debug.Console(2, this, "{0} second reconnect cycle running",
						(float)(AutoReconnectIntervalMs / 1000));
				}
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
			var bytes = e.Data;
			if (bytes.Length > 0)
			{
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
		}

		/// <summary>
		/// Error event handler for client events - disconnect, etc.  Will forward those events via ConnectionChange
		/// event
		/// </summary>
		void Client_ErrorOccurred(object sender, Crestron.SimplSharp.Ssh.Common.ExceptionEventArgs e)
		{
			Debug.Console(0, this, "SSH client error: {0}", e.Exception);
			if (!(e.Exception is SshConnectionException))
			{
				Debug.Console(1, this, "Disconnected by remote");
			}
			if (Client != null)
			{
				Client.Disconnect();
				Client.Dispose();
				Client = null;
			}
			UStatus = 4;
			//IsConnected = false;
			HandleConnectionFailure();
		}

		/// <summary>
		/// Helper for ConnectionChange event
		/// </summary>
		void OnConnectionChange()
		{
			if(ConnectionChange != null)
				ConnectionChange(this, new ConnectionChangeEventArgs(IsConnected, this));
		}

		#region IBasicCommunication Members

		/// <summary>
		/// 
		/// </summary>
		/// <param name="text"></param>
		public void SendText(string text)
		{
			try
			{
				TheStream.Write(text);
				TheStream.Flush();
			}
			catch
			{
				Debug.Console(1, this, "Stream write failed. Disconnected, closing");
				UStatus = 4;
				//IsConnected = false;
				HandleConnectionFailure();
			}
		}

		public void SendBytes(byte[] bytes)
		{
			try
			{
				TheStream.Write(bytes, 0, bytes.Length);
				TheStream.Flush();
			}
			catch
			{
				Debug.Console(1, this, "Stream write failed. Disconnected, closing");
				UStatus = 4;
				//IsConnected = false;
				HandleConnectionFailure();
			}
		}

		#endregion
	}
}
