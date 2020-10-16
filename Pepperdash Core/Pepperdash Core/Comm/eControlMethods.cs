using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PepperDash.Core
{
    /// <summary>
    /// Crestron Control Methods for a comm object
    /// </summary>
    /// <remarks>
    /// Currently Http, Https, Ws, and Wss will not create comm objects, however having them defined in the 
    /// enum resolves the issue where they would cause an exception if passed into the comm factory.
    /// 
    /// When using the eControlMethods in a JSON configuration file use the following example:
    /// <code>
    /// "method": "[eControlMethod Enum]"
    /// </code>
    /// 
    /// Below is a full example of the device JSON configuration
    /// <code>
    /// {
	///		"key": "device-key",	
	///		"name": "Device Name",
	///		"group": "comm",
	///		"type": "genericComm",
	///		"properties": {
	///			"control": {
	///				"method": "Com",
	///				"controlPortDevKey": "processor",
	///				"controlPortNumber": 1,
	///				"comParams": {
	///					"hardwareHandshake": "None",
	///					"parity": "None",
	///					"protocol": "RS232",
	///					"baudRate": 9600,
	///					"dataBits": 8,
	///					"softwareHandshake": "None",
	///					"stopBits": 1
	///				},
	///				"tcpSshProperties": {
	///					"address": "",
	///					"port": 1515,
	///					"username": "",
	///					"password": "",
	///					"autoReconnect": false,
	///					"autoReconnectIntervalMs": 30000
	///				}
	///			}
	///		}
	///	}
    /// </code>
    /// </remarks>
    public enum eControlMethod
    {
        None = 0, Com, IpId, IpidTcp, IR, Ssh, Tcpip, Telnet, Cresnet, Cec, Udp, Http, Https, Ws, Wss
    }
}