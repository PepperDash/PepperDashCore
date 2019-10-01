using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Core.JsonToSimpl;

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
	/// Device class
	/// </summary>
	public class DeviceConfig
	{
		/// <summary>
		/// JSON config key property
		/// </summary>
		public string key { get; set; }
		/// <summary>
		/// JSON config name property
		/// </summary>
		public string name { get; set; }
		/// <summary>
		/// JSON config type property
		/// </summary>
		public string type { get; set; }
		/// <summary>
		/// JSON config properties 
		/// </summary>
		public PropertiesConfig properties { get; set; }

		/// <summary>
		/// Bool change event handler
		/// </summary>
		public event EventHandler<BoolChangeEventArgs> BoolChange;
		/// <summary>
		/// Ushort change event handler
		/// </summary>
		public event EventHandler<UshrtChangeEventArgs> UshrtChange;
		/// <summary>
		/// String change event handler
		/// </summary>
		public event EventHandler<StringChangeEventArgs> StringChange;
		/// <summary>
		/// Object change event handler
		/// </summary>
		public event EventHandler<ObjectChangeEventArgs> ObjectChange;

		/// <summary>
		/// Constructor
		/// </summary>
		public DeviceConfig()
		{
			// add logic here if necessary
		}

		/// <summary>
		/// Initialize Device Module
		/// </summary>
		/// <param name="uniqueID">JSON master unique ID</param>
		/// <param name="key">Device key to search for</param>
		public void Initialize(string uniqueID, string key)
		{
			// S+ set EvaluateFb low
			OnBoolChange(false, 0, JsonStandardDeviceConstants.JsonObjectEvaluated);
			// validate parameters
			if (string.IsNullOrEmpty(uniqueID))
			{
				Debug.Console(1, "UniqueID is null or empty");
				return;
			}
			if (string.IsNullOrEmpty(key))
			{
				Debug.Console(1, "Device key is null or empty");
				return;
			}

			try
			{
				// get the file using the unique ID
				JsonToSimplMaster jsonMaster = J2SGlobal.GetMasterByFile(uniqueID);
				var device = jsonMaster.JsonObject.ToObject<RootObject>().devices.FirstOrDefault(d => d.key.Equals(key));

				name = device.name;
				type = device.type;
				properties = device.properties;
				// Pass object to S+
				OnObjectChange(this, 0, JsonStandardDeviceConstants.JsonObjectChanged);

			}
			catch (Exception e)
			{
				var msg = string.Format("Device lookup failed:\r{0}", e);
				CrestronConsole.PrintLine(msg);
				ErrorLog.Error(msg);
				return;
			}

			// S+ set EvaluteFb high
			OnBoolChange(true, 0, JsonStandardDeviceConstants.JsonObjectEvaluated);
		}

		#region EventHandler Helpers

		/// <summary>
		/// BoolChange event handler helper
		/// </summary>
		/// <param name="state"></param>
		/// <param name="index"></param>
		/// <param name="type"></param>
		protected void OnBoolChange(bool state, ushort index, ushort type)
		{
			if (BoolChange != null)
			{
				var args = new BoolChangeEventArgs(state, type);
				args.Index = index;
				BoolChange(this, args);
			}
		}

		/// <summary>
		/// UshrtChange event handler helper
		/// </summary>
		/// <param name="state"></param>
		/// <param name="index"></param>
		/// <param name="type"></param>
		protected void OnUshrtChange(ushort state, ushort index, ushort type)
		{
			if (UshrtChange != null)
			{
				var args = new UshrtChangeEventArgs(state, type);
				args.Index = index;
				UshrtChange(this, args);
			}
		}

		/// <summary>
		/// StringChange event handler helper
		/// </summary>
		/// <param name="value"></param>
		/// <param name="index"></param>
		/// <param name="type"></param>
		protected void OnStringChange(string value, ushort index, ushort type)
		{
			if (StringChange != null)
			{
				var args = new StringChangeEventArgs(value, type);
				args.Index = index;
				StringChange(this, args);
			}
		}

		/// <summary>
		/// ObjectChange event handler helper
		/// </summary>
		/// <param name="device"></param>
		/// <param name="index"></param>
		/// <param name="type"></param>
		protected void OnObjectChange(DeviceConfig device, ushort index, ushort type)
		{
			if (ObjectChange != null)
			{
				var args = new ObjectChangeEventArgs(device, type);
				args.Index = index;
				ObjectChange(this, args);
			}
		}

		#endregion EventHandler Helpers
	}

	#region JSON Configuration Classes

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
		public ushort simplBaudRate { get { return (ushort)Convert.ToUInt16(baudRate); } }
		public ushort simplDataBits { get { return Convert.ToUInt16(dataBits); } }
		public ushort simplStopBits { get { return Convert.ToUInt16(stopBits); } }
		public ushort simplPacing { get { return Convert.ToUInt16(pacing); } }
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
		public ushort simplPort { get { return (ushort)Convert.ToUInt16(port); } }
		public ushort simplAutoReconnect { get { return (ushort)(autoReconnect ? 1 : 0); } }
		public ushort simplAutoReconnectIntervalMs { get { return Convert.ToUInt16(autoReconnectIntervalMs); } }
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
	}

	/// <summary>
	/// Root device class
	/// </summary>
	public class RootObject
	{
		public List<DeviceConfig> devices { get; set; }
	}

	#endregion JSON Configuration Classes
}