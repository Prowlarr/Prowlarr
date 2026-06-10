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
        private static readonly HashSet<string> CloudflareServerNames = new() { "cloudflare", "cloudflare-nginx", "ddos-guard" };
        private readonly Logger _logger;

        public CloudFlareDetectionService(Logger logger)
        {
            _logger = logger;
        }

        public static bool IsCloudflareProtected(HttpResponse response)
        {
            // detect CloudFlare and DDoS-GUARD via content analysis (most reliable, check first)
            if (response.StatusCode.Equals(HttpStatusCode.ServiceUnavailable) ||
                response.StatusCode.Equals(HttpStatusCode.Forbidden))
            {
                var responseHtml = response.Content;
                if (responseHtml.Contains("<title>Just a moment...</title>") ||
                    responseHtml.Contains("<title>Access denied</title>") ||
                    responseHtml.Contains("<title>Attention Required! | Cloudflare</title>") ||
                    responseHtml.Trim().Equals("error code: 1020") ||
                    responseHtml.Contains("<title>DDOS-GUARD</title>", StringComparison.OrdinalIgnoreCase) ||
                    responseHtml.Contains("cdn-cgi"))
                {
                    return true;
                }
            }

            // detect CloudFlare redirect challenges (HTTP 302 with cdn-cgi URLs or content)
            if (response.StatusCode.Equals(HttpStatusCode.Redirect) ||
                response.StatusCode.Equals(HttpStatusCode.Found) ||
                response.StatusCode.Equals(HttpStatusCode.Moved) ||
                response.StatusCode.Equals(HttpStatusCode.RedirectMethod))
            {
                var location = response.Headers["Location"] ?? string.Empty;
                var content = response.Content ?? string.Empty;
                if (location.Contains("cdn-cgi") || content.Contains("cdn-cgi"))
                {
                    return true;
                }
            }

            // detect CloudFlare and DDoS-GUARD via Server header
            if (response.Headers.Any(i => i.Key != null && i.Key.ToLower() == "server" && CloudflareServerNames.Contains(i.Value.ToLower())))
            {
                return true;
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
