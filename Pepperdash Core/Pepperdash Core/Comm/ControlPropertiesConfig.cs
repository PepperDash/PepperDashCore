using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json;

namespace PepperDash.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class ControlPropertiesConfig
    {
        public eControlMethod Method { get; set; }

        public string ControlPortDevKey { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] // In case "null" is present in config on this value
        public uint ControlPortNumber { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] // In case "null" is present in config on this value
        public string ControlPortName { get; set; }

        public TcpSshPropertiesConfig TcpSshProperties { get; set; }

        public string IrFile { get; set; }

        public string RoomId { get; set; }//RoomId for VC-4

        //public ComPortConfig ComParams { get; set; }

        //[JsonConverter(typeof(ComSpecJsonConverter))]
        //public ComPort.ComPortSpec ComParams { get; set; }

        public string IpId { get; set; }

        [JsonIgnore]
        public uint IpIdInt { get { return Convert.ToUInt32(IpId, 16); } }

        public char EndOfLineChar { get; set; }

        /// <summary>
        /// Defaults to Environment.NewLine;
        /// </summary>
        public string EndOfLineString { get; set; }

        public string DeviceReadyResponsePattern { get; set; }

        public ControlPropertiesConfig()
        {
            EndOfLineString = CrestronEnvironment.NewLine;
        }
    }
}