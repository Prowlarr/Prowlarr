using System;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Common.Http.Proxy;
using NzbDrone.Core.Localization;

namespace NzbDrone.Core.IndexerProxies.Socks5
{
    public class Socks5 : HttpIndexerProxyBase<Socks5Settings>
    {
        public Socks5(IProwlarrCloudRequestBuilder cloudRequestBuilder, IHttpClient httpClient, Logger logger, ILocalizationService localizationService)
            : base(cloudRequestBuilder, httpClient, logger, localizationService)
        {
        }

        public override string Name => "Socks5";

        public override HttpRequest PreRequest(HttpRequest request)
        {
            var uri = GetProxyUri(Settings);

            if (uri == null)
            {
                return null;
            }

            request.ProxySettings = new HttpProxySettings(ProxyType.Socks5,
                                Settings.Host,
                                Settings.Port,
                                null,
                                false,
                                Settings.Username,
                                Settings.Password);

            _logger.Debug("Applying Socks5 Proxy {0} to request {1}", Name, request.Url);

            return request;
        }

        private Uri GetProxyUri(Socks5Settings proxySettings)
        {
            return new Uri("socks5://" + proxySettings.Host + ":" + proxySettings.Port);
        }
    }
}
