using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Common.Http
{
    public class HttpResponse
    {
        private static readonly Regex RegexRefresh = new ("^(.*?url)=(.*?)(?:;|$)", RegexOptions.Compiled);

        public HttpResponse(HttpRequest request, HttpHeader headers, CookieCollection cookies, byte[] binaryData, long elapsedTime = 0, HttpStatusCode statusCode = HttpStatusCode.OK, Version version = null)
        {
            Request = request;
            Headers = headers;
            Cookies = cookies;
            ResponseData = binaryData;
            StatusCode = statusCode;
            ElapsedTime = elapsedTime;
            Version = version;
        }

        public HttpResponse(HttpRequest request, HttpHeader headers, CookieCollection cookies, string content, long elapsedTime = 0, HttpStatusCode statusCode = HttpStatusCode.OK, Version version = null)
        {
            Request = request;
            Headers = headers;
            Cookies = cookies;
            ResponseData = Headers.GetEncodingFromContentType().GetBytes(content);
            _content = content;
            StatusCode = statusCode;
            ElapsedTime = elapsedTime;
            Version = version;
        }

        public HttpRequest Request { get; private set; }
        public HttpHeader Headers { get; private set; }
        public CookieCollection Cookies { get; private set; }
        public HttpStatusCode StatusCode { get; private set; }
        public long ElapsedTime { get; private set; }
        public Version Version { get; private set; }
        public byte[] ResponseData { get; private set; }

        private string _content;

        public string Content
        {
            get
            {
                if (_content == null)
                {
                    if (Request.Encoding != null)
                    {
                        _content = Request.Encoding.GetString(ResponseData);
                    }
                    else
                    {
                        _content = Headers.GetEncodingFromContentType().GetString(ResponseData);
                    }
                }

                return _content;
            }
        }

        public bool HasHttpError => (int)StatusCode >= 400;

        public bool HasHttpServerError => (int)StatusCode >= 500;

        public bool HasHttpRedirect => StatusCode == HttpStatusCode.Moved ||
                                       StatusCode == HttpStatusCode.Found ||
                                       StatusCode == HttpStatusCode.SeeOther ||
                                       StatusCode == HttpStatusCode.TemporaryRedirect ||
                                       StatusCode == HttpStatusCode.MultipleChoices ||
                                       StatusCode == HttpStatusCode.PermanentRedirect ||
                                       Headers.ContainsKey("Refresh");

        public string RedirectUrl
        {
            get
            {
                var newUrl = Headers["Location"];

                if (newUrl == null)
                {
                    newUrl = Headers["Refresh"];

                    if (newUrl == null)
                    {
                        return string.Empty;
                    }

                    var match = RegexRefresh.Match(newUrl);

                    if (match.Success)
                    {
                        return (Request.Url + new HttpUri(match.Groups[2].Value)).FullUri;
                    }

                    return string.Empty;
                }

                return (Request.Url + new HttpUri(newUrl)).FullUri;
            }
        }

        public string[] GetCookieHeaders()
        {
            return Headers.GetValues("Set-Cookie") ?? Array.Empty<string>();
        }

        public Dictionary<string, string> GetCookies()
        {
            var result = new Dictionary<string, string>();

            foreach (Cookie cookie in Cookies)
            {
                result[cookie.Name] = cookie.Value;
            }

            return result;
        }

        public override string ToString()
        {
            var result = $"Res: HTTP/{Version} [{Request.Method}] {Request.Url}: {(int)StatusCode}.{StatusCode} ({ResponseData?.Length ?? 0} bytes)";

            if (HasHttpError && Headers.ContentType.IsNotNullOrWhiteSpace() && !Headers.ContentType.Equals("text/html", StringComparison.InvariantCultureIgnoreCase))
            {
                result += Environment.NewLine + Content;
            }

            return result;
        }
    }

    public class HttpResponse<T> : HttpResponse
        where T : new()
    {
        public HttpResponse(HttpResponse response)
            : base(response.Request, response.Headers, response.Cookies, response.ResponseData, response.ElapsedTime, response.StatusCode, response.Version)
        {
            Resource = Json.Deserialize<T>(response.Content);
        }

        public T Resource { get; private set; }
    }
}
