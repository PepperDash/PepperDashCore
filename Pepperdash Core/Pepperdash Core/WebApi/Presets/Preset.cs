using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

using Newtonsoft.Json;


namespace PepperDash.Core.WebApi.Presets
{
	public class Preset
	{
		public int Id { get; set; }

		public int UserId { get; set; }

		public int RoomTypeId { get; set; }

		public string PresetName { get; set; }

		public int PresetNumber { get; set; }

		public string Data { get; set; }

		public Preset()
		{
			PresetName = "";
			PresetNumber = 1;
			Data = "{}";
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class PresetReceivedEventArgs : EventArgs
	{
		public Preset Preset { get; private set; }

		/// <summary>
		/// For Simpl+
		/// </summary>
		public PresetReceivedEventArgs() { }

		public PresetReceivedEventArgs(Preset preset)
		{
			Preset = preset;
		}
	}
}