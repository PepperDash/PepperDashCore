using PepperDash.Core.Comm;

namespace PepperDash.Core.Net.Interfaces
{
    /// <summary>
    /// Represents a device with stream debugging capablities
    /// </summary>
    public interface IStreamDebugging
    {
        /// <summary>
        /// Object to enable stream debugging
        /// </summary>
        CommunicationStreamDebugging StreamDebugging { get; }
    }
}