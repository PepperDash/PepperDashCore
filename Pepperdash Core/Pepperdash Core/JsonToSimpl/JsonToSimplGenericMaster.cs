using System;
//using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.Net.Https;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PepperDash.Core.JsonToSimpl
{
	public class JsonToSimplGenericMaster : JsonToSimplMaster
    {
		/*****************************************************************************************/
		/** Privates **/

        
		// The JSON file in JObject form
		// For gathering the incoming data
		object StringBuilderLock = new object();
		// To prevent multiple same-file access
		static object WriteLock = new object();

		public Action<string> SaveCallback { get; set; }

		/*****************************************************************************************/

		/// <summary>
        /// SIMPL+ default constructor.
        /// </summary>
		public JsonToSimplGenericMaster()
        {
		}

		/// <summary>
		/// Loads in JSON and triggers evaluation on all children
		/// </summary>
		/// <param name="json"></param>
		public void LoadWithJson(string json)
		{
			OnBoolChange(false, 0, JsonToSimplConstants.JsonIsValidBoolChange);
			try
			{
				JsonObject = JObject.Parse(json);
				foreach (var child in Children)
					child.ProcessAll();
				OnBoolChange(true, 0, JsonToSimplConstants.JsonIsValidBoolChange);
			}
			catch (Exception e)
			{
				var msg = string.Format("JSON parsing failed:\r{0}", e);
				CrestronConsole.PrintLine(msg);
				ErrorLog.Error(msg);
			}
		}

		/// <summary>
		/// Loads JSON into JsonObject, but does not trigger evaluation by children
		/// </summary>
		/// <param name="json"></param>
		public void SetJsonWithoutEvaluating(string json)
		{
			try
			{
				JsonObject = JObject.Parse(json);
			}
			catch (Exception e)
			{
				Debug.Console(0, this, "JSON parsing failed:\r{0}", e);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public override void Save()
		{
			// this code is duplicated in the other masters!!!!!!!!!!!!!
 			UnsavedValues = new Dictionary<string, JValue>();
			// Make each child update their values into master object
			foreach (var child in Children)
			{
				Debug.Console(1, this, "Master. checking child [{0}] for updates to save",  child.Key);
				child.UpdateInputsForMaster();
			}

			if (UnsavedValues == null || UnsavedValues.Count == 0)
			{
				Debug.Console(1, this, "Master. No updated values to save. Skipping");
				return;
			}

			lock (WriteLock)
			{
				Debug.Console(1, this, "Saving");
				foreach (var path in UnsavedValues.Keys)
				{
					var tokenToReplace = JsonObject.SelectToken(path);
					if (tokenToReplace != null)
					{// It's found
						tokenToReplace.Replace(UnsavedValues[path]);
						Debug.Console(1, this, "Master Updating '{0}'", path);
					}
					else // No token.  Let's make one 
					{
						Debug.Console(1, "Master Cannot write value onto missing property: '{0}'", path);
					}
				}
			}
			if (SaveCallback != null)
				SaveCallback(JsonObject.ToString());
			else
				Debug.Console(0, this, "WARNING: No save callback defined.");
		}
	}
}


//            JContainer jpart = JsonObject;
//            // walk down the path and find where it goes
//#warning Does not handle arrays.
//            foreach (var part in path.Split('.'))
//            {

//                var openPos = part.IndexOf('[');
//                if (openPos > -1)
//                {
//                    openPos++; // move to number
//                    var closePos = part.IndexOf(']');
//                    var arrayName = part.Substring(0, openPos - 1); // get the name
//                    var index = Convert.ToInt32(part.Substring(openPos, closePos - openPos));

//                    // Check if the array itself exists and add the item if so
//                    if (jpart[arrayName] != null)
//                    {
//                        var arrayObj = jpart[arrayName] as JArray;
//                        var item = arrayObj[index];
//                        if (item == null)
//                            arrayObj.Add(new JObject());
//                    }

//                    Debug.Console(0, "IGNORING MISSING ARRAY VALUE FOR NOW");
//                    continue;
//                }
//                // Build the 
//                if (jpart[part] == null)
//                    jpart.Add(new JProperty(part, new JObject()));
//                jpart = jpart[part] as JContainer;
//            }
//            jpart.Replace(UnsavedValues[path]);