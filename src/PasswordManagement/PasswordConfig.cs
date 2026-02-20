using System.Collections.Generic;
using Newtonsoft.Json;

namespace PepperDash.Core.PasswordManagement
{
	/// <summary>
	/// JSON password configuration 
	/// </summary>
	public class PasswordConfig
	{
	    /// <summary>
		/// Dictionary of user passwords. Key is the username, value is the password
		/// </summary>
		[JsonProperty("passwords")]
		public Dictionary<string, string> Passwords { get; set; }

	    /// <summary>
	    /// Constructor
	    /// </summary>
	    public PasswordConfig()
	    {	        
	    }
	}
}