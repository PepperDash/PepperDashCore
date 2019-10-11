using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core.SystemInfo
{
	/// <summary>
	/// Control subnet class
	/// </summary>
	public class ControlSubnetConfig
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
		public ControlSubnetConfig()
		{
			// add logic here if necessary
		}
	}

	/// <summary>
	/// Control subnet info class
	/// </summary>
	public class ControlSubnetInfo
	{
		public ControlSubnetConfig properties { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public ControlSubnetInfo()
		{
			properties.Enabled = (ushort)0;
			properties.IsInAutomaticMode = (ushort)0;
			properties.MacAddress = "NA";
			properties.IpAddress = "NA";
			properties.Subnet = "NA";
			properties.RouterPrefix = "NA";
		}

		/// <summary>
		/// Get control subnet info
		/// </summary>
		/// <returns></returns>
		public bool GetInfo()
		{
			try
			{
				// get cs adapter id
				var adapterId = CrestronEthernetHelper.GetAdapterdIdForSpecifiedAdapterType(EthernetAdapterType.EthernetCSAdapter);
				if (!adapterId.Equals(EthernetAdapterType.EthernetUnknownAdapter))
				{
					properties.Enabled = (ushort)EthernetAdapterType.EthernetCSAdapter;
					properties.IsInAutomaticMode = (ushort)(CrestronEthernetHelper.IsControlSubnetInAutomaticMode ? 1 : 0);
					properties.MacAddress = CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_MAC_ADDRESS, adapterId);
					properties.IpAddress = CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_IP_ADDRESS, adapterId);
					properties.Subnet = CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_IP_MASK, adapterId);
					properties.RouterPrefix = CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CONTROL_SUBNET_ROUTER_PREFIX, adapterId);
				}				
			}
			catch (Exception e)
			{
				var msg = string.Format("ControlSubnetInfo.GetInfo() failed:\r{0}", e);
				CrestronConsole.PrintLine(msg);
				ErrorLog.Error(msg);
				return false;
			}

			return true;
		}
	}
}