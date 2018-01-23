using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core
{
    public enum eControlMethod
    {
        None = 0, Com, IpId, IR, Ssh, Tcpip, Telnet, Cresnet
    }
}