using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Http.CloudFlare
{
    public class CloudFlareDetectionService
    {
        private static readonly HashSet<string> CloudflareServerNames = new () { "cloudflare", "cloudflare-nginx", "ddos-guard" };
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
                var responseHtml = response.Content;
                if (responseHtml.Contains("<title>Just a moment...</title>") ||
                    responseHtml.Contains("<title>Access denied</title>") ||
                    responseHtml.Contains("<title>Attention Required! | Cloudflare</title>") ||
                    responseHtml.Trim().Equals("error code: 1020") ||
                    responseHtml.Contains("<title>DDOS-GUARD</title>", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // detect Custom CloudFlare for EbookParadijs, Film-Paleis, MuziekFabriek and Puur-Hollands
            if (response.Headers.Vary == "Accept-Encoding,User-Agent" &&
                response.Headers.ContentEncoding.IsNullOrWhiteSpace() &&
                response.Content.ToLower().Contains("ddos"))
            {
                return true;
            }

            return false;
        }
    }
}
