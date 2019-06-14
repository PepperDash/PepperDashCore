using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core;
using Newtonsoft.Json;

namespace PepperDash_Core.Comm
{
    public class TcpClientConfigObject : TcpSshPropertiesConfig
    {
        public bool Secure { get; set; }
        public bool SharedKeyRequired { get; set; }
        public string SharedKey { get; set; }
        public bool HeartbeatRequired { get; set; }
        public ushort HeartbeatRequiredIntervalInSeconds { get; set; }
        public string HeartbeatStringToMatch { get; set; }
        public int ReceiveQueueSize { get; set; }
    }
}