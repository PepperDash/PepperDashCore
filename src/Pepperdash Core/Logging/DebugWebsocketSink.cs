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
using Crestron.SimplSharp;
using System.Net.Http;
using System.Text.RegularExpressions;
using WebSocketSharp;

namespace PepperDash.Core
{
    public class DebugWebsocketSink : ILogEventSink
    {
        private HttpServer _server;

        private string _path = "/join";

        public int Port 
        { get 
            { 
                
                if(_server == null) return 0;
                return _server.Port;
            } 
        }

        public bool IsListening 
        { get 
            { 
                if (_server == null) return false;
                return _server.IsListening; 
            } 
        }

        private readonly IFormatProvider _formatProvider;

        public DebugWebsocketSink()
        {
            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type == eProgramStatusEventType.Stopping)
                {
                    StopServer();
                }
            };
        }

        public void Emit(LogEvent logEvent)
        {
            if (_server == null || !_server.IsListening) return;

            var message = logEvent.RenderMessage(_formatProvider);
            _server.WebSocketServices.Broadcast(message);
        }

        public void StartServerAndSetPort(int port)
        {
            Debug.Console(0, "Starting Websocket Server on port: {0}", port);
            _server = new HttpServer(port);
            _server.AddWebSocketService<DebugClient>(_path);
            _server.Start();
        }

        public void StopServer()
        {
            Debug.Console(0, "Stopping Websocket Server");
            _server.Stop();
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

    public class DebugClient : WebSocketBehavior
    {
        private DateTime _connectionTime;

        public TimeSpan ConnectedDuration
        {
            get
            {
                if (Context.WebSocket.IsAlive)
                {
                    return DateTime.Now - _connectionTime;
                }
                else
                {
                    return new TimeSpan(0);
                }
            }
        }

        public DebugClient()
        {

        }

        protected override void OnOpen()
        {
            base.OnOpen();

            var url = Context.WebSocket.Url;
            Debug.Console(2, Debug.ErrorLogLevel.Notice, "New WebSocket Connection from: {0}", url);

            //var match = Regex.Match(url.AbsoluteUri, "(?:ws|wss):\\/\\/.*(?:\\/mc\\/api\\/ui\\/join\\/)(.*)");

            //if (match.Success)
            //{
            //    var clientId = match.Groups[1].Value;

            //    // Inform controller of client joining
            //    if (Controller != null)
            //    {
            //        var clientJoined = new MobileControlResponseMessage
            //        {
            //            Type = "/system/roomKey",
            //            ClientId = clientId,
            //            Content = RoomKey,
            //        };

            //        Controller.SendMessageObjectToDirectClient(clientJoined);

            //        var bridge = Controller.GetRoomBridge(RoomKey);

            //        SendUserCodeToClient(bridge, clientId);

            //        bridge.UserCodeChanged += (sender, args) => SendUserCodeToClient((MobileControlEssentialsRoomBridge)sender, clientId);
            //    }
            //    else
            //    {
            //        Debug.Console(2, "WebSocket UiClient Controller is null");
            //    }
            //}

            _connectionTime = DateTime.Now;

            // TODO: Future: Check token to see if there's already an open session using that token and reject/close the session 
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            base.OnMessage(e);

            Debug.Console(0, "WebSocket UiClient Message: {0}", e.Data);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);

            Debug.Console(2, Debug.ErrorLogLevel.Notice, "WebSocket UiClient Closing: {0} reason: {1}", e.Code, e.Reason);

        }

        protected override void OnError(ErrorEventArgs e)
        {
            base.OnError(e);

            Debug.Console(2, Debug.ErrorLogLevel.Notice, "WebSocket UiClient Error: {0} message: {1}", e.Exception, e.Message);
        }
    }
}
