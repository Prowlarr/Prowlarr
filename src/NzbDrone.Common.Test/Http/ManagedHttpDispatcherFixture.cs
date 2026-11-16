using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NLog;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Common.Http.Dispatchers;
using NzbDrone.Common.Http.Proxy;
using NzbDrone.Test.Common;
using HttpRequest = NzbDrone.Common.Http.HttpRequest;

namespace NzbDrone.Common.Test.Http
{
    [TestFixture]
    public class ManagedHttpDispatcherFixture : TestBase<ManagedHttpDispatcherFixture.TestDispatcher>
    {
        public class TestDispatcher : ManagedHttpDispatcher
        {
            private readonly System.Net.Http.HttpClient _client = new(new OkHandler());

            public TestDispatcher(IHttpProxySettingsProvider proxySettingsProvider,
                ICreateManagedWebProxy createManagedWebProxy,
                ICertificateValidationService certificateValidationService,
                IUserAgentBuilder userAgentBuilder,
                ICacheManager cacheManager,
                Logger logger)
                : base(proxySettingsProvider, createManagedWebProxy, certificateValidationService, userAgentBuilder, cacheManager, logger)
            {
            }

            protected override System.Net.Http.HttpClient GetClient(HttpUri uri, HttpProxySettings requestProxy)
            {
                return _client;
            }
        }

        private class OkHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request, Content = new ByteArrayContent(System.Array.Empty<byte>()) });
            }
        }

        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<ICacheManager>(Mocker.Resolve<CacheManager>());
            Mocker.GetMock<IUserAgentBuilder>()
                .Setup(s => s.GetUserAgent(It.IsAny<bool>()))
                .Returns("Prowlarr/1.0");
        }

        [Test]
        public async Task should_handle_concurrent_requests_with_network_credentials()
        {
            var tasks = Enumerable.Range(0, 500).Select(i => Task.Run(() =>
            {
                var request = new HttpRequest($"http://host{i % 50}.local/path{i}")
                {
                    Credentials = new NetworkCredential("user", "pass")
                };

                return Subject.GetResponseAsync(request, new CookieContainer());
            }));

            var responses = await Task.WhenAll(tasks);

            responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
        }
    }
}
