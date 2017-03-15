using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using PepperDash.Core;

namespace PepperDash_Core
{
    #region UNUSED OBJECT JUST IN CASE TILL DONE WITH CODING
    //public class DynamicServer
    //{
    //    public object Server 
    //    { 
    //        get
    //        {
    //            if(Secure)
    //                return secureServer;
    //            else
    //                return unsecureServer;
    //        }
    //        private set; 
    //    }
    //    public bool Secure { get; set; }
    //    private TCPServer unsecureServer;
    //    private SecureTCPServer secureServer;
    //    public DynamicServer(bool secure)
    //    {
    //        Secure = secure;  
    //    }
    //}
    #endregion

    public class DynamicTCPServer : Device
    {
        public event EventHandler<GenericCommMethodReceiveTextArgs> TextReceived;

        public event EventHandler<DynamicTCPServerSocketStatusChangeEventArgs> ClientConnectionChange;

        public event EventHandler<DynamicTCPServerStateChangedEventArgs> ServerStateChange;

        /// <summary>
        /// Secure or unsecure TCP server. Defaults to Unsecure or standard TCP server without SSL
        /// </summary>
        public bool Secure { get; set; }

        /// <summary>
        /// S+ Helper for Secure bool
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

        /// <summary>
        /// Bool showing if socket is connected
        /// </summary>
        public bool IsConnected
        {
            get 
            {
                if (Secure && SecureServer != null)
                    return SecureServer.State == ServerState.SERVER_CONNECTED;
                else if (!Secure && UnsecureServer != null)
                    return UnsecureServer.State == ServerState.SERVER_CONNECTED;
                else
                    return false;
            }
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
            get
            {
                if (Secure && SecureServer != null)
                    return SecureServer.State == ServerState.SERVER_LISTENING;
                else if (!Secure && UnsecureServer != null)
                    return UnsecureServer.State == ServerState.SERVER_LISTENING;
                else
                    return false;
            }
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
        public ushort NumberOfClientsConnected { get; set; }

        /// <summary>
        /// Port on server
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// S+ helper
        /// </summary>
        public ushort UPort
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
                
                _SharedKey = value;
            }
        }
        private string _SharedKey;

        /// <summary>
        /// flags to show the server is waiting for client at index to send the shared key
        /// </summary>
        public List<uint> WaitingForSharedKey = new List<uint>();


        /// <summary>
        /// Defaults to 2000
        /// </summary>
        public int BufferSize { get; set; }

        public string OnlyAcceptConnectionFromAddress { get; set; }

        public SecureTCPServer SecureServer;
        public TCPServer UnsecureServer;


        //base class constructor
        public DynamicTCPServer()
			: base("Uninitialized Dynamic TCP Server")
		{
			CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(CrestronEnvironment_ProgramStatusEventHandler);
            BufferSize = 2000;
            Secure = false; 
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
                SecureServer.WaitForConnectionAsync(IPAddress.Any, SecureConnectCallback);
                Debug.Console(0, "Secure Server Status: {0}, Socket Status: {1}\r\n", SecureServer.State.ToString(), SecureServer.ServerSocketStatus);
            }
            else
            {
                if (UnsecureServer.State == ServerState.SERVER_LISTENING)
                    return;
                UnsecureServer = new TCPServer(Port, MaxConnections);
                UnsecureServer.SocketStatusChange += new TCPServerSocketStatusChangeEventHandler(UnsecureServer_SocketStatusChange);
                UnsecureServer.WaitForConnectionAsync(IPAddress.Any, UnsecureConnectCallback);
                Debug.Console(0, "Unsecure Server Status: {0}, Socket Status: {1}\r\n", UnsecureServer.State.ToString(), UnsecureServer.ServerSocketStatus);
            }
        }

        public void StopListening()
        {
            if (SecureServer != null && SecureServer.State == ServerState.SERVER_LISTENING)
                SecureServer.Stop();
            if (UnsecureServer != null && UnsecureServer.State == ServerState.SERVER_LISTENING)
                UnsecureServer.Stop();
            var handler = ServerStateChange;
            if (ServerStateChange != null)
                ServerStateChange(this, new DynamicTCPServerStateChangedEventArgs(this, Secure));
        }

        public void DisconnectAllClients()
        {
            if (SecureServer != null && SecureServer.NumberOfClientsConnected > 0)
                SecureServer.DisconnectAll();
            if (UnsecureServer != null && UnsecureServer.NumberOfClientsConnected > 0)
                UnsecureServer.DisconnectAll();
        }

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

        void SecureServer_SocketStatusChange(SecureTCPServer mySecureTCPServer, uint clientIndex, SocketStatus serverSocketStatus)
        {
            onConnectionChange();//Go to simpl and send the server and whether it is secure or not. Could check secure from Simpl+ but better to use the passed
            //variable in case we need to use this in Pro. Simpl+ won't use arguments, will just check the uIsConnected Property. Currently Simpl+ is just going to report
            //connected not reporting by client
        }

        void UnsecureServer_SocketStatusChange(TCPServer mySecureTCPServer, uint clientIndex, SocketStatus serverSocketStatus)
        {
            onConnectionChange();
        }

        void SecureConnectCallback(SecureTCPServer mySecureTCPServer, uint clientIndex)
        {
            
        }

        void UnsecureConnectCallback(TCPServer myTCPServer, uint clientIndex)
        {

        }
    }
}