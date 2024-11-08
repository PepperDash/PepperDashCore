using System.IO;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace PepperDash.Core.Logging
{
    public class DebugConsoleSink : ILogEventSink
    {
        private readonly ITextFormatter textFormatter;

        public void Emit(LogEvent logEvent)
        {

            /*string message = $"[{logEvent.Timestamp}][{logEvent.Level}][App {InitialParametersClass.ApplicationNumber}]{logEvent.RenderMessage()}";

            if(logEvent.Properties.TryGetValue("Key",out var value) && value is ScalarValue sv && sv.Value is string rawValue)
            {
                message = $"[{logEvent.Timestamp}][{logEvent.Level}][App {InitialParametersClass.ApplicationNumber}][{rawValue,3}]: {logEvent.RenderMessage()}";
            }*/

            var buffer = new StringWriter(new StringBuilder(256));

            textFormatter.Format(logEvent, buffer);

            var message = buffer.ToString();

            CrestronConsole.PrintLine(message);
        }

        public DebugConsoleSink(ITextFormatter formatProvider)
        {
            textFormatter = formatProvider ?? new JsonFormatter();
        }
    }

    public static class DebugConsoleSinkExtensions
    {
        public static LoggerConfiguration DebugConsoleSink(
            this LoggerSinkConfiguration loggerConfiguration,
            ITextFormatter formatProvider = null,
            LoggingLevelSwitch levelSwitch = null)
        {
            var sink = new DebugConsoleSink(formatProvider);
            return loggerConfiguration.Conditional(Predicate, c => c.Sink(sink, levelSwitch: levelSwitch));

            static bool Predicate(LogEvent @event)
            {
                if (!Debug.IsRunningOnAppliance)
                {
                    return false;
                }

                if (@event.Properties.TryGetValue("Key", out var value) && value is ScalarValue { Value: string rawValue }
                    && DebugContext.TryGetDataForKey(Debug.ConsoleLevelStoreKey, out var data)
                    && data.Devices != null)
                {
                    if (data.Devices.Length == 0)
                    {
                        return true;
                    }

                    if (data.Devices.Any(d => d == rawValue))
                    {
                        return true;
                    }

                    return false;
                }

                return true;
            }
        }
    }
}
