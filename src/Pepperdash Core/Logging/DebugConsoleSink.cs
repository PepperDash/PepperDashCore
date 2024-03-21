using Crestron.SimplSharp;
using Serilog.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace PepperDash.Core
{
    public class DebugConsoleSink : ILogEventSink
    {
        private readonly ITextFormatter _textFormatter;

        public void Emit(LogEvent logEvent)
        {
            if (!Debug.IsRunningOnAppliance) return;            

            string message = $"[{logEvent.Timestamp}][{logEvent.Level}][App {InitialParametersClass.ApplicationNumber}]{logEvent.RenderMessage()}";

            if(logEvent.Properties.TryGetValue("Key",out var value) && value is ScalarValue sv && sv.Value is string rawValue)
            {
                message = $"[{logEvent.Timestamp}][{logEvent.Level}][App {InitialParametersClass.ApplicationNumber}][{rawValue}]: {logEvent.RenderMessage()}";
            }

            CrestronConsole.PrintLine(message);
        }

        public DebugConsoleSink(ITextFormatter formatProvider )
        {
            _textFormatter = formatProvider ?? new JsonFormatter();
        }

    }

    public static class DebugConsoleSinkExtensions
    {
        public static LoggerConfiguration DebugConsoleSink(
                             this LoggerSinkConfiguration loggerConfiguration,
                                              ITextFormatter formatProvider = null)
        {
            return loggerConfiguration.Sink(new DebugConsoleSink(formatProvider));
        }
    }

}
