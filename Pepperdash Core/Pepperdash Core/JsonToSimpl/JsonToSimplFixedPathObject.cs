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

	public class JsonToSimplFixedPathObject : JsonToSimplChildObjectBase
	{
		public JsonToSimplFixedPathObject()
		{
			this.LinkedToObject = true;
		}
	}
}