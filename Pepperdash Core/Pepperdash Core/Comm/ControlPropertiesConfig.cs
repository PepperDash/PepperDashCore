using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;

namespace PepperDash.Core
{
    /// <summary>
    /// Defines the means of communicating with and controlling a device
    /// </summary>
    public class ControlPropertiesConfig :PropertiesConfigBase
    {
        /// <summary>
        /// The control method for the device
        /// </summary>
        [JsonProperty("method", Required = Required.Always)]
        public eControlMethod Method { get; set; }

        /// <summary>
        /// The key of the device where the control port can be found
        /// </summary>
        [JsonProperty("controlPortDevKey")]
        public string ControlPortDevKey { get; set; }

        /// <summary>
        /// Number of the control port on the parent device specified by ControlPortDevKey
        /// </summary>
        [JsonProperty("controlPortNumber", NullValueHandling = NullValueHandling.Ignore)] // In case "null" is present in config on this value
        public uint ControlPortNumber { get; set; }

        /// <summary>
        /// Name of the control port on the parent device specified by ControlPortDevKey
        /// </summary>
        [JsonProperty("controlPortName", NullValueHandling = NullValueHandling.Ignore)] // In case "null" is present in config on this value
        public string ControlPortName { get; set; }

        /// <summary>
        /// ConfigurationProperties for IP connections
        /// </summary>
        [JsonProperty("tcpSshProperties")]
        public TcpSshPropertiesConfig TcpSshProperties { get; set; }

        /// <summary>
        /// Name of the IR file
        /// </summary>
        [JsonProperty("irFile")]
        public string IrFile { get; set; }

        /// <summary>
        /// IpId of the device
        /// </summary>
        [JsonProperty("ipid")]
        public string IpId { get; set; }

        /// <summary>
        /// uint representation of the IpId property
        /// </summary>
        [JsonIgnore]
        public uint IpIdInt { get { return Convert.ToUInt32(IpId, 16); } }

        /// <summary>
        /// Character that delimits the end of a line in multiline repsonses
        /// </summary>
        [JsonProperty("endofLineChar")]
        public char EndOfLineChar { get; set; }

        /// <summary>
        /// String that delimits the end of a line in multiline responses.
        /// Defaults to Environment.NewLine;
        /// </summary>
        [JsonProperty("endofLineString")]
        public string EndOfLineString { get; set; }

        /// <summary>
        /// Regex pattern that defines the prompt indicating a device is ready for communication
        /// </summary>
        [JsonProperty("deviceReadyResponsePattern")]
        public string DeviceReadyResponsePattern { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ControlPropertiesConfig()
        {
            EndOfLineString = CrestronEnvironment.NewLine;
        }
    }
}