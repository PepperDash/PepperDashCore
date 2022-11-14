using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.Reflection;
using Crestron.SimplSharp.CrestronLogger;
using Crestron.SimplSharp.CrestronIO;
using Newtonsoft.Json;
using PepperDash.Core.DebugThings;


namespace PepperDash.Core
{
    /// <summary>
    /// Contains debug commands for use in various situations
    /// </summary>
    public static class Debug
    {
        /// <summary>
        /// Describes the folder location where a given program stores it's debug level memory. By default, the
        /// file written will be named appNdebug where N is 1-10.
        /// </summary>
        public static string OldFilePathPrefix = @"\nvram\debug\";

        /// <summary>
        /// Describes the new folder location where a given program stores it's debug level memory. By default, the
        /// file written will be named appNdebug where N is 1-10.
        /// </summary>
        public static string NewFilePathPrefix = @"\user\debug\";

        /// <summary>
        /// The name of the file containing the current debug settings.
        /// </summary>
        public static string FileName = string.Format(@"app{0}Debug.json", InitialParametersClass.ApplicationNumber);

        /// <summary>
        /// Debug level to set for a given program.
        /// </summary>
        public static int Level { get; private set; }

        /// <summary>
        /// When this is true, the configuration file will NOT be loaded until triggered by either a console command or a signal
        /// </summary>
        public static bool DoNotLoadOnNextBoot { get; private set; }

        private static DebugContextCollection _contexts;

        private const int SaveTimeoutMs = 30000;

        /// <summary>
        /// Version for the currently loaded PepperDashCore dll
        /// </summary>
        public static string PepperDashCoreVersion { get; private set; } 

        private static CTimer _saveTimer;

		/// <summary>
		/// When true, the IncludedExcludedKeys dict will contain keys to include. 
		/// When false (default), IncludedExcludedKeys will contain keys to exclude.
		/// </summary>
		private static bool _excludeAllMode;

		//static bool ExcludeNoKeyMessages;

		private static readonly Dictionary<string, object> IncludedExcludedKeys;

        static Debug()
        {
            // Get the assembly version and print it to console and the log
            GetVersion();

            string msg = "";

            if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Appliance)
            {
                msg = string.Format("[App {0}] Using PepperDash_Core v{1}", InitialParametersClass.ApplicationNumber, PepperDashCoreVersion);
            }
            else if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Server)
            {
                msg = string.Format("[Room {0}] Using PepperDash_Core v{1}", InitialParametersClass.RoomId, PepperDashCoreVersion);
            }

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

            var context = _contexts.GetOrCreateItem("DEFAULT");
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

        private static void GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var ver =
                assembly
                    .GetCustomAttributes(typeof (AssemblyInformationalVersionAttribute), false);

            if (ver != null && ver.Length > 0)
            {
                var verAttribute = ver[0] as AssemblyInformationalVersionAttribute;

                if (verAttribute != null)
                {
                    PepperDashCoreVersion = verAttribute.InformationalVersion;
                }
            }
            else
            {
                var version = assembly.GetName().Version;
                PepperDashCoreVersion = string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build,
                    version.Revision);
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
                if (_saveTimer != null)
                {
                    _saveTimer.Stop();
                    _saveTimer = null;
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
					_excludeAllMode = false;
				}
				else if (lkey == "-all")
				{
					IncludedExcludedKeys.Clear();
					_excludeAllMode = true;
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
					string key;
					if (lkey.StartsWith("-"))
					{
						key = lkey.Substring(1);
						// if in exclude all mode, we need to remove this from the inclusions
						if (_excludeAllMode)
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
						if (_excludeAllMode)
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
                _contexts.GetOrCreateItem("DEFAULT").Level = level;
                SaveMemoryOnTimeout();

                CrestronConsole.PrintLine("[Application {0}], Debug level set to {1}",
                    InitialParametersClass.ApplicationNumber, Level);

                //var err = CrestronDataStoreStatic.SetLocalUintValue("DebugLevel", level);
                //if (err != CrestronDataStore.CDS_ERROR.CDS_SUCCESS)
                //    CrestronConsole.PrintLine("Error saving console debug level setting: {0}", err);
            }
        }

        /// <summary>
        /// sets the settings for a device or creates a new entry
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static void SetDeviceDebugSettings(string deviceKey, object settings)
        {
            _contexts.SetDebugSettingsForKey(deviceKey, settings);
            SaveMemoryOnTimeout();
        }

        /// <summary>
        /// Gets the device settings for a device by key or returns null
        /// </summary>
        /// <param name="deviceKey"></param>
        /// <returns></returns>
        public static object GetDeviceDebugSettingsForKey(string deviceKey)
        {
            return _contexts.GetDebugSettingsForKey(deviceKey);
        }  

        /// <summary>
        /// Sets the flag to prevent application starting on next boot
        /// </summary>
        /// <param name="state"></param>
        public static void SetDoNotLoadOnNextBoot(bool state)
        {
            DoNotLoadOnNextBoot = state;
            _contexts.GetOrCreateItem("DEFAULT").DoNotLoadOnNextBoot = state;
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
            if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Server)
            {
                var logString = string.Format("[level {0}] {1}", level, string.Format(format, items));

                LogError(ErrorLogLevel.Notice, logString);
                return;
            }

            if(Level < level) 
            {
                return;
            }

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

        /// <summary>
        /// Prints message to console if current debug level is equal to or higher than the level of this message. Always sends message to Error Log.
        /// Uses CrestronConsole.PrintLine.
        /// </summary>
        public static void Console(uint level, IKeyed dev, ErrorLogLevel errorLogLevel,
            string format, params object[] items)
        {
            var str = string.Format("[{0}] {1}", dev.Key, string.Format(format, items));
            if (errorLogLevel != ErrorLogLevel.None)
            {
                LogError(errorLogLevel, str);
            }
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
            if (errorLogLevel != ErrorLogLevel.None)
            {
                LogError(errorLogLevel, str);
            }
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

            var msg = CrestronEnvironment.DevicePlatform == eDevicePlatform.Appliance ? string.Format("App {0}:{1}", InitialParametersClass.ApplicationNumber, str) : string.Format("Room {0}:{1}", InitialParametersClass.RoomId, str);
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
            Console(0, "Saving debug settings");
            if (_saveTimer == null)
                _saveTimer = new CTimer(o =>
                {
                    _saveTimer = null;
                    SaveMemory();
                }, SaveTimeoutMs);
            else
                _saveTimer.Reset(SaveTimeoutMs);
        }

        /// <summary>
        /// Writes the memory - use SaveMemoryOnTimeout
        /// </summary>
        static void SaveMemory()
        {
            //var dir = @"\NVRAM\debug";
            //if (!Directory.Exists(dir))
            //    Directory.Create(dir);

            var fileName = GetMemoryFileName();

            Console(0, ErrorLogLevel.Notice, "Loading debug settings file from {0}", fileName);

            using (var sw = new StreamWriter(fileName))
            {
                var json = JsonConvert.SerializeObject(_contexts);
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
                using (var sr = new StreamReader(file))
                {
                    var json = sr.ReadToEnd();
                    _contexts = JsonConvert.DeserializeObject<DebugContextCollection>(json);

                    if (_contexts != null)
                    {
                        Console(1, "Debug memory restored from file");
                        return;
                    }
                }
            }

            _contexts = new DebugContextCollection();
        }

        /// <summary>
        /// Helper to get the file path for this app's debug memory
        /// </summary>
        static string GetMemoryFileName()
        {
            if (CrestronEnvironment.DevicePlatform == eDevicePlatform.Appliance)
            {
                CheckForMigration();
                return string.Format(@"\user\debugSettings\program{0}", InitialParametersClass.ApplicationNumber);
            }

            return string.Format("{0}{1}user{1}debugSettings{1}{2}.json",Directory.GetApplicationRootDirectory(), Path.DirectorySeparatorChar, InitialParametersClass.RoomId);
        }

        private static void CheckForMigration()
        {
            var oldFilePath = String.Format(@"\nvram\debugSettings\program{0}", InitialParametersClass.ApplicationNumber);
            var newFilePath = String.Format(@"\user\debugSettings\program{0}", InitialParametersClass.ApplicationNumber);

            //check for file at old path
            if (!File.Exists(oldFilePath))
            {
                Console(0, ErrorLogLevel.Notice,
                    String.Format(
                        @"Debug settings file migration not necessary. Using file at \user\debugSettings\program{0}",
                        InitialParametersClass.ApplicationNumber));
                return;
            }

            //create the new directory if it doesn't already exist
            if (!Directory.Exists(@"\user\debugSettings"))
            {
                Directory.CreateDirectory(@"\user\debugSettings");
            }

            Console(0, ErrorLogLevel.Notice,
                String.Format(
                    @"File found at \nvram\debugSettings\program{0}. Migrating to \user\debugSettings\program{0}", InitialParametersClass.ApplicationNumber));

            //Copy file from old path to new path, then delete it. This will overwrite the existing file
            File.Copy(oldFilePath, newFilePath, true);
            File.Delete(oldFilePath);

            //Check if the old directory is empty, then delete it if it is
            if (Directory.GetFiles(@"\nvram\debugSettings").Length > 0)
            {
                return;
            }

            Directory.Delete(@"\nvram\debugSettings");
        }

        /// <summary>
        /// Error level to for message to be logged at
        /// </summary>
        public enum ErrorLogLevel
        {
            /// <summary>
            /// Error
            /// </summary>
            Error, 
            /// <summary>
            /// Warning
            /// </summary>
            Warning, 
            /// <summary>
            /// Notice
            /// </summary>
            Notice, 
            /// <summary>
            /// None
            /// </summary>
            None,
        }
    }
}