using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core.SystemInfo
{
	/// <summary>
	/// Processor properties class
	/// </summary>
	public class ProcessorConfig
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
		public ProcessorConfig()
		{
			Model = "";
			SerialNumber = "";
			Firmware = "";
			FirmwareDate = "";
			OsVersion = "";
			RuntimeEnvironment = "";
			DevicePlatform = "";
			ModuleDirectory = "";
			LocalTimeZone = "";
			ProgramIdTag = "";
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class ProcessorInfo
	{
		public ProcessorConfig properties { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public ProcessorInfo()
		{
			properties.Model = "";
			properties.SerialNumber = "";
			properties.Firmware = "";
			properties.FirmwareDate = "";
			properties.OsVersion = "";
			properties.RuntimeEnvironment = "";
			properties.DevicePlatform = "";
			properties.ModuleDirectory = "";
			properties.LocalTimeZone = "";
			properties.ProgramIdTag = "";
		}

		/// <summary>
		/// Get processor info method
		/// </summary>
		/// <returns>bool</returns>
		public bool GetInfo()
		{
			var console = new ConsoleHelper();

			try
			{
				properties.Model = InitialParametersClass.ControllerPromptName;
				properties.SerialNumber = CrestronEnvironment.SystemInfo.SerialNumber;
				properties.ModuleDirectory = InitialParametersClass.ProgramDirectory.ToString();
				properties.ProgramIdTag = InitialParametersClass.ProgramIDTag;
				properties.DevicePlatform = CrestronEnvironment.DevicePlatform.ToString();
				properties.OsVersion = CrestronEnvironment.OSVersion.Version.ToString();
				properties.RuntimeEnvironment = CrestronEnvironment.RuntimeEnvironment.ToString();
				properties.LocalTimeZone = CrestronEnvironment.GetTimeZone().Offset;

				// Does not return firmware version matching a "ver" command
				// returns the "ver -v" 'CAB' version
				// example return ver -v: 
				//		RMC3 Cntrl Eng [v1.503.3568.25373 (Oct 09 2018), #4001E302] @E-00107f4420f0
				//		Build: 14:05:46  Oct 09 2018 (3568.25373)
				//		Cab: 1.503.0070
				//		Applications:  1.0.6855.21351
				//		Updater: 1.4.24
				//		Bootloader: 1.22.00
				//		RMC3-SetupProgram: 1.003.0011
				//		IOPVersion: FPGA [v09] slot:7
				//		PUF: Unknown
				//Firmware = CrestronEnvironment.OSVersion.Firmware;
				//Firmware = InitialParametersClass.FirmwareVersion;

				// Use below logic to get actual firmware ver, not the 'CAB' returned by the above
				// matches console return of a "ver" and on SystemInfo page
				// example return ver: 
				//		RMC3 Cntrl Eng [v1.503.3568.25373 (Oct 09 2018), #4001E302] @E-00107f4420f0
				var response = "";
				CrestronConsole.SendControlSystemCommand("ver", ref response);
				properties.Firmware = console.ParseConsoleResponse(response, "Cntrl Eng", "[", "(");
				properties.FirmwareDate = console.ParseConsoleResponse(response, "Cntrl Eng", "(", ")");
			}
			catch (Exception e)
			{
				var msg = string.Format("ProcessorInfo.GetInfo() failed:\r{0}", e);
				CrestronConsole.PrintLine(msg);
				ErrorLog.Error(msg);
				return false;
			}

			return true;
		}		
	}
}