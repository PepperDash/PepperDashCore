using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Configuration;
using WebSocketSharp.Server;

namespace PepperDash.Core
{
    public class DebugWebsocketSink : ILogEventSink
    {
        public WebSocketServer WSSV { get; private set; }

        private readonly IFormatProvider _formatProvider;

        public DebugWebsocketSink()
        {
            WSSV = new WebSocketServer();

        }

        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage(_formatProvider);
            WSSV.WebSocketServices.Broadcast(message);
        }
    }

    public static class DebugWebsocketSinkExtensions
    {
        public static LoggerConfiguration DebugWebsocketSink(
                             this LoggerSinkConfiguration loggerConfiguration,
                                              IFormatProvider formatProvider = null)
        {
            return loggerConfiguration.Sink(new DebugWebsocketSink());
        }
    }
}
