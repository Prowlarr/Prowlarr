using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Http.CloudFlare
{
    public class CloudFlareDetectionService
    {
        private static readonly HashSet<string> CloudflareServerNames = new HashSet<string> { "cloudflare", "cloudflare-nginx", "ddos-guard" };
        private readonly Logger _logger;

        public CloudFlareDetectionService(Logger logger)
        {
            _logger = logger;
        }

        public static bool IsCloudflareProtected(HttpResponse response)
        {
            if (!response.Headers.Any(i => i.Key != null && i.Key.ToLower() == "server" && CloudflareServerNames.Contains(i.Value.ToLower())))
            {
                return false;
            }

            // detect CloudFlare and DDoS-GUARD
            if (response.StatusCode.Equals(HttpStatusCode.ServiceUnavailable) ||
                response.StatusCode.Equals(HttpStatusCode.Forbidden))
            {
                return true; // Defected CloudFlare and DDoS-GUARD
            }

            // detect Custom CloudFlare for EbookParadijs, Film-Paleis, MuziekFabriek and Puur-Hollands
            if (response.Headers.Vary.ToString() == "Accept-Encoding,User-Agent" &&
                response.Headers.ContentEncoding.ToString() == "" &&
                response.Content.ToLower().Contains("ddos"))
            {
                return true;
            }

            return false;
        }
    }
}
