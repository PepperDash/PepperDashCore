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
		/// Lan Helper property
		/// </summary>
		public static EthernetHelper LanHelper
		{
			get
			{
				if (_LanHelper == null) _LanHelper = new EthernetHelper(0);
				return _LanHelper;
			}
		}
		static EthernetHelper _LanHelper;

		// ADD OTHER HELPERS HERE

		/// <summary>
		/// Port Number property
		/// </summary>
		public int PortNumber { get; private set; }

		private EthernetHelper(int portNumber)
		{
			PortNumber = portNumber;
		}

		/// <summary>
		/// Link Active property
		/// </summary>
		/// <returns>
		/// Current link status
		/// </returns>
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
		/// DHCP Active property
		/// </summary>
		/// <returns>
		/// Current DHCP state
		/// </returns>
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
		/// Hostname property
		/// </summary>
		/// <returns>
		/// Current hostname
		/// </returns>
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
		/// IP Address property
		/// </summary>
		/// <returns>
		/// Current IP address
		/// </returns>
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
		/// Subnet Mask property
		/// </summary>
		/// <returns>
		/// Current subnet mask
		/// </returns>
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
		/// Default Gateway property
		/// </summary>
		/// <returns>
		/// Current router
		/// </returns>
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