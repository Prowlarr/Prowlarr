using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation.Results;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions.Cardigann;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.IndexerTests.CardigannTests
{
    [TestFixture]
    public class CardigannDownloadWaitFixture : CoreTest<CardigannDownloadWaitFixture.TestIndexer>
    {
        private static readonly Uri DownloadUri = new("https://tracker.local/torrents/download/id/123.html");
        private readonly byte[] _torrent = Encoding.ASCII.GetBytes("d4:infod6:lengthi1e4:name1:x12:piece lengthi16384e6:pieces20:aaaaaaaaaaaaaaaaaaaaee");
        private readonly List<HttpRequest> _requests = new();

        [SetUp]
        public void SetUp()
        {
            _requests.Clear();

            Subject.Definition = new IndexerDefinition
            {
                Id = 123,
                Name = "test",
                Settings = new CardigannSettings { DefinitionFile = "test" }
            };

            Subject.CookiesToUse = new Dictionary<string, string>
            {
                ["session"] = "old",
                ["other"] = "keep"
            };
        }

        [Test]
        public async Task should_wait_once_and_continue_to_original_url_with_cookie_state_preserved()
        {
            Subject.WaitPolicy = new DownloadWaitPolicy("Metal Torrent Tracker - Download Torrents", "download_after");
            SetupResponses(
                Html("<html><title> Metal Torrent Tracker - Download Torrents </title><script>var download_after = 60;</script></html>",
                    new Cookie("session", "updated", "/", "tracker.local"),
                    new Cookie("other", "keep", "/", "tracker.local")),
                Binary(_torrent, "text/plain"));

            var result = await Subject.Download(DownloadUri);

            result.Data.Should().Equal(_torrent);
            Subject.Waited.Should().Be(TimeSpan.FromSeconds(60));
            _requests.Should().HaveCount(2);
            _requests[0].Url.FullUri.Should().Be(DownloadUri.AbsoluteUri);
            _requests[1].Url.FullUri.Should().Be(DownloadUri.AbsoluteUri);
            _requests[1].Method.Should().Be(HttpMethod.Get);
            _requests[1].Headers["Referer"].Should().Be(DownloadUri.AbsoluteUri);
            CookieHeader(_requests[0]).Should().Contain("session=old");
            CookieHeader(_requests[1]).Should().Contain("session=updated").And.NotContain("session=old");
            CookieHeader(_requests[1]).Should().Contain("other=keep");
            _requests.Should().OnlyContain(request => request.RateLimit == TimeSpan.FromSeconds(2));
        }

        [Test]
        public async Task should_accept_the_maximum_delay()
        {
            Subject.WaitPolicy = new DownloadWaitPolicy("Metal Torrent Tracker - Download Torrents", "download_after");
            SetupResponses(
                Html("<title>Metal Torrent Tracker - Download Torrents</title><script>var download_after = 120;</script>"),
                Binary(_torrent, "application/x-bittorrent"));

            var result = await Subject.Download(DownloadUri);

            result.Data.Should().Equal(_torrent);
            Subject.Waited.Should().Be(TimeSpan.FromSeconds(120));
            _requests.Should().HaveCount(2);
        }

        [TestCase("<html><title>Other</title><script>var download_after = 1;</script></html>")]
        [TestCase("<html><title>Metal Torrent Tracker - Download Torrents</title><script>var download_after = 0;</script></html>")]
        [TestCase("<html><title>Metal Torrent Tracker - Download Torrents</title><script>var download_after = -1;</script></html>")]
        [TestCase("<html><title>Metal Torrent Tracker - Download Torrents</title><script>var download_after = 1.5;</script></html>")]
        [TestCase("<html><title>Metal Torrent Tracker - Download Torrents</title><script>var download_after = 999999999999999999999;</script></html>")]
        [TestCase("<html><title>Metal Torrent Tracker - Download Torrents</title><script>var download_after = 1,5;</script></html>")]
        [TestCase("<html><title>Metal Torrent Tracker - Download Torrents</title><script>// var download_after = 1;\n</script></html>")]
        [TestCase("<html><title>Metal Torrent Tracker - Download Torrents</title><script>var text = 'var download_after = 1;';</script></html>")]
        [TestCase("<html><title>Metal Torrent Tracker - Download Torrents</title><script>var something_else = 1;</script></html>")]
        [TestCase("<html><title>Metal Torrent Tracker - Download Torrents</title><script>var download_after = 121;</script></html>")]
        [TestCase("<html><title>Metal Torrent Tracker - Download Torrents</title><script>var download_after = 1; var download_after = 2;</script></html>")]
        [TestCase("<html><title>Metal Torrent Tracker - Download Torrents</title><script>var download_after = 1;\nvar download_after = 2;</script></html>")]
        public async Task should_fail_unrecognized_or_invalid_wait_pages_without_continuation(string html)
        {
            Subject.WaitPolicy = new DownloadWaitPolicy("Metal Torrent Tracker - Download Torrents", "download_after");
            SetupResponses(Html(html));

            Assert.ThrowsAsync<ReleaseDownloadException>(() => Subject.Download(DownloadUri));
            ExceptionVerification.ExpectedErrors(1);

            _requests.Should().ContainSingle();
            Subject.Waited.Should().Be(TimeSpan.Zero);
        }

        [TestCase("/* var download_after = 1; */ var download_after = 7;", 7)]
        [TestCase("var s = \"a\\\"var download_after = 1;\"; var download_after = 9;", 9)]
        public async Task should_ignore_assignments_inside_comments_and_strings(string script, int seconds)
        {
            Subject.WaitPolicy = new DownloadWaitPolicy("Metal Torrent Tracker - Download Torrents", "download_after");
            SetupResponses(
                Html($"<title>Metal Torrent Tracker - Download Torrents</title><script>{script}</script>"),
                Binary(_torrent, "application/x-bittorrent"));

            var result = await Subject.Download(DownloadUri);

            result.Data.Should().Equal(_torrent);
            Subject.Waited.Should().Be(TimeSpan.FromSeconds(seconds));
        }

        [Test]
        public async Task should_not_continue_when_wait_page_is_followed_by_html()
        {
            Subject.WaitPolicy = new DownloadWaitPolicy("Metal Torrent Tracker - Download Torrents", "download_after");
            SetupResponses(
                Html("<title>Metal Torrent Tracker - Download Torrents</title><script>var download_after = 1;</script>"),
                Html("<title>Login</title>"));

            Assert.ThrowsAsync<ReleaseDownloadException>(() => Subject.Download(DownloadUri));
            ExceptionVerification.ExpectedErrors(1);

            _requests.Should().HaveCount(2);
            Subject.Waited.Should().Be(TimeSpan.FromSeconds(1));
        }

        [Test]
        public async Task should_validate_continuation_bytes_even_with_html_content_type()
        {
            Subject.WaitPolicy = new DownloadWaitPolicy("Metal Torrent Tracker - Download Torrents", "download_after");
            SetupResponses(
                Html("<title>Metal Torrent Tracker - Download Torrents</title><script>var download_after = 1;</script>"),
                Binary(_torrent, "text/html"));

            var result = await Subject.Download(DownloadUri);

            result.Data.Should().Equal(_torrent);
            _requests.Should().HaveCount(2);
        }

        [Test]
        public async Task should_validate_final_bytes_as_a_torrent()
        {
            Subject.WaitPolicy = new DownloadWaitPolicy("Metal Torrent Tracker - Download Torrents", "download_after");
            SetupResponses(
                Html("<title>Metal Torrent Tracker - Download Torrents</title><script>var download_after = 1;</script>"),
                Binary(Encoding.ASCII.GetBytes("not a torrent"), "application/x-bittorrent"));

            Assert.ThrowsAsync<NotSupportedException>(() => Subject.Download(DownloadUri));
            ExceptionVerification.ExpectedErrors(1);

            _requests.Should().HaveCount(2);
        }

        [Test]
        public async Task should_reject_cross_origin_redirect_before_sending_another_request()
        {
            Subject.WaitPolicy = new DownloadWaitPolicy("Metal Torrent Tracker - Download Torrents", "download_after");
            var redirectHeaders = new HttpHeader { ["Location"] = "https://other.local/private" };
            SetupResponses(new HttpResponse(new HttpRequest(DownloadUri.AbsoluteUri), redirectHeaders, new CookieCollection(), Array.Empty<byte>(), statusCode: HttpStatusCode.Found));

            Assert.ThrowsAsync<ReleaseDownloadException>(() => Subject.Download(DownloadUri));
            ExceptionVerification.ExpectedErrors(1);

            _requests.Should().ContainSingle();
        }

        [Test]
        public async Task should_follow_same_origin_redirect_within_the_existing_limit()
        {
            Subject.WaitPolicy = new DownloadWaitPolicy("Metal Torrent Tracker - Download Torrents", "download_after");
            var redirectHeaders = new HttpHeader { ["Location"] = "/torrents/final" };
            SetupResponses(
                new HttpResponse(new HttpRequest(DownloadUri.AbsoluteUri), redirectHeaders, new CookieCollection(), Array.Empty<byte>(), statusCode: HttpStatusCode.Found),
                Binary(_torrent, "text/plain"));

            var result = await Subject.Download(DownloadUri);

            result.Data.Should().Equal(_torrent);
            _requests.Should().HaveCount(2);
            _requests[1].Url.FullUri.Should().Be("https://tracker.local/torrents/final");
        }

        [Test]
        public async Task should_fail_when_same_origin_redirect_returns_http_error()
        {
            Subject.WaitPolicy = new DownloadWaitPolicy("Metal Torrent Tracker - Download Torrents", "download_after");
            var redirectHeaders = new HttpHeader { ["Location"] = "/torrents/final" };
            SetupResponses(
                new HttpResponse(new HttpRequest(DownloadUri.AbsoluteUri), redirectHeaders, new CookieCollection(), Array.Empty<byte>(), statusCode: HttpStatusCode.Found),
                new HttpResponse(new HttpRequest("https://tracker.local/torrents/final"), new HttpHeader(), new CookieCollection(), "missing", statusCode: HttpStatusCode.NotFound));

            Assert.ThrowsAsync<ReleaseUnavailableException>(() => Subject.Download(DownloadUri));
            ExceptionVerification.ExpectedErrors(1);

            _requests.Should().HaveCount(2);
            Subject.Waited.Should().Be(TimeSpan.Zero);
        }

        [Test]
        public async Task should_not_retry_initial_http_failure()
        {
            Subject.WaitPolicy = new DownloadWaitPolicy("Metal Torrent Tracker - Download Torrents", "download_after");
            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(client => client.ExecuteProxiedAsync(It.IsAny<HttpRequest>(), It.IsAny<ProviderDefinition>()))
                .Callback<HttpRequest, ProviderDefinition>((request, _) => _requests.Add(Snapshot(request)))
                .ThrowsAsync(new WebException("offline", WebExceptionStatus.Timeout));

            Assert.ThrowsAsync<ReleaseDownloadException>(() => Subject.Download(DownloadUri));
            ExceptionVerification.ExpectedErrors(1);

            _requests.Should().ContainSingle();
            Subject.Waited.Should().Be(TimeSpan.Zero);
        }

        [Test]
        public async Task should_not_continue_after_wait_cancellation()
        {
            Subject.WaitPolicy = new DownloadWaitPolicy("Metal Torrent Tracker - Download Torrents", "download_after");
            Subject.CancelWait = true;
            SetupResponses(Html("<title>Metal Torrent Tracker - Download Torrents</title><script>var download_after = 1;</script>"));

            Assert.ThrowsAsync<OperationCanceledException>(() => Subject.Download(DownloadUri));
            ExceptionVerification.ExpectedErrors(1);

            _requests.Should().ContainSingle();
            Subject.Waited.Should().Be(TimeSpan.FromSeconds(1));
        }

        [Test]
        public async Task should_not_retry_when_continuation_request_fails()
        {
            Subject.WaitPolicy = new DownloadWaitPolicy("Metal Torrent Tracker - Download Torrents", "download_after");
            var calls = 0;
            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(client => client.ExecuteProxiedAsync(It.IsAny<HttpRequest>(), It.IsAny<ProviderDefinition>()))
                .Returns((HttpRequest request, ProviderDefinition _) =>
                {
                    _requests.Add(Snapshot(request));
                    calls++;
                    if (calls == 1)
                    {
                        return Task.FromResult(new HttpResponse(
                            request,
                            new HttpHeader { ContentType = "text/html" },
                            new CookieCollection(),
                            "<title>Metal Torrent Tracker - Download Torrents</title><script>var download_after = 1;</script>"));
                    }

                    throw new WebException("offline", WebExceptionStatus.Timeout);
                });

            Assert.ThrowsAsync<ReleaseDownloadException>(() => Subject.Download(DownloadUri));
            ExceptionVerification.ExpectedErrors(1);

            _requests.Should().HaveCount(2);
            Subject.Waited.Should().Be(TimeSpan.FromSeconds(1));
        }

        [Test]
        public async Task should_stop_at_the_existing_redirect_limit()
        {
            Subject.WaitPolicy = new DownloadWaitPolicy("Metal Torrent Tracker - Download Torrents", "download_after");
            var redirectHeaders = new HttpHeader { ["Location"] = "/torrents/next" };
            SetupResponses(
                new HttpResponse(new HttpRequest(DownloadUri.AbsoluteUri), redirectHeaders, new CookieCollection(), Array.Empty<byte>(), statusCode: HttpStatusCode.Found),
                new HttpResponse(new HttpRequest(DownloadUri.AbsoluteUri), redirectHeaders, new CookieCollection(), Array.Empty<byte>(), statusCode: HttpStatusCode.Found),
                new HttpResponse(new HttpRequest(DownloadUri.AbsoluteUri), redirectHeaders, new CookieCollection(), Array.Empty<byte>(), statusCode: HttpStatusCode.Found),
                new HttpResponse(new HttpRequest(DownloadUri.AbsoluteUri), redirectHeaders, new CookieCollection(), Array.Empty<byte>(), statusCode: HttpStatusCode.Found),
                new HttpResponse(new HttpRequest(DownloadUri.AbsoluteUri), redirectHeaders, new CookieCollection(), Array.Empty<byte>(), statusCode: HttpStatusCode.Found));

            Assert.ThrowsAsync<ReleaseDownloadException>(() => Subject.Download(DownloadUri));
            ExceptionVerification.ExpectedErrors(1);

            _requests.Should().HaveCount(5);
            Subject.Waited.Should().Be(TimeSpan.Zero);
        }

        [Test]
        public async Task should_keep_cookies_added_by_proxy_for_the_continuation()
        {
            Subject.WaitPolicy = new DownloadWaitPolicy("Metal Torrent Tracker - Download Torrents", "download_after");
            var calls = 0;
            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(client => client.ExecuteProxiedAsync(It.IsAny<HttpRequest>(), It.IsAny<ProviderDefinition>()))
                .Returns((HttpRequest request, ProviderDefinition _) =>
                {
                    _requests.Add(Snapshot(request));
                    calls++;
                    if (calls == 1)
                    {
                        // Simulates FlareSolverr adding a clearance cookie to the request object after it was sent.
                        request.Cookies["cf_clearance"] = "solved";
                        var html = "<title>Metal Torrent Tracker - Download Torrents</title><script>var download_after = 1;</script>";
                        return Task.FromResult(new HttpResponse(request, new HttpHeader { ContentType = "text/html" }, new CookieCollection(), html));
                    }

                    return Task.FromResult(new HttpResponse(request, new HttpHeader { ContentType = "application/x-bittorrent" }, new CookieCollection(), _torrent));
                });

            var result = await Subject.Download(DownloadUri);

            result.Data.Should().Equal(_torrent);
            CookieHeader(_requests[1]).Should().Contain("cf_clearance=solved");
        }

        [Test]
        public async Task should_not_replay_cookies_that_expired_during_the_wait()
        {
            Subject.WaitPolicy = new DownloadWaitPolicy("Metal Torrent Tracker - Download Torrents", "download_after");
            Subject.RealWait = true;
            SetupResponses(
                HtmlWithSetCookies("<title>Metal Torrent Tracker - Download Torrents</title><script>var download_after = 1;</script>", "session=short; Path=/; Max-Age=1"),
                Binary(_torrent, "application/x-bittorrent"));

            var result = await Subject.Download(DownloadUri);

            result.Data.Should().Equal(_torrent);
            CookieHeader(_requests[1]).Should().NotContain("session=").And.Contain("other=keep");
        }

        [Test]
        public async Task should_send_same_name_cookies_with_different_paths_independently()
        {
            Subject.WaitPolicy = new DownloadWaitPolicy("Metal Torrent Tracker - Download Torrents", "download_after");
            SetupResponses(
                HtmlWithSetCookies("<title>Metal Torrent Tracker - Download Torrents</title><script>var download_after = 1;</script>", "session=new; Path=/torrents"),
                Binary(_torrent, "application/x-bittorrent"));

            var result = await Subject.Download(DownloadUri);

            result.Data.Should().Equal(_torrent);
            var header = CookieHeader(_requests[1]);
            header.Should().Contain("session=new");
            header.Should().Contain("session=old");
        }

        [TestCase("<html><title>Metal Torrent Tracker - Download Torrents</title><script>var download_after = 1\n+ 120;</script></html>")]
        [TestCase("<html><title>Metal Torrent Tracker - Download Torrents</title><script>/download_after = 60;/</script></html>")]
        [TestCase("<html><title>Metal Torrent Tracker - Download Torrents</title><script>var download_after = 1 + 120;</script></html>")]
        public async Task should_reject_delay_expressions_and_regex_text_without_continuation(string html)
        {
            Subject.WaitPolicy = new DownloadWaitPolicy("Metal Torrent Tracker - Download Torrents", "download_after");
            SetupResponses(Html(html));

            Assert.ThrowsAsync<ReleaseDownloadException>(() => Subject.Download(DownloadUri));
            ExceptionVerification.ExpectedErrors(1);

            _requests.Should().ContainSingle();
            Subject.Waited.Should().Be(TimeSpan.Zero);
        }

        [Test]
        public async Task should_preserve_one_request_behavior_without_wait_policy()
        {
            SetupResponses(Binary(_torrent, "application/x-bittorrent"));

            var result = await Subject.Download(DownloadUri);

            result.Data.Should().Equal(_torrent);
            _requests.Should().ContainSingle();
            Subject.Waited.Should().Be(TimeSpan.Zero);
        }

        private void SetupResponses(params HttpResponse[] responses)
        {
            var nextResponse = 0;
            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(client => client.ExecuteProxiedAsync(It.IsAny<HttpRequest>(), It.IsAny<ProviderDefinition>()))
                .ReturnsAsync((HttpRequest request, ProviderDefinition _) =>
                {
                    var response = responses[nextResponse++];
                    _requests.Add(Snapshot(request));
                    return new HttpResponse(request, response.Headers, response.Cookies, response.ResponseData, statusCode: response.StatusCode);
                });
        }

        [TearDown]
        public void TearDown()
        {
            ExceptionVerification.AssertNoUnexpectedLogs();
        }

        private static HttpResponse HtmlWithSetCookies(string content, params string[] setCookieHeaders)
        {
            var headers = new HttpHeader { ContentType = "text/html" };
            foreach (var header in setCookieHeaders)
            {
                headers.Add("Set-Cookie", header);
            }

            return new HttpResponse(new HttpRequest(DownloadUri.AbsoluteUri), headers, new CookieCollection(), content);
        }

        private static string CookieHeader(HttpRequest request)
        {
            return request.CookieContainer?.GetCookieHeader(new Uri(request.Url.FullUri)) ?? string.Empty;
        }

        private static HttpResponse Html(string content, params Cookie[] cookies)
        {
            var cookieCollection = new CookieCollection();
            foreach (var cookie in cookies)
            {
                cookieCollection.Add(cookie);
            }

            var headers = new HttpHeader { ContentType = "text/html" };
            foreach (Cookie cookie in cookieCollection)
            {
                headers.Add("Set-Cookie", $"{cookie.Name}={cookie.Value}; Path={cookie.Path}");
            }

            return new HttpResponse(new HttpRequest(DownloadUri.AbsoluteUri), headers, cookieCollection, content);
        }

        private static HttpResponse Binary(byte[] content, string contentType)
        {
            return new HttpResponse(new HttpRequest(DownloadUri.AbsoluteUri), new HttpHeader { ContentType = contentType }, new CookieCollection(), content);
        }

        private static HttpRequest Snapshot(HttpRequest request)
        {
            var snapshot = new HttpRequest(request.Url.FullUri)
            {
                Method = request.Method,
                RateLimit = request.RateLimit,
                StoreRequestCookie = request.StoreRequestCookie,
                StoreResponseCookie = request.StoreResponseCookie
            };

            foreach (var header in request.Headers)
            {
                snapshot.Headers[header.Key] = header.Value;
            }

            foreach (var cookie in request.Cookies)
            {
                snapshot.Cookies[cookie.Key] = cookie.Value;
            }

            if (request.CookieContainer != null)
            {
                // Copy the jar as it is when the request is sent, keeping path and expiry.
                var uri = new Uri(request.Url.FullUri);
                snapshot.CookieContainer = new CookieContainer();
                snapshot.CookieContainer.Add(uri, request.CookieContainer.GetCookies(uri));
            }

            return snapshot;
        }

        public class TestIndexer : TorrentIndexerBase<CardigannSettings>
        {
            public DownloadWaitPolicy WaitPolicy { get; set; }
            public IDictionary<string, string> CookiesToUse { get; set; }
            public bool CancelWait { get; set; }
            public bool RealWait { get; set; }
            public TimeSpan Waited { get; private set; }

            public override string Name => "Test";
            public override string[] IndexerUrls => new[] { "https://tracker.local/" };
            public override string[] LegacyUrls => Array.Empty<string>();
            public override string Description => string.Empty;
            public override Encoding Encoding => Encoding.UTF8;
            public override string Language => "en-US";
            public override bool FollowRedirect => false;
            public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
            public override bool SupportsRss => true;
            public override bool SupportsSearch => true;
            public override bool SupportsRedirect => false;
            public override bool SupportsPagination => false;
            public override IndexerCapabilities Capabilities { get; protected set; } = new();

            public TestIndexer(IIndexerHttpClient httpClient, NzbDrone.Core.Messaging.Events.IEventAggregator eventAggregator, IIndexerStatusService statusService, NzbDrone.Core.Configuration.IConfigService configService, NLog.Logger logger)
                : base(httpClient, eventAggregator, statusService, configService, logger)
            {
            }

            public override IIndexerRequestGenerator GetRequestGenerator() => throw new NotImplementedException();
            public override IParseIndexerResponse GetParser() => throw new NotImplementedException();
            public override Task<IndexerPageableQueryResult> Fetch(NzbDrone.Core.IndexerSearch.Definitions.MovieSearchCriteria criteria) => Task.FromResult(new IndexerPageableQueryResult());
            public override Task<IndexerPageableQueryResult> Fetch(NzbDrone.Core.IndexerSearch.Definitions.MusicSearchCriteria criteria) => Task.FromResult(new IndexerPageableQueryResult());
            public override Task<IndexerPageableQueryResult> Fetch(NzbDrone.Core.IndexerSearch.Definitions.TvSearchCriteria criteria) => Task.FromResult(new IndexerPageableQueryResult());
            public override Task<IndexerPageableQueryResult> Fetch(NzbDrone.Core.IndexerSearch.Definitions.BookSearchCriteria criteria) => Task.FromResult(new IndexerPageableQueryResult());
            public override Task<IndexerPageableQueryResult> Fetch(NzbDrone.Core.IndexerSearch.Definitions.BasicSearchCriteria criteria) => Task.FromResult(new IndexerPageableQueryResult());
            public override IndexerCapabilities GetCapabilities() => Capabilities;
            protected override Task WaitForDownloadAsync(TimeSpan delay)
            {
                Waited = delay;
                if (CancelWait)
                {
                    throw new OperationCanceledException();
                }

                // Real waiting is only used by the expiry test, which needs cookies to age past Max-Age.
                return RealWait ? Task.Delay(TimeSpan.FromSeconds(1.5)) : Task.CompletedTask;
            }

            protected override DownloadWaitPolicy GetDownloadWaitPolicy() => WaitPolicy;
            protected override IDictionary<string, string> GetCookies() => CookiesToUse;
            protected override Task Test(List<ValidationFailure> failures) => Task.CompletedTask;
        }
    }
}
