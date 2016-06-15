using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

using Newtonsoft.Json;

namespace PepperDash.Core
{
	public class EthernetHelper
	{
		/// <summary>
		/// 
		/// </summary>
		public static EthernetHelper LanHelper
		{
			get
			{
				if (_LanHelper == null) _LanHelper = new EthernetHelper(0);
				return LanHelper;
			}
		}
		static EthernetHelper _LanHelper;

		// ADD OTHER HELPERS HERE

		/// <summary>
		/// 
		/// </summary>
		public int PortNumber { get; private set; }

		private EthernetHelper(int portNumber)
		{
			PortNumber = portNumber;
		}

		/// <summary>
		/// 
		/// </summary>
		[JsonProperty("linkActive")]
		public bool LinkActive
		{
			get
			{
				var status = CrestronEthernetHelper.GetEthernetParameter(
					CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_LINK_STATUS, 0);
				Debug.Console(0, "LinkActive = {0}", status);
				return status == "";
			}
		}

		/// <summary>
		/// 
		/// </summary>
		[JsonProperty("dchpActive")]
		public bool DhcpActive
		{
			get
			{
				return CrestronEthernetHelper.GetEthernetParameter(
					CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_DHCP_STATE, 0) == "ON";
			}
		}

		/// <summary>
		/// 
		/// </summary>
		[JsonProperty("hostname")]
		public string Hostname
		{
			get
			{
				return CrestronEthernetHelper.GetEthernetParameter(
					CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_HOSTNAME, 0);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		[JsonProperty("ipAddress")]
		public string IPAddress
		{
			get
			{
				return CrestronEthernetHelper.GetEthernetParameter(
					CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_IP_ADDRESS, 0);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		[JsonProperty("subnetMask")]
		public string SubnetMask
		{
			get
			{
				return CrestronEthernetHelper.GetEthernetParameter(
					CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_IP_MASK, 0);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		[JsonProperty("defaultGateway")]
		public string DefaultGateway
		{
			get
			{
				return CrestronEthernetHelper.GetEthernetParameter(
					CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_ROUTER, 0);
			}
		}
	}
}