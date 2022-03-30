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
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.IndexerProxies.FlareSolverr
{
    public class FlareSolverr : HttpIndexerProxyBase<FlareSolverrSettings>
    {
        private static readonly HashSet<string> CloudflareServerNames = new HashSet<string> { "cloudflare", "cloudflare-nginx" };
        private readonly ICached<string> _cache;

        public FlareSolverr(IProwlarrCloudRequestBuilder cloudRequestBuilder, IHttpClient httpClient, Logger logger, ILocalizationService localizationService, ICacheManager cacheManager)
            : base(cloudRequestBuilder, httpClient, logger, localizationService)
        {
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
            if (!IsCloudflareProtected(response))
            {
                _logger.Debug("CF Protection not detected, returning original response");
                return response;
            }

            var flaresolverrResponse = _httpClient.Execute(GenerateFlareSolverrRequest(response.Request));

            FlareSolverrResponse result = null;

            if (flaresolverrResponse.StatusCode != HttpStatusCode.OK && flaresolverrResponse.StatusCode != HttpStatusCode.InternalServerError)
            {
                throw new FlareSolverrException("HTTP StatusCode not 200 or 500. Status is :" + response.StatusCode);
            }

            result = JsonConvert.DeserializeObject<FlareSolverrResponse>(flaresolverrResponse.Content);

            var newRequest = response.Request;

            //Cache the user-agent so we can inject it in next request to avoid re-solve
            _cache.Set(response.Request.Url.Host, result.Solution.UserAgent);
            newRequest.Headers.UserAgent = result.Solution.UserAgent;

            InjectCookies(newRequest, result);

            //Request again with User-Agent and Cookies from Flaresolverr
            var finalResponse = _httpClient.Execute(newRequest);

            return finalResponse;
        }

        private static bool IsCloudflareProtected(HttpResponse response)
        {
            // check status code
            if (response.StatusCode.Equals(HttpStatusCode.ServiceUnavailable) ||
                response.StatusCode.Equals(HttpStatusCode.Forbidden))
            {
                {
                    // check response headers for CloudFlare
                    if (response.Headers.Any(i =>
                        i.Key != null && i.Key.ToLower() == "server" && CloudflareServerNames.Contains(i.Value.ToLower())))
                    {
                        return true;
                    }

                    return false;
                }
            }

            // detect Custom CloudFlare/DDOS Guard - HTTP will likely return 200 so we do this outside of checking the code
            if (response.Headers.Any(i =>
                        i.Key != null && i.Key.ToLower() == "Accept-Encoding,User-Agent") &&
                        response.Headers.ContentType.ToString() == "" &&
                        response.Content.ToLower().Contains("ddos"))
            {
                return true;
            }

            return false;
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
            var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.138 Safari/537.36";
            var maxTimeout = Settings.RequestTimeout * 1000;

            if (request.Method == HttpMethod.Get)
            {
                req = new FlareSolverrRequestGet
                {
                    Cmd = "request.get",
                    Url = url,
                    MaxTimeout = maxTimeout,
                    UserAgent = userAgent
                };
            }
            else if (request.Method == HttpMethod.Post)
            {
                var contentTypeType = request.Headers.ContentType;

                if (contentTypeType == "application/x-www-form-urlencoded")
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
                        UserAgent = userAgent
                    };
                }
                else if (contentTypeType.Contains("multipart/form-data"))
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
            newRequest.SetContent(req.ToJson());

            _logger.Debug("Applying FlareSolverr Proxy {0} to request {1}", Name, request.Url);

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

        public class FlareSolverrRequest
        {
            public string Cmd { get; set; }
            public string Url { get; set; }
            public string UserAgent { get; set; }
            public Cookie[] Cookies { get; set; }
        }

        public class FlareSolverrRequestGet : FlareSolverrRequest
        {
            public string Headers { get; set; }
            public int MaxTimeout { get; set; }
        }

        public class FlareSolverrRequestPost : FlareSolverrRequest
        {
            public string PostData { get; set; }
            public int MaxTimeout { get; set; }
        }

        public class FlareSolverrRequestPostUrlEncoded : FlareSolverrRequestPost
        {
            public HeadersPost Headers { get; set; }
        }

        public class HeadersPost
        {
            public string ContentType { get; set; }
            public string ContentLength { get; set; }
        }

        public class FlareSolverrResponse
        {
            public string Status { get; set; }
            public string Message { get; set; }
            public long StartTimestamp { get; set; }
            public long EndTimestamp { get; set; }
            public string Version { get; set; }
            public Solution Solution { get; set; }
        }

        public class Solution
        {
            public string Url { get; set; }
            public string Status { get; set; }
            public Headers Headers { get; set; }
            public string Response { get; set; }
            public Cookie[] Cookies { get; set; }
            public string UserAgent { get; set; }
        }

        public class Cookie
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

        public class Headers
        {
            public string Status { get; set; }
            public string Date { get; set; }

            [JsonProperty(PropertyName = "content-type")]
            public string ContentType { get; set; }
        }
    }
}
