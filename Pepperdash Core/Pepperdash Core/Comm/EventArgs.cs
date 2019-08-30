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
    public delegate void GenericSocketStatusChangeEventDelegate(ISocketStatus client);
	public class GenericSocketStatusChageEventArgs : EventArgs
	{
		public ISocketStatus Client { get; private set; }

		public GenericSocketStatusChageEventArgs(ISocketStatus client)
		{
			Client = client;
		}
		/// <summary>
		/// Stupid S+ Constructor
		/// </summary>
		public GenericSocketStatusChageEventArgs() { }
    }

    public delegate void GenericTcpServerStateChangedEventDelegate(ServerState state);
    public class GenericTcpServerStateChangedEventArgs : EventArgs
    {
        public ServerState State { get; private set; }

        public GenericTcpServerStateChangedEventArgs(ServerState state)
        {
            State = state;
        }
		/// <summary>
		/// Stupid S+ Constructor
		/// </summary>
		public GenericTcpServerStateChangedEventArgs() { }
    }

    public delegate void GenericTcpServerSocketStatusChangeEventDelegate(object socket, uint clientIndex, SocketStatus clientStatus);
    public class GenericTcpServerSocketStatusChangeEventArgs : EventArgs
    {
        public object Socket { get; private set; }
        public uint ReceivedFromClientIndex { get; private set; }
        public SocketStatus ClientStatus { get; set; }

        public GenericTcpServerSocketStatusChangeEventArgs(object socket, SocketStatus clientStatus)
        {
            Socket = socket;
            ClientStatus = clientStatus;
        }

        public GenericTcpServerSocketStatusChangeEventArgs(object socket, uint clientIndex, SocketStatus clientStatus)
        {
            Socket = socket;
            ReceivedFromClientIndex = clientIndex;
            ClientStatus = clientStatus;
        }
		/// <summary>
		/// Stupid S+ Constructor
		/// </summary>
		public GenericTcpServerSocketStatusChangeEventArgs() { }
    }

    public class GenericTcpServerCommMethodReceiveTextArgs : EventArgs
    {
        public uint ReceivedFromClientIndex { get; private set; }
        public string Text { get; private set; }

        public GenericTcpServerCommMethodReceiveTextArgs(string text)
        {
            Text = text;
        }

        public GenericTcpServerCommMethodReceiveTextArgs(string text, uint clientIndex)
        {
            Text = text;
            ReceivedFromClientIndex = clientIndex;
        }
		/// <summary>
		/// Stupid S+ Constructor
		/// </summary>
		public GenericTcpServerCommMethodReceiveTextArgs() { }
    }

    public class GenericTcpServerClientReadyForcommunicationsEventArgs : EventArgs
    {
        public bool IsReady;
        public GenericTcpServerClientReadyForcommunicationsEventArgs(bool isReady)
        {
            IsReady = isReady;
        }
		/// <summary>
		/// Stupid S+ Constructor
		/// </summary>
		public GenericTcpServerClientReadyForcommunicationsEventArgs() { }
    }

    public class GenericUdpConnectedEventArgs : EventArgs
    {
        public ushort UConnected;
        public bool Connected;

        public GenericUdpConnectedEventArgs() { }

        public GenericUdpConnectedEventArgs(ushort uconnected)
        {
            UConnected = uconnected;
        }

        public GenericUdpConnectedEventArgs(bool connected)
        {
            Connected = connected;
        }

    }

   

}