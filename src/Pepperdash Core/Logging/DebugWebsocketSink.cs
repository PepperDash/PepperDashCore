using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Configuration;
using System.Net.WebSockets;
using System.Threading;
using System.IO;
using Serilog.Formatting;
using Newtonsoft.Json.Linq;
using Serilog.Formatting.Json;

namespace PepperDash.Core
{
    public class DebugWebsocketSink : ILogEventSink, IDisposable
    {
        private readonly ITextFormatter _formatter;
        private readonly List<WebSocket> _clients;
        private readonly object _lock = new object();
        private bool _disposed;

        public DebugWebsocketSink(ITextFormatter formatter)
        {
            _formatter = formatter ?? new JsonFormatter();
            _clients = new List<WebSocket>();
        }

        public void AddClient(WebSocket socket)
        {
            lock (_lock)
            {
                _clients.Add(socket);
            }
        }

        public void RemoveClient(WebSocket socket)
        {
            lock (_lock)
            {
                _clients.Remove(socket);
            }
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null || _disposed) return;

            var writer = new StringWriter();
            _formatter.Format(logEvent, writer);
            var json = writer.ToString();
            var buffer = Encoding.UTF8.GetBytes(json);

            lock (_lock)
            {
                foreach (var client in _clients.ToArray())
                {
                    try
                    {
                        if (client.State == WebSocketState.Open)
                        {
                            Task.Run(async () =>
                            {
                                await client.SendAsync(
                                    new ArraySegment<byte>(buffer),
                                    WebSocketMessageType.Text,
                                    true,
                                    CancellationToken.None);
                            });
                        }
                        else
                        {
                            RemoveClient(client);
                        }
                    }
                    catch (Exception)
                    {
                        RemoveClient(client);
                    }
                }
            }
        }

        public void StopServer()
        {
            if (_disposed) return;
            lock (_lock)
            {
                foreach (var client in _clients)
                {
                    if (client.State == WebSocketState.Open)
                    {
                        client.Abort();
                    }
                }
                _clients.Clear();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            StopServer();
        }
    }

    public static class DebugWebsocketSinkExtensions
    {
        public static LoggerConfiguration DebugWebsocketSink(
                             this LoggerSinkConfiguration loggerConfiguration,
                                              ITextFormatter formatProvider = null)
        {
            return loggerConfiguration.Sink(new DebugWebsocketSink(formatProvider));
        }
    }
}
