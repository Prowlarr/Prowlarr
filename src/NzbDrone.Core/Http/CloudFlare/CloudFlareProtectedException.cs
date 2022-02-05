using NzbDrone.Common.Exceptions;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Http.CloudFlare
{
    public class CloudFlareProtectedException : NzbDroneException
    {
        public HttpResponse Response { get; set; }
        public CloudFlareProtectedException(HttpResponse response)
            : base("Cloudflare Detected. Flaresolverr may be required. {0} has been blocked by CloudFlare", response.Request.Url.Host)
        {
            Response = response;
        }
    }
}
