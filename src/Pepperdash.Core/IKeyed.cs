namespace PepperDash.Core
{
    /// <summary>
    /// Unique key interface to require a unique key for the class
    /// </summary>
    public interface IKeyed
	{
        /// <summary>
        /// Unique Key
        /// </summary>
		string Key { get; }
    }

}