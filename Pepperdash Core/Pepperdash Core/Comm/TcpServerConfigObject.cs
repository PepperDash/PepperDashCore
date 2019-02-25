using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core
{
    public class TcpServerConfigObject
    {
        public string Key { get; set; }
        public bool Secure { get; set; }
        public ushort MaxClients { get; set; }
        public int Port { get; set; }
        public bool SharedKeyRequired { get; set; }
        public string SharedKey { get; set; }
        public bool HeartbeatRequired { get; set; }
        public ushort HeartbeatRequiredIntervalInSeconds { get; set; }
        public string HeartbeatStringToMatch { get; set; }
        public int BufferSize { get; set; }
    }
}