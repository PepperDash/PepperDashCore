using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core.JsonToSimpl
{
	/// <summary>
	/// Constants for Simpl modules
	/// </summary>
	public class JsonToSimplConstants
	{
		public const ushort BoolValueChange = 1;
		public const ushort JsonIsValidBoolChange = 2;

		public const ushort UshortValueChange = 101;
		
		public const ushort StringValueChange = 201;
		public const ushort FullPathToArrayChange = 202;
		public const ushort ActualFilePathChange = 203;

		// TODO: pdc-20: Added below constants for passing file path and filename back to S+
		public const ushort FilenameResolvedChange = 204;
		public const ushort FilePathResolvedChange = 205;
	}

	/// <summary>
	/// S+ values delegate
	/// </summary>
	public delegate void SPlusValuesDelegate();

	/// <summary>
	/// S+ values wrapper
	/// </summary>
	public class SPlusValueWrapper
	{
		public SPlusType ValueType { get; private set; }
		public ushort Index { get; private set; }
		public ushort BoolUShortValue { get; set; }
		public string StringValue { get; set; }

		public SPlusValueWrapper() { }

		public SPlusValueWrapper(SPlusType type, ushort index)
		{
			ValueType = type;
			Index = index;
		}
	}

	/// <summary>
	/// S+ types enum
	/// </summary>
	public enum SPlusType
	{
		Digital, Analog, String
	}
}