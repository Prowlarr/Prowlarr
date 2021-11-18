using System;
using System.Net;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
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

            if (Settings.Username.IsNotNullOrWhiteSpace() && Settings.Password.IsNotNullOrWhiteSpace())
            {
                request.Proxy = new WebProxy(uri, false, null, new NetworkCredential(Settings.Username, Settings.Password));
            }
            else
            {
                request.Proxy = new WebProxy(uri);
            }

            _logger.Debug("Applying Socks5 Proxy {0} to request {1}", Name, request.Url);

            return request;
        }

        private Uri GetProxyUri(Socks5Settings proxySettings)
        {
            return new Uri("socks5://" + proxySettings.Host + ":" + proxySettings.Port);
        }
    }
}
