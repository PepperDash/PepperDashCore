using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

using Newtonsoft.Json;

namespace PepperDash.Core
{
    /// <summary>
    /// Configuration properties for IP Connections
    /// </summary>    
    public class TcpSshPropertiesConfig : PropertiesConfigBase
    {
        /// <summary>
        /// Address to connect to
        /// </summary>
        [JsonProperty("address", Required = Required.Always)]
        public string Address { get; set; }

        /// <summary>
        /// Port to connect to
        /// </summary>
        [JsonProperty("port", Required = Required.Always)]
        public int Port { get; set; }

        /// <summary>
        /// Username credential.  Defaults to ""
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// Passord credential. Defaults to ""
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Buffer size. Defaults to 32768
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// Indicates if automatic reconnection attemtps should be made.Defaults to true.
        /// Uses AutoReconnectIntervalMs to determine frequency of attempts.
        /// </summary>
        public bool AutoReconnect { get; set; }

        /// <summary>
        /// Defaults to 5000ms.  Requires AutoReconnect to be true.
        /// </summary>
        public int AutoReconnectIntervalMs { get; set; }

        /// <summary>
        /// Construtor
        /// </summary>
        public TcpSshPropertiesConfig()
        {
            BufferSize = 32768;
            AutoReconnect = true;
            AutoReconnectIntervalMs = 5000;
            Username = "";
            Password = "";
        }

    }
}