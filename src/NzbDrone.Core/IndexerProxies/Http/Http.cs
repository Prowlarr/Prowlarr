using System.Net;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
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
            if (Settings.Username.IsNotNullOrWhiteSpace() && Settings.Password.IsNotNullOrWhiteSpace())
            {
                request.Proxy = new WebProxy(Settings.Host + ":" + Settings.Port, false, null, new NetworkCredential(Settings.Username, Settings.Password));
            }
            else
            {
                request.Proxy = new WebProxy(Settings.Host + ":" + Settings.Port, false, null);
            }

            _logger.Debug("Applying HTTP(S) Proxy {0} to request {1}", Name, request.Url);

            return request;
        }
    }
}
