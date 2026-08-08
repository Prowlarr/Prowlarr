using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.IndexerProxies;
using NzbDrone.Core.IndexerProxies.FlareSolverr;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerProxies.FlareSolverr
{
    [TestFixture]
    public class FlareSolverrFixture : CoreTest<NzbDrone.Core.IndexerProxies.FlareSolverr.FlareSolverr>
    {
        private HttpResponse _cloudflareResponse;

        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerProxyDefinition
            {
                Settings = new FlareSolverrSettings
                {
                    Host = "http://localhost:8191/",
                    RequestTimeout = 60
                }
            };

            var headers = new HttpHeader();
            headers.Add("server", "cloudflare");

            var request = new HttpRequest("http://example.com/search");
            var content = "<title>Just a moment...</title>";

            _cloudflareResponse = new HttpResponse(
                request,
                headers,
                new CookieCollection(),
                content,
                100,
                HttpStatusCode.Forbidden);
        }

        private string BuildFlareSolverrJson(string responseBody, string contentType = null)
        {
            var solution = new Dictionary<string, object>
            {
                ["url"] = "http://example.com/search",
                ["status"] = "200",
                ["response"] = responseBody,
                ["cookies"] = new[]
                {
                    new Dictionary<string, object>
                    {
                        ["name"] = "cf_clearance",
                        ["value"] = "abc123",
                        ["domain"] = "example.com",
                        ["path"] = "/",
                        ["expires"] = 0,
                        ["size"] = 10,
                        ["httpOnly"] = false,
                        ["secure"] = true,
                        ["session"] = false,
                        ["sameSite"] = "None"
                    }
                },
                ["userAgent"] = "Mozilla/5.0 (X11; Linux x86_64) FlareSolverr"
            };

            if (contentType != null)
            {
                solution["headers"] = new Dictionary<string, object>
                {
                    ["status"] = "200",
                    ["date"] = "Thu, 01 Jan 2026 00:00:00 GMT",
                    ["content-type"] = contentType
                };
            }

            var fsResponse = new Dictionary<string, object>
            {
                ["status"] = "ok",
                ["message"] = "Challenge solved!",
                ["startTimestamp"] = 1000,
                ["endTimestamp"] = 5000,
                ["version"] = "3.5.0",
                ["solution"] = solution
            };

            return fsResponse.ToJson();
        }

        private void GivenFlareSolverrReturns(string responseBody, string contentType = null)
        {
            var json = BuildFlareSolverrJson(responseBody, contentType);

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Execute(It.Is<HttpRequest>(r => r.Url.ToString().Contains("/v1"))))
                  .Returns<HttpRequest>(r => new HttpResponse(
                      r,
                      new HttpHeader(),
                      new CookieCollection(),
                      json,
                      50,
                      HttpStatusCode.OK));
        }

        [Test]
        public void should_use_flaresolverr_response_body_directly()
        {
            var solvedHtml = "<html><body>Search results</body></html>";
            GivenFlareSolverrReturns(solvedHtml);

            var result = Subject.PostResponse(_cloudflareResponse);

            result.Content.Should().Be(solvedHtml);
            result.StatusCode.Should().Be(HttpStatusCode.OK);

            Mocker.GetMock<IHttpClient>()
                  .Verify(v => v.Execute(It.Is<HttpRequest>(r => r.Url.ToString().Contains("/v1"))), Times.Once());

            // Should NOT make a second request to the original URL
            Mocker.GetMock<IHttpClient>()
                  .Verify(v => v.Execute(It.Is<HttpRequest>(r => r.Url.ToString() == "http://example.com/search")), Times.Never());
        }

        [Test]
        public void should_update_content_type_from_flaresolverr_headers()
        {
            GivenFlareSolverrReturns("<html>results</html>", "text/html; charset=utf-8");

            var result = Subject.PostResponse(_cloudflareResponse);

            result.Headers.ContentType.Should().Be("text/html; charset=utf-8");
        }

        [Test]
        public void should_fall_back_to_cookie_retry_when_no_response_body()
        {
            GivenFlareSolverrReturns(null);

            var retryResponse = new HttpResponse(
                _cloudflareResponse.Request,
                new HttpHeader(),
                new CookieCollection(),
                "<html>retry results</html>",
                200,
                HttpStatusCode.OK);

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Execute(It.Is<HttpRequest>(r => r.Url.ToString() == "http://example.com/search")))
                  .Returns(retryResponse);

            var result = Subject.PostResponse(_cloudflareResponse);

            result.Content.Should().Be("<html>retry results</html>");

            Mocker.GetMock<IHttpClient>()
                  .Verify(v => v.Execute(It.Is<HttpRequest>(r => r.Url.ToString() == "http://example.com/search")), Times.Once());
        }

        [Test]
        public void should_return_original_response_when_not_cloudflare_protected()
        {
            var normalHeaders = new HttpHeader();
            var request = new HttpRequest("http://example.com/search");
            var normalResponse = new HttpResponse(
                request,
                normalHeaders,
                new CookieCollection(),
                "<html>normal</html>",
                50,
                HttpStatusCode.OK);

            var result = Subject.PostResponse(normalResponse);

            result.Should().BeSameAs(normalResponse);
        }

        [Test]
        public void should_throw_when_flaresolverr_returns_bad_status()
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Execute(It.Is<HttpRequest>(r => r.Url.ToString().Contains("/v1"))))
                  .Returns<HttpRequest>(r => new HttpResponse(
                      r,
                      new HttpHeader(),
                      new CookieCollection(),
                      "error",
                      50,
                      HttpStatusCode.BadGateway));

            Assert.Throws<FlareSolverrException>(() => Subject.PostResponse(_cloudflareResponse));
        }

        [Test]
        public void should_cache_user_agent_from_flaresolverr()
        {
            GivenFlareSolverrReturns("<html>solved</html>");

            Subject.PostResponse(_cloudflareResponse);

            // Make a second request, the UA should be injected via PreRequest
            var newRequest = new HttpRequest("http://example.com/other");

            var result = Subject.PreRequest(newRequest);

            result.Headers.UserAgent.Should().Be("Mozilla/5.0 (X11; Linux x86_64) FlareSolverr");
        }
    }
}
