using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronDataStore;
using Crestron.SimplSharp.CrestronLogger;
using PepperDash.Core.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Json;
using Serilog.Templates;

namespace PepperDash.Core
{
    /// <summary>
    /// Contains debug commands for use in various situations
    /// </summary>
    public static class Debug
    {
        public const string DoNotLoadConfigOnNextBootKey = "DoNotLoadConfigOnNextBoot";
        public const string ConsoleLevelStoreKey = "ConsoleDebugLevel";
        public const string WebSocketLevelStoreKey = "WebsocketDebugLevel";
        public const string ErrorLogLevelStoreKey = "ErrorLogDebugLevel";
        public const string FileLevelStoreKey = "FileDebugLevel";

        private static readonly Dictionary<int, LogEventLevel> LogLevels = new()
        {
            {0, LogEventLevel.Information },
            {3, LogEventLevel.Warning },
            {4, LogEventLevel.Error },
            {5, LogEventLevel.Fatal },
            {1, LogEventLevel.Debug },
            {2, LogEventLevel.Verbose },
        };

        private static readonly LoggingLevelSwitch ConsoleLoggingLevelSwitch = new();

        private static readonly LoggingLevelSwitch WebsocketLoggingLevelSwitch = new(LogEventLevel.Debug);

        private static readonly LoggingLevelSwitch ErrorLogLevelSwitch = new(LogEventLevel.Error);

        private static readonly LoggingLevelSwitch FileLevelSwitch = new(LogEventLevel.Debug);

        /// <summary>
        /// When this is true, the configuration file will NOT be loaded until triggered by either a console command or a signal
        /// </summary>
        public static bool DoNotLoadConfigOnNextBoot =>
            CrestronDataStoreStatic.GetLocalBoolValue(DoNotLoadConfigOnNextBootKey, out var value) switch
            {
                CrestronDataStore.CDS_ERROR.CDS_SUCCESS => value,
                _ => false
            };

        /// <summary>
        /// Version for the currently loaded PepperDashCore dll
        /// </summary>
        public static string PepperDashCoreVersion { get; }

        public static string WebsocketUrl { get; } = "";

        public static LogEventLevel WebsocketMinimumLevel => WebsocketLoggingLevelSwitch.MinimumLevel;
        public static LogEventLevel ConsoleMinimumLevel => ConsoleLoggingLevelSwitch.MinimumLevel;
        public static LogEventLevel ErrorLogMinimumLevel => ErrorLogLevelSwitch.MinimumLevel;
        public static LogEventLevel FileLogMinimumLevel => FileLevelSwitch.MinimumLevel;

        public static readonly LoggerConfiguration DefaultLoggerConfiguration;

        public static readonly bool IsRunningOnAppliance = CrestronEnvironment.DevicePlatform == eDevicePlatform.Appliance;

        static Debug()
        {
            CrestronDataStoreStatic.InitCrestronDataStore();
            var directorySeparator = Path.DirectorySeparatorChar.ToString();

            var logFilePath = CrestronEnvironment.DevicePlatform == eDevicePlatform.Appliance 
                ? Path.Combine(directorySeparator, "user", "program" + InitialParametersClass.ApplicationNumber, "logs", "global-log.log")
                : Path.Combine(directorySeparator, "User", "logs", "global-log.log");

            CrestronConsole.PrintLine($"Saving log files to {logFilePath}");

            const string consoleTemplate =
                "[{@t:yyyy-MM-dd HH:mm:ss.fff}][{@l:u4}][{App}]{#if Key is not null}[{Key}]{#end} {@m}{#if @x is not null}\r\n{@x}{#end}";

            var errorLogTemplate = CrestronEnvironment.DevicePlatform == eDevicePlatform.Appliance 
                ? "{@t:fff}ms [{@l:u4}]{#if Key is not null}[{Key}]{#end} {@m}{#if @x is not null}\r\n{@x}{#end}"
                : "[{@t:yyyy-MM-dd HH:mm:ss.fff}][{@l:u4}][{App}]{#if Key is not null}[{Key}]{#end} {@m}{#if @x is not null}\r\n{@x}{#end}";

            DefaultLoggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .Enrich.With(new CrestronEnricher())
                .WriteTo.DebugConsoleSink(new ExpressionTemplate(template: consoleTemplate), levelSwitch: ConsoleLoggingLevelSwitch)
                .WriteTo.Sink(new DebugErrorLogSink(new ExpressionTemplate(template: errorLogTemplate)), levelSwitch: ErrorLogLevelSwitch)
                .WriteTo.File(new RenderedCompactJsonFormatter(), logFilePath,                                    
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: LogEventLevel.Debug,
                    retainedFileCountLimit: CrestronEnvironment.DevicePlatform == eDevicePlatform.Appliance ? 30 : 60,
                    levelSwitch: FileLevelSwitch
                );

#if NET472
            var certPath = IsRunningOnAppliance ? Path.Combine("/", "user") : Path.Combine("/", "User");
            var websocket = new DebugNet472WebSocket(certPath);
            WebsocketUrl = websocket.Url;
            DefaultLoggerConfiguration.WriteTo.Sink(new DebugWebsocketSink(websocket, new JsonFormatter(renderMessage: true)), levelSwitch: WebsocketLoggingLevelSwitch);
#endif

            var supportsRemovableDrive = false;
            try
            {
                CrestronLogger.Initialize(1, LoggerModeEnum.RM);
                supportsRemovableDrive = true;
            }
            catch (Exception)
            {
                CrestronConsole.PrintLine("No RM Drive(s) Present. Not using Crestron Logger");
            }

            if (supportsRemovableDrive)
                DefaultLoggerConfiguration.WriteTo.Sink(new DebugCrestronLoggerSink());

            var storedConsoleLevel = DebugContext.GetOrCreateDataForKey(ConsoleLevelStoreKey, LogEventLevel.Information);
            ConsoleLoggingLevelSwitch.MinimumLevel = storedConsoleLevel.Level;
            CrestronConsole.PrintLine("Beginning console logging with level:{0}", storedConsoleLevel.Level);

            Log.Logger = DefaultLoggerConfiguration.CreateLogger();
            PepperDashCoreVersion = GetVersion();
            LogMessage(LogEventLevel.Information, "Using PepperDash_Core v{PepperDashCoreVersion}", PepperDashCoreVersion);

            if (CrestronEnvironment.RuntimeEnvironment == eRuntimeEnvironment.SimplSharpPro)
            {
                CrestronConsole.AddNewConsoleCommand(SetDoNotLoadOnNextBootFromConsole, "donotloadonnextboot", 
                    "donotloadonnextboot:P [true/false]: Should the application load on next boot", ConsoleAccessLevelEnum.AccessOperator);

                CrestronConsole.AddNewConsoleCommand(SetDebugFromConsole, "appdebug",
                    "appdebug:[p] [0-5] --devices [devices]: Sets console debug message level",
                    ConsoleAccessLevelEnum.AccessOperator);

                CrestronConsole.AddNewConsoleCommand(ShowDebugLog, "appdebuglog",
                    "appdebuglog:P [all] Use \"all\" for full log.", 
                    ConsoleAccessLevelEnum.AccessOperator);
            }

            if(DoNotLoadConfigOnNextBoot)
                CrestronConsole.PrintLine(string.Format("Program {0} will not load config after next boot.  Use console command go:{0} to load the config manually", InitialParametersClass.ApplicationNumber));
        }

        private static string GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var ver =
                assembly
                    .GetCustomAttributes(typeof (AssemblyInformationalVersionAttribute), false);

            if (ver.Length > 0 && ver[0] is AssemblyInformationalVersionAttribute verAttribute)
            {
                return verAttribute.InformationalVersion;
            }

            var name = assembly.GetName();
            return name.Version?.ToString() ?? "0.0.0";
        }

        private static void SetDebugFromConsole(string levelString)
        {
            try
            {
                if (levelString.Trim() == "?")
                {
                    CrestronConsole.ConsoleCommandResponse(
                        $"""
                         Used to set the minimum level of debug messages to be printed to the console:
                         {LogLevels[0]} = 0
                         {LogLevels[1]} = 1
                         {LogLevels[2]} = 2
                         {LogLevels[3]} = 3
                         {LogLevels[4]} = 4
                         {LogLevels[5]} = 5
                         """);
                    return;
                }

                if (string.IsNullOrEmpty(levelString.Trim()))
                {
                    CrestronConsole.ConsoleCommandResponse("AppDebug level = {0}", ConsoleLoggingLevelSwitch.MinimumLevel);
                    return;
                }

                const string pattern = @"^(\d+)(?:\s+--devices\s+([\w\s,-]+))?$";
                var          match   = Regex.Match(levelString, pattern);
                if (match.Success)
                {
                    var level = int.Parse(match.Groups[1].Value);
                    var devices = match.Groups[2].Success
                        ? match.Groups[2].Value.Split(',')
                            .Select(device => device.Trim())
                            .ToArray()
                        : [];


                    if (LogLevels.TryGetValue(level, out var logEventLevel))
                    {
                        if (devices.Length == 1 && devices[0].ToLower() == "all")
                        {
                            SetConsoleDebugLevel(logEventLevel, true, []);
                        }
                        else
                        {
                            SetConsoleDebugLevel(logEventLevel, false, devices);
                        }
                    }
                    else
                    {
                        CrestronConsole.ConsoleCommandResponse($"Error: Unable to parse {levelString} to valid log level");
                    }
                }
                else
                {
                    CrestronConsole.ConsoleCommandResponse($"Error: Unable to parse {levelString} to valid log level:does not match the pattern");
                }
            }          
            catch
            {
                CrestronConsole.ConsoleCommandResponse("Usage: appdebug:P [0-5] --devices [deviceKeys]");
            }
        }

        public static void SetConsoleDebugLevel(LogEventLevel level, bool logAllDevices = false, string[] includedDevices = null)
        {
            ConsoleLoggingLevelSwitch.MinimumLevel = level;
            DebugContext.SetDataForKey(ConsoleLevelStoreKey, level, logAllDevices, includedDevices);

            var includedDevicesMessage = includedDevices == null || includedDevices.Length == 0 ? "all" : string.Join(",", includedDevices);
            CrestronConsole.ConsoleCommandResponse($"Success: set console debug level to {level} with devices:{includedDevicesMessage}");

        }

        public static void SetWebSocketMinimumDebugLevel(LogEventLevel level)
        {
            WebsocketLoggingLevelSwitch.MinimumLevel = level;            
            LogMessage(LogEventLevel.Information, "Websocket debug level set to {0}", WebsocketLoggingLevelSwitch.MinimumLevel);
        }

        public static void SetErrorLogMinimumDebugLevel(LogEventLevel level)
        {
            ErrorLogLevelSwitch.MinimumLevel = level;
            LogMessage(LogEventLevel.Information, "Error log debug level set to {0}", WebsocketLoggingLevelSwitch.MinimumLevel);
        }

        public static void SetFileMinimumDebugLevel(LogEventLevel level)
        {
            ErrorLogLevelSwitch.MinimumLevel = level;
            LogMessage(LogEventLevel.Information, "File debug level set to {0}", WebsocketLoggingLevelSwitch.MinimumLevel);
        }

        public static void SetDebugLevel(uint level)
        {
            if (LogLevels.TryGetValue((int)level, out var logEventLevel))
            {
                SetConsoleDebugLevel(logEventLevel);
            }
            else
            {
                CrestronConsole.ConsoleCommandResponse($"Error: Unable to parse {level} to valid log level");
            }
        }

        /// <summary>
        /// Callback for console command
        /// </summary>
        /// <param name="stateString"></param>
        private static void SetDoNotLoadOnNextBootFromConsole(string stateString)
        {
            try
            {
                if (string.IsNullOrEmpty(stateString.Trim()))
                {
                    CrestronConsole.ConsoleCommandResponse("DoNotLoadOnNextBoot = {0}", DoNotLoadConfigOnNextBoot);
                    return;
                }

                SetDoNotLoadConfigOnNextBoot(bool.Parse(stateString));
            }
            catch
            {
                CrestronConsole.ConsoleCommandResponse("Usage: donotloadonnextboot:P [true/false]");
            }
        }

        /// <summary>
        /// Sets the flag to prevent application starting on next boot
        /// </summary>
        /// <param name="state"></param>
        public static void SetDoNotLoadConfigOnNextBoot(bool state)
        {
            CrestronDataStoreStatic.SetLocalBoolValue(DoNotLoadConfigOnNextBootKey, state);
            CrestronConsole.ConsoleCommandResponse("[Application {0}], Do Not Load Config on Next Boot set to {1}", InitialParametersClass.ApplicationNumber, DoNotLoadConfigOnNextBoot);
        }

        /// <summary>
        /// Prints message to console if current debug level is equal to or higher than the level of this message.
        /// Uses CrestronConsole.PrintLine.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="format">Console format string</param>
        /// <param name="items">Object parameters</param>
        [Obsolete("Use LogMessage methods")]
        public static void Console(uint level, string format, params object[] items)
        {
            var logLevel = LogLevels.TryGetValue((int)level, out var value) ? value : LogEventLevel.Information;
            LogMessage(logLevel, format, items);

            //if (IsRunningOnAppliance)
            //{
            //    CrestronConsole.PrintLine("[{0}]App {1} Lvl {2}:{3}", DateTime.Now.ToString("HH:mm:ss.fff"),
            //        InitialParametersClass.ApplicationNumber,
            //        level,
            //        string.Format(format, items));
            //}
        }

        /// <summary>
		/// Logs to Console when at-level, and all messages to error log, including device key			
        /// </summary>
        [Obsolete("Use LogMessage methods")]
        public static void Console(uint level, IKeyed dev, string format, params object[] items)
        {
            var logLevel = LogLevels.TryGetValue((int)level, out var value) ? value : LogEventLevel.Information;
            LogMessage(logLevel, dev, format, items);

            //if (Level >= level)
            //    Console(level, "[{0}] {1}", dev.Key, message);
        }

        /// <summary>
        /// Prints message to console if current debug level is equal to or higher than the level of this message. Always sends message to Error Log.
        /// Uses CrestronConsole.PrintLine.
        /// </summary>
        [Obsolete("Use LogMessage methods")]
        public static void Console(uint level, IKeyed dev, ErrorLogLevel errorLogLevel, string format, params object[] items)
        {
            var logLevel = LogLevels.TryGetValue((int)level, out var value) ? value : LogEventLevel.Information;
            LogMessage(logLevel, dev, format, items);
        }

        /// <summary>
        /// Logs to Console when at-level, and all messages to error log
        /// </summary>
        [Obsolete("Use LogMessage methods")]
        public static void Console(uint level, ErrorLogLevel errorLogLevel, string format, params object[] items)
        {
            var logLevel = LogLevels.TryGetValue((int)level, out var value) ? value : LogEventLevel.Information;
            LogMessage(logLevel, format, items);
        }

        /// <summary>
        /// Logs to both console and the custom user log (not the built-in error log). If appdebug level is set at
        /// or above the level provided, then the output will be written to both console and the log. Otherwise
        /// it will only be written to the log.
        /// </summary>
        [Obsolete("Use LogMessage methods")]
        public static void ConsoleWithLog(uint level, string format, params object[] items)
        {
            var logLevel = LogLevels.TryGetValue((int)level, out var value) ? value : LogEventLevel.Information;
            LogMessage(logLevel, format, items);

            // var str = string.Format(format, items);
            //if (Level >= level)
            //    CrestronConsole.PrintLine("App {0}:{1}", InitialParametersClass.ApplicationNumber, str);
            // CrestronLogger.WriteToLog(str, level);
        }

        /// <summary>
        /// Logs to both console and the custom user log (not the built-in error log). If appdebug level is set at
        /// or above the level provided, then the output will be written to both console and the log. Otherwise
        /// it will only be written to the log.
        /// </summary>
        [Obsolete("Use LogMessage methods")]
        public static void ConsoleWithLog(uint level, IKeyed dev, string format, params object[] items)
        {
            var logLevel = LogLevels.TryGetValue((int)level, out var value) ? value : LogEventLevel.Information;
            LogMessage(logLevel, dev, format, items);

            // var str = string.Format(format, items);
            // CrestronLogger.WriteToLog(string.Format("[{0}] {1}", dev.Key, str), level);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void ShowDebugLog(string s)
        {
            var logList = CrestronLogger.PrintTheLog(s.ToLower() == "all");
            foreach (var l in logList)
                CrestronConsole.ConsoleCommandResponse(l + CrestronEnvironment.NewLine);
        }

        public static ILogger CreateLoggerForDevice<T>(T device) where T : IKeyed =>
            Log.ForContext<T>().ForContext("Key", device?.Key);

        //Message
        public static void LogMessage(LogEventLevel level, string message, params object[] args)
        {
            Log.Write(level, message, args);
        }

        public static void LogMessage(LogEventLevel level, IKeyed device, string message, params object[] args)
        {
            Log.ForContext("Key", device?.Key).Write(level, message, args);
        }

        public static void LogMessage(LogEventLevel level, IKeyed device, Exception ex, string message, params object[] args)
        {
            Log.ForContext("Key", device?.Key).Write(level, ex, message, args);
        }

        public static void LogMessage<T>(LogEventLevel level, IKeyed device, string message, params object[] args)
        {
            Log.ForContext("Key", device?.Key).ForContext<T>().Write(level, message, args);
        }

        public static void LogMessage<T>(LogEventLevel level, string message, params object[] args)
        {
            Log.ForContext<T>().Write(level, message, args);
        }

        public static void LogMessage<T>(LogEventLevel level, IKeyed device, Exception ex, string message, params object[] args)
        {
            Log.ForContext<T>().ForContext("Key", device?.Key).Write(level, ex, message, args);
        }

        //Error
        public static void LogError(string message, params object[] args)
        {
            Log.Error(message, args);
        }

        public static void LogError(Exception ex, string message, params object[] args)
        {
            Log.Error(ex, message, args);
        }

        public static void LogError<T>(string message, params object[] args)
        {
            Log.ForContext<T>().Error(message, args);
        }

        public static void LogError<T>(Exception ex, string message, params object[] args)
        {
            Log.ForContext<T>().Error(ex, message, args);
        }

        public static void LogError(IKeyed device, string message, params object[] args)
        {
            Log.ForContext("Key", device?.Key).Error(message, args);
        }

        public static void LogError(IKeyed device, Exception ex, string message, params object[] args)
        {
            Log.ForContext("Key", device?.Key).Error(ex, message, args);
        }

        public static void LogError<T>(IKeyed device, string message, params object[] args)
        {
            Log.ForContext<T>().ForContext("Key", device?.Key).Error(message, args);
        }

        public static void LogError<T>(IKeyed device, Exception ex, string message, params object[] args)
        {
            Log.ForContext<T>().ForContext("Key", device?.Key).Error(ex, message, args);
        }

        //Verbose
        public static void LogVerbose(string message, params object[] args)
        {
            Log.Verbose(message, args);
        }
        public static void LogVerbose(Exception ex, string message, params object[] args)
        {
            Log.Verbose(ex, message, args);
        }

        public static void LogVerbose<T>(string message, params object[] args)
        {
            Log.ForContext<T>().Verbose(message, args);
        }

        public static void LogVerbose<T>(Exception ex, string message, params object[] args)
        {
            Log.ForContext<T>().ForContext<T>().Verbose(ex, message, args);
        }

        public static void LogVerbose(IKeyed device, string message, params object[] args)
        {
            Log.ForContext("Key", device?.Key).Verbose(message, args);
        }
        public static void LogVerbose(IKeyed device, Exception ex, string message, params object[] args)
        {
            Log.ForContext("Key", device?.Key).Verbose(ex, message, args);
        }

        public static void LogVerbose<T>(IKeyed device, string message, params object[] args)
        {
            Log.ForContext("Key", device?.Key).ForContext<T>().Verbose(message, args);
        }

        public static void LogVerbose<T>(IKeyed device, Exception ex, string message, params object[] args)
        {
            Log.ForContext<T>().ForContext<T>().Verbose(ex, message, args);
        }

        //Debug
        public static void LogDebug(IKeyed device, string message, params object[] args)
        {
            Log.ForContext("Key", device?.Key).Debug(message, args);
        }
        public static void LogDebug(IKeyed device, Exception ex, string message, params object[] args)
        {
            Log.ForContext("Key", device?.Key).Debug(ex, message, args);
        }

        public static void LogDebug<T>(IKeyed device, string message, params object[] args)
        {
            Log.ForContext("Key", device?.Key).ForContext<T>().Debug(message, args);
        }

        public static void LogDebug<T>(IKeyed device, Exception ex, string message, params object[] args)
        {
            Log.ForContext<T>().ForContext<T>().Debug(ex, message, args);
        }

        public static void LogDebug(string message, params object[] args)
        {
            Log.Debug(message, args);
        }

        public static void LogDebug<T>(string message, params object[] args)
        {
            Log.ForContext<T>().Debug(message, args);
        }

        public static void LogDebug(Exception ex, string message, params object[] args)
        {
            Log.Debug(ex, message, args);
        }

        public static void LogDebug<T>(Exception ex, string message, params object[] args)
        {
            Log.ForContext<T>().Debug(ex, message, args);
        }

        //Information
        public static void LogInformation(IKeyed device, string message, params object[] args)
        {
            Log.ForContext("Key", device?.Key).Information(message, args);
        }
        public static void LogInformation(IKeyed device, Exception ex, string message, params object[] args)
        {
            Log.ForContext("Key", device?.Key).Information(ex, message, args);
        }

        public static void LogInformation<T>(IKeyed device, string message, params object[] args)
        {
            Log.ForContext("Key", device?.Key).ForContext<T>().Information(message, args);
        }

        public static void LogInformation<T>(IKeyed device, Exception ex, string message, params object[] args)
        {
            Log.ForContext<T>().ForContext<T>().Information(ex, message, args);
        }

        public static void LogInformation(string message, params object[] args)
        {
            Log.Information(message, args);
        }

        public static void LogInformation<T>(string message, params object[] args)
        {
            Log.ForContext<T>().Information(message, args);
        }

        public static void LogInformation(Exception ex, string message, params object[] args)
        {
            Log.Information(ex, message, args);
        }

        public static void LogInformation<T>(Exception ex, string message, params object[] args)
        {
            Log.ForContext<T>().Information(ex, message, args);
        }

        //Fatal
        public static void LogWarning(IKeyed device, string message, params object[] args)
        {
            Log.ForContext("Key", device?.Key).Warning(message, args);
        }
        public static void LogWarning(IKeyed device, Exception ex, string message, params object[] args)
        {
            Log.ForContext("Key", device?.Key).Warning(ex, message, args);
        }

        public static void LogWarning<T>(IKeyed device, string message, params object[] args)
        {
            Log.ForContext("Key", device?.Key).ForContext<T>().Warning(message, args);
        }

        public static void LogWarning<T>(IKeyed device, Exception ex, string message, params object[] args)
        {
            Log.ForContext<T>().ForContext<T>().Warning(ex, message, args);
        }

        public static void LogWarning(string message, params object[] args)
        {
            Log.Warning(message, args);
        }

        public static void LogWarning<T>(string message, params object[] args)
        {
            Log.ForContext<T>().Warning(message, args);
        }

        public static void LogWarning(Exception ex, string message, params object[] args)
        {
            Log.Warning(ex, message, args);
        }
        public static void LogWarning<T>(Exception ex, string message, params object[] args)
        {
            Log.ForContext<T>().Warning(ex, message, args);
        }

        //Fatal
        public static void LogFatal(IKeyed device, string message, params object[] args)
        {
            Log.ForContext("Key", device?.Key).Fatal(message, args);
        }
        public static void LogFatal(IKeyed device, Exception ex, string message, params object[] args)
        {
            Log.ForContext("Key", device?.Key).Fatal(ex, message, args);
        }

        public static void LogFatal<T>(IKeyed device, string message, params object[] args)
        {
            Log.ForContext("Key", device?.Key).ForContext<T>().Fatal(message, args);
        }

        public static void LogFatal<T>(IKeyed device, Exception ex, string message, params object[] args)
        {
            Log.ForContext<T>().ForContext<T>().Fatal(ex, message, args);
        }

        public static void LogFatal(string message, params object[] args)
        {
            Log.Fatal(message, args);
        }

        public static void LogFatal<T>(string message, params object[] args)
        {
            Log.ForContext<T>().Fatal(message, args);
        }

        public static void LogFatal(Exception ex, string message, params object[] args)
        {
            Log.Fatal(ex, message, args);
        }
        public static void LogFatal<T>(Exception ex, string message, params object[] args)
        {
            Log.ForContext<T>().Fatal(ex, message, args);
        }

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
