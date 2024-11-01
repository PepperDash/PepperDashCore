using System;
using Serilog;
using Serilog.Events;
using Log = PepperDash.Core.Debug;

namespace PepperDash.Core.Logging
{
    public static class DebugExtensions
    {
        public static void LogVerbose(this IKeyed device, string message, params object[] args)
        {
            Log.LogMessage(LogEventLevel.Verbose, device, message, args);
        }

        public static void LogDebug(this IKeyed device, string message, params object[] args)
        {
            Log.LogMessage(LogEventLevel.Debug, device, message, args);
        }

        public static void LogInformation(this IKeyed device, string message, params object[] args)
        {
            Log.LogMessage(LogEventLevel.Information, device, message, args);
        }

        public static void LogWarning(this IKeyed device, string message, params object[] args)
        {
            Log.LogMessage(LogEventLevel.Warning, device, message, args);
        }

        public static void LogError(this IKeyed device, string message, params object[] args)
        {
            Log.LogMessage(LogEventLevel.Error, device, message, args);
        }
    }
}
