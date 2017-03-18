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

    //#region DynamicTCPServerStateChangedEventArgs
    //public delegate void DynamicTCPServerStateChangedEventDelegate(object server);

    //public class DynamicTCPServerStateChangedEventArgs : EventArgs
    //{
    //    public bool Secure { get; private set; }
    //    public object Server { get; private set; }

    //    public DynamicTCPServerStateChangedEventArgs() { }

    //    public DynamicTCPServerStateChangedEventArgs(object server, bool secure)
    //    {
    //        Secure = secure;
    //        Server = server;
    //    }
    //}
    //#endregion

    //#region DynamicTCPServerSocketStatusChangeEventArgs
    //public delegate void DynamicTCPServerSocketStatusChangeEventDelegate(object server);

    //public class DynamicTCPServerSocketStatusChangeEventArgs : EventArgs
    //{        
    //    public bool Secure { get; private set; }
    //    public object Server { get; private set; }

    //    public DynamicTCPServerSocketStatusChangeEventArgs() { }

    //    public DynamicTCPServerSocketStatusChangeEventArgs(object server, bool secure)
    //    {
    //        Secure = secure;
    //        Server = server;
    //    }
    //}
    //#endregion

}