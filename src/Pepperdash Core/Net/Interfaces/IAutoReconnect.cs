namespace PepperDash.Core.Net.Interfaces
{
    /// <summary>
    /// Describes a device that can automatically attempt to reconnect
    /// </summary>
    public interface IAutoReconnect
    {
        /// <summary>
        /// Enable automatic recconnect
        /// </summary>
		bool AutoReconnect { get; set; }
        /// <summary>
        /// Interval in ms to attempt automatic recconnections
        /// </summary>
		int AutoReconnectIntervalMs { get; set; }
    }
}