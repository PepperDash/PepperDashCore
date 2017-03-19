/*PepperDash Technology Corp.
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
    #region GenericSocketStatusChangeEventArgs
    public delegate void GenericSocketStatusChangeEventDelegate(ISocketStatus client);

	public class GenericSocketStatusChageEventArgs : EventArgs
	{
		public ISocketStatus Client { get; private set; }

		public GenericSocketStatusChageEventArgs() { }

		public GenericSocketStatusChageEventArgs(ISocketStatus client)
		{
			Client = client;
		}
    }
    #endregion

    #region DynamicTCPServerStateChangedEventArgs
    public delegate void DynamicTCPServerStateChangedEventDelegate(object server);

    public class DynamicTCPServerStateChangedEventArgs : EventArgs
    {
        public bool Secure { get; private set; }
        public object Server { get; private set; }

        public DynamicTCPServerStateChangedEventArgs() { }

        public DynamicTCPServerStateChangedEventArgs(object server, bool secure)
        {
            Secure = secure;
            Server = server;
        }
    }
    #endregion

    #region DynamicTCPSocketStatusChangeEventDelegate
    public delegate void DynamicTCPSocketStatusChangeEventDelegate(object server);

    public class DynamicTCPSocketStatusChangeEventArgs : EventArgs
    {
        public bool Secure { get; private set; }
        public object Server { get; private set; }

        public DynamicTCPSocketStatusChangeEventArgs() { }

        public DynamicTCPSocketStatusChangeEventArgs(object server, bool secure)
        {
            Secure = secure;
            Server = server;
        }
    }
    #endregion


   

}