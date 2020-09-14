using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Reflection;
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

        /// <summary>
        /// Current debgu level
        /// </summary>
        public static int Level { get; private set; }

        /// <summary>
        /// Indicates if config should be loaded on next boot
        /// </summary>
        public static bool DoNotLoadOnNextBoot { get; private set; }

        static DebugContextCollection Contexts;

        static int SaveTimeoutMs = 30000;

        /// <summary>
        /// Default debug timeout (30 min)
        /// </summary>
        public static long DebugTimoutMs = 1800000;

        /// <summary>
        /// Current version string
        /// </summary>
        public static string PepperDashCoreVersion { get; private set; } 

        static CTimer SaveTimer;

        /// <summary>
        /// Resets the debug level to 0 when the timer expires
        /// </summary>
        static CTimer DebugTimer;

		/// <summary>
		/// When true, the IncludedExcludedKeys dict will contain keys to include. 
		/// When false (default), IncludedExcludedKeys will contain keys to exclude.
		/// </summary>
		static bool ExcludeAllMode;

		//static bool ExcludeNoKeyMessages;

		static Dictionary<string, object> IncludedExcludedKeys;

        static Debug()
        {
            // Get the assembly version and print it to console and the log
            var version = Assembly.GetExecutingAssembly().GetName().Version;

            PepperDashCoreVersion = string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);

            var msg = string.Format("[App {0}] Using PepperDash_Core v{1}", InitialParametersClass.ApplicationNumber, PepperDashCoreVersion);

            CrestronConsole.PrintLine(msg);

            LogError(ErrorLogLevel.Notice, msg);

			IncludedExcludedKeys = new Dictionary<string, object>();

            //CrestronDataStoreStatic.InitCrestronDataStore();
            if (CrestronEnvironment.RuntimeEnvironment == eRuntimeEnvironment.SimplSharpPro)
            {
                // Add command to console
                CrestronConsole.AddNewConsoleCommand(SetDoNotLoadOnNextBootFromConsole, "donotloadonnextboot", 
                    "donotloadonnextboot:P [true/false]: Should the application load on next boot", ConsoleAccessLevelEnum.AccessOperator);

                CrestronConsole.AddNewConsoleCommand(SetDebugFromConsole, "appdebug",
                    "appdebug:P [level 0-2] [(min)]: Set the console debug message level",
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

            var context = Contexts.GetOrCreateItem("DEFAULT");
            Level = context.Level;
            DoNotLoadOnNextBoot = context.DoNotLoadOnNextBoot;

            if(DoNotLoadOnNextBoot)
                CrestronConsole.PrintLine(string.Format("Program {0} will not load config after next boot.  Use console command go:{0} to load the config manually", InitialParametersClass.ApplicationNumber));

            try
            {
                if (InitialParametersClass.NumberOfRemovableDrives > 0)
                {
                    CrestronConsole.PrintLine("{0} RM Drive(s) Present.", InitialParametersClass.NumberOfRemovableDrives);
                    CrestronLogger.Initialize(2, LoggerModeEnum.DEFAULT); // Use RM instead of DEFAULT as not to double-up console messages.
                }
                else
                    CrestronConsole.PrintLine("No RM Drive(s) Present.");
            }
            catch (Exception e)
            {

                CrestronConsole.PrintLine("Initializing of CrestronLogger failed: {0}", e);
            }
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
        /// Callback for console command.  Starts the debug Timer
        /// </summary>
        /// <param name="levelString"></param>
        public static void SetDebugFromConsole(string levelString)
        {
            try
            {
                var args = levelString.Split(' ');

                var level = Convert.ToInt32(args[0]);
                var timeoutMs = DebugTimoutMs;

                if(args.Length > 1)
                {
                    timeoutMs = Convert.ToInt32(args[1]) * 60000;
                }


                // Only start the timer if setting to level 1 or 2
                if (level > 0 && level <= 2)
                {
                    if (DebugTimer != null)
                    {
                        DebugTimer = new CTimer((o) => SetDebugLevel(0), timeoutMs);
                    }
                    else
                    {
                        DebugTimer.Reset(timeoutMs);
                    }
                }


                if (string.IsNullOrEmpty(levelString.Trim()))
                {
                    CrestronConsole.PrintLine("AppDebug level = {0}", Level);
                    return;
                }

                SetDebugLevel(level, timeoutMs);
            }
            catch
            {
                CrestronConsole.PrintLine("Usage: appdebug:P [level: 0-2] [(timeout in minutes: 0-480)]");
            }
        }

        /// <summary>
        /// Callback for console command
        /// </summary>
        /// <param name="stateString"></param>
        public static void SetDoNotLoadOnNextBootFromConsole(string stateString)
        {
            try
            {
                if (string.IsNullOrEmpty(stateString.Trim()))
                {
                    CrestronConsole.PrintLine("DoNotLoadOnNextBoot = {0}", DoNotLoadOnNextBoot);
                    return;
                }

                SetDoNotLoadOnNextBoot(Boolean.Parse(stateString));
            }
            catch
            {
                CrestronConsole.PrintLine("Usage: donotloadonnextboot:P [true/false]");
            }
        }

        /// <summary>
        /// Callback for console command
        /// </summary>
        /// <param name="items"></param>
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
                if (level == 0)
                {
                    DebugTimer.Stop();
                    DebugTimer.Dispose();
                    DebugTimer = null;
                }

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
        /// Sets the debug level (Default timeout is 30 minutes)
        /// </summary>
        /// <param name="level"> Valid values 0 (no debug), 1 (critical), 2 (all messages)</param>
        /// <param name="timeoutMs">Timeout value in milliseconds </param>
        public static void SetDebugLevel(int level, long timeoutMs)
        {
            if (level <= 2 && level > 0)
            {
                CrestronConsole.PrintLine("[Application {0}], Debug timer will expire in {1} minutes",
                    InitialParametersClass.ApplicationNumber, timeoutMs / 60000);
            }

            SetDebugLevel(level);
        }

        /// <summary>
        /// sets the settings for a device or creates a new entry
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static void SetDeviceDebugSettings(string deviceKey, object settings)
        {
            Contexts.SetDebugSettingsForKey(deviceKey, settings);
            SaveMemoryOnTimeout();
        }

        /// <summary>
        /// Gets the device settings for a device by key or returns null
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <returns></returns>
        public static object GetDeviceDebugSettingsForKey(string deviceKey)
        {
            return Contexts.GetDebugSettingsForKey(deviceKey);
        }  

        /// <summary>
        /// Sets the flag to prevent application starting on next boot
        /// </summary>
        /// <param name="state"></param>
        public static void SetDoNotLoadOnNextBoot(bool state)
        {
            DoNotLoadOnNextBoot = state;
            Contexts.GetOrCreateItem("DEFAULT").DoNotLoadOnNextBoot = state;
            SaveMemoryOnTimeout();

            CrestronConsole.PrintLine("[Application {0}], Do Not Start on Next Boot set to {1}",
                InitialParametersClass.ApplicationNumber, DoNotLoadOnNextBoot);
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
		/// Logs to Console when at-level, and all messages to error log, including device key			
        /// </summary>
        public static void Console(uint level, IKeyed dev, string format, params object[] items)
        {
            if (Level >= level)
                Console(level, "[{0}] {1}", dev.Key, string.Format(format, items));
        }

        public static void Console(uint level, IKeyed dev, ErrorLogLevel errorLogLevel,
            string format, params object[] items)
        {
            var str = string.Format("[{0}] {1}", dev.Key, string.Format(format, items));
			LogError(errorLogLevel, str);
            if (Level >= level)
            {
                Console(level, str);
            }
        }

		/// <summary>
		/// Logs to Console when at-level, and all messages to error log
		/// </summary>
        public static void Console(uint level, ErrorLogLevel errorLogLevel,
            string format, params object[] items)
        {
            var str = string.Format(format, items);
            LogError(errorLogLevel, str);
			if (Level >= level)
			{
				Console(level, str);
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