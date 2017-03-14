using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash_Core
{
    public class DynamicTCPServer
    {
        public bool Secure { get; set; }
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


    }
}