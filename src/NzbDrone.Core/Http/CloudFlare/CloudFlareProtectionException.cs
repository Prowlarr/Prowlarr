using NzbDrone.Common.Exceptions;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Http.CloudFlare
{
    public class CloudFlareProtectionException : NzbDroneException
    {
        public HttpResponse Response { get; set; }

        public CloudFlareProtectionException(HttpResponse response)
            : base("Unable to access {0}, blocked by CloudFlare Protection.", response.Request.Url.Host)
        {
            Response = response;
        }
    }
}
