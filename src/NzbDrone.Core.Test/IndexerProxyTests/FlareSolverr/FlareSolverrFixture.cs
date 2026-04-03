using System;
using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Common.Http.Proxy;
using NzbDrone.Core.IndexerProxies;
using NzbDrone.Core.IndexerProxies.FlareSolverr;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerProxyTests.FlareSolverr
{
    [TestFixture]
    public class FlareSolverrFixture : CoreTest<IndexerProxies.FlareSolverr.FlareSolverr>
    {
        private const string ProtectedUrl = "https://example.com/api/results";
        private const string SolverUserAgent = "solver-agent";

        private Cached<string> _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new Cached<string>();
            Mocker.SetConstant<IProwlarrCloudRequestBuilder>(new ProwlarrCloudRequestBuilder());

            Mocker.GetMock<ICacheManager>()
                .Setup(v => v.GetCache<string>(typeof(string), "UserAgent"))
                .Returns(_cache);

            Mocker.GetMock<IHttpProxySettingsProvider>()
                .Setup(v => v.GetProxySettings())
                .Returns((HttpProxySettings)null);

            Mocker.GetMock<ILocalizationService>()
                .Setup(v => v.GetLocalizedString(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                .Returns<string, Dictionary<string, object>>((phrase, _) => phrase);

            Subject.Definition = new IndexerProxyDefinition
            {
                Settings = new FlareSolverrSettings
                {
                    Host = "http://localhost:8191",
                    RequestTimeout = 60
                }
            };
        }

        [Test]
        public void should_apply_solver_user_agent_after_successful_proxy_test()
        {
            GivenSolverResponse();

            var validationResult = Subject.Test();
            var request = Subject.PreRequest(new HttpRequest("https://another.example/api/results"));

            validationResult.IsValid.Should().BeTrue();
            request.Headers.UserAgent.Should().Be(SolverUserAgent);
        }

        [Test]
        public void should_preserve_explicit_user_agent()
        {
            GivenSolverResponse();
            Subject.Test().IsValid.Should().BeTrue();
            var request = new HttpRequest(ProtectedUrl);
            request.Headers.UserAgent = "custom-agent";

            Subject.PreRequest(request);

            request.Headers.UserAgent.Should().Be("custom-agent");
        }

        [Test]
        public void should_fall_back_to_user_agent_cached_for_indexer_host()
        {
            _cache.Set("example.com", "host-agent");
            var request = new HttpRequest(ProtectedUrl);

            Subject.PreRequest(request);

            request.Headers.UserAgent.Should().Be("host-agent");
        }

        [Test]
        public void should_scope_cached_user_agent_to_solver_host()
        {
            GivenSolverResponse();
            Subject.Test().IsValid.Should().BeTrue();
            ((FlareSolverrSettings)Subject.Definition.Settings).Host = "http://another-solver:8191";
            var request = new HttpRequest("https://another.example/api/results");

            Subject.PreRequest(request);

            request.Headers.UserAgent.Should().BeNull();
        }

        [Test]
        public void should_not_cache_user_agent_from_failed_proxy_test()
        {
            GivenSolverResponse(status: "error", userAgent: null, message: "Unable to solve request");

            var validationResult = Subject.Test();
            var request = Subject.PreRequest(new HttpRequest("https://another.example/api/results"));

            validationResult.IsValid.Should().BeFalse();
            request.Headers.UserAgent.Should().BeNull();
        }

        [Test]
        public void should_refresh_solver_user_agent_after_solving_a_challenge()
        {
            GivenSolverResponse(userAgent: "old-solver-agent");
            Subject.Test().IsValid.Should().BeTrue();
            var originalResponse = CloudflareResponse(new HttpRequest(ProtectedUrl));
            var finalResponse = CloudflareResponse(originalResponse.Request);
            Mocker.GetMock<IHttpClient>()
                .SetupSequence(v => v.Execute(It.IsAny<HttpRequest>()))
                .Returns(SolverResponse())
                .Returns(finalResponse);

            var response = Subject.PostResponse(originalResponse);
            var otherRequest = Subject.PreRequest(new HttpRequest("https://another.example/api/results"));

            response.Should().BeSameAs(finalResponse);
            otherRequest.Headers.UserAgent.Should().Be(SolverUserAgent);
        }

        private void GivenSolverResponse(string status = "ok", string userAgent = SolverUserAgent, string message = "Success")
        {
            Mocker.GetMock<IHttpClient>()
                .Setup(v => v.Execute(It.IsAny<HttpRequest>()))
                .Returns(SolverResponse(status, userAgent, message));
        }

        private static HttpResponse CloudflareResponse(HttpRequest request)
        {
            return new HttpResponse(
                request,
                new HttpHeader
                {
                    { "Server", "cloudflare" },
                    { "Content-Type", "text/html" }
                },
                new CookieCollection(),
                "<html><head><title>Just a moment...</title></head></html>",
                statusCode: HttpStatusCode.Forbidden);
        }

        private static HttpResponse SolverResponse(string status = "ok", string userAgent = SolverUserAgent, string message = "Success")
        {
            object solution = null;

            if (userAgent != null)
            {
                solution = new
                {
                    url = ProtectedUrl,
                    status = 200,
                    headers = new Dictionary<string, string>(),
                    response = "<html>solver response</html>",
                    cookies = Array.Empty<object>(),
                    userAgent
                };
            }

            var content = JsonConvert.SerializeObject(new
            {
                status,
                message,
                solution
            });

            return new HttpResponse(
                new HttpRequest("http://localhost:8191/v1"),
                new HttpHeader { ContentType = "application/json" },
                new CookieCollection(),
                content);
        }
    }
}
