using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core.SystemInfo
{
	/// <summary>
	/// Processor info class
	/// </summary>
	public class ProcessorInfo
	{
		public string Model { get; set; }
		public string SerialNumber { get; set; }
		public string Firmware { get; set; }
		public string FirmwareDate { get; set; }
		public string OsVersion { get; set; }
		public string RuntimeEnvironment { get; set; }
		public string DevicePlatform { get; set; }
		public string ModuleDirectory { get; set; }
		public string LocalTimeZone { get; set; }		
		public string ProgramIdTag { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public ProcessorInfo()
		{
			
		}
	}

	/// <summary>
	/// Ethernet info class
	/// </summary>
	public class EthernetInfo
	{
		public ushort DhcpIsOn { get; set; }
		public string Hostname { get; set; }
		public string MacAddress { get; set; }
		public string IpAddress { get; set; }
		public string Subnet { get; set; }
		public string Gateway { get; set; }
		public string Dns1 { get; set; }
		public string Dns2 { get; set; }
		public string Dns3 { get; set; }
		public string Domain { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public EthernetInfo()
		{
			
		}
	}

	/// <summary>
	/// Control subnet info class
	/// </summary>
	public class ControlSubnetInfo
	{
		public ushort Enabled { get; set; }
		public ushort IsInAutomaticMode { get; set; }
		public string MacAddress { get; set; }
		public string IpAddress { get; set; }
		public string Subnet { get; set; }
		public string RouterPrefix { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public ControlSubnetInfo()
		{
		
		}
	}

	/// <summary>
	/// Program info class
	/// </summary>
	public class ProgramInfo
	{
		public string Name { get; set; }
		public string Header { get; set; }
		public string System { get; set; }
		public string ProgramIdTag { get; set; }
		public string CompileTime { get; set; }
		public string Database { get; set; }
		public string Environment { get; set; }
		public string Programmer { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public ProgramInfo()
		{
			
		}
	}
}