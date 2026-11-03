using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Common.Http.Dispatchers;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerProxies;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests
{
    [TestFixture]
    public class IndexerHttpClientFixture : CoreTest<IndexerHttpClient>
    {
        private const string CustomUserAgent = "Mozilla/5.0 (X11; Linux x86_64) Gecko/20100101 Firefox/141.0";
        private const string GlobalUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/141.0.0.0";

        private HttpRequest _request;
        private HttpRequest _dispatchedRequest;

        [SetUp]
        public void Setup()
        {
            _request = new HttpRequest("https://my.indexer.tld/api");
            _dispatchedRequest = null;

            Mocker.GetMock<IConfigService>()
                  .SetupGet(v => v.IndexerUserAgent)
                  .Returns(string.Empty);

            Mocker.SetConstant<ICacheManager>(new CacheManager());
            Mocker.SetConstant<IEnumerable<IHttpRequestInterceptor>>(Array.Empty<IHttpRequestInterceptor>());

            Mocker.GetMock<IHttpDispatcher>()
                  .Setup(v => v.GetResponseAsync(It.IsAny<HttpRequest>(), It.IsAny<CookieContainer>()))
                  .Returns<HttpRequest, CookieContainer>((request, _) =>
                  {
                      _dispatchedRequest = request;

                      return Task.FromResult(new HttpResponse(request, new HttpHeader(), new CookieCollection(), Array.Empty<byte>()));
                  });
        }

        private static IndexerDefinition GivenDefinition(string userAgent = null, params int[] tags)
        {
            return new IndexerDefinition
            {
                Id = 1,
                UserAgent = userAgent,
                Tags = new HashSet<int>(tags)
            };
        }

        private Mock<IIndexerProxy> GivenProxy(int tag)
        {
            var proxy = new Mock<IIndexerProxy>();

            proxy.SetupGet(v => v.Definition).Returns(new IndexerProxyDefinition { Id = 1, Tags = new HashSet<int> { tag } });
            proxy.Setup(v => v.PreRequest(It.IsAny<HttpRequest>())).Returns<HttpRequest>(r => r);
            proxy.Setup(v => v.PostResponse(It.IsAny<HttpResponse>())).Returns<HttpResponse>(r => r);

            Mocker.GetMock<IIndexerProxyFactory>()
                  .Setup(v => v.GetAvailableProviders())
                  .Returns(new List<IIndexerProxy> { proxy.Object });

            return proxy;
        }

        [Test]
        public async Task should_apply_configured_user_agent()
        {
            await Subject.ExecuteProxiedAsync(_request, GivenDefinition(CustomUserAgent));

            _dispatchedRequest.Headers.UserAgent.Should().Be(CustomUserAgent);
        }

        [Test]
        public void should_apply_configured_user_agent_on_sync_path()
        {
            Subject.ExecuteProxied(_request, GivenDefinition(CustomUserAgent));

            _dispatchedRequest.Headers.UserAgent.Should().Be(CustomUserAgent);
        }

        [Test]
        public async Task should_not_set_user_agent_when_none_configured()
        {
            await Subject.ExecuteProxiedAsync(_request, GivenDefinition());

            _dispatchedRequest.Headers.UserAgent.Should().BeNull();
        }

        [Test]
        public async Task should_not_set_user_agent_when_configured_value_is_whitespace()
        {
            await Subject.ExecuteProxiedAsync(_request, GivenDefinition("   "));

            _dispatchedRequest.Headers.UserAgent.Should().BeNull();
        }

        [Test]
        public async Task should_override_user_agent_already_set_on_request()
        {
            _request.Headers.UserAgent = "SetByTheIndexerDefinition/1.0";

            await Subject.ExecuteProxiedAsync(_request, GivenDefinition(CustomUserAgent));

            _dispatchedRequest.Headers.UserAgent.Should().Be(CustomUserAgent);
        }

        [Test]
        public async Task should_not_apply_indexer_user_agent_for_non_indexer_definitions()
        {
            _request.Headers.UserAgent = "SetByTheIndexerDefinition/1.0";

            await Subject.ExecuteProxiedAsync(_request, new IndexerProxyDefinition { Id = 1 });

            _dispatchedRequest.Headers.UserAgent.Should().Be("SetByTheIndexerDefinition/1.0");
        }

        [Test]
        public async Task should_fall_back_to_the_global_user_agent()
        {
            Mocker.GetMock<IConfigService>().SetupGet(v => v.IndexerUserAgent).Returns(GlobalUserAgent);

            await Subject.ExecuteProxiedAsync(_request, GivenDefinition());

            _dispatchedRequest.Headers.UserAgent.Should().Be(GlobalUserAgent);
        }

        [Test]
        public async Task should_prefer_the_indexer_user_agent_over_the_global_one()
        {
            Mocker.GetMock<IConfigService>().SetupGet(v => v.IndexerUserAgent).Returns(GlobalUserAgent);

            await Subject.ExecuteProxiedAsync(_request, GivenDefinition(CustomUserAgent));

            _dispatchedRequest.Headers.UserAgent.Should().Be(CustomUserAgent);
        }

        [Test]
        public async Task should_apply_user_agent_before_proxies_see_the_request()
        {
            string userAgentSeenByProxy = null;

            var proxy = GivenProxy(1);
            proxy.Setup(v => v.PreRequest(It.IsAny<HttpRequest>()))
                 .Returns<HttpRequest>(r =>
                 {
                     userAgentSeenByProxy = r.Headers.UserAgent;

                     return r;
                 });

            await Subject.ExecuteProxiedAsync(_request, GivenDefinition(CustomUserAgent, 1));

            proxy.Verify(v => v.PreRequest(It.IsAny<HttpRequest>()), Times.Once());
            userAgentSeenByProxy.Should().Be(CustomUserAgent);
        }
    }
}
