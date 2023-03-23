namespace PepperDash.Core.Net.Interfaces
{
    /// <summary>
    /// This delegate defines handler for IBasicCommunication status changes
    /// </summary>
    /// <param name="comm">Device firing the status change</param>
    /// <param name="status"></param>
    public delegate void GenericCommMethodStatusHandler(IBasicCommunication comm, eGenericCommMethodStatusChangeType status);    

    /// <summary>
    /// Represents a device that uses basic connection
    /// </summary>
    public interface IBasicCommunication : ICommunicationReceiver
    {
        /// <summary>
        /// Send text to the device
        /// </summary>
        /// <param name="text"></param>
		void SendText(string text);

        /// <summary>
        /// Send bytes to the device
        /// </summary>
        /// <param name="bytes"></param>
		void SendBytes(byte[] bytes);
    }
}