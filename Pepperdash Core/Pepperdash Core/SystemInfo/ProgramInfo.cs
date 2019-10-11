using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core.SystemInfo
{
	/// <summary>
	/// Program class
	/// </summary>
	public class ProgramConfig
	{
		public string Key { get; set; }
		public string Name { get; set; }
		public string Header { get; set; }
		public string System { get; set; }
		public string CompileTime { get; set; }
		public string Database { get; set; }
		public string Environment { get; set; }
		public string Programmer { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public ProgramConfig()
		{
			// add logic here if necessary
		}
	}

	/// <summary>
	/// Program info class
	/// </summary>
	public class ProgramInfo
	{
		public ProgramConfig properties { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public ProgramInfo()
		{

		}

		/// <summary>
		/// Get program info by index
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool GetInfoByIndex(int index)
		{
			var console = new ConsoleHelper();

			try
			{
				string response = "";
				CrestronConsole.SendControlSystemCommand(string.Format("progcomments:{0}", index), ref response);

				// SIMPL returns
				properties.Name = console.ParseConsoleResponse(response, "Program File", ":", "\x0D");
				properties.System = console.ParseConsoleResponse(response, "System Name", ":", "\x0D");
				properties.Programmer = console.ParseConsoleResponse(response, "Programmer", ":", "\x0D");
				properties.CompileTime = console.ParseConsoleResponse(response, "Compiled On", ":", "\x0D");
				properties.Database = console.ParseConsoleResponse(response, "CrestronDB", ":", "\x0D");
				properties.Environment = console.ParseConsoleResponse(response, "Source Env", ":", "\x0D");

				// S# returns
				if (properties.System.Length == 0)
					properties.System = console.ParseConsoleResponse(response, "Application Name", ":", "\x0D");
				if (properties.Database.Length == 0)
					properties.Database = console.ParseConsoleResponse(response, "PlugInVersion", ":", "\x0D");
				if (properties.Environment.Length == 0)
					properties.Environment = console.ParseConsoleResponse(response, "Program Tool", ":", "\x0D");
			}
			catch (Exception e)
			{
				var msg = string.Format("ProgramInfo.GetInfoByIndex({0}) failed:\r{1}", index, e);
				CrestronConsole.PrintLine(msg);
				ErrorLog.Error(msg);
				return false;
			}

			return true;
		}
	}
}