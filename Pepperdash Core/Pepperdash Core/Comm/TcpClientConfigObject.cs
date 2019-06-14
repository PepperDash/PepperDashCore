using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using Newtonsoft.Json;

namespace PepperDash.Core
{
    /// <summary>
    /// Client config object for TCP client with server that inherits from TcpSshPropertiesConfig and adds properties for shared key and heartbeat
    /// </summary>
    public class TcpClientConfigObject : TcpSshPropertiesConfig
    {
        /// <summary>
        /// Bool value for secure. Currently not implemented in TCP sockets as they are not dynamic
        /// </summary>
        public bool Secure { get; set; }
        /// <summary>
        /// Require a shared key that both server and client negotiate. If negotiation fails server disconnects the client
        /// </summary>
        public bool SharedKeyRequired { get; set; }

        /// <summary>
        /// The shared key that must match on the server and client
        /// </summary>
        public string SharedKey { get; set; }
        /// <summary>
        /// Require a heartbeat on the client/server connection that will cause the server/client to disconnect if the heartbeat is not received. 
        /// heartbeats do not raise received events. 
        /// </summary>
        public bool HeartbeatRequired { get; set; }
        /// <summary>
        /// The interval in seconds for the heartbeat from the client. If not received client is disconnected
        /// </summary>
        public ushort HeartbeatRequiredIntervalInSeconds { get; set; }
        /// <summary>
        /// HeartbeatString that will be checked against the message received. defaults to heartbeat if no string is provided. 
        /// </summary>
        public string HeartbeatStringToMatch { get; set; }
        /// <summary>
        /// Receive Queue size must be greater than 20 or defaults to 20
        /// </summary>
        public int ReceiveQueueSize { get; set; }
    }
}