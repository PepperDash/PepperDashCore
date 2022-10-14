using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using Crestron.SimplSharp.Ssh;
using Crestron.SimplSharp.Ssh.Common;

namespace PepperDash.Core
{
    /// <summary>
    /// Generic ssh client
    /// </summary>
    public class GenericSshClient : Device, IStreamDebugging, ISocketStatus, IAutoReconnect, IDisposable
    {
        private static class ConnectionProgress
        {
            public const int Idle = 0;
            public const int InProgress = 1;
        }

        private SshClient client;
        private ShellStream stream;
        private CTimer connectTimer;
        private CTimer disconnectTimer;
        private int connectionProgress = ConnectionProgress.Idle;
        private KeyboardInteractiveAuthenticationMethod keyboardAuth;
        private PasswordAuthenticationMethod passwordAuth;
        private SocketStatus socketSatus;

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
        /// Client socket status
        /// </summary>
        public SocketStatus ClientStatus
        {
            get { return socketSatus; }
            private set
            {
                if (socketSatus == value)
                    return;
                socketSatus = value;
                OnConnectionChange();
            }
        }

        /// <summary>
        /// Will be set and unset by connect and disconnect only
        /// </summary>
        public bool ConnectEnabled { get; private set; }

        private void OnConnectionChange()
        {
            var handler = ConnectionChange;
            if (handler != null)
                handler(this, new GenericSocketStatusChageEventArgs(this));
        }

        private const string SPlusKey = "Uninitialized SshClient";

        /// <summary>
        /// S+ Constructor - Must set all properties before calling Connect
        /// </summary>
        public GenericSshClient()
			: base(SPlusKey)
		{
            StreamDebugging = new CommunicationStreamDebugging(SPlusKey);
			AutoReconnectIntervalMs = 10000;

            connectTimer = new CTimer(_ =>
            {
                Debug.Console(1, this, "Attempting to reconnect");
                Connect();
            }, Timeout.Infinite);

            disconnectTimer = new CTimer(_ =>
            {
                Debug.Console(1, this, "Attempting to disconnect");
                Disconnect();
            }, Timeout.Infinite);

            CrestronEnvironment.ProgramStatusEventHandler += OnProgramShutdown;
		}

        /// <summary>
        /// Typical constructor.
        /// </summary>
        public GenericSshClient(string key, string hostname, int port, string username, string password) : base(key)
        {
            Key = key;
            Hostname = hostname;
            Port = port;
            Username = username;
            Password = password;
            AutoReconnectIntervalMs = 10000;

            connectTimer = new CTimer(_ =>
            {
                Debug.Console(1, this, "Attempting to reconnect");
                Connect();
            }, Timeout.Infinite);

            disconnectTimer = new CTimer(_ =>
            {
                Debug.Console(1, this, "Attempting to disconnect");
                Disconnect();
            }, Timeout.Infinite);

            StreamDebugging = new CommunicationStreamDebugging(key);
            CrestronEnvironment.ProgramStatusEventHandler += OnProgramShutdown;
        }

        private void OnProgramShutdown(eProgramStatusEventType type)
        {
            try
            {
                if (type == eProgramStatusEventType.Stopping)
                    Disconnect();
            }
            catch (Exception ex)
            {
                Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Error at shutdown:{0}", ex);
            }
        }

        /// <summary>
        /// Just to help S+ set the key
        /// </summary>
        public void Initialize(string key)
        {
            Key = key;
        }

        /// <summary>
        /// True when the server is connected - when status == 2.
        /// </summary>
        public bool IsConnected
        {
            get { return client != null && client.IsConnected; }
        }

        /// <summary>
        /// S+ helper for IsConnected
        /// </summary>
        public ushort UIsConnected
        {
            get { return (ushort)(IsConnected ? 1 : 0); }
        }

        /// <summary>
        /// Event fired when bytes are received on the shell
        /// </summary>
        public event EventHandler<GenericCommMethodReceiveBytesArgs> BytesReceived;

        /// <summary>
        /// Event fired when test is received on the shell
        /// </summary>
        public event EventHandler<GenericCommMethodReceiveTextArgs> TextReceived;

        /// <summary>
        /// Sends text to the server
        /// </summary>
        /// <param name="text"></param>
        public void SendText(string text)
        {
            if (CanSend())
            {
                if (StreamDebugging.TxStreamDebuggingIsEnabled)
                    Debug.Console(0, this, "Sending {0} characters of text: '{1}'", text.Length, ComTextHelper.GetDebugText(text.Trim()));

                stream.WriteLine(text); 
            }
            else
            {
                Debug.Console(1, this, "SendText() called but not connected");
                Disconnect(SocketStatus.SOCKET_STATUS_NO_CONNECT); 
            }
        }

        /// <summary>
        /// Sends bytes to the server
        /// </summary>
        /// <param name="bytes"></param>
        public void SendBytes(byte[] bytes)
        {
            if (CanSend())
            {
                if (StreamDebugging.TxStreamDebuggingIsEnabled)
                    Debug.Console(0, this, "Sending {0} bytes: '{1}'", bytes.Length, ComTextHelper.GetEscapedText(bytes));

                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();
            }
            else
            {
                Debug.Console(1, this, "SendText() called but not connected");
                Disconnect(SocketStatus.SOCKET_STATUS_NO_CONNECT);
            }
        }

        private bool CanSend()
        {
            return client != null && stream != null && client.IsConnected;
        }

        /// <summary>
        /// Connect to the server, using the provided properties.
        /// </summary>
        public void Connect()
        {
            ConnectEnabled = true;
            DisposeOfTimer(disconnectTimer);

            if (
                Interlocked.CompareExchange(ref connectionProgress, ConnectionProgress.InProgress,
                    ConnectionProgress.Idle) == ConnectionProgress.Idle)
            {
                if (client != null && client.IsConnected)
                {
                    Debug.Console(1, this, "Ignoring connection request... already connected");
                    Interlocked.Exchange(ref connectionProgress, ConnectionProgress.Idle);
                    return;
                }

                CrestronInvoke.BeginInvoke(_ =>
                {
                    const int defaultPort = 22;
                    var p = Port == default(int) ? defaultPort : Port;

                    if (keyboardAuth != null)
                    {
                        keyboardAuth.AuthenticationPrompt -= AuthenticationPromptHandler;
                        keyboardAuth.Dispose();
                    }

                    if (passwordAuth != null)
                    {
                        passwordAuth.Dispose();
                    }

                    keyboardAuth = new KeyboardInteractiveAuthenticationMethod(Username);
                    passwordAuth = new PasswordAuthenticationMethod(Username, Password);
                    keyboardAuth.AuthenticationPrompt += AuthenticationPromptHandler;

                    var connectionInfo = new ConnectionInfo(Hostname, p, Username, passwordAuth, keyboardAuth);

                    if (connectTimer.Disposed && AutoReconnect)
                    {
                        connectTimer = new CTimer(obj =>
                        {
                            Debug.Console(1, this, "Attempting to reconnect");
                            Connect();
                        }, Timeout.Infinite);
                    }

                    ConnectInternal(connectionInfo);
                });
            }
            else
            {

                Debug.Console(1, this, "Ingoring connection request while connect/disconnect in progress...");
                if (!connectTimer.Disposed && AutoReconnect)
                    connectTimer.Reset(10000);
            }
        }

        private void ConnectInternal(ConnectionInfo connectionInfo)
        {
            Debug.Console(1, this, "Attempting connection to: " + Hostname);
            try
            {
                // Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Creating new client...");
                client = new SshClient(connectionInfo);
                client.ErrorOccurred += ClientErrorHandler;
                client.HostKeyReceived += HostKeyReceivedHandler;
                ClientStatus = SocketStatus.SOCKET_STATUS_WAITING;

                // Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Connecting...");
                client.Connect();

                // Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Creating shell...");
                stream = client.CreateShellStream("PDTShell", 100, 80, 100, 200, 65534);
                stream.DataReceived += StreamDataReceivedHandler;
                stream.ErrorOccurred += StreamErrorOccurredHandler;
                // Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Shell created");

                if (client.IsConnected)
                {
                    Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Connected");
                    connectTimer.Stop();
                    ClientStatus = SocketStatus.SOCKET_STATUS_CONNECTED;
                    Interlocked.Exchange(ref connectionProgress, ConnectionProgress.Idle);
                }
                else
                {
                    throw new Exception("Unknown connection error");
                }
            }
            catch (SshConnectionException e)
            {
                var ie = e.InnerException; // The details are inside!!

                if (ie is SocketException)
                    Debug.Console(1, this, Debug.ErrorLogLevel.Error,
                        "'{0}' CONNECTION failure: Cannot reach host, ({1})",
                        Key, ie.Message);
                else if (ie is System.Net.Sockets.SocketException)
                    Debug.Console(1, this, Debug.ErrorLogLevel.Error,
                        "'{0}' Connection failure: Cannot reach host '{1}' on port {2}, ({3})",
                        Key, Hostname, Port, ie.GetType());
                else if (ie is SshAuthenticationException)
                {
                    Debug.Console(1, this, Debug.ErrorLogLevel.Error, "Authentication failure for username '{0}', ({1})",
                        Username, ie.Message);
                }
                else
                    Debug.Console(1, this, Debug.ErrorLogLevel.Error, "Error on connect:({0})", ie.Message);

                DisconnectInternal(SocketStatus.SOCKET_STATUS_CONNECT_FAILED);
            }
            catch (Exception e)
            {
                Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Connection error: " + e.Message);
                DisconnectInternal(SocketStatus.SOCKET_STATUS_CONNECT_FAILED);
            }
        }

        /// <summary>
        /// Disconnect the clients and put away it's resources.
        /// </summary>
        public void Disconnect()
        {
            ConnectEnabled = false;
            Disconnect(SocketStatus.SOCKET_STATUS_BROKEN_LOCALLY);
        }

        private void Disconnect(SocketStatus status)
        {
            // kill the reconnect timer if we are disconnecting locally
            if (status == SocketStatus.SOCKET_STATUS_BROKEN_LOCALLY)
                DisposeOfTimer(connectTimer);

            if (
                Interlocked.CompareExchange(ref connectionProgress, ConnectionProgress.InProgress,
                    ConnectionProgress.Idle) == ConnectionProgress.Idle)
            {
                Debug.Console(1, this, "Disconnecting...");
                CrestronInvoke.BeginInvoke(_ => DisconnectInternal(status));
            }
            else
            {
                if (!disconnectTimer.Disposed)
                    DisposeOfTimer(disconnectTimer);

                disconnectTimer = new CTimer(_ => Disconnect(status), 10000);
                Debug.Console(1, this, "Ingoring disconnect request while connect/disconnect in progress... will try again soon");
            }
        }

        private void DisconnectInternal(SocketStatus status)
        {
            DisposeOfStream();
            DisposeOfClient();
            DisposeOfAuthMethods();

            Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Disconnect Complete");
            ClientStatus = status;

            DisposeOfTimer(disconnectTimer);
            if (!connectTimer.Disposed && AutoReconnect)
            {
                Debug.Console(1, this, "Autoreconnect try again soon");
                connectTimer.Reset(AutoReconnectIntervalMs);
            }

            Interlocked.Exchange(ref connectionProgress, ConnectionProgress.Idle);
        }

        private void DisposeOfAuthMethods()
        {
            try
            {
                if (keyboardAuth != null)
                {
                    keyboardAuth.AuthenticationPrompt -= AuthenticationPromptHandler;
                    keyboardAuth.Dispose();
                    keyboardAuth = null;
                }

                if (passwordAuth != null)
                {
                    passwordAuth.Dispose();
                    passwordAuth = null;
                }
            }
            catch (Exception e)
            {
                Debug.Console(1, this, Debug.ErrorLogLevel.Notice,
                    "Disconnect() Exception occured freeing auth: " + e.Message);
            }
        }

        private void DisposeOfClient()
        {
            try
            {
                if (client != null)
                {
                    if (client.IsConnected)
                        client.Disconnect();

                    client.ErrorOccurred -= ClientErrorHandler;
                    client.HostKeyReceived -= HostKeyReceivedHandler;
                    client.Dispose();
                }
            }
            catch (Exception e)
            {
                Debug.Console(1, this, Debug.ErrorLogLevel.Notice,
                    "Disconnect() Exception occured freeing client: " + e.Message);
            }
        }

        private void DisposeOfStream()
        {
            try
            {
                if (stream != null)
                {
                    stream.DataReceived -= StreamDataReceivedHandler;
                    stream.ErrorOccurred -= StreamErrorOccurredHandler;
                    stream.Close();
                    stream.Dispose();
                }
            }
            catch (Exception e)
            {
                Debug.Console(1, this, Debug.ErrorLogLevel.Notice,
                    "Disconnect() exception occured freeing stream: " + e.Message);
            }
        }

        private static void DisposeOfTimer(CTimer timer)
        {
            if (timer == null || timer.Disposed) return;
            timer.Stop();
            timer.Dispose();
        }

        private void StreamDataReceivedHandler(object sender, ShellDataEventArgs e)
        {
            var bytes = e.Data;
            if (bytes.Length > 0)
            {
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
                        Debug.Console(0, this, "Received: '{0}'", ComTextHelper.GetDebugText(str));

                    textHandler(this, new GenericCommMethodReceiveTextArgs(str));
                }
            }
        }

        private void ClientErrorHandler(object sender, ExceptionEventArgs e)
        {
            Debug.Console(1, this, "SSH client error:{0}", e.Exception);
            Disconnect(SocketStatus.SOCKET_STATUS_BROKEN_REMOTELY);
        }

        private void StreamErrorOccurredHandler(object sender, EventArgs e)
        {
            Debug.Console(1, this, "SSH Shellstream error");
            Disconnect(SocketStatus.SOCKET_STATUS_BROKEN_REMOTELY);
        }

        private void AuthenticationPromptHandler(object sender, AuthenticationPromptEventArgs e)
        {
            foreach (
                var prompt in
                    e.Prompts.Where(
                        prompt => prompt.Request.IndexOf("Password:", StringComparison.InvariantCultureIgnoreCase) != -1)
                )
                prompt.Response = Password;
        }

        private static void HostKeyReceivedHandler(object sender, HostKeyEventArgs e)
        {
            e.CanTrust = true;
        }

        private static IEnumerable<string> SplitDataReceived(string str, int maxChunkSize)
        {
            for (var i = 0; i < str.Length; i += maxChunkSize)
            {
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
            }
        }

        /// <summary>
        /// Stream debugging properties
        /// </summary>
        public CommunicationStreamDebugging StreamDebugging { get; private set; }

        /// <summary>
        /// Event fired on a connection status change
        /// </summary>
        public event EventHandler<GenericSocketStatusChageEventArgs> ConnectionChange;

        /// <summary>
        /// Determines if autoreconnect is enabled
        /// </summary>
        public bool AutoReconnect { get; set; }

        /// <summary>
        /// Millisecond value, determines the timeout period in between reconnect attempts.
        /// Set to 10000 by default
        /// </summary>
        public int AutoReconnectIntervalMs { get; set; }

        private bool disposed;
        public void Dispose()
        {
            Dispose(true);
            CrestronEnvironment.GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                Disconnect();

            disposed = true;
        }
    }
}