using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core.JsonStandardObjects
{
	#region Constants

	/// <summary>
	/// Constants for simpl modules
	/// </summary>
	public class JsonStandardDeviceConstants
	{
		public const ushort JsonObjectEvaluated = 2;

		public const ushort JsonObjectChanged = 104;
	}

	#endregion Constants


	#region ObjectChangeEventArgs

	/// <summary>
	/// 
	/// </summary>
	public class ObjectChangeEventArgs : EventArgs
	{
		public DeviceConfig Device { get; set; }
		public ushort Type { get; set; }
		public ushort Index { get; set; }

		/// <summary>
		/// Default constructor
		/// </summary>
		public ObjectChangeEventArgs()
		{

		}

		/// <summary>
		/// Constructor overload
		/// </summary>
		/// <param name="device"></param>
		/// <param name="type"></param>
		public ObjectChangeEventArgs(DeviceConfig device, ushort type)
		{
			Device = device;
			Type = type;
		}

		/// <summary>
		/// Constructor overload
		/// </summary>
		/// <param name="device"></param>
		/// <param name="type"></param>
		/// <param name="index"></param>
		public ObjectChangeEventArgs(DeviceConfig device, ushort type, ushort index)
		{
			Device = device;
			Type = type;
			Index = index;
		}
	}

	#endregion ObjectChangeEventArgs
}