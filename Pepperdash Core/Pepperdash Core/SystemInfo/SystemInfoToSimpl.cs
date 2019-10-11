using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;

namespace PepperDash.Core.SystemInfo
{
	/// <summary>
	/// Processor Info to Simpl Class
	/// Ported from Technics_Core
	/// </summary>
	public class SystemInfoToSimpl
	{
		public event EventHandler<BoolChangeEventArgs> BoolChange;
		public event EventHandler<StringChangeEventArgs> StringChange;		
		public event EventHandler<ProcessorChangeEventArgs> ProcessorChange;
		public event EventHandler<EthernetChangeEventArgs> EthernetChange;
		public event EventHandler<ControlSubnetChangeEventArgs> ControlSubnetChange;
		public event EventHandler<ProgramChangeEventArgs> ProgramChange;

		/// <summary>
		/// Constructor
		/// </summary>
		public SystemInfoToSimpl()
		{

		}

		/// <summary>
		/// Get processor info method
		/// </summary>
		public void GetProcessorInfo()
		{
			OnBoolChange(true, 0, SystemInfoConstants.BusyBoolChange);

			var processor = new ProcessorInfo();
			if (processor.GetInfo() == true)
				OnProcessorChange(processor.properties, 0, SystemInfoConstants.ProcessorObjectChange);

			OnBoolChange(false, 0, SystemInfoConstants.BusyBoolChange);
		}

		/// <summary>
		/// Get processor uptime method
		/// </summary>
		public void RefreshProcessorUptime()
		{
			var console = new ConsoleHelper();

			try
			{
				string response = "";
				CrestronConsole.SendControlSystemCommand("uptime", ref response);
				var uptime = console.ParseConsoleResponse(response, "running for", "running for", "\x0D");
				OnStringChange(uptime, 0, SystemInfoConstants.ProcessorUptimeChange);
			}
			catch (Exception e)
			{
				var msg = string.Format("RefreshProcessorUptime failed:\r{0}", e);
				CrestronConsole.PrintLine(msg);
				ErrorLog.Error(msg);
			}
		}

		/// <summary>
		///  get ethernet info method
		/// </summary>
		public void GetEthernetInfo()
		{
			OnBoolChange(true, 0, SystemInfoConstants.BusyBoolChange);

			var ethernet = new EthernetInfo();
			if(ethernet.GetInfo() == true)
				OnEthernetInfoChange(ethernet.properties, 0, SystemInfoConstants.ObjectChange);

			OnBoolChange(false, 0, SystemInfoConstants.BusyBoolChange);
		}

		/// <summary>
		/// Get control subnet ethernet info method
		/// </summary>
		public void GetControlSubnetInfo()
		{
			OnBoolChange(true, 0, SystemInfoConstants.BusyBoolChange);

			var ethernet = new ControlSubnetInfo();
			if(ethernet.GetInfo() == true)
				OnControlSubnetInfoChange(ethernet.properties, 0, SystemInfoConstants.ObjectChange);

			OnBoolChange(false, 0, SystemInfoConstants.BusyBoolChange);
		}

		/// <summary>
		/// Get program info by index method
		/// </summary>
		/// <param name="index"></param>
		public void GetProgramInfoByIndex(ushort index)
		{
			OnBoolChange(true, 0, SystemInfoConstants.BusyBoolChange);
			
			var program = new ProgramInfo();
			if (program.GetInfoByIndex(index) == true)
				OnProgramChange(program.properties, index, SystemInfoConstants.ObjectChange);
						
			OnBoolChange(false, 0, SystemInfoConstants.BusyBoolChange);
		}

		/// <summary>
		/// Refresh program uptime by index
		/// </summary>
		/// <param name="index"></param>
		public void RefreshProgramUptimeByIndex(int index)
		{
			var console = new ConsoleHelper();

			try
			{
				string response = "";
				CrestronConsole.SendControlSystemCommand(string.Format("proguptime:{0}", index), ref response);
				string uptime = console.ParseConsoleResponse(response, "running for", "running for", "\x0D");
				OnStringChange(uptime, (ushort)index, SystemInfoConstants.ProgramUptimeChange);
			}
			catch (Exception e)
			{
				var msg = string.Format("RefreshProgramUptimebyIndex({0}) failed:\r{1}", index, e);
				CrestronConsole.PrintLine(msg);
				ErrorLog.Error(msg);
			}
		}

		/// <summary>
		/// Send console command
		/// </summary>
		/// <param name="cmd"></param>
		public void SendConsoleCommand(string cmd)
		{
			if (string.IsNullOrEmpty(cmd))
				return;

			string consoleResponse = "";
			CrestronConsole.SendControlSystemCommand(cmd, ref consoleResponse);
			if (!string.IsNullOrEmpty(consoleResponse))
			{
				if (consoleResponse.Contains("\x0D\x0A"))
					consoleResponse.Trim('\n');
				OnStringChange(consoleResponse, 0, SystemInfoConstants.ConsoleResponseChange);
			}
		}		

		/// <summary>
		/// Boolean change event handler
		/// </summary>
		/// <param name="state"></param>
		/// <param name="index"></param>
		/// <param name="type"></param>
		protected void OnBoolChange(bool state, ushort index, ushort type)
		{
			var handler = BoolChange;
			if (handler != null)
			{
				var args = new BoolChangeEventArgs(state, type);
				args.Index = index;
				BoolChange(this, args);
			}
		}

		/// <summary>
		/// String change event handler
		/// </summary>
		/// <param name="value"></param>
		/// <param name="index"></param>
		/// <param name="type"></param>
		protected void OnStringChange(string value, ushort index, ushort type)
		{
			var handler = StringChange;
			if (handler != null)
			{
				var args = new StringChangeEventArgs(value, type);
				args.Index = index;
				StringChange(this, args);
			}
		}

		/// <summary>
		/// Processor change event handler
		/// </summary>
		/// <param name="processor"></param>
		/// <param name="index"></param>
		/// <param name="type"></param>
		protected void OnProcessorChange(ProcessorConfig processor, ushort index, ushort type)
		{
			var handler = ProcessorChange;
			if (handler != null)
			{
				var args = new ProcessorChangeEventArgs(processor, type);
				args.Index = index;
				ProcessorChange(this, args);
			}
		}

		/// <summary>
		/// Ethernet change event handler
		/// </summary>
		/// <param name="ethernet"></param>
		/// <param name="index"></param>
		/// <param name="type"></param>
		protected void OnEthernetInfoChange(EthernetConfig ethernet, ushort index, ushort type)
		{
			var handler = EthernetChange;
			if (handler != null)
			{
				var args = new EthernetChangeEventArgs(ethernet, type);
				args.Index = index;
				EthernetChange(this, args);
			}
		}

		/// <summary>
		/// Control Subnet change event handler
		/// </summary>
		/// <param name="ethernet"></param>
		/// <param name="index"></param>
		/// <param name="type"></param>
		protected void OnControlSubnetInfoChange(ControlSubnetConfig ethernet, ushort index, ushort type)
		{
			var handler = ControlSubnetChange;
			if (handler != null)
			{
				var args = new ControlSubnetChangeEventArgs(ethernet, type);
				args.Index = index;
				ControlSubnetChange(this, args);
			}
		}

		/// <summary>
		/// Program change event handler
		/// </summary>
		/// <param name="program"></param>
		/// <param name="index"></param>
		/// <param name="type"></param>
		protected void OnProgramChange(ProgramConfig program, ushort index, ushort type)
		{
			var handler = ProgramChange;
			if (handler != null)
			{
				var args = new ProgramChangeEventArgs(program, type);
				args.Index = index;
				ProgramChange(this, args);
			}
		}
	}
}