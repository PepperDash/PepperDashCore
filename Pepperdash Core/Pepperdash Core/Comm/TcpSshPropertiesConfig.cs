using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

using Newtonsoft.Json;

namespace PepperDash.Core
{
    /// <summary>
    /// Configuration properties for TCP/SSH Connections
    /// </summary>
    
    public class TcpSshPropertiesConfig : PropertiesConfigBase
    {
        /// <summary>
        /// Address to connect to
        /// </summary>
        [JsonProperty("address", Required = Required.Always)]
        public string Address { get; set; }

        /// <summary>
        /// Port to connect to
        /// </summary>
        [JsonProperty("port", Required = Required.Always)]
        public int Port { get; set; }

        /// <summary>
        /// Username credential.  Defaults to ""
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// Passord credential. Defaults to ""
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Buffer size. Defaults to 32768
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// Indicates if automatic reconnection attemtps should be made.Defaults to true.
        /// Uses AutoReconnectIntervalMs to determine frequency of attempts.
        /// </summary>
        public bool AutoReconnect { get; set; }

        /// <summary>
        /// Defaults to 5000ms.  Requires AutoReconnect to be true.
        /// </summary>
        public int AutoReconnectIntervalMs { get; set; }

        public TcpSshPropertiesConfig()
        {
            SchemaJson = @"
{
  'definitions': {},
  '$schema': 'http://json-schema.org/draft-07/schema#',
  '$id': 'http://example.com/root.json',
  'type': 'object',
  'title': 'The Root Schema',
  'properties': {
    'username': {
      '$id': '#/properties/username',
      'type': 'string',
      'title': 'The Username Schema',
      'default': '',
      'examples': [
        'admin'
      ],
      'pattern': '^(.*)$'
    },
    'port': {
      '$id': '#/properties/port',
      'type': 'integer',
      'title': 'The Port Schema',
      'default': 0,
      'examples': [
        22
      ]
    },
    'address': {
      '$id': '#/properties/address',
      'type': 'string',
      'title': 'The Address Schema',
      'default': '',
      'examples': [
        '10.11.50.135'
      ],
      'pattern': '^(.*)$'
    },
    'password': {
      '$id': '#/properties/password',
      'type': 'string',
      'title': 'The Password Schema',
      'default': '',
      'examples': [
        'password'
      ],
      'pattern': '^(.*)$'
    },
    'autoReconnect': {
      '$id': '#/properties/autoReconnect',
      'type': 'boolean',
      'title': 'The Autoreconnect Schema',
      'default': false,
      'examples': [
        true
      ]
    },
    'autoReconnectIntervalMs': {
      '$id': '#/properties/autoReconnectIntervalMs',
      'type': 'integer',
      'title': 'The Autoreconnectintervalms Schema',
      'default': 0,
      'examples': [
        2000
      ]
    },
    'bufferSize': {
      '$id': '#/properties/bufferSize',
      'type': 'integer',
      'title': 'The Buffersize Schema',
      'default': 0,
      'examples': [
        32768
      ]
    }
  },
  'required': [
    'port',
    'address'
  ]
}
";


            BufferSize = 32768;
            AutoReconnect = true;
            AutoReconnectIntervalMs = 5000;
            Username = "";
            Password = "";
        }

    }
}