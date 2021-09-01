using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using com.LandonKey.SocksWebProxy;
using com.LandonKey.SocksWebProxy.Proxy;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Localization;

namespace NzbDrone.Core.IndexerProxies.Socks4
{
    public class Socks4 : HttpIndexerProxyBase<Socks4Settings>
    {
        public Socks4(IProwlarrCloudRequestBuilder cloudRequestBuilder, IHttpClient httpClient, Logger logger, ILocalizationService localizationService)
            : base(cloudRequestBuilder, httpClient, logger, localizationService)
        {
        }

        public override string Name => "Socks4";
        public override HttpRequest PreRequest(HttpRequest request)
        {
            if (Settings.Username.IsNotNullOrWhiteSpace() && Settings.Password.IsNotNullOrWhiteSpace())
            {
                request.Proxy = new SocksWebProxy(new ProxyConfig(IPAddress.Loopback, GetNextFreePort(), GetProxyIpAddress(Settings.Host), Settings.Port, ProxyConfig.SocksVersion.Four, Settings.Username, Settings.Password), false);
            }
            else
            {
                request.Proxy = new SocksWebProxy(new ProxyConfig(IPAddress.Loopback, GetNextFreePort(), GetProxyIpAddress(Settings.Host), Settings.Port, ProxyConfig.SocksVersion.Four), false);
            }

            _logger.Debug("Applying Socks4 Proxy {0} to request {1}", Name, request.Url);

            return request;
        }

        private static int GetNextFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            return port;
        }

        private static IPAddress GetProxyIpAddress(string host)
        {
            IPAddress ipAddress;
            if (!IPAddress.TryParse(host, out ipAddress))
            {
                try
                {
                    ipAddress = Dns.GetHostEntry(host).AddressList.OrderByDescending(a => a.AddressFamily == AddressFamily.InterNetwork).First();
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(string.Format("Unable to resolve proxy hostname '{0}' to a valid IP address.", host), e);
                }
            }

            return ipAddress;
        }
    }
}
