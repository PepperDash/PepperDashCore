using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronLogger;
using Crestron.SimplSharp.CrestronIO;
using Newtonsoft.Json;
using PepperDash.Core.DebugThings;


namespace PepperDash.Core
{
    public static class Debug
    {
        /// <summary>
        /// Describes the folder location where a given program stores it's debug level memory. By default, the
        /// file written will be named appNdebug where N is 1-10.
        /// </summary>
        public static string FilePathPrefix = @"\nvram\debug\";

        /// <summary>
        /// The name of the file containing the current debug settings.
        /// </summary>
        public static string FileName = string.Format(@"app{0}Debug.json", InitialParametersClass.ApplicationNumber);

        public static int Level { get; private set; }

        static DebugContextCollection Contexts;

        static int SaveTimeoutMs = 30000;

        static CTimer SaveTimer;

		/// <summary>
		/// When true, the IncludedExcludedKeys dict will contain keys to include. 
		/// When false (default), IncludedExcludedKeys will contain keys to exclude.
		/// </summary>
		static bool ExcludeAllMode;

		//static bool ExcludeNoKeyMessages;

		static Dictionary<string, object> IncludedExcludedKeys;

        static Debug()
        {
			IncludedExcludedKeys = new Dictionary<string, object>();

            //CrestronDataStoreStatic.InitCrestronDataStore();
            if (CrestronEnvironment.RuntimeEnvironment == eRuntimeEnvironment.SimplSharpPro)
            {
                // Add command to console
                CrestronConsole.AddNewConsoleCommand(SetDebugFromConsole, "appdebug",
                    "appdebug:P [0-2]: Sets the application's console debug message level",
                    ConsoleAccessLevelEnum.AccessOperator);
                CrestronConsole.AddNewConsoleCommand(ShowDebugLog, "appdebuglog",
                    "appdebuglog:P [all] Use \"all\" for full log.", 
                    ConsoleAccessLevelEnum.AccessOperator);
                CrestronConsole.AddNewConsoleCommand(s => CrestronLogger.Clear(false), "appdebugclear",
                    "appdebugclear:P Clears the current custom log", 
                    ConsoleAccessLevelEnum.AccessOperator);
				CrestronConsole.AddNewConsoleCommand(SetDebugFilterFromConsole, "appdebugfilter",
					"appdebugfilter [params]", ConsoleAccessLevelEnum.AccessOperator);
            }

            CrestronEnvironment.ProgramStatusEventHandler += CrestronEnvironment_ProgramStatusEventHandler;

            LoadMemory();
            Level = Contexts.GetOrCreateItem("DEFAULT").Level;

            CrestronLogger.Initialize(2, LoggerModeEnum.RM); // Use RM instead of DEFAULT as not to double-up console messages.
        }

        /// <summary>
        /// Used to save memory when shutting down
        /// </summary>
        /// <param name="programEventType"></param>
        static void CrestronEnvironment_ProgramStatusEventHandler(eProgramStatusEventType programEventType)
        {
            if (programEventType == eProgramStatusEventType.Stopping)
            {
                if (SaveTimer != null)
                {
                    SaveTimer.Stop();
                    SaveTimer = null;
                }
                Console(0, "Saving debug settings");
                SaveMemory();
            }
        }

        /// <summary>
        /// Callback for console command
        /// </summary>
        /// <param name="levelString"></param>
        public static void SetDebugFromConsole(string levelString)
        {
            try
            {
                if (string.IsNullOrEmpty(levelString.Trim()))
                {
                    CrestronConsole.PrintLine("AppDebug level = {0}", Level);
                    return;
                }

                SetDebugLevel(Convert.ToInt32(levelString));
            }
            catch
            {
                CrestronConsole.PrintLine("Usage: appdebug:P [0-2]");
            }
        }

		public static void SetDebugFilterFromConsole(string items)
		{
			var str = items.Trim();
			if (str == "?")
			{
				CrestronConsole.ConsoleCommandResponse("Usage:\r APPDEBUGFILTER key1 key2 key3....\r " +
					"+all: at beginning puts filter into 'default include' mode\r" +
					"      All keys that follow will be excluded from output.\r" +
					"-all: at beginning puts filter into 'default excluse all' mode.\r" +
					"      All keys that follow will be the only keys that are shown\r" +
					"+nokey: Enables messages with no key (default)\r" +
					"-nokey: Disables messages with no key.\r" +
					"(nokey settings are independent of all other settings)");
				return;
			}
			var keys = Regex.Split(str, @"\s*");
			foreach (var keyToken in keys)
			{
				var lkey = keyToken.ToLower();
				if (lkey == "+all")
				{
					IncludedExcludedKeys.Clear();
					ExcludeAllMode = false;
				}
				else if (lkey == "-all")
				{
					IncludedExcludedKeys.Clear();
					ExcludeAllMode = true;
				}
				//else if (lkey == "+nokey")
				//{
				//    ExcludeNoKeyMessages = false;
				//}
				//else if (lkey == "-nokey")
				//{
				//    ExcludeNoKeyMessages = true;
				//}
				else
				{
					string key = null; ;
					if (lkey.StartsWith("-"))
					{
						key = lkey.Substring(1);
						// if in exclude all mode, we need to remove this from the inclusions
						if (ExcludeAllMode)
						{
							if (IncludedExcludedKeys.ContainsKey(key))
								IncludedExcludedKeys.Remove(key);
						}
						// otherwise include all mode, add to the exclusions
						else
						{
							IncludedExcludedKeys[key] = new object();
						}
					}
					else if (lkey.StartsWith("+"))
					{
						key = lkey.Substring(1);
						// if in exclude all mode, we need to add this as inclusion
						if (ExcludeAllMode)
						{

							IncludedExcludedKeys[key] = new object();
						}
						// otherwise include all mode, remove this from exclusions
						else
						{
							if (IncludedExcludedKeys.ContainsKey(key))
								IncludedExcludedKeys.Remove(key);
						}
					}
				}
			}
		}


        /// <summary>
        /// Sets the debug level
        /// </summary>
        /// <param name="level"> Valid values 0 (no debug), 1 (critical), 2 (all messages)</param>
        public static void SetDebugLevel(int level)
        {
            if (level <= 2)
            {
                Level = level;
                Contexts.GetOrCreateItem("DEFAULT").Level = level;
                SaveMemoryOnTimeout();

                CrestronConsole.PrintLine("[Application {0}], Debug level set to {1}",
                    InitialParametersClass.ApplicationNumber, Level);

                //var err = CrestronDataStoreStatic.SetLocalUintValue("DebugLevel", level);
                //if (err != CrestronDataStore.CDS_ERROR.CDS_SUCCESS)
                //    CrestronConsole.PrintLine("Error saving console debug level setting: {0}", err);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void ShowDebugLog(string s)
        {
            var loglist = CrestronLogger.PrintTheLog(s.ToLower() == "all");
            foreach (var l in loglist)
                CrestronConsole.ConsoleCommandResponse(l + CrestronEnvironment.NewLine);
        }

        /// <summary>
        /// Prints message to console if current debug level is equal to or higher than the level of this message.
        /// Uses CrestronConsole.PrintLine.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="format">Console format string</param>
        /// <param name="items">Object parameters</param>
        public static void Console(uint level, string format, params object[] items)
        {
            if (Level >= level)
                CrestronConsole.PrintLine("[{0}]App {1}:{2}", DateTime.Now.ToString("HH:mm:ss.fff"), InitialParametersClass.ApplicationNumber,
                    string.Format(format, items));
        }

        /// <summary>
        /// Appends a device Key to the beginning of a message
        /// </summary>
        public static void Console(uint level, IKeyed dev, string format, params object[] items)
        {
            if (Level >= level)
                Console(level, "[{0}] {1}", dev.Key, string.Format(format, items));
        }

        public static void Console(uint level, IKeyed dev, ErrorLogLevel errorLogLevel,
            string format, params object[] items)
        {
            if (Level >= level)
            {
                var str = string.Format("[{0}] {1}", dev.Key, string.Format(format, items));
                Console(level, str);
                LogError(errorLogLevel, str);
            }
        }

        public static void Console(uint level, ErrorLogLevel errorLogLevel,
            string format, params object[] items)
        {
            if (Level >= level)
            {
                var str = string.Format(format, items);
                Console(level, str);
                LogError(errorLogLevel, str);
            }
        }

        /// <summary>
        /// Logs to both console and the custom user log (not the built-in error log). If appdebug level is set at
        /// or above the level provided, then the output will be written to both console and the log. Otherwise
        /// it will only be written to the log.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="items"></param>
        public static void ConsoleWithLog(uint level, string format, params object[] items)
        {
            var str = string.Format(format, items);
            if (Level >= level)
                CrestronConsole.PrintLine("App {0}:{1}", InitialParametersClass.ApplicationNumber, str);
            CrestronLogger.WriteToLog(str, level);
        }

        /// <summary>
        /// Logs to both console and the custom user log (not the built-in error log). If appdebug level is set at
        /// or above the level provided, then the output will be written to both console and the log. Otherwise
        /// it will only be written to the log.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="dev"></param>
        /// <param name="format">String.format string</param>
        /// <param name="items">Parameters for substitution in the format string.</param>
        public static void ConsoleWithLog(uint level, IKeyed dev, string format, params object[] items)
        {
            var str = string.Format(format, items);
            if (Level >= level)
                ConsoleWithLog(level, "[{0}] {1}", dev.Key, str);
        }

        /// <summary>
        /// Prints to log and error log
        /// </summary>
        /// <param name="errorLogLevel"></param>
        /// <param name="str"></param>
        public static void LogError(ErrorLogLevel errorLogLevel, string str)
        {
            string msg = string.Format("App {0}:{1}", InitialParametersClass.ApplicationNumber, str);
            switch (errorLogLevel)
            {
                case ErrorLogLevel.Error:
                    ErrorLog.Error(msg);
                    break;
                case ErrorLogLevel.Warning:
                    ErrorLog.Warn(msg);
                    break;
                case ErrorLogLevel.Notice:
                    ErrorLog.Notice(msg);
                    break;
            }
        }

        /// <summary>
        /// Writes the memory object after timeout
        /// </summary>
        static void SaveMemoryOnTimeout()
        {
            if (SaveTimer == null)
                SaveTimer = new CTimer(o =>
                {
                    SaveTimer = null;
                    SaveMemory();
                }, SaveTimeoutMs);
            else
                SaveTimer.Reset(SaveTimeoutMs);
        }

        /// <summary>
        /// Writes the memory - use SaveMemoryOnTimeout
        /// </summary>
        static void SaveMemory()
        {
            //var dir = @"\NVRAM\debug";
            //if (!Directory.Exists(dir))
            //    Directory.Create(dir);

            using (StreamWriter sw = new StreamWriter(GetMemoryFileName()))
            {
                var json = JsonConvert.SerializeObject(Contexts);
                sw.Write(json);
                sw.Flush();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        static void LoadMemory()
        {
            var file = GetMemoryFileName();
            if (File.Exists(file))
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    var json = sr.ReadToEnd();
                    Contexts = JsonConvert.DeserializeObject<DebugContextCollection>(json);

                    if (Contexts != null)
                    {
                        Debug.Console(1, "Debug memory restored from file");
                        return;
                    }
                }
            }

            Contexts = new DebugContextCollection();
        }

        /// <summary>
        /// Helper to get the file path for this app's debug memory
        /// </summary>
        static string GetMemoryFileName()
        {
            return string.Format(@"\NVRAM\debugSettings\program{0}", InitialParametersClass.ApplicationNumber);
        }

        public enum ErrorLogLevel
        {
            Error, Warning, Notice, None
        }
    }
}