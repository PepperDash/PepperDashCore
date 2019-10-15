using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core.SystemInfo
{
	/// <summary>
	/// Constants 
	/// </summary>
	public class SystemInfoConstants
	{
		public const ushort BoolValueChange = 1;
		public const ushort CompleteBoolChange = 2;
		public const ushort BusyBoolChange = 3;

		public const ushort UshortValueChange = 101;

		public const ushort StringValueChange = 201;
		public const ushort ConsoleResponseChange = 202;
		public const ushort ProcessorUptimeChange = 203;
		public const ushort ProgramUptimeChange = 204;

		public const ushort ObjectChange = 301;
		public const ushort ProcessorConfigChange = 302;
		public const ushort EthernetConfigChange = 303;
		public const ushort ControlSubnetConfigChange = 304;
		public const ushort ProgramConfigChange = 305;
	}

	/// <summary>
	/// Processor Change Event Args Class
	/// </summary>
	public class ProcessorChangeEventArgs : EventArgs
	{
		public ProcessorInfo Processor { get; set; }
		public ushort Type { get; set; }
		public ushort Index { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public ProcessorChangeEventArgs()
		{

		}

		/// <summary>
		/// Constructor overload
		/// </summary>
		public ProcessorChangeEventArgs(ProcessorInfo processor, ushort type)
		{
			Processor = processor;
			Type = type;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public ProcessorChangeEventArgs(ProcessorInfo processor, ushort type, ushort index)
		{
			Processor = processor;
			Type = type;
			Index = index;
		}
	}

	/// <summary>
	/// Ethernet Change Event Args Class
	/// </summary>
	public class EthernetChangeEventArgs : EventArgs
	{
		public EthernetInfo Adapter { get; set; }
		public ushort Type { get; set; }
		public ushort Index { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public EthernetChangeEventArgs()
		{

		}

		/// <summary>
		/// Constructor overload
		/// </summary>
		/// <param name="Ethernet"></param>
		/// <param name="type"></param>
		public EthernetChangeEventArgs(EthernetInfo ethernet, ushort type)
		{
			Adapter = ethernet;
			Type = type;
		}

		/// <summary>
		/// Constructor overload
		/// </summary>
		/// <param name="Ethernet"></param>
		/// <param name="type"></param>
		public EthernetChangeEventArgs(EthernetInfo ethernet, ushort type, ushort index)
		{
			Adapter = ethernet;
			Type = type;
			Index = index;
		}
	}

	/// <summary>
	/// Control Subnet Chage Event Args Class
	/// </summary>
	public class ControlSubnetChangeEventArgs : EventArgs
	{
		public ControlSubnetInfo Adapter { get; set; }
		public ushort Type { get; set; }
		public ushort Index { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public ControlSubnetChangeEventArgs()
		{

		}

		/// <summary>
		/// Constructor overload
		/// </summary>
		public ControlSubnetChangeEventArgs(ControlSubnetInfo controlSubnet, ushort type)
		{
			Adapter = controlSubnet;
			Type = type;
		}

		/// <summary>
		/// Constructor overload
		/// </summary>
		public ControlSubnetChangeEventArgs(ControlSubnetInfo controlSubnet, ushort type, ushort index)
		{
			Adapter = controlSubnet;
			Type = type;
			Index = index;
		}
	}

	/// <summary>
	/// Program Change Event Args Class
	/// </summary>
	public class ProgramChangeEventArgs : EventArgs
	{
		public ProgramInfo Program { get; set; }
		public ushort Type { get; set; }
		public ushort Index { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public ProgramChangeEventArgs()
		{

		}

		/// <summary>
		/// Constructor overload
		/// </summary>
		/// <param name="Program"></param>
		/// <param name="type"></param>
		public ProgramChangeEventArgs(ProgramInfo program, ushort type)
		{
			Program = program;
			Type = type;
		}

		/// <summary>
		/// Constructor overload
		/// </summary>
		/// <param name="Program"></param>
		/// <param name="type"></param>
		public ProgramChangeEventArgs(ProgramInfo program, ushort type, ushort index)
		{
			Program = program;
			Type = type;
			Index = index;
		}
	}
}