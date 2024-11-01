#if NET472
using System;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace PepperDash.Core.Logging
{
    public class DebugClient : WebSocketBehavior
    {
        private DateTime connectionTime;

        public TimeSpan ConnectedDuration
        {
            get
            {
                if (Context.WebSocket.IsAlive)
                {
                    return DateTime.Now - connectionTime;
                }

                return new TimeSpan(0);
            }
        }

        public DebugClient()
        {
            Debug.Console(0, "DebugClient Created");
        }

        protected override void OnOpen()
        {
            base.OnOpen();

            var url = Context.WebSocket.Url;
            Debug.Console(0, Debug.ErrorLogLevel.Notice, "New WebSocket Connection from: {0}", url);

            connectionTime = DateTime.Now;
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            base.OnMessage(e);

            Debug.Console(0, "WebSocket UiClient Message: {0}", e.Data);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);

            Debug.Console(0, Debug.ErrorLogLevel.Notice, "WebSocket UiClient Closing: {0} reason: {1}", e.Code, e.Reason);
        }

        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            base.OnError(e);

            Debug.Console(2, Debug.ErrorLogLevel.Notice, "WebSocket UiClient Error: {0} message: {1}", e.Exception, e.Message);
        }
    }
}
#endif