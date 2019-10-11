using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core.JsonStandardObjects
{
	/*
	Convert JSON snippt to C#: http://json2csharp.com/#
	
	JSON Snippet:
	{
		"devices": [
			{
				"key": "deviceKey",
				"name": "deviceName",
				"type": "deviceType",
				"properties": {
					"deviceId": 1,
					"enabled": true,
					"control": {
						"method": "methodName",
						"controlPortDevKey": "deviceControlPortDevKey",
						"controlPortNumber": 1,
						"comParams": {
							"baudRate": 9600,
							"dataBits": 8,
							"stopBits": 1,
							"parity": "None",
							"protocol": "RS232",
							"hardwareHandshake": "None",
							"softwareHandshake": "None",
							"pacing": 0
						},
						"tcpSshProperties": {
							"address": "172.22.1.101",
							"port": 23,
							"username": "user01",
							"password": "password01",
							"autoReconnect": false,
							"autoReconnectIntervalMs": 10000
						}
					}
				}
			}
		]
	}
	*/
	/// <summary>
	/// Device communication parameter class
	/// </summary>
	public class ComParamsConfig
	{
		public int baudRate { get; set; }
		public int dataBits { get; set; }
		public int stopBits { get; set; }
		public string parity { get; set; }
		public string protocol { get; set; }
		public string hardwareHandshake { get; set; }
		public string softwareHandshake { get; set; }
		public int pacing { get; set; }

		// convert properties for simpl
		public ushort simplBaudRate { get { return Convert.ToUInt16(baudRate); } }
		public ushort simplDataBits { get { return Convert.ToUInt16(dataBits); } }
		public ushort simplStopBits { get { return Convert.ToUInt16(stopBits); } }
		public ushort simplPacing { get { return Convert.ToUInt16(pacing); } }

		/// <summary>
		/// Constructor
		/// </summary>
		public ComParamsConfig()
		{

		}
	}

	/// <summary>
	/// Device TCP/SSH properties class
	/// </summary>
	public class TcpSshPropertiesConfig
	{
		public string address { get; set; }
		public int port { get; set; }
		public string username { get; set; }
		public string password { get; set; }
		public bool autoReconnect { get; set; }
		public int autoReconnectIntervalMs { get; set; }

		// convert properties for simpl
		public ushort simplPort { get { return Convert.ToUInt16(port); } }
		public ushort simplAutoReconnect { get { return (ushort)(autoReconnect ? 1 : 0); } }
		public ushort simplAutoReconnectIntervalMs { get { return Convert.ToUInt16(autoReconnectIntervalMs); } }

		/// <summary>
		/// Constructor
		/// </summary>
		public TcpSshPropertiesConfig()
		{

		}
	}

	/// <summary>
	/// Device control class
	/// </summary>
	public class ControlConfig
	{
		public string method { get; set; }
		public string controlPortDevKey { get; set; }
		public int controlPortNumber { get; set; }
		public ComParamsConfig comParams { get; set; }
		public TcpSshPropertiesConfig tcpSshProperties { get; set; }

		// convert properties for simpl
		public ushort simplControlPortNumber { get { return Convert.ToUInt16(controlPortNumber); } }

		/// <summary>
		/// Constructor
		/// </summary>
		public ControlConfig()
		{
			comParams = new ComParamsConfig();
			tcpSshProperties = new TcpSshPropertiesConfig();
		}
	}

	/// <summary>
	/// Device properties class
	/// </summary>
	public class PropertiesConfig
	{
		public int deviceId { get; set; }
		public bool enabled { get; set; }
		public ControlConfig control { get; set; }

		// convert properties for simpl
		public ushort simplDeviceId { get { return Convert.ToUInt16(deviceId); } }
		public ushort simplEnabled { get { return (ushort)(enabled ? 1 : 0); } }

		/// <summary>
		/// Constructor
		/// </summary>
		public PropertiesConfig()
		{
			control = new ControlConfig();
		}
	}

	/// <summary>
	/// Root device class
	/// </summary>
	public class RootObject
	{
		public List<DeviceConfig> devices { get; set; }
	}
}