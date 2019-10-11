using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core.SystemInfo
{
	/// <summary>
	/// Ethernet class
	/// </summary>
	public class EthernetConfig
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
		public EthernetConfig()
		{
			// add logic here if necessary
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public class EthernetInfo
	{
		public EthernetConfig properties { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public EthernetInfo()
		{

		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public bool GetInfo()
		{
			try
			{
				// get lan adapter id
				var adapterId = CrestronEthernetHelper.GetAdapterdIdForSpecifiedAdapterType(EthernetAdapterType.EthernetLANAdapter);

				// get lan adapter info
				var dhcpState = CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_DHCP_STATE, adapterId);
				if (!string.IsNullOrEmpty(dhcpState))
					properties.DhcpIsOn = (ushort)(dhcpState.ToLower().Contains("on") ? 1 : 0);

				properties.Hostname = CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_HOSTNAME, adapterId);
				properties.MacAddress = CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_MAC_ADDRESS, adapterId);
				properties.IpAddress = CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_IP_ADDRESS, adapterId);
				properties.Subnet = CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_IP_MASK, adapterId);
				properties.Gateway = CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_ROUTER, adapterId);
				properties.Domain = CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_DOMAIN_NAME, adapterId);

				// returns comma seperate list of dns servers with trailing comma
				// example return: "8.8.8.8 (DHCP),8.8.4.4 (DHCP),"
				string dns = CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_DNS_SERVER, adapterId);
				if (string.IsNullOrEmpty(dns))
				{
					properties.Dns1 = "0.0.0.0";
					properties.Dns2 = "0.0.0.0";
					properties.Dns3 = "0.0.0.0";
				}

				if (dns.Contains(","))
				{
					string[] dnsList = dns.Split(',');
					properties.Dns1 = !string.IsNullOrEmpty(dnsList[0]) ? dnsList[0] : "0.0.0.0";
					properties.Dns2 = !string.IsNullOrEmpty(dnsList[1]) ? dnsList[1] : "0.0.0.0";
					properties.Dns3 = !string.IsNullOrEmpty(dnsList[2]) ? dnsList[2] : "0.0.0.0";
				}
				else
				{

					properties.Dns1 = !string.IsNullOrEmpty(dns) ? dns : "0.0.0.0";
					properties.Dns2 = "0.0.0.0";
					properties.Dns3 = "0.0.0.0";
				}
			}
			catch(Exception e)
			{
				var msg = string.Format("EthernetInfo.GetInfo() failed:\r{0}", e);
				CrestronConsole.PrintLine(msg);
				ErrorLog.Error(msg);
				return false;
			}
			
			return true;
		}
	}
}