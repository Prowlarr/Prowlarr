using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NLog;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Http.Dispatchers;
using NzbDrone.Common.Http.Proxy;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Core.Indexers.Definitions.Cardigann;
using NzbDrone.Core.IndexerProxies;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Security;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.LostFilmTests
{
    [TestFixture]
    public class LostFilmLiveFixture : CoreTest<LostFilm>
    {
        private const string BaseUrl = "https://www.lostfilm.tv";
        private IIndexerHttpClient _realHttpClient;
        private IDictionary<string, string> _storedCookies;

        [SetUp]
        public void Setup()
        {
            if (Environment.GetEnvironmentVariable("PROWLARR_LIVE_TEST") != "1")
            {
                Assert.Ignore("Live test disabled. Set PROWLARR_LIVE_TEST=1 to run.");
            }

            var logger = LogManager.GetLogger("LostFilmLive");
            var cacheManager = new CacheManager();

            _realHttpClient = new IndexerHttpClient(
                Mock.Of<IIndexerProxyFactory>(f => f.GetAvailableProviders() == new List<IIndexerProxy>()),
                Array.Empty<IHttpRequestInterceptor>(),
                cacheManager,
                new RateLimitService(cacheManager, logger),
                new ManagedHttpDispatcher(
                    Mock.Of<IHttpProxySettingsProvider>(),
                    Mock.Of<ICreateManagedWebProxy>(),
                    new X509CertificateValidationService(Mock.Of<IConfigService>(), logger),
                    new UserAgentBuilder(new OsInfo(Array.Empty<IOsVersionAdapter>(), logger)),
                    cacheManager,
                    logger),
                logger);

            Mocker.SetConstant<IIndexerHttpClient>(_realHttpClient);

            _storedCookies = null;
            var envCookies = Environment.GetEnvironmentVariable("LOSTFILM_COOKIES");
            if (envCookies.IsNotNullOrWhiteSpace())
            {
                _storedCookies = envCookies.Split(';', StringSplitOptions.TrimEntries)
                    .Select(c => c.Split('=', 2))
                    .ToDictionary(p => p[0].Trim(), p => p[1].Trim());
            }

            var statusService = new Mock<IIndexerStatusService>();
            statusService.Setup(s => s.UpdateCookies(It.IsAny<int>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<DateTime?>()))
                .Callback((int id, IDictionary<string, string> cookies, DateTime? expiration) => _storedCookies = cookies);
            statusService.Setup(s => s.GetIndexerCookies(It.IsAny<int>())).Returns(() => _storedCookies);
            statusService.Setup(s => s.GetIndexerCookiesExpirationDate(It.IsAny<int>())).Returns(DateTime.Now.AddDays(30));
            Mocker.SetConstant<IIndexerStatusService>(statusService.Object);

            Subject.Definition = new IndexerDefinition
            {
                Name = "LostFilm",
                Settings = new LostFilmSettings
                {
                    BaseUrl = BaseUrl,
                    Username = Environment.GetEnvironmentVariable("LOSTFILM_USER") ?? "testuser",
                    Password = Environment.GetEnvironmentVariable("LOSTFILM_PASSWORD")
                }
            };
        }

        [Test]
        public async Task should_fetch_episode_releases_live()
        {
            if (!await EnsureAuthenticatedAsync())
            {
                Assert.Fail("All captcha login attempts failed");
                return;
            }

            var result = await Subject.Fetch(new TvSearchCriteria { SearchTerm = "breaking bad", Season = 5, Episode = "16", Categories = new[] { 5000 } });

            AssertEpisodeReleases(result);
        }

        [Test]
        public async Task should_fetch_recent_releases_live()
        {
            // Mirrors the indexer configuration "Test" flow (HttpIndexerBase.TestConnection):
            // an empty BasicSearchCriteria must hit the RSS feed (/new) and return releases.
            if (!await EnsureAuthenticatedAsync())
            {
                Assert.Fail("All captcha login attempts failed");
                return;
            }

            var result = await Subject.Fetch(new BasicSearchCriteria());

            result.Queries.Should().HaveCount(1);
            result.Releases.Should().NotBeEmpty();
        }

        private void AssertEpisodeReleases(IndexerPageableQueryResult result)
        {
            result.Queries.Should().HaveCount(1);
            var releases = result.Queries[0].Releases;
            releases.Should().OnlyContain(c => c.GetType() == typeof(TorrentInfo));

            // "breaking bad" also matches "El Camino: A Breaking Bad Movie", so only assert on the
            // Breaking Bad season 5 episode 16 releases.
            var breakingBad = releases.Where(r => r.Title.Contains("Breaking Bad - S5E16")).ToList();
            breakingBad.Should().HaveCount(3);
            breakingBad.Should().Contain(r => r.Title == "Breaking Bad - S5E16 - rus WEBDL (LostFilm)");

            var torrentInfo = breakingBad.First() as TorrentInfo;
            torrentInfo.Title.Should().Contain("Breaking Bad - S5E16");
            torrentInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            torrentInfo.DownloadUrl.Should().StartWith("https://n.tracktor.site/td.php?s=");
            torrentInfo.InfoUrl.Should().Be($"{BaseUrl}/series/Breaking_Bad/season_5/episode_16");
            torrentInfo.Seeders.Should().BeGreaterThanOrEqualTo(1);
            torrentInfo.DownloadVolumeFactor.Should().Be(0);
        }

        [Test]
        public async Task should_login_with_captcha_and_fetch_live()
        {
            if (!await EnsureAuthenticatedAsync())
            {
                Assert.Fail("All captcha login attempts failed");
                return;
            }

            var result = await Subject.Fetch(new TvSearchCriteria { SearchTerm = "breaking bad", Season = 5, Episode = "16", Categories = new[] { 5000 } });

            AssertEpisodeReleases(result);
        }

        [Test]
        public async Task should_search_basic_live()
        {
            if (!await EnsureAuthenticatedAsync())
            {
                Assert.Fail("All captcha login attempts failed");
                return;
            }

            var result = await Subject.Fetch(new BasicSearchCriteria { SearchTerm = "breaking bad" });

            result.Queries.Should().HaveCount(1);
            result.Releases.Should().NotBeEmpty();
            result.Releases.Should().Contain(r => r.Title.Contains("Breaking Bad - S"));
        }

        [Test]
        public async Task should_reuse_session_cookies_without_relogin_live()
        {
            var criteria = new TvSearchCriteria { SearchTerm = "breaking bad", Season = 5, Episode = "16", Categories = new[] { 5000 } };

            if (!await EnsureAuthenticatedAsync())
            {
                Assert.Fail("All captcha login attempts failed");
                return;
            }

            // The durable lf_session must be persisted to the DB rather than clobbered
            // by the stale request snapshot after the login.
            _storedCookies.Should().NotBeNull();
            _storedCookies.Should().ContainKey("lf_session");
            var firstLfSession = _storedCookies["lf_session"];

            var result = await Subject.Fetch(criteria);
            AssertEpisodeReleases(result);

            // A second search must reuse the same persisted session without re-logging in.
            // An unwanted DoLogin would fail without a captcha answer and throw an IndexerAuthException.
            _storedCookies.Should().Contain("lf_session", firstLfSession);

            var secondResult = await Subject.Fetch(criteria);
            AssertEpisodeReleases(secondResult);
        }

        private async Task<bool> EnsureAuthenticatedAsync()
        {
            // Overridable so a host-side poller can share the same files across a volume
            // mount (e.g. LOSTFILM_CAPTCHA_DIR=/logs mapping to the host's /tmp/opencode).
            var captchaDir = Environment.GetEnvironmentVariable("LOSTFILM_CAPTCHA_DIR");
            if (captchaDir.IsNullOrWhiteSpace())
            {
                captchaDir = System.IO.Path.GetTempPath();
            }

            var captchaImageFile = System.IO.Path.Combine(captchaDir, "live_captcha.gif");
            var captchaAnswerFile = System.IO.Path.Combine(captchaDir, "live_captcha_answer.txt");

            try
            {
                // Reuse the existing session (injected cookies or a previous login) if it is still valid.
                var probe = await Subject.Fetch(new TvSearchCriteria { SearchTerm = "breaking bad", Season = 5, Episode = "16", Categories = new[] { 5000 } });

                if (probe.Queries.Count > 0)
                {
                    return true;
                }
            }
            catch (IndexerAuthException)
            {
                // Session expired, fall through to captcha login below.
            }

            for (var attempt = 1; attempt <= 6; attempt++)
            {
                System.IO.File.Delete(captchaAnswerFile);

                var action = Subject.RequestAction("checkCaptcha", new Dictionary<string, string>());
                var captchaRequest = action.GetType().GetProperty("captchaRequest").GetValue(action) as Captcha;

                if (captchaRequest == null || captchaRequest.ImageData.Length == 0)
                {
                    continue;
                }

                try
                {
                    System.IO.File.WriteAllBytes(captchaImageFile, captchaRequest.ImageData);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("LostFilmLive: failed to write captcha to {0}: {1}", captchaImageFile, ex.Message);
                    continue;
                }

                string answer = null;
                for (var i = 0; i < 40 && answer == null; i++)
                {
                    await Task.Delay(3000);
                    if (System.IO.File.Exists(captchaAnswerFile))
                    {
                        answer = System.IO.File.ReadAllText(captchaAnswerFile).Trim();
                    }
                }

                if (answer.IsNullOrWhiteSpace())
                {
                    continue;
                }

                ((LostFilmSettings)Subject.Definition.Settings).Captcha = answer;

                var result = await Subject.Fetch(new TvSearchCriteria { SearchTerm = "breaking bad", Season = 5, Episode = "16", Categories = new[] { 5000 } });

                if (result.Queries.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
