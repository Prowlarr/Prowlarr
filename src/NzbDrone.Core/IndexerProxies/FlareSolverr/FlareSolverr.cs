using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Http.Proxy;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Http.CloudFlare;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.IndexerProxies.FlareSolverr
{
    public class FlareSolverr : HttpIndexerProxyBase<FlareSolverrSettings>
    {
        private readonly ICached<string> _cache;
        private readonly IHttpProxySettingsProvider _proxySettingsProvider;

        public FlareSolverr(IHttpProxySettingsProvider proxySettingsProvider, IProwlarrCloudRequestBuilder cloudRequestBuilder, IHttpClient httpClient, Logger logger, ILocalizationService localizationService, ICacheManager cacheManager)
            : base(cloudRequestBuilder, httpClient, logger, localizationService)
        {
            _proxySettingsProvider = proxySettingsProvider;
            _cache = cacheManager.GetCache<string>(typeof(string), "UserAgent");
        }

        public override string Name => "FlareSolverr";

        public override HttpRequest PreRequest(HttpRequest request)
        {
            //Try original request first, ignore errors, detect CF in post response
            request.SuppressHttpError = true;

            //Inject UA if not present
            if (_cache.Find(request.Url.Host).IsNotNullOrWhiteSpace() && request.Headers.UserAgent.IsNullOrWhiteSpace())
            {
                request.Headers.UserAgent = _cache.Find(request.Url.Host);
            }

            return request;
        }

        public override HttpResponse PostResponse(HttpResponse response)
        {
            if (!CloudFlareDetectionService.IsCloudflareProtected(response))
            {
                _logger.Debug("CF Protection not detected, returning original response");
                return response;
            }

            var flaresolverrResponse = _httpClient.Execute(GenerateFlareSolverrRequest(response.Request));

            if (flaresolverrResponse.StatusCode != HttpStatusCode.OK && flaresolverrResponse.StatusCode != HttpStatusCode.InternalServerError)
            {
                throw new FlareSolverrException("HTTP StatusCode not 200 or 500. Status is :" + response.StatusCode);
            }

            var result = JsonConvert.DeserializeObject<FlareSolverrResponse>(flaresolverrResponse.Content);

            var newRequest = response.Request;

            //Cache the user-agent so we can inject it in next request to avoid re-solve
            _cache.Set(response.Request.Url.Host, result.Solution.UserAgent);
            newRequest.Headers.UserAgent = result.Solution.UserAgent;

            InjectCookies(newRequest, result);

            //Request again with User-Agent and Cookies from Flaresolverr
            var finalResponse = _httpClient.Execute(newRequest);

            return finalResponse;
        }

        private void InjectCookies(HttpRequest request, FlareSolverrResponse flareSolverrResponse)
        {
            var rCookies = flareSolverrResponse.Solution.Cookies;

            if (!rCookies.Any())
            {
                return;
            }

            var rCookiesList = rCookies.Select(x => x.Name).ToList();

            foreach (var rCookie in rCookies)
            {
                request.Cookies.Remove(rCookie.Name);
                request.Cookies.Add(rCookie.Name, rCookie.Value);
            }
        }

        private HttpRequest GenerateFlareSolverrRequest(HttpRequest request)
        {
            FlareSolverrRequest req;

            var url = request.Url.ToString();
            var maxTimeout = Settings.RequestTimeout * 1000;

            // Use Proxy if no credentials are set (creds not supported as of FS 2.2.9)
            var proxySettings = _proxySettingsProvider.GetProxySettings();
            var proxyUrl = proxySettings != null && proxySettings.Username.IsNullOrWhiteSpace() && proxySettings.Password.IsNullOrWhiteSpace() ? GetProxyUri(proxySettings) : null;

            if (request.Method == HttpMethod.Get)
            {
                req = new FlareSolverrRequestGet
                {
                    Cmd = "request.get",
                    Url = url,
                    MaxTimeout = maxTimeout,
                    Proxy = new FlareSolverrProxy
                    {
                        Url = proxyUrl?.AbsoluteUri
                    }
                };
            }
            else if (request.Method == HttpMethod.Post)
            {
                var contentTypeType = request.Headers.ContentType.ToLower() ?? "<null>";

                if (contentTypeType.Contains("application/x-www-form-urlencoded"))
                {
                    var contentTypeValue = request.Headers.ContentType.ToString();
                    var postData = request.GetContent();

                    req = new FlareSolverrRequestPostUrlEncoded
                    {
                        Cmd = "request.post",
                        Url = url,
                        PostData = postData,
                        Headers = new HeadersPost
                        {
                            ContentType = contentTypeValue,
                            ContentLength = null
                        },
                        MaxTimeout = maxTimeout,
                        Proxy = new FlareSolverrProxy
                        {
                            Url = proxyUrl?.AbsoluteUri
                        }
                    };
                }
                else if (contentTypeType.Contains("multipart/form-data")
                         || contentTypeType.Contains("text/html"))
                {
                    //TODO Implement - check if we just need to pass the content-type with the relevant headers
                    throw new FlareSolverrException("Unimplemented POST Content-Type: " + request.Headers.ContentType);
                }
                else
                {
                    throw new FlareSolverrException("Unsupported POST Content-Type: " + request.Headers.ContentType);
                }
            }
            else
            {
                throw new FlareSolverrException("Unsupported HttpMethod: " + request.Method);
            }

            var apiUrl = string.Format("{0}/v1", Settings.Host.TrimEnd('/'));
            var newRequest = new HttpRequest(apiUrl, HttpAccept.Json);

            newRequest.Headers.ContentType = "application/json";
            newRequest.Method = HttpMethod.Post;
            newRequest.LogResponseContent = true;
            newRequest.RequestTimeout = TimeSpan.FromSeconds(Settings.RequestTimeout + 5);
            newRequest.SetContent(req.ToJson());

            _logger.Debug("Cloudflare Detected, Applying FlareSolverr Proxy {0} to request {1}", Name, request.Url);

            return newRequest;
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            var request = GenerateFlareSolverrRequest(_cloudRequestBuilder.Create()
                                  .Resource("/ping")
                                  .Build());

            try
            {
                var response = _httpClient.Execute(request);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    _logger.Error("Proxy Health Check failed: {0}", response.StatusCode);
                    failures.Add(new NzbDroneValidationFailure("Host", string.Format(_localizationService.GetLocalizedString("ProxyCheckBadRequestMessage"), response.StatusCode)));
                }

                var result = JsonConvert.DeserializeObject<FlareSolverrResponse>(response.Content);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Proxy Health Check failed");
                failures.Add(new NzbDroneValidationFailure("Host", string.Format(_localizationService.GetLocalizedString("ProxyCheckFailedToTestMessage"), request.Url.Host)));
            }

            return new ValidationResult(failures);
        }

        private Uri GetProxyUri(HttpProxySettings proxySettings)
        {
            switch (proxySettings.Type)
            {
                case ProxyType.Http:
                    return new Uri("http://" + proxySettings.Host + ":" + proxySettings.Port);
                case ProxyType.Socks4:
                    return new Uri("socks4://" + proxySettings.Host + ":" + proxySettings.Port);
                case ProxyType.Socks5:
                    return new Uri("socks5://" + proxySettings.Host + ":" + proxySettings.Port);
                default:
                    return null;
            }
        }

        private class FlareSolverrRequest
        {
            public string Cmd { get; set; }
            public string Url { get; set; }
            public Cookie[] Cookies { get; set; }
            public FlareSolverrProxy Proxy { get; set; }
        }

        private class FlareSolverrRequestGet : FlareSolverrRequest
        {
            public string Headers { get; set; }
            public int MaxTimeout { get; set; }
        }

        private class FlareSolverrRequestPost : FlareSolverrRequest
        {
            public string PostData { get; set; }
            public int MaxTimeout { get; set; }
        }

        private class FlareSolverrRequestPostUrlEncoded : FlareSolverrRequestPost
        {
            public HeadersPost Headers { get; set; }
        }

        private class FlareSolverrProxy
        {
            public string Url { get; set; }
        }

        private class HeadersPost
        {
            public string ContentType { get; set; }
            public string ContentLength { get; set; }
        }

        private class FlareSolverrResponse
        {
            public string Status { get; set; }
            public string Message { get; set; }
            public long StartTimestamp { get; set; }
            public long EndTimestamp { get; set; }
            public string Version { get; set; }
            public Solution Solution { get; set; }
        }

        private class Solution
        {
            public string Url { get; set; }
            public string Status { get; set; }
            public Headers Headers { get; set; }
            public string Response { get; set; }
            public Cookie[] Cookies { get; set; }
            public string UserAgent { get; set; }
        }

        private class Cookie
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public string Domain { get; set; }
            public string Path { get; set; }
            public double Expires { get; set; }
            public int Size { get; set; }
            public bool HttpOnly { get; set; }
            public bool Secure { get; set; }
            public bool Session { get; set; }
            public string SameSite { get; set; }

            public string ToHeaderValue() => $"{Name}={Value}";
            public System.Net.Cookie ToCookieObj() => new System.Net.Cookie(Name, Value);
        }

        private class Headers
        {
            public string Status { get; set; }
            public string Date { get; set; }

            [JsonProperty(PropertyName = "content-type")]
            public string ContentType { get; set; }
        }
    }
}
