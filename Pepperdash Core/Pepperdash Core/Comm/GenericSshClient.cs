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

	public class GenericSshClient : Device, IBasicCommunication
	{
		public event EventHandler<GenericCommMethodReceiveBytesArgs> BytesReceived;
		public event EventHandler<GenericCommMethodReceiveTextArgs> TextReceived;

		public event EventHandler<ConnectionChangeEventArgs> ConnectionChange;
		//public event EventHandler<DataReceiveEventArgs> DataReceive;

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
			get { return (Client != null ? Client.IsConnected : false); }
			set
			{
				if (value)
					UStatus = 2;
				OnConnectionChange();
			}
		}
		/// <summary>
		/// Contains the familiar Simpl analog status values 
		/// </summary>
		public ushort UStatus { get; private set; }

		/// <summary>
		/// Determines whether client will attempt reconnection on failure
		/// </summary>
	
		public bool AutoReconnect { get; set; }
		/// <summary>
		/// S+ helper for bool value
		/// </summary>		
		public ushort UAutoReconnect
		{
			set { AutoReconnect = value == 1; }
		}

		/// <summary>
		/// Millisecond value, determines the timeout period in between reconnect attempts
		/// </summary>
		public ushort AutoReconnectIntervalMs { get; set; }

		SshClient Client;
		ShellStream TheStream;
		CTimer ReconnectTimer;
		bool ReconnectTimerRunning;

		public GenericSshClient(string key, string hostname, int port, string username, string password) :
			base(key)
		{
			AutoReconnectIntervalMs = 5000;
			
			Hostname = hostname;
			Port = port;
			Username = username;
			Password = password;
		}

		/// <summary>
		/// Connect to the server, using the provided properties.
		/// </summary>
		public void Connect()
		{
			ReconnectTimerRunning = false;
			if (Hostname != null && Hostname != string.Empty && Port > 0 &&
				Username != null && Password != null)
			{
				Debug.Console(1, this, "attempting connect, IsConnected={0}", IsConnected);
				if (!IsConnected)
				{
					UStatus = 1;
					IsConnected = false;

					// This handles both password and keyboard-interactive (like on OS-X, 'nixes)
					KeyboardInteractiveAuthenticationMethod kauth = new KeyboardInteractiveAuthenticationMethod(Username);
					kauth.AuthenticationPrompt += new EventHandler<AuthenticationPromptEventArgs>(kauth_AuthenticationPrompt);
					PasswordAuthenticationMethod pauth = new PasswordAuthenticationMethod(Username, Password);
					ConnectionInfo connectionInfo = new ConnectionInfo(Hostname, Port, Username, pauth, kauth);
					Client = new SshClient(connectionInfo);
					Client.ErrorOccurred += Client_ErrorOccurred;
					try
					{
						Client.Connect();
						if (Client.IsConnected)
						{
							Client.KeepAliveInterval = TimeSpan.FromSeconds(2);
							Client.SendKeepAlive();
							IsConnected = true;
							Debug.Console(1, this, "Connected");
							TheStream = Client.CreateShellStream("PDTShell", 100, 80, 100, 200, 65534);
							TheStream.DataReceived += Stream_DataReceived;
							TheStream.ErrorOccurred += Stream_ErrorOccurred;
							
						}
						return;
					}
					catch (SshConnectionException e)
					{
						var ie = e.InnerException; // The details are inside!!
						string msg;
						if (ie is SocketException)
							msg = string.Format("'{0}' CONNECTION failure: Cannot reach host, ({1})", Key, ie.GetType());
						else if (ie is System.Net.Sockets.SocketException)
							msg = string.Format("'{0}' Connection failure: Cannot reach host '{1}' on port {2}, ({3})",
								Key, Hostname, Port, ie.GetType());
						else if (ie is SshAuthenticationException)
						{
							msg = string.Format("'{0}' Authentication failure for username '{1}', ({2})", 
								Username, Key, ie.GetType());
							Debug.Console(0, this, "Authentication failure for username '{0}', ({1})", 
								Username, ie.GetType());
						}
						else
							Debug.Console(0, this, "Error on connect:\r({0})", e);
					}
				}
			}
			else
			{
				Debug.Console(0, this, "Connect failed.  Check hostname, port, username and password are set or not null");
			}

			// Sucess will not make it this far
			UStatus = 3;
			IsConnected = false;
			HandleConnectionFailure();
		}

		/// <summary>
		/// Disconnect the clients and put away it's resources.
		/// </summary>
		public void Disconnect()
		{
			// Stop trying reconnects, if we are
			if(ReconnectTimer != null) ReconnectTimer.Stop();
			// Otherwise just close up
			if (Client != null) // && Client.IsConnected) <-- Doesn't always report properly...
			{
				Debug.Console(1, this, "Disconnecting");
				Client.Disconnect();
				Cleanup();
				UStatus = 5;
				IsConnected = false;
			}
		}

		/// <summary>
		/// Anything to do with reestablishing connection on failures
		/// </summary>
		void HandleConnectionFailure()
		{
			Debug.Console(2, this, "Checking autoreconnect: {0}, {1}ms", 
				AutoReconnect, AutoReconnectIntervalMs);
			if (AutoReconnect)
			{
				if (ReconnectTimer == null || !ReconnectTimerRunning)
				{
					ReconnectTimer = new CTimer(o => Connect(), AutoReconnectIntervalMs);
					ReconnectTimerRunning = true;
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

		void Cleanup()
		{
			Debug.Console(2, this, "cleaning up resources");
			Client = null;
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
		/// Error event handler for stream events
		/// </summary>
		void Stream_ErrorOccurred(object sender, ExceptionEventArgs e)
		{
			Debug.Console(2, this, "CRITICAL: PLEASE REPORT - SSH client stream error:\r{0}", e.Exception);
		}

		/// <summary>
		/// Error event handler for client events - disconnect, etc.  Will forward those events via ConnectionChange
		/// event
		/// </summary>
		void Client_ErrorOccurred(object sender, Crestron.SimplSharp.Ssh.Common.ExceptionEventArgs e)
		{
			Debug.Console(0, this, "SSH client error: {0}", e.Exception);
			if (e.Exception is SocketException)
			{
				// ****LOG SOMETHING
				UStatus = 4;
			}
			IsConnected = false;
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
				IsConnected = false;
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
				IsConnected = false;
				HandleConnectionFailure();
			}
		}

		#endregion
	}
}
