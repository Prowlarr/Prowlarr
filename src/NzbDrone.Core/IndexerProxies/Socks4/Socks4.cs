using System;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Common.Http.Proxy;
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
            var uri = GetProxyUri(Settings);

            if (uri == null)
            {
                return null;
            }

            request.ProxySettings = new HttpProxySettings(ProxyType.Socks4,
                                Settings.Host,
                                Settings.Port,
                                null,
                                false,
                                Settings.Username,
                                Settings.Password);

            _logger.Debug("Applying Socks4 Proxy {0} to request {1}", Name, request.Url);

            return request;
        }

        private Uri GetProxyUri(Socks4Settings proxySettings)
        {
            return new Uri("socks4://" + proxySettings.Host + ":" + proxySettings.Port);
        }
    }
}
