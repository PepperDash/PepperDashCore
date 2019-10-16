using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core.PasswordManager
{
	// Example JSON password configuration object
	//{
	//    "global":{
	//        "passwords":[
	//            {
	//                "key": "Password01",
	//                "name": "Technician Password",
	//                "enable": true,
	//                "password": "1988"
	//            }
	//        ]
	//    }
	//}

	/// <summary>
	/// Passwrod manager JSON configuration
	/// </summary>
	public class PasswordConfig
	{
		/// <summary>
		/// Key used to search for object in JSON array
		/// </summary>
		public string key { get; set; }
		/// <summary>
		/// Friendly name of password object
		/// </summary>
		public string name { get; set; }
		/// <summary>
		/// Password object enabled
		/// </summary>
		public bool enable { get; set; }
		/// <summary>
		/// Password object configured password
		/// </summary>
		public string password { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public PasswordConfig()
		{

		}
	}

	/// <summary>
	/// Global JSON object
	/// </summary>
	public class GlobalConfig
	{
		public List<PasswordConfig> passwords { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public GlobalConfig()
		{
			
		}
	}

	/// <summary>
	/// Root JSON object
	/// </summary>
	public class RootObject
	{
		public GlobalConfig global { get; set; }
	}
}