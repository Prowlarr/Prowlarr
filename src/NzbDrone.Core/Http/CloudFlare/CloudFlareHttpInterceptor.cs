using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Http.CloudFlare
{
    public class CloudFlareHttpInterceptor : IHttpRequestInterceptor
    {
        private const string _cloudFlareChallengeScript = "cdn-cgi/scripts/cf.challenge.js";
        private readonly Logger _logger;
        private static readonly Regex _cloudFlareRegex = new Regex(@"data-ray=""(?<Ray>[\w-_]+)"".*?data-sitekey=""(?<SiteKey>[\w-_]+)"".*?data-stoken=""(?<SecretToken>[\w-_]+)""", RegexOptions.Compiled);
        private static readonly HashSet<string> CloudflareServerNames = new HashSet<string> { "cloudflare", "cloudflare-nginx" };

        public CloudFlareHttpInterceptor(Logger logger)
        {
            _logger = logger;
        }

        public HttpRequest PreRequest(HttpRequest request)
        {
            return request;
        }

        public HttpResponse PostResponse(HttpResponse response)
        {
            //ToDo: Determine if the ChallengeScript is still valid and update if needed
            if (response.StatusCode == HttpStatusCode.Forbidden && response.Content.Contains(_cloudFlareChallengeScript))
            {
                _logger.Debug("CloudFlare CAPTCHA block on {0}", response.Request.Url);
                throw new CloudFlareCaptchaException(response, CreateCaptchaRequest(response));
            }

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable && IsCloudflareProtected(response))
            {
                _logger.Warn("Unable to connect. CloudFlare detected FlareSolver may be needed", response.Request.Url);
                throw new CloudFlareProtectedException(response);
            }

            return response;
        }

        private CloudFlareCaptchaRequest CreateCaptchaRequest(HttpResponse response)
        {
            var match = _cloudFlareRegex.Match(response.Content);

            if (!match.Success)
            {
                return null;
            }

            return new CloudFlareCaptchaRequest
            {
                Host = response.Request.Url.Host,
                SiteKey = match.Groups["SiteKey"].Value,
                Ray = match.Groups["Ray"].Value,
                SecretToken = match.Groups["SecretToken"].Value,
                ResponseUrl = response.Request.Url + new HttpUri("/cdn-cgi/l/chk_captcha")
            };
        }

        private bool IsCloudflareProtected(HttpResponse response)
        {
            // check response headers for cloudflare
            return response.Headers.Any(i =>
                i.Key != null && i.Key.ToLower() == "server" && CloudflareServerNames.Contains(i.Value.ToLower()));
        }
    }
}
