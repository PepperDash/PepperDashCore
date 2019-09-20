using System;
//using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using PepperDash.Core.Config;

namespace PepperDash.Core.JsonToSimpl
{
    public class JsonToSimplPortalFileMaster : JsonToSimplMaster
    {
		/// <summary>
		///  Sets the filepath as well as registers this with the Global.Masters list
		/// </summary>
		public string PortalFilepath { get; private set; }

        public string ActualFilePath { get; private set; }

		/*****************************************************************************************/
		/** Privates **/

		// To prevent multiple same-file access
		object StringBuilderLock = new object();
		static object FileLock = new object();

		/*****************************************************************************************/

		/// <summary>
        /// SIMPL+ default constructor.
        /// </summary>
		public JsonToSimplPortalFileMaster()
        {
		}

		/// <summary>
		/// Read, evaluate and udpate status
		/// </summary>
		public void EvaluateFile(string portalFilepath)
		{
			PortalFilepath = portalFilepath;

			OnBoolChange(false, 0, JsonToSimplConstants.JsonIsValidBoolChange);
			if (string.IsNullOrEmpty(PortalFilepath))
			{
				CrestronConsole.PrintLine("Cannot evaluate file. JSON file path not set");
				return;
			}

			// Resolve possible wildcarded filename

			// If the portal file is xyz.json, then 
			// the file we want to check for first will be called xyz.local.json
			var localFilepath = Path.ChangeExtension(PortalFilepath, "local.json");
			Debug.Console(0, this, "Checking for local file {0}", localFilepath);
			var actualLocalFile = GetActualFileInfoFromPath(localFilepath);

			if (actualLocalFile != null)
			{
				ActualFilePath = actualLocalFile.FullName;
                OnStringChange(ActualFilePath, 0, JsonToSimplConstants.JsonActualFileChange);
			}
			// If the local file does not exist, then read the portal file xyz.json
			// and create the local.
			else
			{
				Debug.Console(1, this, "Local JSON file not found {0}\rLoading portal JSON file", localFilepath);
				var actualPortalFile = GetActualFileInfoFromPath(portalFilepath);
				if (actualPortalFile != null)
				{
					var newLocalPath = Path.ChangeExtension(actualPortalFile.FullName, "local.json");
					// got the portal file, hand off to the merge / save method				
					PortalConfigReader.ReadAndMergeFileIfNecessary(actualPortalFile.FullName, newLocalPath);
					ActualFilePath = newLocalPath;
				}
				else
				{
					var msg = string.Format("Portal JSON file not found: {0}", PortalFilepath);
					Debug.Console(1, this, msg);
					ErrorLog.Error(msg);
					return;
				}
			}

			// At this point we should have a local file.  Do it.
			Debug.Console(1, "Reading local JSON file {0}", ActualFilePath);

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

		/// <summary>
		/// Returns the FileInfo object for a given path, with possible wildcards
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		FileInfo GetActualFileInfoFromPath(string path)
		{
			var dir = Path.GetDirectoryName(path);
			var localFilename = Path.GetFileName(path);
			var directory = new DirectoryInfo(dir);
			// search the directory for the file w/ wildcards
			return directory.GetFiles(localFilename).FirstOrDefault();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="level"></param>
		public void setDebugLevel(int level)
		{
			Debug.SetDebugLevel(level);
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
