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
            Debug.LogInformation<DebugClient>("DebugClient Created");
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            var url = Context.WebSocket.Url;
            Debug.LogInformation<DebugClient>("New WebSocket Connection from: {Url}", url);
            connectionTime = DateTime.Now;
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            base.OnMessage(e);
            Debug.LogVerbose<DebugClient>("WebSocket UiClient Message: {Data}", e.Data);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);
            Debug.LogInformation<DebugClient>("WebSocket UiClient Closing: {Code} Reason: {Reason} Total Time: {Time}", e.Code, e.Reason, ConnectedDuration);
        }

        protected override void OnError(ErrorEventArgs e)
        {
            base.OnError(e);
            Debug.LogError<DebugClient>(e.Exception, "WebSocket UiClient Error");
        }
    }
}

#endif
