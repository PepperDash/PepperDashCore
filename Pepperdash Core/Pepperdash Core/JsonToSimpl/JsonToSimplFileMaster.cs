using System;
//using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PepperDash.Core.JsonToSimpl
{
	public class JsonToSimplFileMaster : JsonToSimplMaster
	{
		/// <summary>
		///  Sets the filepath as well as registers this with the Global.Masters list
		/// </summary>
		public string Filepath { get; private set; }

		public string ActualFilePath { get; private set; }

		// TODO: pdc-20: added to return filename back to SIMPL
		public string Filename { get; private set; }
		public string FilePathName { get; private set; }

		/*****************************************************************************************/
		/** Privates **/


		// The JSON file in JObject form
		// For gathering the incoming data
		object StringBuilderLock = new object();
		// To prevent multiple same-file access
		static object FileLock = new object();

		/*****************************************************************************************/

		/// <summary>
		/// SIMPL+ default constructor.
		/// </summary>
		public JsonToSimplFileMaster()
		{
		}

		/// <summary>
		/// Read, evaluate and udpate status
		/// </summary>
		public void EvaluateFile(string filepath)
		{
			Filepath = filepath;

			OnBoolChange(false, 0, JsonToSimplConstants.JsonIsValidBoolChange);
			if (string.IsNullOrEmpty(Filepath))
			{
				CrestronConsole.PrintLine("Cannot evaluate file. JSON file path not set");
				return;
			}

			// Resolve wildcard
			var dir = Path.GetDirectoryName(Filepath);
			Debug.Console(1, "Checking directory {0}", dir);
			var fileName = Path.GetFileName(Filepath);
			var directory = new DirectoryInfo(dir);

			var actualFile = directory.GetFiles(fileName).FirstOrDefault();
			if (actualFile == null)
			{
				var msg = string.Format("JSON file not found: {0}", Filepath);
				CrestronConsole.PrintLine(msg);
				ErrorLog.Error(msg);
				return;
			}

			// \xSE\xR\PDT000-Template_Main_Config-Combined_DSP_v00.02.json
			// \USER\PDT000-Template_Main_Config-Combined_DSP_v00.02.json
			ActualFilePath = actualFile.FullName;
			OnStringChange(ActualFilePath, 0, JsonToSimplConstants.ActualFilePathChange);
			Debug.Console(1, "Actual JSON file is {0}", ActualFilePath);

			// TODO: pdc-20: added to retrun filename to SIMPL
			Filename = actualFile.Name;
			OnStringChange(Filename, 0, JsonToSimplConstants.FilenameResolvedChange);
			Debug.Console(1, "JSON Filename is {0}", Filename);

			// TODO: pdc-20: added to return the file path to SIMPL
			FilePathName = string.Format(@"{0}\", actualFile.DirectoryName);
			OnStringChange(FilePathName, 0, JsonToSimplConstants.FilePathResolvedChange);
			Debug.Console(1, "JSON File Path is {0}", FilePathName);

			string json = File.ReadToEnd(ActualFilePath, System.Text.Encoding.ASCII);

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
				return;
			}
		}
		public void setDebugLevel(int level)
		{
			Debug.SetDebugLevel(level);
		}
		public override void Save()
		{
			// this code is duplicated in the other masters!!!!!!!!!!!!!
			UnsavedValues = new Dictionary<string, JValue>();
			// Make each child update their values into master object
			foreach (var child in Children)
			{
				Debug.Console(1, "Master [{0}] checking child [{1}] for updates to save", UniqueID, child.Key);
				child.UpdateInputsForMaster();
			}

			if (UnsavedValues == null || UnsavedValues.Count == 0)
			{
				Debug.Console(1, "Master [{0}] No updated values to save. Skipping", UniqueID);
				return;
			}
			lock (FileLock)
			{
				Debug.Console(1, "Saving");
				foreach (var path in UnsavedValues.Keys)
				{
					var tokenToReplace = JsonObject.SelectToken(path);
					if (tokenToReplace != null)
					{// It's found
						tokenToReplace.Replace(UnsavedValues[path]);
						Debug.Console(1, "JSON Master[{0}] Updating '{1}'", UniqueID, path);
					}
					else // No token.  Let's make one 
					{
						//http://stackoverflow.com/questions/17455052/how-to-set-the-value-of-a-json-path-using-json-net
						Debug.Console(1, "JSON Master[{0}] Cannot write value onto missing property: '{1}'", UniqueID, path);

						//                        JContainer jpart = JsonObject;
						//                        // walk down the path and find where it goes
						//#warning Does not handle arrays.
						//                        foreach (var part in path.Split('.'))
						//                        {

						//                            var openPos = part.IndexOf('[');
						//                            if (openPos > -1)
						//                            {
						//                                openPos++; // move to number
						//                                var closePos = part.IndexOf(']');
						//                                var arrayName = part.Substring(0, openPos - 1); // get the name
						//                                var index = Convert.ToInt32(part.Substring(openPos, closePos - openPos));

						//                                // Check if the array itself exists and add the item if so
						//                                if (jpart[arrayName] != null)
						//                                {
						//                                    var arrayObj = jpart[arrayName] as JArray;
						//                                    var item = arrayObj[index];
						//                                    if (item == null)
						//                                        arrayObj.Add(new JObject());
						//                                }

						//                                Debug.Console(0, "IGNORING MISSING ARRAY VALUE FOR NOW");
						//                                continue;
						//                            }
						//                            // Build the 
						//                            if (jpart[part] == null)
						//                                jpart.Add(new JProperty(part, new JObject()));
						//                            jpart = jpart[part] as JContainer;
						//                        }
						//                        jpart.Replace(UnsavedValues[path]);
					}
				}
				using (StreamWriter sw = new StreamWriter(ActualFilePath))
				{
					try
					{
						sw.Write(JsonObject.ToString());
						sw.Flush();
					}
					catch (Exception e)
					{
						string err = string.Format("Error writing JSON file:\r{0}", e);
						Debug.Console(0, err);
						ErrorLog.Warn(err);
						return;
					}
				}
			}
		}
	}
}
