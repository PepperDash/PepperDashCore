using System.IO;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;

namespace PepperDash.Core.Logging
{
    public class DebugWebsocketSink : ILogEventSink
    {
        private readonly DebugWebSocket webSocket;
        private readonly ITextFormatter textFormatter;

        public DebugWebsocketSink(DebugWebSocket webSocket, ITextFormatter formatProvider = null)
        {
            this.webSocket = webSocket;
            textFormatter  = formatProvider ?? new JsonFormatter();
        }


        public void Emit(LogEvent logEvent)
        {
            if (!webSocket.IsListening) return;

            using var sw = new StringWriter();
            textFormatter.Format(logEvent, sw);
            webSocket.Broadcast(sw.ToString());
        }
    }

    public class NoopSink : ILogEventSink
    {
        public static readonly NoopSink Instance = new NoopSink();
        public void Emit(LogEvent logEvent)
        {
        }
    }
}
