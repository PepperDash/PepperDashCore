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

        public ControlPropertiesConfig()
        {
            SchemaJson = @"
{
  'definitions': {},
  '$schema': 'http://json-schema.org/draft-07/schema#',
  '$id': 'http://example.com/root.json',
  'type': 'object',
  'title': 'The Root Schema',
  'properties': {
    'tcpSshProperties': {
      '$id': '#/properties/tcpSshProperties',
      'type': 'object',
      'title': 'The Tcpsshproperties Schema',
      'default': null
    },
    'method': {
      '$id': '#/properties/method',
      'type': 'string',
      'title': 'The Method Schema',
      'default': '',
      'examples': [
        'ssh'
      ],
      'pattern': '^(.*)$'
    },
    'controlPortDevKey': {
      '$id': '#/properties/controlPortDevKey',
      'type': 'string',
      'title': 'The Controlportdevkey Schema',
      'default': '',
      'examples': [
        'processor'
      ],
      'pattern': '^(.*)$'
    },
    'controlPortNumber': {
      '$id': '#/properties/controlPortNumber',
      'type': 'integer',
      'title': 'The Controlportnumber Schema',
      'default': 0,
      'examples': [
        1
      ]
    },
    'controlPortName': {
      '$id': '#/properties/controlPortName',
      'type': 'string',
      'title': 'The Controlportname Schema',
      'default': '',
      'examples': [
        'hdmi1'
      ],
      'pattern': '^(.*)$'
    },
    'irFile': {
      '$id': '#/properties/irFile',
      'type': 'string',
      'title': 'The Irfile Schema',
      'default': '',
      'examples': [
        'Comcast Motorola DVR.ir'
      ],
      'pattern': '^(.*)$'
    },
    'ipid': {
      '$id': '#/properties/ipid',
      'type': 'string',
      'title': 'The Ipid Schema',
      'default': '',
      'examples': [
        '13'
      ],
      'pattern': '^(.*)$'
    },
    'endOfLineChar': {
      '$id': '#/properties/endOfLineChar',
      'type': 'string',
      'title': 'The Endoflinechar Schema',
      'default': '',
      'examples': [
        '\\x0d'
      ],
      'pattern': '^(.*)$'
    },
    'endOfLineString': {
      '$id': '#/properties/endOfLineString',
      'type': 'string',
      'title': 'The Endoflinestring Schema',
      'default': '',
      'examples': [
        '\n'
      ],
      'pattern': '^(.*)$'
    },
    'deviceReadyResponsePattern': {
      '$id': '#/properties/deviceReadyResponsePattern',
      'type': 'string',
      'title': 'The Devicereadyresponsepattern Schema',
      'default': '',
      'examples': [
        '.*>'
      ],
      'pattern': '^(.*)$'
    }
  },
  'required': [
    'method'
  ]
}
";


            EndOfLineString = CrestronEnvironment.NewLine;
        }
    }
}