using System;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Crestron.SimplSharp;

#if NET472
using System.IO;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace PepperDash.Core.Logging
{
    internal class DebugNet472WebSocket : DebugWebSocket
    {
        private const string WebsocketPath = "/debug/join/";

        public string Url => 
            $"wss://{CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_IP_ADDRESS, 0)}:{server.Port}{server.WebSocketServices[WebsocketPath].Path}";

        private readonly WebSocketServer server;

        public DebugNet472WebSocket(string certPath = "") : base(certPath)
        {
            server = new WebSocketServer(Port, IsSecure);

            if (IsSecure)
            {
                var filename = Path.Combine(certPath, CertificateName + ".pfx");
                server.SslConfiguration = new ServerSslConfiguration(new X509Certificate2(filename, CertificatePassword))
                {
                    ClientCertificateRequired  = false,
                    CheckCertificateRevocation = false,
                    EnabledSslProtocols        = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls,

                    ClientCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        Debug.LogInformation<DebugWebsocketSink>("HTTPS ClientCertificateValidation Callback triggered");
                        return true;
                    }
                };
            }

            server.AddWebSocketService<DebugClient>(WebsocketPath);
            server.Start();
        }

        public override bool IsListening => server.IsListening;

        public override void Broadcast(string message) => server.WebSocketServices.Broadcast(message);
    }

    //TODO: NETCORE version
    internal class DebugNetWebSocket : DebugWebSocket
    {
        private const string Path = "/debug/join/";

        private readonly HttpListener server = new();

        public DebugNetWebSocket(int port, string certPath = "") : base(certPath)
        {
            server.Prefixes.Add("wss://*:" + port + Path);
        }

        public override bool IsListening => server.IsListening;

        public override void Broadcast(string message) => throw new NotImplementedException();
    }
}
#endif
