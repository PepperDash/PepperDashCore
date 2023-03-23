using PepperDash.Core.Interfaces;
using System;

namespace PepperDash.Core.Net.Interfaces
{
    /// <summary>
    /// An incoming communication stream
    /// </summary>
    public interface ICommunicationReceiver : IKeyed
    {
        /// <summary>
        /// Notifies of bytes received
        /// </summary>
        event EventHandler<GenericCommMethodReceiveBytesArgs> BytesReceived;
        /// <summary>
        /// Notifies of text received
        /// </summary>
        event EventHandler<GenericCommMethodReceiveTextArgs> TextReceived;

        /// <summary>
        /// Indicates connection status
        /// </summary>
        bool IsConnected { get; }
        /// <summary>
        /// Connect to the device
        /// </summary>
        void Connect();
        /// <summary>
        /// Disconnect from the device
        /// </summary>
        void Disconnect();
    }
}