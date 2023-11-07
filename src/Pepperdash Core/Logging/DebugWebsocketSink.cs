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
        private WebSocketServer _wssv;

        private readonly IFormatProvider _formatProvider;

        public DebugWebsocketSink()
        {
            _wssv = new WebSocketServer();

        }

        public void Emit(LogEvent logEvent)
        {
            if (_wssv == null || !_wssv.IsListening) return;

            var message = logEvent.RenderMessage(_formatProvider);
            _wssv.WebSocketServices.Broadcast(message);
        }

        public void StartServerAndSetPort(int port)
        {
            _wssv = new WebSocketServer(port);
            _wssv.Start();
        }

        public void StopServer()
        {             
            _wssv.Stop();
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
