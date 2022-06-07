using System;
//using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PepperDash.Core.JsonToSimpl
{
    /// <summary>
    /// 
    /// </summary>
	public class JsonToSimplFixedPathObject : JsonToSimplChildObjectBase
	{
        /// <summary>
        /// Constructor
        /// </summary>
		public JsonToSimplFixedPathObject()
		{
			this.LinkedToObject = true;
		}
	}
}