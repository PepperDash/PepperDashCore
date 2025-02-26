using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronLogger;
using Serilog.Core;
using Serilog.Events;
using System; // Add this for Exception class

namespace PepperDash.Core.Logging
{
    public class DebugCrestronLoggerSink : ILogEventSink
    {
        public void Emit(LogEvent logEvent)
        {
            if (!Debug.IsRunningOnAppliance) return;

            string message = $"[{logEvent.Timestamp}][{logEvent.Level}][App {InitialParametersClass.ApplicationNumber}]{logEvent.RenderMessage()}";

            if (logEvent.Properties.TryGetValue("Key", out var value) && value is ScalarValue sv && sv.Value is string rawValue)
            {
                message = $"[{logEvent.Timestamp}][{logEvent.Level}][App {InitialParametersClass.ApplicationNumber}][{rawValue}]: {logEvent.RenderMessage()}";
            }

            CrestronLogger.WriteToLog(message, (uint)logEvent.Level);
        }

        public DebugCrestronLoggerSink()
        {
            try
            {
                // The Crestron SDK appears to be using Windows-style paths internally
                // We'll wrap this in a try/catch to handle path errors
                
                CrestronLogger.Initialize(1, LoggerModeEnum.DEFAULT);
            }
            catch (Crestron.SimplSharp.CrestronIO.InvalidDirectoryLocationException ex)
            {
                // Log the error but allow the application to continue without the RM logger
                CrestronConsole.PrintLine("Error initializing CrestronLogger in RM mode: {0}", ex.Message);
                
                // Just report the error and continue - don't try to use other logger modes
                // since LoggerModeEnum doesn't have a Default value
                CrestronConsole.PrintLine("CrestronLogger will not be available");
            }
        }
    }
}