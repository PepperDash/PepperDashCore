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
    /// Base class for all properties config classes to derive from
    /// </summary>
    public abstract class PropertiesConfigBase
    {
        /// <summary>
        /// The schema json string for the class
        /// </summary>
        public string SchemaJson { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public PropertiesConfigBase()
        {
            SchemaJson = null;
        }

        /// <summary>
        /// Parses SchemaJson
        /// </summary>
        /// <returns>A JsonSchema</returns>
        public JsonSchema ParseSchema()
        {
            if (!string.IsNullOrEmpty(SchemaJson))
            {
                JsonSchema schema = JsonSchema.Parse(SchemaJson);

                return schema;
            }
            else
                return null;

        }
    }
}