using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;

using PepperDash.Core;

namespace PepperDash.Core.Config
{
    /// <summary>
    /// Reads a Portal formatted config file
    /// </summary>
	public class PortalConfigReader
	{
		/// <summary>
		/// Reads the config file, checks if it needs a merge, merges and saves, then returns the merged Object.
		/// </summary>
		/// <returns>JObject of config file</returns>
		public static void ReadAndMergeFileIfNecessary(string filePath, string savePath)
		{
			try
			{
				if (!File.Exists(filePath))
				{
					Debug.Console(1, Debug.ErrorLogLevel.Error,
						"ERROR: Configuration file not present. Please load file to {0} and reset program", filePath);
				}

				using (StreamReader fs = new StreamReader(filePath))
				{
					var jsonObj = JObject.Parse(fs.ReadToEnd());
					if(jsonObj["template"] != null && jsonObj["system"] != null)
					{
						// it's a double-config, merge it.
                        Debug.Console(2, "Merging template & system configurations");

						var merged = MergeConfigs(jsonObj);
						if (jsonObj["system_url"] != null)
						{
							merged["systemUrl"] = jsonObj["system_url"].Value<string>();
						}

						if (jsonObj["template_url"] != null)
						{
							merged["templateUrl"] = jsonObj["template_url"].Value<string>();
						}

						jsonObj = merged;
					}

					using (StreamWriter fw = new StreamWriter(savePath))
					{
						fw.Write(jsonObj.ToString(Formatting.Indented));
						Debug.Console(1, "JSON config merged and saved to {0}", savePath);
					}

				}
			}
			catch (Exception e)
			{
				Debug.Console(1, Debug.ErrorLogLevel.Error, "ERROR: Config load failed: \r{0}", e);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="doubleConfig"></param>
		/// <returns></returns>
		public static JObject MergeConfigs(JObject doubleConfig)
		{
			var system = JObject.FromObject(doubleConfig["system"]);
			var template = JObject.FromObject(doubleConfig["template"]);
			var merged = new JObject();

			// Put together top-level objects
			if (system["info"] != null)
				merged.Add("info", Merge(template["info"], system["info"], "info"));
			else
				merged.Add("info", template["info"]);

			merged.Add("devices", MergeArraysOnTopLevelProperty(template["devices"] as JArray,
				system["devices"] as JArray, "uid", "devices"));

			if (system["rooms"] == null)
				merged.Add("rooms", template["rooms"]);
			else
				merged.Add("rooms", MergeArraysOnTopLevelProperty(template["rooms"] as JArray,
					system["rooms"] as JArray, "key", "rooms"));

			if (system["sourceLists"] == null)
				merged.Add("sourceLists", template["sourceLists"]);
			else
				merged.Add("sourceLists", Merge(template["sourceLists"], system["sourceLists"], "sourceLists"));

		    if (system["destinationLists"] == null)
		        merged.Add("destinationLists", template["destinationLists"]);
		    else
		        merged.Add("destinationLists",
		            Merge(template["destinationLists"], system["destinationLists"], "destinationLists"));

			// Template tie lines take precedence.  Config tool doesn't do them at system
			// level anyway...
			if (template["tieLines"] != null)
				merged.Add("tieLines", template["tieLines"]);
			else if (system["tieLines"] != null)
				merged.Add("tieLines", system["tieLines"]);
			else
				merged.Add("tieLines", new JArray());

            if (template["joinMaps"] != null)
                merged.Add("joinMaps", template["joinMaps"]);
            else
                merged.Add("joinMaps", new JObject());

			if (system["global"] != null)
				merged.Add("global", Merge(template["global"], system["global"], "global"));
			else
				merged.Add("global", template["global"]);

			Debug.Console(2, "MERGED CONFIG RESULT: \x0d\x0a{0}", merged);
			return merged;
		}

		/// <summary>
		/// Merges the contents of a base and a delta array, matching the entries on a top-level property
		/// given by propertyName.  Returns a merge of them. Items in the delta array that do not have
		/// a matched item in base array will not be merged. Non keyed system items will replace the template items.
		/// </summary>
		static JArray MergeArraysOnTopLevelProperty(JArray template, JArray system, string propertyName, string path)
		{
			var result = new JArray();
            if (system == null || system.Count == 0) // If the system array is null or empty, return the template array
            {
                return template;
            }

            if (template == null)
            {
                return result;
            }
			
            if (system[0]["key"] == null) // If the first item in the system array has no key, overwrite the template array
            {                                                       // with the system array
                return system;
            }

            for (int i = 0; i < template.Count(); i++)
            {
                var templateObject = template[i];
                // Try to get a system device and if found, merge it onto template
                var systemObject = system.FirstOrDefault(t => t[propertyName].Equals(templateObject[propertyName]));// t.Value<int>("uid") == tmplDev.Value<int>("uid"));

                var objectToAdd = templateObject;

                if (systemObject != null)
                {
                    objectToAdd = Merge(templateObject, systemObject, string.Format("{0}[{1}].", path, i));// Merge(JObject.FromObject(a1Dev), JObject.FromObject(a2Match));                                                
                }

                result.Add(objectToAdd);
            }               
			
			return result;
		}


		/// <summary>
		/// Helper for using with JTokens.  Converts to JObject 
		/// </summary>
		static JObject Merge(JToken t1, JToken t2, string path)
		{
			return Merge(JObject.FromObject(t1), JObject.FromObject(t2), path);
		}

		/// <summary>
		/// Merge o2 onto o1
		/// </summary>
        /// <param name="o1"></param>
        /// <param name="o2"></param>
        /// <param name="path"></param>
		static JObject Merge(JObject parent, JObject child, string path)
		{
			foreach (var childProperty in child)
			{
				var childPropertyKey = childProperty.Key;
				var parentValue = parent[childPropertyKey];
				var childValue = child[childPropertyKey];

				// if the property doesn't exist on o1, then add it.
				if (parentValue == null)
				{
					parent.Add(childPropertyKey, childValue);
                    continue;
				}
				// otherwise merge them
				
				// Drill down
				var propPath = String.Format("{0}.{1}", path, childPropertyKey);
				try
				{

					if (parentValue is JArray && childValue is JArray)
					{						
						parentValue.Replace(MergeArraysOnTopLevelProperty(parentValue as JArray, childValue as JArray, "key", propPath));	
                        continue;
					}
					
                    if (childProperty.Value.HasValues && parentValue.HasValues)
					{
						parentValue.Replace(Merge(parentValue, childValue, propPath));
                        continue;
					}
					
					parentValue.Replace(childProperty.Value);					
				}
				catch (Exception e)
				{
					Debug.Console(1, Debug.ErrorLogLevel.Warning, "Cannot merge items at path {0}: \r{1}", propPath, e);
				}			
			}
			return parent;
		}
	}
}