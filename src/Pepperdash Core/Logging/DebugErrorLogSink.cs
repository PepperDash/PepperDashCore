using Crestron.SimplSharp;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PepperDash.Core.Logging
{
    public class DebugErrorLogSink : ILogEventSink
    {
        private Dictionary<LogEventLevel, Action<string>> _errorLogMap = new Dictionary<LogEventLevel, Action<string>>
        {
            { LogEventLevel.Verbose, (msg) => ErrorLog.Notice(msg) },
            {LogEventLevel.Debug, (msg) => ErrorLog.Notice(msg) },
            {LogEventLevel.Information, (msg) => ErrorLog.Notice(msg) },
            {LogEventLevel.Warning, (msg) => ErrorLog.Warn(msg) },
            {LogEventLevel.Error, (msg) => ErrorLog.Error(msg) },
            {LogEventLevel.Fatal, (msg) => ErrorLog.Error(msg) }
        };
        public void Emit(LogEvent logEvent)
        {
            var programId = CrestronEnvironment.DevicePlatform == eDevicePlatform.Appliance
                ? $"App {InitialParametersClass.ApplicationNumber}"
                : $"Room {InitialParametersClass.RoomId}";

            string message = $"[{logEvent.Timestamp}][{logEvent.Level}][{programId}]{logEvent.RenderMessage()}";

            if (logEvent.Properties.TryGetValue("Key", out var value) && value is ScalarValue sv && sv.Value is string rawValue)
            {
                message = $"[{logEvent.Timestamp}][{logEvent.Level}][{programId}][{rawValue}]: {logEvent.RenderMessage()}";
            }

            if(!_errorLogMap.TryGetValue(logEvent.Level, out var handler))
            {
                return;
            }

            handler(message);
        }
    }
}
