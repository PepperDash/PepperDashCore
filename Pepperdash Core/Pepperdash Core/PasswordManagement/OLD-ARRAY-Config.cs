using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core.PasswordManagement
{
	// Example JSON password array configuration object
	//{
	//    "global":{
	//        "passwords":[
	//            {	
	//                "key": "Password01",
	//                "name": "Technician Password",
	//                "enabled": true,
	//                "password": "1988"
	//            }
	//        ]
	//    }
	//}

	/// <summary>
	/// JSON password array configuration object
	/// </summary>
	//public class PasswordConfig
	//{
	//    /// <summary>
	//    /// Key used to search for object in JSON array
	//    /// </summary>
	//    public string key { get; set; }
	//    /// <summary>
	//    /// Friendly name of password object
	//    /// </summary>
	//    public string name { get; set; }
	//    /// <summary>
	//    /// Password object enabled
	//    /// </summary>
	//    public bool enabled { get; set; }
	//    /// <summary>
	//    /// 
	//    /// </summary>
	//    public ushort simplEnabled
	//    {
	//        get { return (ushort)(enabled ? 1 : 0); }
	//        set { enabled = Convert.ToBoolean(value); }
	//    }
	//    /// <summary>
	//    /// Password object configured password
	//    /// </summary>
	//    public string password { get; set; }
	//    /// <summary>
	//    /// Password type
	//    /// </summary>
	//    private int type { get; set; }
	//    /// <summary>
	//    /// Password Type for S+
	//    /// </summary>
	//    public ushort simplType
	//    {
	//        get { return Convert.ToUInt16(type); }
	//        set { type = value; }
	//    }
	//    /// <summary>
	//    /// Password path
	//    /// **FUTURE** implementation of saving passwords recieved from Fusion or other external sources back to config
	//    /// </summary>
	//    public string path { get; set; }
	//    /// <summary>
	//    /// Constructor
	//    /// </summary>
	//    public PasswordConfig()
	//    {
	//        simplEnabled = 0;
	//        simplType = 0;
	//    }
	//}

	// Example JSON password collections configuration object
	//{
	//    "global": {
	//        "passwords": {
	//            "1": {
	//                "name": "Technician Password",
	//                "password": "2468"
	//            },
	//            "2": {
	//                "name": "System Password",
	//                "password": "123456"
	//            },
	//            "3": {
	//                "name": "Master Password",
	//                "password": "abc123"
	//            },
	//            "5": {
	//                "name": "Backdoor Password",
	//                "password": "1988"
	//            },
	//            "10": {
	//                "name": "Backdoor Password",
	//                "password": "1988"
	//            }
	//        }
	//    }
	//}

	/// <summary>
	/// JSON password array configuration object
	/// </summary>
	public class PasswordConfig
	{
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
	//public class GlobalConfig
	//{
	//    //public List<PasswordConfig> passwords { get; set; }
	//    public Dictionary<uint, PasswordConfig> passwords { get; set; }

	//    /// <summary>
	//    /// Constructor
	//    /// </summary>
	//    public GlobalConfig()
	//    {

	//    }
	//}

	/// <summary>
	/// Root JSON object
	/// </summary>
	//public class RootObject
	//{
	//    public GlobalConfig global { get; set; }
	//}
}