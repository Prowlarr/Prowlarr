using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Common.Http.Proxy;
using NzbDrone.Core.Localization;

namespace NzbDrone.Core.IndexerProxies.Http
{
    public class Http : HttpIndexerProxyBase<HttpSettings>
    {
        public Http(IProwlarrCloudRequestBuilder cloudRequestBuilder, IHttpClient httpClient, Logger logger, ILocalizationService localizationService)
            : base(cloudRequestBuilder, httpClient, logger, localizationService)
        {
        }

        public override string Name => "Http";

        public override HttpRequest PreRequest(HttpRequest request)
        {
            request.ProxySettings = new HttpProxySettings(ProxyType.Http,
                                Settings.Host,
                                Settings.Port,
                                null,
                                false,
                                Settings.Username,
                                Settings.Password);

            _logger.Debug("Applying HTTP(S) Proxy {0} to request {1}", Name, request.Url);

            return request;
        }
    }
}
