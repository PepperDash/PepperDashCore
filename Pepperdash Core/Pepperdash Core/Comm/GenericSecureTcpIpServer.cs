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

namespace PepperDash.Core
{
    public class GenericSecureTcpIpServer : Device
    {
        #region Events
        /// <summary>
        /// Event for Receiving text
        /// </summary>
        public event EventHandler<GenericCommMethodReceiveTextArgs> TextReceived;

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
        /// Text representation of the Socket Status enum values for the server
        /// </summary>
        public string Status
        {
            get
            {
                if (Server != null)
                    return Server.State.ToString();
                else
                    return ServerState.SERVER_NOT_LISTENING.ToString();
            }

        }

        /// <summary>
        /// Bool showing if socket is connected
        /// </summary>
        public bool IsConnected
        {
            get { return (Server != null) && (Server.State == ServerState.SERVER_CONNECTED); }
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
            get { return (Server != null) && (Server.State == ServerState.SERVER_LISTENING); }
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
                if (Server != null)
                    return (ushort)Server.NumberOfClientsConnected;
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

        //flags to show the server is waiting for client at index to send the shared key
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
        SecureTCPServer Server;

        #endregion

        #region Constructors
        /// <summary>
        /// constructor
        /// </summary>
        public GenericSecureTcpIpServer()
            : base("Uninitialized Dynamic TCP Server")
        {
            CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(CrestronEnvironment_ProgramStatusEventHandler);
            BufferSize = 2000;
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
                    Debug.Console(1, Debug.ErrorLogLevel.Warning, "Server '{0}': Invalid port", Key);
                    ErrorLog.Warn(string.Format("Server '{0}': Invalid port", Key));
                    return;
                }
                if (string.IsNullOrEmpty(SharedKey) && SharedKeyRequired)
                {
                    Debug.Console(1, Debug.ErrorLogLevel.Warning, "Server '{0}': No Shared Key set", Key);
                    ErrorLog.Warn(string.Format("Server '{0}': No Shared Key set", Key));
                    return;
                }
                if (IsListening)
                    return;
                Server = new SecureTCPServer(Port, MaxClients);
                Server.SocketStatusChange += new SecureTCPServerSocketStatusChangeEventHandler(SocketStatusChange);
                ServerStopped = false;
                Server.WaitForConnectionAsync(IPAddress.Any, ConnectCallback);
                onServerStateChange();
                Debug.Console(2, "Server Status: {0}, Socket Status: {1}\r\n", Server.State.ToString(), Server.ServerSocketStatus);
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
            if (Server != null)
                Server.Stop();
            ServerStopped = true;
            onServerStateChange();
        }

        /// <summary>
        /// Disconnect All Clients
        /// </summary>
        public void DisconnectAllClients()
        {
            Debug.Console(2, "Disconnecting All Clients");
            if (Server != null)
                Server.DisconnectAll();
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
                foreach (uint i in ConnectedClientsIndexes)
                    Server.SendDataAsync(i, b, b.Length, SendDataAsyncCallback);
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
            Server.SendDataAsync(clientIndex, b, b.Length, SendDataAsyncCallback);
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

        #region Methods - Callbacks
        /// <summary>
        /// Callback to disconnect if heartbeat timer finishes without being reset
        /// </summary>
        /// <param name="o"></param>
        void HeartbeatTimer_CallbackFunction(object o)
        {
            uint clientIndex = (uint)o;

            string address = Server.GetAddressServerAcceptedConnectionFromForSpecificClient(clientIndex);

            ErrorLog.Error("Heartbeat not received for Client at IP: {0}, DISCONNECTING BECAUSE HEARTBEAT REQUIRED IS TRUE", address);
            Debug.Console(2, "Heartbeat not received for Client at IP: {0}, DISCONNECTING BECAUSE HEARTBEAT REQUIRED IS TRUE", address);

            SendTextToClient("Heartbeat not received by server, closing connection", clientIndex);
            Server.Disconnect(clientIndex);
            HeartbeatTimerDictionary.Remove(clientIndex);
        }

        /// <summary>
        /// TCP Server Socket Status Change Callback
        /// </summary>
        /// <param name="server"></param>
        /// <param name="clientIndex"></param>
        /// <param name="serverSocketStatus"></param>
        void SocketStatusChange(SecureTCPServer server, uint clientIndex, SocketStatus serverSocketStatus)
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
            if (Server.ServerSocketStatus.ToString() != Status)
                onConnectionChange();
        }

        /// <summary>
        /// TCP Client Connected to Server Callback
        /// </summary>
        /// <param name="mySecureTCPServer"></param>
        /// <param name="clientIndex"></param>
        void ConnectCallback(SecureTCPServer mySecureTCPServer, uint clientIndex)
        {
            if (mySecureTCPServer.ClientConnected(clientIndex))
            {
                if (SharedKeyRequired)
                {
                    byte[] b = Encoding.GetEncoding(28591).GetBytes(SharedKey + "\n");
                    mySecureTCPServer.SendDataAsync(clientIndex, b, b.Length, SendDataAsyncCallback);
                    Debug.Console(2, "Sent Shared Key to client at {0}", mySecureTCPServer.GetAddressServerAcceptedConnectionFromForSpecificClient(clientIndex));
                }
                if (HeartbeatRequired)
                {
                    CTimer HeartbeatTimer = new CTimer(HeartbeatTimer_CallbackFunction, clientIndex, HeartbeatRequiredIntervalMs);
                    HeartbeatTimerDictionary.Add(clientIndex, HeartbeatTimer);
                }
                mySecureTCPServer.ReceiveDataAsync(clientIndex, ReceivedDataAsyncCallback);
                if (mySecureTCPServer.State != ServerState.SERVER_LISTENING && MaxClients > 1 && !ServerStopped)
                    mySecureTCPServer.WaitForConnectionAsync(IPAddress.Any, ConnectCallback);
            }
            if (mySecureTCPServer.State != ServerState.SERVER_LISTENING && MaxClients > 1 && !ServerStopped)
                mySecureTCPServer.WaitForConnectionAsync(IPAddress.Any, ConnectCallback);
        }

        /// <summary>
        /// Send Data Asyc Callback
        /// </summary>
        /// <param name="mySecureTCPServer"></param>
        /// <param name="clientIndex"></param>
        /// <param name="numberOfBytesSent"></param>
        void SendDataAsyncCallback(SecureTCPServer mySecureTCPServer, uint clientIndex, int numberOfBytesSent)
        {
            //Seems there is nothing to do here
        }

        /// <summary>
        /// Received Data Async Callback
        /// </summary>
        /// <param name="mySecureTCPServer"></param>
        /// <param name="clientIndex"></param>
        /// <param name="numberOfBytesReceived"></param>
        void ReceivedDataAsyncCallback(SecureTCPServer mySecureTCPServer, uint clientIndex, int numberOfBytesReceived)
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
                        mySecureTCPServer.ReceiveDataAsync(ReceivedDataAsyncCallback);
                    WaitingForSharedKey.Remove(clientIndex);
                    byte[] skResponse = Encoding.GetEncoding(28591).GetBytes("Shared Key Match, Connected and ready for communication");
                    mySecureTCPServer.SendDataAsync(clientIndex, skResponse, skResponse.Length, null);
                    mySecureTCPServer.ReceiveDataAsync(ReceivedDataAsyncCallback);
                }
                else
                {
                    mySecureTCPServer.ReceiveDataAsync(ReceivedDataAsyncCallback);
                    Debug.Console(2, "Server Listening on Port: {0}, client IP: {1}, NumberOfBytesReceived: {2}, Received: {3}\r\n",
                        mySecureTCPServer.PortNumber, mySecureTCPServer.GetAddressServerAcceptedConnectionFromForSpecificClient(clientIndex), numberOfBytesReceived, received);
                    onTextReceived(received);
                }
                checkHeartbeat(clientIndex, received);
            }
            if (mySecureTCPServer.GetServerSocketStatusForSpecificClient(clientIndex) == SocketStatus.SOCKET_STATUS_CONNECTED)
                mySecureTCPServer.ReceiveDataAsync(clientIndex, ReceivedDataAsyncCallback);
        }
        #endregion

        #region Methods - EventHelpers/Callbacks
        //Private Helper method to call the Connection Change Event
        void onConnectionChange()
        {
            var handler = ClientConnectionChange;
            if (handler != null)
                handler(this, new DynamicTCPSocketStatusChangeEventArgs(Server, false));
        }

        //Private Helper Method to call the Text Received Event
        void onTextReceived(string text)
        {
            var handler = TextReceived;
            if (handler != null)
                handler(this, new GenericCommMethodReceiveTextArgs(text));
        }

        //Private Helper Method to call the Server State Change Event
        void onServerStateChange()
        {
            var handler = ServerStateChange;
            if (handler != null)
                handler(this, new DynamicTCPServerStateChangedEventArgs(Server, false));
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