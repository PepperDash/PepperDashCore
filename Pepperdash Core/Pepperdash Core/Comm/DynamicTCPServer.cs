using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using PepperDash.Core;

namespace PepperDash_Core
{
    public class DynamicTCPServer : Device
    {
        #region Events
        public event EventHandler<GenericCommMethodReceiveTextArgs> TextReceived;

        public event EventHandler<DynamicTCPServerSocketStatusChangeEventArgs> ClientConnectionChange;

        public event EventHandler<DynamicTCPServerStateChangedEventArgs> ServerStateChange;
        #endregion

        #region Properties/Variables
        /// <summary>
        /// Secure or unsecure TCP server. Defaults to Unsecure or standard TCP server without SSL
        /// </summary>
        public bool Secure { get; set; }

        /// <summary>
        /// S+ Helper for Secure bool. Parameter in SIMPL+ so there is no get, one way set from simpl+ Param to property in func main of SIMPL+
        /// </summary>
        public ushort uSecure
        {
            set
            {
                if (value == 1)
                    Secure = true;
                else if (value == 0)
                    Secure = false;
            }
        }

        public string status
        {
            get { return Secure ? SecureServer.State.ToString() : UnsecureServer.State.ToString(); }
        }

        /// <summary>
        /// Bool showing if socket is connected
        /// </summary>
        public bool IsConnected
        {
            get { return Secure ? SecureServer.State == ServerState.SERVER_CONNECTED : UnsecureServer.State == ServerState.SERVER_CONNECTED; }
        }

        /// <summary>
        /// S+ helper for IsConnected
        /// </summary>
        public ushort uIsConnected
        {
            get { return (ushort)(IsConnected ? 1 : 0); }
        }

        /// <summary>
        /// Bool showing if socket is connected
        /// </summary>
        public bool IsListening
        {
            get { return Secure ? SecureServer.State == ServerState.SERVER_LISTENING : UnsecureServer.State == ServerState.SERVER_LISTENING; }
        }

        public ushort MaxConnections { get; set; } // should be set by parameter in SIMPL+ in the MAIN method, Should not ever need to be configurable

        /// <summary>
        /// S+ helper for IsConnected
        /// </summary>
        public ushort uIsListening
        {
            get { return (ushort)(IsListening ? 1 : 0); }
        }

        /// <summary>
        /// Number of clients currently connected.
        /// </summary>
        public ushort NumberOfClientsConnected
        {
            get { return Secure ? (ushort)SecureServer.NumberOfClientsConnected : (ushort)UnsecureServer.NumberOfClientsConnected; }
        }

        /// <summary>
        /// Port on server
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// S+ helper
        /// </summary>
        public ushort uPort
        {
            get { return Convert.ToUInt16(Port); }
            set { Port = Convert.ToInt32(value); }
        }

        /// <summary>
        /// Bool to show whether the server requires a preshared key. Must be set the same in the client, and if true shared keys must be identical on server/client
        /// </summary>
        public bool RequiresPresharedKey { get; set; }

        /// <summary>
        /// S+ helper for requires shared key bool
        /// </summary>
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
        /// SharedKey is sent for varification to the server. Shared key can be any text (255 char limit in SIMPL+ Module), but must match the Shared Key on the Server module. 
        /// If SharedKey changes while server is listening or clients are connected, disconnect and stop listening will be called
        /// </summary>
        public string SharedKey
        {
            get
            {
                return _SharedKey;
            }
            set
            {
                DisconnectAllClients();
                _SharedKey = value;
            }
        }
        private string _SharedKey;

        /// <summary>
        /// flags to show the secure server is waiting for client at index to send the shared key
        /// </summary>
        public List<uint> WaitingForSharedKey = new List<uint>();

        /// <summary>
        /// Store the connected client indexes
        /// </summary>
        public List<uint> ConnectedClientsIndexes = new List<uint>();


        /// <summary>
        /// Defaults to 2000
        /// </summary>
        public int BufferSize { get; set; }

        public string OnlyAcceptConnectionFromAddress
        {
            get { return _OnlyAcceptConnectionFromAddress; }
            set
            {
                DisconnectAllClients();
                MaxConnections = 1;
                _OnlyAcceptConnectionFromAddress = value;
            }
        }
        private string _OnlyAcceptConnectionFromAddress;

        private bool ServerStopped { get; set; }

        public SecureTCPServer SecureServer;
        public TCPServer UnsecureServer;

        #endregion

        #region Constructors
        //base class constructor
        public DynamicTCPServer()
            : base("Uninitialized Dynamic TCP Server")
        {
            CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(CrestronEnvironment_ProgramStatusEventHandler);
            BufferSize = 2000;
            Secure = false;
        }
        #endregion

        #region Methods - Server Actions
        public void Initialize(string key)
        {
            Key = key;
        }

        public void Listen()
        {
            if (Port < 1 || Port > 65535)
            {
                Debug.Console(1, Debug.ErrorLogLevel.Warning, "GenericSecureTcpClient '{0}': Invalid port", Key);
                ErrorLog.Warn(string.Format("GenericSecureTcpClient '{0}': Invalid port", Key));
                return;
            }
            if (string.IsNullOrEmpty(SharedKey) && RequiresPresharedKey)
            {
                Debug.Console(1, Debug.ErrorLogLevel.Warning, "GenericSecureTcpClient '{0}': No Shared Key set", Key);
                ErrorLog.Warn(string.Format("GenericSecureTcpClient '{0}': No Shared Key set", Key));
                return;
            }
            if (Secure)
            {
                if (SecureServer.State == ServerState.SERVER_LISTENING)
                    return;
                SecureServer = new SecureTCPServer(Port, MaxConnections);
                SecureServer.SocketStatusChange += new SecureTCPServerSocketStatusChangeEventHandler(SecureServer_SocketStatusChange);
                ServerStopped = false;
                if (!string.IsNullOrEmpty(OnlyAcceptConnectionFromAddress))
                    SecureServer.WaitForConnectionAsync(OnlyAcceptConnectionFromAddress, SecureConnectCallback);
                else
                    SecureServer.WaitForConnectionAsync(IPAddress.Any, SecureConnectCallback);
                Debug.Console(0, "Secure Server Status: {0}, Socket Status: {1}\r\n", SecureServer.State.ToString(), SecureServer.ServerSocketStatus);
            }
            else
            {
                if (UnsecureServer.State == ServerState.SERVER_LISTENING)
                    return;
                UnsecureServer = new TCPServer(Port, MaxConnections);
                UnsecureServer.SocketStatusChange += new TCPServerSocketStatusChangeEventHandler(UnsecureServer_SocketStatusChange);
                ServerStopped = false;
                if (!string.IsNullOrEmpty(OnlyAcceptConnectionFromAddress))
                    UnsecureServer.WaitForConnectionAsync(OnlyAcceptConnectionFromAddress, UnsecureConnectCallback);
                else
                    UnsecureServer.WaitForConnectionAsync(IPAddress.Any, UnsecureConnectCallback);
                Debug.Console(0, "Unsecure Server Status: {0}, Socket Status: {1}\r\n", UnsecureServer.State.ToString(), UnsecureServer.ServerSocketStatus);
            }
        }

        public void StopListening()
        {
            Debug.Console(0, "Stopping Listener");
            if (SecureServer != null && SecureServer.State == ServerState.SERVER_LISTENING)
                SecureServer.Stop();
            if (UnsecureServer != null && UnsecureServer.State == ServerState.SERVER_LISTENING)
                UnsecureServer.Stop();
            var handler = ServerStateChange;
            if (ServerStateChange != null)
                ServerStateChange(this, new DynamicTCPServerStateChangedEventArgs(this, Secure));
            ServerStopped = true;
        }

        public void DisconnectAllClients()
        {
            Debug.Console(0, "Disconnecting All Clients");
            if (SecureServer != null && SecureServer.NumberOfClientsConnected > 0)
                SecureServer.DisconnectAll();
            if (UnsecureServer != null && UnsecureServer.NumberOfClientsConnected > 0)
                UnsecureServer.DisconnectAll();
        }

        public void BroadcastText(string text)
        {
            if (ConnectedClientsIndexes.Count > 0)
            {
                if (Secure)
                {
                    foreach (uint i in ConnectedClientsIndexes)
                    {
                        byte[] b = Encoding.ASCII.GetBytes(text);
                        SecureServer.SendDataAsync(i, b, b.Length, SecureSendDataAsyncCallback);
                    }
                }
                else
                {
                    foreach (uint i in ConnectedClientsIndexes)
                    {
                        byte[] b = Encoding.ASCII.GetBytes(text);
                        UnsecureServer.SendDataAsync(i, b, b.Length, UnsecureSendDataAsyncCallback);
                    }
                }
            }
        }

        /// <summary>
        /// Not sure this is useful in library, maybe Pro??
        /// </summary>
        /// <param name="text"></param>
        /// <param name="clientIndex"></param>
        public void SendTextToClient(string text, uint clientIndex)
        {
            if (Secure)
            {
                byte[] b = Encoding.ASCII.GetBytes(text);
                SecureServer.SendDataAsync(clientIndex, b, b.Length, SecureSendDataAsyncCallback);
            }
            else
            {
                byte[] b = Encoding.ASCII.GetBytes(text);
                UnsecureServer.SendDataAsync(clientIndex, b, b.Length, UnsecureSendDataAsyncCallback);
            }
        }
        #endregion

        #region Methods - Socket Status Changed Callbacks
        void SecureServer_SocketStatusChange(SecureTCPServer mySecureTCPServer, uint clientIndex, SocketStatus serverSocketStatus)
        {
            Debug.Console(0, "Client at {0} ServerSocketStatus {1}",
                mySecureTCPServer.GetAddressServerAcceptedConnectionFromForSpecificClient(clientIndex), serverSocketStatus.ToString());
            if (mySecureTCPServer.GetServerSocketStatusForSpecificClient(clientIndex) == SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                if (RequiresPresharedKey && !WaitingForSharedKey.Contains(clientIndex))
                    WaitingForSharedKey.Add(clientIndex);
                if (!ConnectedClientsIndexes.Contains(clientIndex))
                    ConnectedClientsIndexes.Add(clientIndex);
            }
            if (mySecureTCPServer.GetServerSocketStatusForSpecificClient(clientIndex) != SocketStatus.SOCKET_STATUS_CONNECTED && ConnectedClientsIndexes.Contains(clientIndex))
                ConnectedClientsIndexes.Remove(clientIndex);

            onConnectionChange();//Go to simpl and send the server and whether it is secure or not. Could check secure from Simpl+ but better to use the passed
            //variable in case we need to use this in Pro. Simpl+ won't use arguments, will just check the uIsConnected Property. Currently Simpl+ is just going to report
            //connected not reporting by client
        }

        void UnsecureServer_SocketStatusChange(TCPServer mySecureTCPServer, uint clientIndex, SocketStatus serverSocketStatus)
        {
            Debug.Console(0, "Client at {0} ServerSocketStatus {1}",
                mySecureTCPServer.GetAddressServerAcceptedConnectionFromForSpecificClient(clientIndex), serverSocketStatus.ToString());
            if (mySecureTCPServer.GetServerSocketStatusForSpecificClient(clientIndex) == SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                if (RequiresPresharedKey && !WaitingForSharedKey.Contains(clientIndex))
                    WaitingForSharedKey.Add(clientIndex);
                if (!ConnectedClientsIndexes.Contains(clientIndex))
                    ConnectedClientsIndexes.Add(clientIndex);
            }
            if (mySecureTCPServer.GetServerSocketStatusForSpecificClient(clientIndex) != SocketStatus.SOCKET_STATUS_CONNECTED && ConnectedClientsIndexes.Contains(clientIndex))
                ConnectedClientsIndexes.Remove(clientIndex);

            onConnectionChange();
        }
        #endregion

        #region Methods Connected Callbacks
        void SecureConnectCallback(SecureTCPServer mySecureTCPServer, uint clientIndex)
        {
            if (mySecureTCPServer.ClientConnected(clientIndex))
            {
                if (RequiresPresharedKey)
                {
                    byte[] b = Encoding.ASCII.GetBytes(SharedKey + "\n");
                    mySecureTCPServer.SendDataAsync(clientIndex, b, b.Length, SecureSendDataAsyncCallback);
                    Debug.Console(0, "Sent Shared Key to client at {0}", mySecureTCPServer.GetAddressServerAcceptedConnectionFromForSpecificClient(clientIndex));
                }
                mySecureTCPServer.ReceiveDataAsync(clientIndex, SecureReceivedCallback);
                if (mySecureTCPServer.State != ServerState.SERVER_LISTENING && MaxConnections > 1 && !ServerStopped)
                    SecureServer.WaitForConnectionAsync(IPAddress.Any, SecureConnectCallback);
            }
        }

        void UnsecureConnectCallback(TCPServer myTCPServer, uint clientIndex)
        {
            if (myTCPServer.ClientConnected(clientIndex))
            {
                Debug.Console(0, "Connected to client at {0}", myTCPServer.GetAddressServerAcceptedConnectionFromForSpecificClient(clientIndex));
                myTCPServer.ReceiveDataAsync(clientIndex, UnsecureReceivedCallback);
            }
            if (myTCPServer.State != ServerState.SERVER_LISTENING && MaxConnections > 1 && !ServerStopped)
                UnsecureServer.WaitForConnectionAsync(IPAddress.Any, UnsecureConnectCallback);
        }
        #endregion

        #region Methods - Send/Receive Callbacks
        void SecureSendDataAsyncCallback(SecureTCPServer mySecureTCPServer, uint clientIndex, int numberOfBytesSent)
        {

        }

        void UnsecureSendDataAsyncCallback(TCPServer myTCPServer, uint clientIndex, int numberOfBytesSent)
        {

        }

        void SecureReceivedCallback(SecureTCPServer mySecureTCPServer, uint clientIndex, int numberOfBytesReceived)
        {
            if (numberOfBytesReceived > 0)
            {
                string received = "Nothing";
                byte[] bytes = mySecureTCPServer.GetIncomingDataBufferForSpecificClient(clientIndex);
                received = System.Text.Encoding.ASCII.GetString(bytes, 0, numberOfBytesReceived);
                Debug.Console(0, "Secure Server Listening on Port: {0}, client IP: {1}, NumberOfBytesReceived: {2}, Received: {3}\r\n",
                        mySecureTCPServer.PortNumber, mySecureTCPServer.AddressServerAcceptedConnectionFrom, numberOfBytesReceived, received);
                if (WaitingForSharedKey.Contains(clientIndex))
                {
                    received = received.Replace("\r", "");
                    received = received.Replace("\n", "");
                    if (received != SharedKey)
                    {
                        byte[] b = Encoding.ASCII.GetBytes("Shared key did not match server. Disconnecting");
                        Debug.Console(0, "Client at index {0} Shared key did not match the server, disconnecting client", clientIndex);
                        ErrorLog.Error("Client at index {0} Shared key did not match the server, disconnecting client", clientIndex);
                        mySecureTCPServer.SendDataAsync(clientIndex, b, b.Length, null);
                        mySecureTCPServer.Disconnect(clientIndex);
                    }
                    if (mySecureTCPServer.NumberOfClientsConnected > 0)
                        mySecureTCPServer.ReceiveDataAsync(SecureReceivedCallback);
                    WaitingForSharedKey.Remove(clientIndex);
                    byte[] skResponse = Encoding.ASCII.GetBytes("Shared Key Match, Connected and ready for communication");
                    mySecureTCPServer.SendDataAsync(clientIndex, skResponse, skResponse.Length, null);
                    mySecureTCPServer.ReceiveDataAsync(SecureReceivedCallback);
                }
                else
                {
                    onTextReceived(received);
                    mySecureTCPServer.ReceiveDataAsync(SecureReceivedCallback);
                }
            }
            if (mySecureTCPServer.GetServerSocketStatusForSpecificClient(clientIndex) == SocketStatus.SOCKET_STATUS_CONNECTED)
                mySecureTCPServer.ReceiveDataAsync(clientIndex, SecureReceivedCallback);
        }

        void UnsecureReceivedCallback(TCPServer myTCPServer, uint clientIndex, int numberOfBytesReceived)
        {
            if (numberOfBytesReceived > 0)
            {
                string received = "Nothing";
                byte[] bytes = myTCPServer.GetIncomingDataBufferForSpecificClient(clientIndex);
                received = System.Text.Encoding.ASCII.GetString(bytes, 0, numberOfBytesReceived);
                Debug.Console(0, "Unsecure Server Listening on Port: {0}, client IP: {1}, NumberOfBytesReceived: {2}, Received: {3}\r\n",
                        myTCPServer.PortNumber, myTCPServer.AddressServerAcceptedConnectionFrom, numberOfBytesReceived, received);
                onTextReceived(received);
                myTCPServer.ReceiveDataAsync(UnsecureReceivedCallback);                
            }
            if (myTCPServer.GetServerSocketStatusForSpecificClient(clientIndex) == SocketStatus.SOCKET_STATUS_CONNECTED)
                myTCPServer.ReceiveDataAsync(clientIndex, UnsecureReceivedCallback);
        }
        #endregion

        #region Methods - EventHelpers/Callbacks
        void onConnectionChange()
        {
            var handler = ClientConnectionChange;
            if (handler != null)
            {
                if (Secure)
                    ClientConnectionChange(this, new DynamicTCPServerSocketStatusChangeEventArgs(SecureServer, Secure));
                else
                    ClientConnectionChange(this, new DynamicTCPServerSocketStatusChangeEventArgs(UnsecureServer, Secure));
            }
        }

        void onTextReceived(string text)
        {
            var handler = TextReceived;
            if (handler != null)
                TextReceived(this, new GenericCommMethodReceiveTextArgs(text));
        }

        void CrestronEnvironment_ProgramStatusEventHandler(eProgramStatusEventType programEventType)
        {
            if (programEventType == eProgramStatusEventType.Stopping)
            {
                Debug.Console(1, this, "Program stopping. Closing server");
                DisconnectAllClients();
                StopListening();
            }
        }
        #endregion
    }
}