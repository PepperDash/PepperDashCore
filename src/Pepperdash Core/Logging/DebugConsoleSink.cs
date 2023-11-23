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
    internal class DebugConsoleSink : ILogEventSink
    {
        private readonly ITextFormatter _textFormatter;

        public void Emit(LogEvent logEvent)
        {
            if (!Debug.IsRunningOnAppliance) return;

            CrestronConsole.PrintLine("[{0}][App {1}][Lvl {2}]: {3}", logEvent.Timestamp,
                InitialParametersClass.ApplicationNumber,
                logEvent.Level,
                logEvent.RenderMessage());
        }

        public DebugConsoleSink(ITextFormatter formatProvider)
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
