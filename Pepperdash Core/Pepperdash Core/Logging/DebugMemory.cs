using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PepperDash.Core.DebugThings
{
	public class DebugContextCollection
	{
        /// <summary>
        /// To prevent threading issues with the DeviceDebugSettings collection
        /// </summary>
        private CCriticalSection DeviceDebugSettingsLock;

		[JsonProperty("items")]
		Dictionary<string, DebugContextItem> Items;

        /// <summary>
        /// Collection of the debug settings for each device where the dictionary key is the device key
        /// </summary>
        [JsonProperty("deviceDebugSettings")]
        private Dictionary<string, object> DeviceDebugSettings { get; set; }


		public DebugContextCollection()
		{
            DeviceDebugSettingsLock = new CCriticalSection();
            DeviceDebugSettings = new Dictionary<string, object>();
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


        /// <summary>
        /// sets the settings for a device or creates a new entry
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public void SetDebugSettingsForKey(string deviceKey, object settings)
        {
            var existingSettings = DeviceDebugSettings[deviceKey];

            if (existingSettings != null)
            {
                existingSettings = settings;
            }
            else
                DeviceDebugSettings.Add(deviceKey, settings);
        }

        /// <summary>
        /// Gets the device settings for a device by key or returns null
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <returns></returns>
        public object GetDebugSettingsForKey(string deviceKey)
        {
            return DeviceDebugSettings[deviceKey];
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