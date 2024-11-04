using System;
using System.IO;
using Crestron.SimplSharp;

#if NET472
using Org.BouncyCastle.Asn1.X509;
#endif

namespace PepperDash.Core.Logging
{
    public abstract class DebugWebSocket
    {
        public const string CertificateName = "selfCres";
        public const string CertificatePassword = "cres12345";

        protected DebugWebSocket(string certPath = "")
        {
            IsSecure = !string.IsNullOrEmpty(certPath);

            if (!IsSecure)
            {
                return;
            }

            if (!File.Exists(certPath))
            {
                CreateCert(certPath);
            }
        }

        protected bool IsSecure { get; }

        private static void CreateCert(string filePath)
        {
#if NET472
            try
            {
                var utility     = new BouncyCertificate();
                var ipAddress   = CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_CURRENT_IP_ADDRESS, 0);
                var hostName    = CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_HOSTNAME, 0);
                var domainName  = CrestronEthernetHelper.GetEthernetParameter(CrestronEthernetHelper.ETHERNET_PARAMETER_TO_GET.GET_DOMAIN_NAME, 0);
                var certificate = utility.CreateSelfSignedCertificate($"CN={hostName}.{domainName}", [$"{hostName}.{domainName}", ipAddress], new[] { KeyPurposeID.IdKPServerAuth, KeyPurposeID.IdKPClientAuth });

                utility.CertificatePassword = CertificatePassword;
                utility.WriteCertificate(certificate, filePath, CertificateName);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex, "WSS failed to create cert");
            }
#endif
        }

        public abstract bool IsListening { get; }

        public abstract void Broadcast(string message);

        public int Port { get; } = new Random().Next(65435, 65535);
    }
}
