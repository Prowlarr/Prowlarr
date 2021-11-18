using System;
using System.Net;
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

            _logger.Debug("Applying Socks4 Proxy {0} to request {1}", Name, request.Url);

            return request;
        }

        private Uri GetProxyUri(Socks4Settings proxySettings)
        {
            return new Uri("socks4://" + proxySettings.Host + ":" + proxySettings.Port);
        }
    }
}
