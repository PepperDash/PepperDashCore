/*PepperDash Technology Corp.
JAG
Copyright:		2017
------------------------------------
***Notice of Ownership and Copyright***
The material in which this notice appears is the property of PepperDash Technology Corporation, 
which claims copyright under the laws of the United States of America in the entire body of material 
and in all parts thereof, regardless of the use to which it is being put.  Any use, in whole or in part, 
of this material by another party without the express written permission of PepperDash Technology Corporation is prohibited.  
PepperDash Technology Corporation reserves all rights under applicable laws.
------------------------------------ */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using PepperDash.Core;

namespace DynamicTCP
{
    public class DynamicTCPServer : Device
    {
        #region Events
        /// <summary>
        /// Event for Receiving text
        /// </summary>
        public event EventHandler<CopyCoreForSimplpGenericCommMethodReceiveTextArgs> TextReceived;

        /// <summary>
        /// Event for client connection socket status change
        /// </summary>
        public event EventHandler<DynamicTCPSocketStatusChangeEventArgs> ClientConnectionChange;

        /// <summary>
        /// Event for Server State Change
        /// </summary>
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
        public ushort USecure
        {
            set
            {
                if (value == 1)
                    Secure = true;
                else if (value == 0)
                    Secure = false;
            }
        }

        /// <summary>
        /// Text representation of the Socket Status enum values for the server
        /// </summary>
        public string Status
        {
            get 
            {
                if (Secure ? SecureServer != null : UnsecureServer != null)
                    return Secure ? SecureServer.State.ToString() : UnsecureServer.State.ToString();
                else
                    return "";
            }

        }

        /// <summary>
        /// Bool showing if socket is connected
        /// </summary>
        public bool IsConnected
        {
            get 
            { 
                return (Secure ? SecureServer != null : UnsecureServer != null) && 
                (Secure ? SecureServer.State == ServerState.SERVER_CONNECTED : UnsecureServer.State == ServerState.SERVER_CONNECTED); 
            }
        }

        /// <summary>
        /// S+ helper for IsConnected
        /// </summary>
        public ushort UIsConnected
        {
            get { return (ushort)(IsConnected ? 1 : 0); }
        }

        /// <summary>
        /// Bool showing if socket is connected
        /// </summary>
        public bool IsListening
        {
            get { return (Secure ? SecureServer != null : UnsecureServer != null) && 
                (Secure ? SecureServer.State == ServerState.SERVER_LISTENING : UnsecureServer.State == ServerState.SERVER_LISTENING); }
        }
        
        /// <summary>
        /// S+ helper for IsConnected
        /// </summary>
        public ushort UIsListening
        {
            get { return (ushort)(IsListening ? 1 : 0); }
        }

        public ushort MaxClients { get; set; } // should be set by parameter in SIMPL+ in the MAIN method, Should not ever need to be configurable
        /// <summary>
        /// Number of clients currently connected.
        /// </summary>
        public ushort NumberOfClientsConnected
        {
            get 
            {
                if (Secure ? SecureServer != null : UnsecureServer != null)
                    return Secure ? (ushort)SecureServer.NumberOfClientsConnected : (ushort)UnsecureServer.NumberOfClientsConnected;
                return 0;
            }            
        }

        /// <summary>
        /// Port Server should listen on
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// S+ helper for Port
        /// </summary>
        public ushort UPort
        {
            get { return Convert.ToUInt16(Port); }
            set { Port = Convert.ToInt32(value); }
        }

        /// <summary>
        /// Bool to show whether the server requires a preshared key. Must be set the same in the client, and if true shared keys must be identical on server/client
        /// </summary>
        public bool SharedKeyRequired { get; set; }

        /// <summary>
        /// S+ helper for requires shared key bool
        /// </summary>
        public ushort USharedKeyRequired
        {
            set
            {
                if (value == 1)
                    SharedKeyRequired = true;
                else
                    SharedKeyRequired = false;
            }
        }

        /// <summary>
        /// SharedKey is sent for varification to the server. Shared key can be any text (255 char limit in SIMPL+ Module), but must match the Shared Key on the Server module. 
        /// If SharedKey changes while server is listening or clients are connected, disconnect and stop listening will be called
        /// </summary>
        public string SharedKey { get; set; }

        /// <summary>
        /// Heartbeat Required bool sets whether server disconnects client if heartbeat is not received
        /// </summary>
        public bool HeartbeatRequired { get; set; }

        /// <summary>
        /// S+ Helper for Heartbeat Required
        /// </summary>
        public ushort UHeartbeatRequired
        {
            set
            {
                if (value == 1)
                    HeartbeatRequired = true;
                else
                    HeartbeatRequired = false;
            }
        }

        /// <summary>
        /// Milliseconds before server expects another heartbeat. Set by property HeartbeatRequiredIntervalInSeconds which is driven from S+
        /// </summary>
        public int HeartbeatRequiredIntervalMs { get; set; }

        /// <summary>
        /// Simpl+ Heartbeat Analog value in seconds
        /// </summary>
        public ushort HeartbeatRequiredIntervalInSeconds { set { HeartbeatRequiredIntervalMs = (value * 1000); } }

        /// <summary>
        /// String to Match for heartbeat. If null or empty any string will reset heartbeat timer
        /// </summary>
        public string HeartbeatStringToMatch { get; set; }

        //private timers for Heartbeats per client
        Dictionary<uint, CTimer> HeartbeatTimerDictionary = new Dictionary<uint, CTimer>(); 
                     
        //flags to show the secure server is waiting for client at index to send the shared key
        List<uint> WaitingForSharedKey = new List<uint>();

        //Store the connected client indexes
        List<uint> ConnectedClientsIndexes = new List<uint>();

        /// <summary>
        /// Defaults to 2000
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// Private flag to note that the server has stopped intentionally
        /// </summary>
        private bool ServerStopped { get; set; }

        //Servers
        SecureTCPServer SecureServer;
        TCPServer UnsecureServer;

        #endregion

        #region Constructors
        /// <summary>
        /// constructor
        /// </summary>
        public DynamicTCPServer()
            : base("Uninitialized Dynamic TCP Server")
        {
            CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(CrestronEnvironment_ProgramStatusEventHandler);
            BufferSize = 2000;
            Secure = false;
        }
        #endregion

        #region Methods - Server Actions
        /// <summary>
        /// Initialize Key for device using client name from SIMPL+. Called on Listen from SIMPL+
        /// </summary>
        /// <param name="key"></param>
        public void Initialize(string key)
        {
            Key = key;
        }

        /// <summary>
        /// Start listening on the specified port
        /// </summary>
        public void Listen()
        {
            try
            {
                if (Port < 1 || Port > 65535)
                {
                    Debug.Console(1, Debug.ErrorLogLevel.Warning, "GenericSecureTcpClient '{0}': Invalid port", Key);
                    ErrorLog.Warn(string.Format("GenericSecureTcpClient '{0}': Invalid port", Key));
                    return;
                }
                if (string.IsNullOrEmpty(SharedKey) && SharedKeyRequired)
                {
                    Debug.Console(1, Debug.ErrorLogLevel.Warning, "GenericSecureTcpClient '{0}': No Shared Key set", Key);
                    ErrorLog.Warn(string.Format("GenericSecureTcpClient '{0}': No Shared Key set", Key));
                    return;
                }
                if (IsListening)
                    return;
                if (Secure)
                {
                    SecureServer = new SecureTCPServer(Port, MaxClients);
                    SecureServer.SocketStatusChange += new SecureTCPServerSocketStatusChangeEventHandler(SecureServer_SocketStatusChange);
                    ServerStopped = false;                    
                    SecureServer.WaitForConnectionAsync(IPAddress.Any, SecureConnectCallback);
                    onServerStateChange();
                    Debug.Console(2, "Secure Server Status: {0}, Socket Status: {1}\r\n", SecureServer.State.ToString(), SecureServer.ServerSocketStatus);
                }
                else
                {
                    UnsecureServer = new TCPServer(Port, MaxClients);
                    UnsecureServer.SocketStatusChange += new TCPServerSocketStatusChangeEventHandler(UnsecureServer_SocketStatusChange);
                    ServerStopped = false;
                    UnsecureServer.WaitForConnectionAsync(IPAddress.Any, UnsecureConnectCallback);
                    onServerStateChange();
                    Debug.Console(2, "Unsecure Server Status: {0}, Socket Status: {1}\r\n", UnsecureServer.State.ToString(), UnsecureServer.ServerSocketStatus);
                }
            }
            catch (Exception ex)
            {
                ErrorLog.Error("Error with Dynamic Server: {0}", ex.ToString()); 
            }
        }

        /// <summary>
        /// Stop Listeneing
        /// </summary>
        public void StopListening()
        {
            Debug.Console(2, "Stopping Listener");
            if (SecureServer != null)
                SecureServer.Stop();
            if (UnsecureServer != null)
                UnsecureServer.Stop();
            ServerStopped = true;
            onServerStateChange();
        }

        /// <summary>
        /// Disconnect All Clients
        /// </summary>
        public void DisconnectAllClients()
        {
            Debug.Console(2, "Disconnecting All Clients");
            if (SecureServer != null)
                SecureServer.DisconnectAll();
            if (UnsecureServer != null)
                UnsecureServer.DisconnectAll();
            onConnectionChange();
            onServerStateChange(); //State shows both listening and connected
        }

        /// <summary>
        /// Broadcast text from server to all connected clients
        /// </summary>
        /// <param name="text"></param>
        public void BroadcastText(string text)
        {
            if (ConnectedClientsIndexes.Count > 0)
            {
                byte[] b = Encoding.GetEncoding(28591).GetBytes(text);
                if (Secure)
                    foreach (uint i in ConnectedClientsIndexes)
                        SecureServer.SendDataAsync(i, b, b.Length, SecureSendDataAsyncCallback);
                else
                    foreach (uint i in ConnectedClientsIndexes)
                        UnsecureServer.SendDataAsync(i, b, b.Length, UnsecureSendDataAsyncCallback);
            }
        }

        /// <summary>
        /// Not sure this is useful in library, maybe Pro??
        /// </summary>
        /// <param name="text"></param>
        /// <param name="clientIndex"></param>
        public void SendTextToClient(string text, uint clientIndex)
        {
            byte[] b = Encoding.GetEncoding(28591).GetBytes(text);
            if (Secure)
                SecureServer.SendDataAsync(clientIndex, b, b.Length, SecureSendDataAsyncCallback);
            else
                UnsecureServer.SendDataAsync(clientIndex, b, b.Length, UnsecureSendDataAsyncCallback);
        }

        //private method to check heartbeat requirements and start or reset timer
        void checkHeartbeat(uint clientIndex, string received)
        {
            if (HeartbeatRequired)
            {
                if (!string.IsNullOrEmpty(HeartbeatStringToMatch))
                {
                    if (received == HeartbeatStringToMatch)
                    {
                        if (HeartbeatTimerDictionary.ContainsKey(clientIndex))
                            HeartbeatTimerDictionary[clientIndex].Reset(HeartbeatRequiredIntervalMs);
                        else
                        {
                            CTimer HeartbeatTimer = new CTimer(HeartbeatTimer_CallbackFunction, clientIndex, HeartbeatRequiredIntervalMs);
                            HeartbeatTimerDictionary.Add(clientIndex, HeartbeatTimer);
                        }
                    }
                }
                else
                {
                    if (HeartbeatTimerDictionary.ContainsKey(clientIndex))
                        HeartbeatTimerDictionary[clientIndex].Reset(HeartbeatRequiredIntervalMs);
                    else
                    {
                        CTimer HeartbeatTimer = new CTimer(HeartbeatTimer_CallbackFunction, clientIndex, HeartbeatRequiredIntervalMs);
                        HeartbeatTimerDictionary.Add(clientIndex, HeartbeatTimer);
                    }
                }
            }
        }
        #endregion

        #region Methods - HeartbeatTimer Callback

        void HeartbeatTimer_CallbackFunction(object o)
        {
            uint clientIndex = (uint)o;

            string address = Secure ? SecureServer.GetAddressServerAcceptedConnectionFromForSpecificClient(clientIndex) :
                UnsecureServer.GetAddressServerAcceptedConnectionFromForSpecificClient(clientIndex);

            ErrorLog.Error("Heartbeat not received for Client at IP: {0}, DISCONNECTING BECAUSE HEARTBEAT REQUIRED IS TRUE", address);
            Debug.Console(2, "Heartbeat not received for Client at IP: {0}, DISCONNECTING BECAUSE HEARTBEAT REQUIRED IS TRUE", address);

            SendTextToClient("Heartbeat not received by server, closing connection", clientIndex);

            if (Secure)
                SecureServer.Disconnect(clientIndex);
            else
                UnsecureServer.Disconnect(clientIndex);
            HeartbeatTimerDictionary.Remove(clientIndex);
        }

        #endregion

        #region Methods - Socket Status Changed Callbacks
        /// <summary>
        /// Secure Server Socket Status Changed Callback
        /// </summary>
        /// <param name="mySecureTCPServer"></param>
        /// <param name="clientIndex"></param>
        /// <param name="serverSocketStatus"></param>
        void SecureServer_SocketStatusChange(SecureTCPServer server, uint clientIndex, SocketStatus serverSocketStatus)
        {
            Debug.Console(2, "Client at {0} ServerSocketStatus {1}",
                server.GetAddressServerAcceptedConnectionFromForSpecificClient(clientIndex), serverSocketStatus.ToString());            
            if (server.GetServerSocketStatusForSpecificClient(clientIndex) == SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                if (SharedKeyRequired && !WaitingForSharedKey.Contains(clientIndex))
                    WaitingForSharedKey.Add(clientIndex);
                if (!ConnectedClientsIndexes.Contains(clientIndex))
                    ConnectedClientsIndexes.Add(clientIndex);
            }
            else
            {
                if (ConnectedClientsIndexes.Contains(clientIndex))
                    ConnectedClientsIndexes.Remove(clientIndex);
                if (HeartbeatRequired && HeartbeatTimerDictionary.ContainsKey(clientIndex))
                    HeartbeatTimerDictionary.Remove(clientIndex);
            }
            if(SecureServer.ServerSocketStatus.ToString() != Status)
                onConnectionChange();
        }

        /// <summary>
        /// TCP Server (Unsecure) Socket Status Change Callback
        /// </summary>
        /// <param name="mySecureTCPServer"></param>
        /// <param name="clientIndex"></param>
        /// <param name="serverSocketStatus"></param>
        void UnsecureServer_SocketStatusChange(TCPServer server, uint clientIndex, SocketStatus serverSocketStatus)
        {
            Debug.Console(2, "Client at {0} ServerSocketStatus {1}",
                server.GetAddressServerAcceptedConnectionFromForSpecificClient(clientIndex), serverSocketStatus.ToString());
            if (server.GetServerSocketStatusForSpecificClient(clientIndex) == SocketStatus.SOCKET_STATUS_CONNECTED)
            {
                if (SharedKeyRequired && !WaitingForSharedKey.Contains(clientIndex))
                    WaitingForSharedKey.Add(clientIndex);
                if (!ConnectedClientsIndexes.Contains(clientIndex))
                    ConnectedClientsIndexes.Add(clientIndex);
            }
            else
            {
                if (ConnectedClientsIndexes.Contains(clientIndex))
                    ConnectedClientsIndexes.Remove(clientIndex);
                if (HeartbeatRequired && HeartbeatTimerDictionary.ContainsKey(clientIndex))
                    HeartbeatTimerDictionary.Remove(clientIndex);
            }
            if (UnsecureServer.ServerSocketStatus.ToString() != Status)
                onConnectionChange();
        }
        #endregion

        #region Methods Connected Callbacks
        /// <summary>
        /// Secure TCP Client Connected to Secure Server Callback
        /// </summary>
        /// <param name="mySecureTCPServer"></param>
        /// <param name="clientIndex"></param>
        void SecureConnectCallback(SecureTCPServer mySecureTCPServer, uint clientIndex)
        {
            if (mySecureTCPServer.ClientConnected(clientIndex))
            {
                if (SharedKeyRequired)
                {
                    byte[] b = Encoding.GetEncoding(28591).GetBytes(SharedKey + "\n");
                    mySecureTCPServer.SendDataAsync(clientIndex, b, b.Length, SecureSendDataAsyncCallback);
                    Debug.Console(2, "Sent Shared Key to client at {0}", mySecureTCPServer.GetAddressServerAcceptedConnectionFromForSpecificClient(clientIndex));
                }
                if (HeartbeatRequired)
                {
                    CTimer HeartbeatTimer = new CTimer(HeartbeatTimer_CallbackFunction, clientIndex, HeartbeatRequiredIntervalMs);
                    HeartbeatTimerDictionary.Add(clientIndex, HeartbeatTimer);
                }
                mySecureTCPServer.ReceiveDataAsync(clientIndex, SecureReceivedDataAsyncCallback);
                if (mySecureTCPServer.State != ServerState.SERVER_LISTENING && MaxClients > 1 && !ServerStopped)
                    mySecureTCPServer.WaitForConnectionAsync(IPAddress.Any, SecureConnectCallback);
            }
        }

        /// <summary>
        /// Unsecure TCP Client Connected to Unsecure Server Callback
        /// </summary>
        /// <param name="myTCPServer"></param>
        /// <param name="clientIndex"></param>
        void UnsecureConnectCallback(TCPServer myTCPServer, uint clientIndex)
        {
            if (myTCPServer.ClientConnected(clientIndex))
            {
                if (SharedKeyRequired)
                {
                    byte[] b = Encoding.GetEncoding(28591).GetBytes(SharedKey + "\n");
                    myTCPServer.SendDataAsync(clientIndex, b, b.Length, UnsecureSendDataAsyncCallback);
                    Debug.Console(2, "Sent Shared Key to client at {0}", myTCPServer.GetAddressServerAcceptedConnectionFromForSpecificClient(clientIndex));
                }
                if (HeartbeatRequired)
                {
                    CTimer HeartbeatTimer = new CTimer(HeartbeatTimer_CallbackFunction, clientIndex, HeartbeatRequiredIntervalMs);
                    HeartbeatTimerDictionary.Add(clientIndex, HeartbeatTimer);
                }
                myTCPServer.ReceiveDataAsync(clientIndex, UnsecureReceivedDataAsyncCallback);
                if (myTCPServer.State != ServerState.SERVER_LISTENING && MaxClients > 1 && !ServerStopped)
                    myTCPServer.WaitForConnectionAsync(IPAddress.Any, UnsecureConnectCallback);
            }
            if (myTCPServer.State != ServerState.SERVER_LISTENING && MaxClients > 1 && !ServerStopped)
                myTCPServer.WaitForConnectionAsync(IPAddress.Any, UnsecureConnectCallback);
        }
        #endregion

        #region Methods - Send/Receive Callbacks
        /// <summary>
        /// Secure Send Data Async Callback
        /// </summary>
        /// <param name="mySecureTCPServer"></param>
        /// <param name="clientIndex"></param>
        /// <param name="numberOfBytesSent"></param>
        void SecureSendDataAsyncCallback(SecureTCPServer mySecureTCPServer, uint clientIndex, int numberOfBytesSent)
        {
            //Seems there is nothing to do here
        }

        /// <summary>
        /// Unsecure Send Data Asyc Callback
        /// </summary>
        /// <param name="myTCPServer"></param>
        /// <param name="clientIndex"></param>
        /// <param name="numberOfBytesSent"></param>
        void UnsecureSendDataAsyncCallback(TCPServer myTCPServer, uint clientIndex, int numberOfBytesSent)
        {
            //Seems there is nothing to do here
        }

        /// <summary>
        /// Secure Received Data Async Callback
        /// </summary>
        /// <param name="mySecureTCPServer"></param>
        /// <param name="clientIndex"></param>
        /// <param name="numberOfBytesReceived"></param>
        void SecureReceivedDataAsyncCallback(SecureTCPServer mySecureTCPServer, uint clientIndex, int numberOfBytesReceived)
        {
            if (numberOfBytesReceived > 0)
            {
                string received = "Nothing";
                byte[] bytes = mySecureTCPServer.GetIncomingDataBufferForSpecificClient(clientIndex);
                received = System.Text.Encoding.GetEncoding(28591).GetString(bytes, 0, numberOfBytesReceived);                
                if (WaitingForSharedKey.Contains(clientIndex))
                {
                    received = received.Replace("\r", "");
                    received = received.Replace("\n", "");
                    if (received != SharedKey)
                    {
                        byte[] b = Encoding.GetEncoding(28591).GetBytes("Shared key did not match server. Disconnecting");
                        Debug.Console(2, "Client at index {0} Shared key did not match the server, disconnecting client", clientIndex);
                        ErrorLog.Error("Client at index {0} Shared key did not match the server, disconnecting client", clientIndex);
                        mySecureTCPServer.SendDataAsync(clientIndex, b, b.Length, null);
                        mySecureTCPServer.Disconnect(clientIndex);
                    }
                    if (mySecureTCPServer.NumberOfClientsConnected > 0)
                        mySecureTCPServer.ReceiveDataAsync(SecureReceivedDataAsyncCallback);
                    WaitingForSharedKey.Remove(clientIndex);
                    byte[] skResponse = Encoding.GetEncoding(28591).GetBytes("Shared Key Match, Connected and ready for communication");
                    mySecureTCPServer.SendDataAsync(clientIndex, skResponse, skResponse.Length, null);
                    mySecureTCPServer.ReceiveDataAsync(SecureReceivedDataAsyncCallback);
                }
                else
                {
                    mySecureTCPServer.ReceiveDataAsync(SecureReceivedDataAsyncCallback);
                    Debug.Console(2, "Secure Server Listening on Port: {0}, client IP: {1}, NumberOfBytesReceived: {2}, Received: {3}\r\n",
                        mySecureTCPServer.PortNumber, mySecureTCPServer.GetAddressServerAcceptedConnectionFromForSpecificClient(clientIndex), numberOfBytesReceived, received);
                    onTextReceived(received);
                }
                checkHeartbeat(clientIndex, received);
            }
            if (mySecureTCPServer.GetServerSocketStatusForSpecificClient(clientIndex) == SocketStatus.SOCKET_STATUS_CONNECTED)
                mySecureTCPServer.ReceiveDataAsync(clientIndex, SecureReceivedDataAsyncCallback);
        }

        /// <summary>
        /// Unsecure Received Data Async Callback
        /// </summary>
        /// <param name="myTCPServer"></param>
        /// <param name="clientIndex"></param>
        /// <param name="numberOfBytesReceived"></param>
        void UnsecureReceivedDataAsyncCallback(TCPServer myTCPServer, uint clientIndex, int numberOfBytesReceived)
        {
            if (numberOfBytesReceived > 0)
            {
                string received = "Nothing";
                byte[] bytes = myTCPServer.GetIncomingDataBufferForSpecificClient(clientIndex);
                received = System.Text.Encoding.GetEncoding(28591).GetString(bytes, 0, numberOfBytesReceived);
                if (WaitingForSharedKey.Contains(clientIndex))
                {
                    received = received.Replace("\r", "");
                    received = received.Replace("\n", "");
                    if (received != SharedKey)
                    {
                        byte[] b = Encoding.GetEncoding(28591).GetBytes("Shared key did not match server. Disconnecting");
                        Debug.Console(2, "Client at index {0} Shared key did not match the server, disconnecting client", clientIndex);
                        ErrorLog.Error("Client at index {0} Shared key did not match the server, disconnecting client", clientIndex);
                        myTCPServer.SendDataAsync(clientIndex, b, b.Length, null);
                        myTCPServer.Disconnect(clientIndex);
                    }
                    if (myTCPServer.NumberOfClientsConnected > 0)
                        myTCPServer.ReceiveDataAsync(UnsecureReceivedDataAsyncCallback);
                    WaitingForSharedKey.Remove(clientIndex);
                    byte[] skResponse = Encoding.GetEncoding(28591).GetBytes("Shared Key Match, Connected and ready for communication");
                    myTCPServer.SendDataAsync(clientIndex, skResponse, skResponse.Length, null);
                    myTCPServer.ReceiveDataAsync(UnsecureReceivedDataAsyncCallback);
                }
                else
                {
                    myTCPServer.ReceiveDataAsync(UnsecureReceivedDataAsyncCallback);
                    Debug.Console(2, "Secure Server Listening on Port: {0}, client IP: {1}, NumberOfBytesReceived: {2}, Received: {3}\r\n",
                        myTCPServer.PortNumber, myTCPServer.GetAddressServerAcceptedConnectionFromForSpecificClient(clientIndex), numberOfBytesReceived, received);
                    onTextReceived(received);
                }
                checkHeartbeat(clientIndex, received);
            }
            if (myTCPServer.GetServerSocketStatusForSpecificClient(clientIndex) == SocketStatus.SOCKET_STATUS_CONNECTED)
                myTCPServer.ReceiveDataAsync(clientIndex, UnsecureReceivedDataAsyncCallback);
        }
        #endregion

        #region Methods - EventHelpers/Callbacks
        //Private Helper method to call the Connection Change Event
        void onConnectionChange()
        {
            var handler = ClientConnectionChange;
            if (handler != null)
            {
                if (Secure)
                    handler(this, new DynamicTCPSocketStatusChangeEventArgs(SecureServer, Secure));
                else
                    handler(this, new DynamicTCPSocketStatusChangeEventArgs(UnsecureServer, Secure));
            }
        }

        //Private Helper Method to call the Text Received Event
        void onTextReceived(string text)
        {
            var handler = TextReceived;
            if (handler != null)
                handler(this, new CopyCoreForSimplpGenericCommMethodReceiveTextArgs(text));
        }

        //Private Helper Method to call the Server State Change Event
        void onServerStateChange()
        {
            var handler = ServerStateChange;
            if(handler != null)
            {
                if(Secure)
                    handler(this, new DynamicTCPServerStateChangedEventArgs(SecureServer, Secure));
                else
                    handler(this, new DynamicTCPServerStateChangedEventArgs(UnsecureServer, Secure));
            }
        }

        //Private Event Handler method to handle the closing of connections when the program stops
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