
using Crestron.SimplSharp.CrestronSockets;
using PepperDash.Core.Comm;
using System;

namespace PepperDash.Core.Net.Interfaces
{
    /// <summary>
    /// For IBasicCommunication classes that have SocketStatus. GenericSshClient,
    /// GenericTcpIpClient
    /// </summary>
    public interface ISocketStatus : IBasicCommunication
    {
        /// <summary>
        /// Notifies of socket status changes
        /// </summary>
		event EventHandler<GenericSocketStatusChageEventArgs> ConnectionChange;

        /// <summary>
        /// The current socket status of the client
        /// </summary>
		SocketStatus ClientStatus { get; }
    }
}