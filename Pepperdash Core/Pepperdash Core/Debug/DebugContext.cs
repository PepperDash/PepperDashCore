using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash_Core.Debug
{
	public class DebugContext
	{
		public string Name { get; private set; }

		public int Level { get; private set; }

		public List<string> Keys { get; private set; }



		public DebugContext(string name)
		{

		}


	}
}