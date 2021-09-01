using System;
using System.Collections.Generic;
using System.Net;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.IndexerProxies.FlareSolverr
{
    public class FlareSolverr : HttpIndexerProxyBase<FlareSolverrSettings>
    {
        public FlareSolverr(IProwlarrCloudRequestBuilder cloudRequestBuilder, IHttpClient httpClient, Logger logger, ILocalizationService localizationService)
            : base(cloudRequestBuilder, httpClient, logger, localizationService)
        {
        }

        public override string Name => "FlareSolverr";

        public override HttpRequest PreRequest(HttpRequest request)
        {
            return GenerateFlareSolverrRequest(request);
        }

        public override HttpResponse PostResponse(HttpResponse response)
        {
            FlareSolverrResponse result = null;

            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.InternalServerError)
            {
                throw new FlareSolverrException("HTTP StatusCode not 200 or 500. Status is :" + response.StatusCode);
            }

            result = JsonConvert.DeserializeObject<FlareSolverrResponse>(response.Content);

            var cookieCollection = new CookieCollection();
            var responseHeader = new HttpHeader();

            foreach (var cookie in result.Solution.Cookies)
            {
                cookieCollection.Add(cookie.ToCookieObj());
            }

            //Build new response with FS Cookie and Site Response
            var newResponse = new HttpResponse(response.Request, responseHeader, cookieCollection, result.Solution.Response);

            return newResponse;
        }

        private HttpRequest GenerateFlareSolverrRequest(HttpRequest request)
        {
            FlareSolverrRequest req;

            var url = request.Url.ToString();
            var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.138 Safari/537.36";
            var maxTimeout = 60000;

            if (request.Method == HttpMethod.GET)
            {
                req = new FlareSolverrRequestGet
                {
                    Cmd = "request.get",
                    Url = url,
                    MaxTimeout = maxTimeout,
                    UserAgent = userAgent
                };
            }
            else if (request.Method == HttpMethod.POST)
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

            newRequest.SetContent(req.ToJson());
            newRequest.Method = HttpMethod.POST;

            _logger.Debug("Applying FlareSolverr Proxy {0} to request {1}", Name, request.Url);

            return newRequest;
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            var request = PreRequest(_cloudRequestBuilder.Create()
                                  .Resource("/ping")
                                  .Build());

            try
            {
                var response = _httpClient.Execute(request);

                // We only care about 400 responses, other error codes can be ignored
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    _logger.Error("Proxy Health Check failed: {0}", response.StatusCode);
                    failures.Add(new NzbDroneValidationFailure("Host", string.Format(_localizationService.GetLocalizedString("ProxyCheckBadRequestMessage"), response.StatusCode)));
                }
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
