using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

using Newtonsoft.Json;

namespace PepperDash.Core.DebugThings
{
	public class DebugContextCollection
	{
		[JsonProperty("items")]
		Dictionary<string, DebugContextItem> Items;

		public DebugContextCollection()
		{
			Items = new Dictionary<string, DebugContextItem>();
		}

		/// <summary>
		/// Sets the level of a given context item, and adds that item if it does not 
		/// exist
		/// </summary>
		/// <param name="contextKey"></param>
		/// <param name="level"></param>
		/// <returns>True if the 0 <= level <= 2 and the conte </returns>
		public void SetLevel(string contextKey, int level)
		{
			if (level < 0 || level > 2)
				return;
			GetOrCreateItem(contextKey).Level = level;
		}

		/// <summary>
		/// Gets a level or creates it if not existing
		/// </summary>
		/// <param name="contextKey"></param>
		/// <returns></returns>
		public DebugContextItem GetOrCreateItem(string contextKey)
		{
			if (!Items.ContainsKey(contextKey))
				Items[contextKey] = new DebugContextItem(this) { Level = 0 };
			return Items[contextKey];
		}
	}

	public class DebugContextItem
	{
        /// <summary>
        /// The level of debug messages to print
        /// </summary>
		[JsonProperty("level")]
		public int Level { get; set; }

        /// <summary>
        /// Property to tell the program not to intitialize when it boots, if desired
        /// </summary>
        [JsonProperty("doNotLoadOnNextBoot")]
        public bool DoNotLoadOnNextBoot { get; set; }


		public DebugContextItem(DebugContextCollection parent)
		{

		}
	}
}