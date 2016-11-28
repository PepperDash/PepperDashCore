using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronDataStore;
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
		/// This should called from the ControlSystem Initiailize method. Will attempt to call
		/// CrestronDataStoreStatic.InitCrestronDataStore which may have been called elsewhere.
		/// </summary>
		public static void Initialize()
		{
			CrestronDataStoreStatic.InitCrestronDataStore();
			if (CrestronEnvironment.RuntimeEnvironment == eRuntimeEnvironment.SimplSharpPro)
			{
				// Add command to console
				CrestronConsole.AddNewConsoleCommand(SetDebugFromConsole, "appdebug",
					"appdebug:P [0-2]: Sets the application's console debug message level",
					ConsoleAccessLevelEnum.AccessOperator);
			}

			CrestronEnvironment.ProgramStatusEventHandler += CrestronEnvironment_ProgramStatusEventHandler;

			LoadMemory();
			Level = Contexts.GetOrCreateItem("DEFAULT").Level;
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
		/// Prints message to console if current debug level is equal to or higher than the level of this message.
		/// Uses CrestronConsole.PrintLine.
		/// </summary>
		/// <param name="level"></param>
		/// <param name="format">Console format string</param>
		/// <param name="items">Object parameters</param>
		public static void Console(uint level, string format, params object[] items)
		{
			if (Level >= level)
				CrestronConsole.PrintLine("App {0}:{1}", InitialParametersClass.ApplicationNumber,
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

		public enum ErrorLogLevel
		{
			Error, Warning, Notice, None
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

		static void LoadMemory()
		{
			if (File.Exists(GetMemoryFileName()))
			{
				using (StreamReader sr = new StreamReader(GetMemoryFileName()))
				{
					var json = sr.ReadToEnd();
					Contexts = JsonConvert.DeserializeObject<DebugContextCollection>(json);
				}
			}
			else
				Contexts = new DebugContextCollection();
		}

		/// <summary>
		/// Helper to get the file path for this app's debug memory
		/// </summary>
		static string GetMemoryFileName()
		{
			return string.Format(@"\NVRAM\debugSettings\program{0}", InitialParametersClass.ApplicationNumber);
		}
	}
}