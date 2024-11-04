using System;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Crestron.SimplSharp;

#if NET472
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace PepperDash.Core.Logging
{
    internal class DebugNet472WebSocket : DebugWebSocket
    {
        private const string Path = "/debug/join/";

        public string Url => 
            $"wss://{CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_IP_ADDRESS, 0)}:{server.Port}{server.WebSocketServices[Path].Path}";

        private readonly WebSocketServer server;

        public DebugNet472WebSocket(string certPath = "") : base(certPath)
        {
            server = new WebSocketServer(Port, IsSecure);

            if (IsSecure)
            {
                server.SslConfiguration = new ServerSslConfiguration(new X509Certificate2(certPath, CertificatePassword))
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

            server.AddWebSocketService<DebugClient>(Path);
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
