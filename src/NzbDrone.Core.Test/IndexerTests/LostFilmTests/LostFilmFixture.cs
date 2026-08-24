using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Core.Indexers.Definitions.Cardigann;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.LostFilmTests
{
    [TestFixture]
    public class LostFilmFixture : CoreTest<LostFilm>
    {
        private const string BaseUrl = "http://www.lostfilm.tv";

        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition
            {
                Name = "LostFilm",
                Settings = new LostFilmSettings
                {
                    BaseUrl = BaseUrl,
                    Username = "testuser",
                    Password = "testpass"
                }
            };
        }

        private void MockResponse(HttpMethod method, string path, string content, string contentType = "text/html")
        {
            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == method && v.Url.Path == path), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) =>
                    Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", contentType } }, new CookieCollection(), content)));
        }

        private void MockResponse(HttpMethod method, string path, string content, CookieCollection cookies, string contentType = "text/html")
        {
            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == method && v.Url.Path == path), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) =>
                    Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", contentType } }, cookies, content)));
        }

        private void MockResponse(HttpMethod method, string path, byte[] content, string contentType)
        {
            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == method && v.Url.Path == path), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) =>
                    Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", contentType } }, new CookieCollection(), content)));
        }

        private void MockAllResponses()
        {
            // Fallback to an empty page so unmatched requests do not fail the test.
            // Must be registered first: in Moq the last matching setup wins.
            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.IsAny<HttpRequest>(), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) =>
                    Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "text/html" } }, new CookieCollection(), "<html><body></body></html>")));

            MockResponse(HttpMethod.Post, "/ajaxik.php", ReadAllText(@"Files/Indexers/LostFilm/search_breakingbad.json"), "application/json");
            MockResponse(HttpMethod.Get, "/series/Breaking_Bad/season_5/episode_16", ReadAllText(@"Files/Indexers/LostFilm/episode_auth.html"));
            MockResponse(HttpMethod.Get, "/movies/El_Camino_A_Breaking_Bad_Movie/season_5/episode_16", "<html><body></body></html>");
            MockResponse(HttpMethod.Get, "/v_search.php", ReadAllText(@"Files/Indexers/LostFilm/vsearch_bb.html"));
            MockResponse(HttpMethod.Get, "/V/", ReadAllText(@"Files/Indexers/LostFilm/tracker_bb.html"));
        }

        [Test]
        public async Task should_parse_episode_releases_from_LostFilm()
        {
            MockAllResponses();

            var result = await Subject.Fetch(new TvSearchCriteria { SearchTerm = "breaking bad", Season = 5, Episode = "16", Categories = new[] { 5000 } });

            result.Queries.Should().HaveCount(1);
            result.Queries[0].Releases.Should().HaveCount(3);

            var releases = result.Releases;

            releases.Should().HaveCount(3);
            releases.Should().OnlyContain(c => c.GetType() == typeof(TorrentInfo));

            var torrentInfo = releases.First() as TorrentInfo;

            torrentInfo.Title.Should().Be("Breaking Bad - S5E16 - rus WEBDL (LostFilm)");
            torrentInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            torrentInfo.DownloadUrl.Should().StartWith("https://n.tracktor.site/td.php?s=");
            torrentInfo.InfoUrl.Should().Be($"{BaseUrl}/series/Breaking_Bad/season_5/episode_16");
            torrentInfo.CommentUrl.Should().BeNullOrEmpty();
            torrentInfo.Indexer.Should().Be(Subject.Definition.Name);
            torrentInfo.PublishDate.Should().Be(DateTime.Parse("2013-09-29"));
            torrentInfo.Size.Should().Be(11800422645);
            torrentInfo.Seeders.Should().Be(1);
            torrentInfo.Peers.Should().Be(2);
            torrentInfo.DownloadVolumeFactor.Should().Be(0);
            torrentInfo.UploadVolumeFactor.Should().Be(1);
            torrentInfo.Categories.Should().ContainSingle();
        }

        [Test]
        public void should_use_half_second_rate_limit()
        {
            Subject.RateLimit.Should().Be(TimeSpan.FromSeconds(0.5));
        }

        [Test]
        public void should_expose_captcha_as_cardigann_captcha_schema_field()
        {
            // The captcha input is rendered by the generic schema pipeline: the field type
            // string must serialize to "cardigannCaptcha" (FieldType -> FirstCharToLower).
            var attribute = typeof(LostFilmSettings)
                .GetProperty(nameof(LostFilmSettings.Captcha))
                ?.GetCustomAttribute<FieldDefinitionAttribute>(false);

            attribute.Should().NotBeNull();
            attribute.Type.Should().Be(FieldType.CardigannCaptcha);
            attribute.Order.Should().Be(4);
        }

        [Test]
        public void should_return_captcha_from_checkCaptcha_action()
        {
            MockResponse(HttpMethod.Post, "/ajaxik.php", "{\"success\":true,\"result\":\"ok\"}", "application/json");
            MockResponse(HttpMethod.Get, "/login", ReadAllText(@"Files/Indexers/LostFilm/login.html"));
            MockResponse(HttpMethod.Get, "/simple_captcha.php", new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "image/jpeg");

            var result = Subject.RequestAction("checkCaptcha", new Dictionary<string, string>());

            var captchaRequest = result.GetType().GetProperty("captchaRequest").GetValue(result) as Captcha;

            captchaRequest.Should().NotBeNull();
            captchaRequest.ContentType.Should().Be("image/jpeg");
            captchaRequest.ImageData.Should().Equal(0x89, 0x50, 0x4E, 0x47);
        }

        [Test]
        public void should_fetch_captcha_page_as_guest_even_with_valid_session()
        {
            // Regression: LostFilm only renders the captcha form to guests. When a valid
            // lf_session is present, checkCaptcha must still fetch /login without the session
            // cookie, otherwise the authenticated layout (no captcha) is returned and the
            // settings UI shows an empty image.
            IDictionary<string, string> storedCookies = new Dictionary<string, string>
            {
                { "lf_session", "VALID_SESSION" },
                { "PHPSESSID", "VALID_PHP" }
            };

            var statusService = Mocker.GetMock<IIndexerStatusService>();
            statusService.Setup(s => s.UpdateCookies(It.IsAny<int>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<DateTime?>()))
                .Callback((int id, IDictionary<string, string> cookies, DateTime? expiration) => storedCookies = cookies);
            statusService.Setup(s => s.GetIndexerCookies(It.IsAny<int>())).Returns(() => storedCookies);
            statusService.Setup(s => s.GetIndexerCookiesExpirationDate(It.IsAny<int>())).Returns(() => DateTime.Now.AddDays(30));

            var client = Mocker.GetMock<IIndexerHttpClient>();
            client.Setup(o => o.ExecuteProxiedAsync(
                    It.Is<HttpRequest>(v => v.Method == HttpMethod.Get && v.Url.Path == "/login"
                        && (!v.Cookies.ContainsKey("lf_session") || v.Cookies["lf_session"] == null)),
                    Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) =>
                    Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "text/html" } }, new CookieCollection(), ReadAllText(@"Files/Indexers/LostFilm/login.html"))));
            MockResponse(HttpMethod.Get, "/simple_captcha.php", new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "image/jpeg");

            var result = Subject.RequestAction("checkCaptcha", new Dictionary<string, string>());
            var captchaRequest = result.GetType().GetProperty("captchaRequest").GetValue(result) as Captcha;

            captchaRequest.Should().NotBeNull();
            captchaRequest.ContentType.Should().Be("image/jpeg");
            captchaRequest.ImageData.Should().Equal(0x89, 0x50, 0x4E, 0x47);
        }

        [Test]
        public async Task should_find_releases_for_movie_search()
        {
            MockResponse(HttpMethod.Post, "/ajaxik.php", ReadAllText(@"Files/Indexers/LostFilm/search_avatar.json"), "application/json");
            MockResponse(HttpMethod.Get, "/movies/Avatar_Aang_The_Last_Airbender/seasons", ReadAllText(@"Files/Indexers/LostFilm/episode_auth.html"));
            MockResponse(HttpMethod.Get, "/movies/Avatar_Aang_The_Last_Airbender", ReadAllText(@"Files/Indexers/LostFilm/episode_auth.html"));
            MockResponse(HttpMethod.Get, "/v_search.php", ReadAllText(@"Files/Indexers/LostFilm/vsearch_bb.html"));
            MockResponse(HttpMethod.Get, "/V/", ReadAllText(@"Files/Indexers/LostFilm/tracker_bb.html"));

            var result = await Subject.Fetch(new MovieSearchCriteria { SearchTerm = "avatar aang", Categories = new[] { 2000 } });

            result.Releases.Should().HaveCount(3);
            result.Releases.Should().OnlyContain(r => r.InfoUrl == $"{BaseUrl}/movies/Avatar_Aang_The_Last_Airbender");
            result.Releases.Should().OnlyContain(r => r.Categories.Contains(NewznabStandardCategory.Movies));
            result.Releases.Should().OnlyContain(r => !r.Title.Contains("S1E1"));
            result.Releases.Should().OnlyContain(r => r.Title.StartsWith("Breaking Bad - Movie - 2013 -"));
            result.Releases.Should().OnlyContain(r => r.Title.EndsWith("(LostFilm)"));
        }

        [Test]
        public async Task should_add_movie_component_even_when_title_ends_with_movie()
        {
            MockResponse(HttpMethod.Post, "/ajaxik.php", ReadAllText(@"Files/Indexers/LostFilm/search_avatar.json"), "application/json");
            MockResponse(HttpMethod.Get, "/movies/Avatar_Aang_The_Last_Airbender/seasons", ReadAllText(@"Files/Indexers/LostFilm/episode_auth.html"));
            MockResponse(HttpMethod.Get, "/movies/Avatar_Aang_The_Last_Airbender", ReadAllText(@"Files/Indexers/LostFilm/episode_auth.html"));
            MockResponse(HttpMethod.Get, "/v_search.php", ReadAllText(@"Files/Indexers/LostFilm/vsearch_bb.html"));
            var trackerHtml = ReadAllText(@"Files/Indexers/LostFilm/tracker_bb.html")
                .Replace("Breaking Bad, сериал", "El Camino: A Breaking Bad Movie, сериал");
            MockResponse(HttpMethod.Get, "/V/", trackerHtml);

            var result = await Subject.Fetch(new MovieSearchCriteria { SearchTerm = "avatar aang", Categories = new[] { 2000 } });

            result.Releases.Should().HaveCount(3);
            result.Releases.Should().OnlyContain(r => r.Title.StartsWith("El Camino: A Breaking Bad Movie - Movie - 2013 -"));
            result.Releases.Should().OnlyContain(r => r.Title.EndsWith("(LostFilm)"));
        }

        [Test]
        public async Task should_persist_cookies_from_relogin()
        {
            IDictionary<string, string> storedCookies = null;
            DateTime? storedExpiration = null;

            var statusService = Mocker.GetMock<IIndexerStatusService>();
            statusService.Setup(s => s.UpdateCookies(It.IsAny<int>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<DateTime?>()))
                .Callback((int id, IDictionary<string, string> cookies, DateTime? expiration) =>
                {
                    storedCookies = cookies;
                    storedExpiration = expiration;
                });
            statusService.Setup(s => s.GetIndexerCookies(It.IsAny<int>())).Returns(() => storedCookies);
            statusService.Setup(s => s.GetIndexerCookiesExpirationDate(It.IsAny<int>())).Returns(() => storedExpiration ?? DateTime.Now.AddDays(30));

            // First /new response is anonymous which triggers a re-login; the login response
            // sets a fresh durable lf_session cookie with a far-future Expires.
            const string anonPage = "<html><body><a href=\"/login\" class=\"link\">Вход</a></body></html>";
            const string authedPage = "<html><body><div class=\"row\"><a href=\"/series/Test_Show/season_1/episode_1\">Test</a></div></body></html>";

            var client = Mocker.GetMock<IIndexerHttpClient>();
            var newRequestCount = 0;
            client.Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get && v.Url.Path == "/new"), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) =>
                    Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "text/html" } }, new CookieCollection(), ++newRequestCount == 1 ? anonPage : authedPage)));

            var loginCookies = new CookieCollection
            {
                new Cookie("lf_session", "NEWSESSION", "/") { Expires = new DateTime(2027, 6, 1) },
                new Cookie("PHPSESSID", "NEWPHP", "/")
            };
            MockResponse(HttpMethod.Post, "/ajaxik.php", "{\"success\":true,\"result\":\"ok\"}", loginCookies, "application/json");
            MockResponse(HttpMethod.Get, "/series/Test_Show/season_1/episode_1", ReadAllText(@"Files/Indexers/LostFilm/episode_auth.html"));
            MockResponse(HttpMethod.Get, "/v_search.php", ReadAllText(@"Files/Indexers/LostFilm/vsearch_bb.html"));
            MockResponse(HttpMethod.Get, "/V/", ReadAllText(@"Files/Indexers/LostFilm/tracker_bb.html"));

            var result = await Subject.Fetch(new BasicSearchCriteria());

            result.Releases.Should().NotBeEmpty();

            // The fresh lf_session from the login must survive in the DB after the request,
            // with the server-derived expiry, instead of being clobbered by the stale request snapshot.
            storedCookies.Should().Contain("lf_session", "NEWSESSION");
            storedCookies.Should().Contain("PHPSESSID", "NEWPHP");
            storedExpiration.Should().Be(new DateTime(2027, 6, 1));
        }

        [Test]
        public async Task should_replay_request_with_fresh_cookies_after_stale_session_relogin()
        {
            // Seed expired-but-unexpired (stale) session cookies: the first request is sent with
            // a dead PHPSESSID, which the server answers with the login page. After the re-login
            // the replayed request must carry the fresh session, not the stale request snapshot.
            IDictionary<string, string> storedCookies = new Dictionary<string, string>
            {
                { "lf_session", "STALE_SESSION" },
                { "PHPSESSID", "STALE_PHP" }
            };
            DateTime? storedExpiration = DateTime.Now.AddDays(30);

            var statusService = Mocker.GetMock<IIndexerStatusService>();
            statusService.Setup(s => s.UpdateCookies(It.IsAny<int>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<DateTime?>()))
                .Callback((int id, IDictionary<string, string> cookies, DateTime? expiration) =>
                {
                    storedCookies = cookies;
                    storedExpiration = expiration;
                });
            statusService.Setup(s => s.GetIndexerCookies(It.IsAny<int>())).Returns(() => storedCookies);
            statusService.Setup(s => s.GetIndexerCookiesExpirationDate(It.IsAny<int>())).Returns(() => storedExpiration ?? DateTime.Now.AddDays(30));

            const string anonPage = "<html><body><a href=\"/login\" class=\"link\">Вход</a></body></html>";
            const string authedPage = "<html><body><div class=\"row\"><a href=\"/series/Test_Show/season_1/episode_1\">Test</a></div></body></html>";

            var client = Mocker.GetMock<IIndexerHttpClient>();
            var newRequestCount = 0;
            client.Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get && v.Url.Path == "/new"), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) =>
                {
                    newRequestCount++;
                    if (newRequestCount == 1)
                    {
                        r.Cookies.Should().Contain("lf_session", "STALE_SESSION");
                    }
                    else
                    {
                        // Without re-applying the fresh session the replayed request would still
                        // carry STALE_SESSION and the server would bounce us back to the login page.
                        r.Cookies.Should().Contain("lf_session", "NEWSESSION");
                        r.Cookies.Should().Contain("PHPSESSID", "NEWPHP");
                    }

                    return Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "text/html" } }, new CookieCollection(), newRequestCount == 1 ? anonPage : authedPage));
                });

            var loginCookies = new CookieCollection
            {
                new Cookie("lf_session", "NEWSESSION", "/") { Expires = new DateTime(2027, 6, 1) },
                new Cookie("PHPSESSID", "NEWPHP", "/")
            };
            MockResponse(HttpMethod.Post, "/ajaxik.php", "{\"success\":true,\"result\":\"ok\"}", loginCookies, "application/json");
            MockResponse(HttpMethod.Get, "/series/Test_Show/season_1/episode_1", ReadAllText(@"Files/Indexers/LostFilm/episode_auth.html"));
            MockResponse(HttpMethod.Get, "/v_search.php", ReadAllText(@"Files/Indexers/LostFilm/vsearch_bb.html"));
            MockResponse(HttpMethod.Get, "/V/", ReadAllText(@"Files/Indexers/LostFilm/tracker_bb.html"));

            var result = await Subject.Fetch(new BasicSearchCriteria());

            result.Releases.Should().NotBeEmpty();
            newRequestCount.Should().Be(2);
        }

        [Test]
        public async Task should_skip_tracker_row_without_description_and_keep_valid_siblings()
        {
            MockAllResponses();
            MockResponse(HttpMethod.Get, "/new", "<html><body><div class=\"row\"><a href=\"/series/Breaking_Bad/season_5/episode_16\">Breaking Bad</a></div></body></html>");

            // One tracker row is missing div.inner-box--desc (previously an NRE that killed the
            // whole RSS sync); the sibling row is valid and must still be parsed.
            const string trackerPage = "<html><body><div class=\"inner-box general\">" +
                "<div class=\"inner-box--subtitle\">Breaking Bad, сериал</div>" +
                "<div class=\"inner-box--text\">5 сезон</div>" +
                "<div class=\"inner-box--list\">" +
                "<div class=\"inner-box--item\"><div class=\"inner-box--link main\"><a href=\"https://n.tracktor.site/td.php?s=broken\">Bad row</a></div></div>" +
                "<div class=\"inner-box--item\"><div class=\"inner-box--link main\"><a href=\"https://n.tracktor.site/td.php?s=good\">Good row</a></div>" +
                "<div class=\"inner-box--desc\">Видео: 1080p WEB-DLRip. Размер: 40.61 ГБ. Перевод: Многоголосый закадровый (LostFilm.TV)</div></div>" +
                "</div></div></body></html>";
            MockResponse(HttpMethod.Get, "/V/", trackerPage);

            var result = await Subject.Fetch(new BasicSearchCriteria());

            result.Releases.Should().HaveCount(1);
            var release = result.Releases.Single();
            release.Title.Should().Be("Breaking Bad - S5E16 - rus 1080p WEBDL (LostFilm)");
            release.DownloadUrl.Should().Be("https://n.tracktor.site/td.php?s=good");
        }

        [Test]
        public async Task should_resolve_relative_download_url_against_base_url()
        {
            MockAllResponses();
            MockResponse(HttpMethod.Get, "/new", "<html><body><div class=\"row\"><a href=\"/series/Breaking_Bad/season_5/episode_16\">Breaking Bad</a></div></body></html>");

            // A relative tracker href used to throw UriFormatException; it must resolve against BaseUrl.
            const string trackerPage = "<html><body><div class=\"inner-box general\">" +
                "<div class=\"inner-box--subtitle\">Breaking Bad, сериал</div>" +
                "<div class=\"inner-box--text\">5 сезон</div>" +
                "<div class=\"inner-box--list\">" +
                "<div class=\"inner-box--item\"><div class=\"inner-box--link main\"><a href=\"td.php?s=abc123\">Relative row</a></div>" +
                "<div class=\"inner-box--desc\">Видео: 1080p WEB-DLRip. Размер: 40.61 ГБ. Перевод: Многоголосый закадровый (LostFilm.TV)</div></div>" +
                "</div></div></body></html>";
            MockResponse(HttpMethod.Get, "/V/", trackerPage);

            var result = await Subject.Fetch(new BasicSearchCriteria());

            result.Releases.Should().ContainSingle();
            result.Releases.Single().DownloadUrl.Should().Be($"{BaseUrl}/td.php?s=abc123");
        }

        [Test]
        public async Task should_skip_series_row_without_title_element_during_filtered_search()
        {
            // The first /ajaxik.php call (searching "breaking bad crystal") returns nothing, which
            // makes the search loop drop a keyword and retry with "breaking bad". The leftover
            // "crystal" keyword becomes the episode-title filter and exercises the td.gamma guard.
            const string singleSeriesJson = "{\"data\":{\"series\":[{\"id\":\"119\",\"title\":\"Во все тяжкие\",\"title_orig\":\"Breaking Bad\",\"link\":\"/series/Breaking_Bad\"}]},\"result\":\"ok\"}";

            var client = Mocker.GetMock<IIndexerHttpClient>();
            client.Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Post && v.Url.Path == "/ajaxik.php"), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) =>
                {
                    var body = r.GetContent() ?? string.Empty;
                    var content = body.Contains("crystal") ? "{\"data\":false,\"result\":\"ok\"}" : singleSeriesJson;
                    return Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), content));
                });

            // Season 5 block with two rows: the first has no td.gamma > div (previously an NRE),
            // the second matches the "crystal" filter and must be parsed normally.
            const string seasonPage = "<html><body><div class=\"serie-block\">" +
                "<div class=\"movie-details-block\"><div class=\"external-btn\" onclick=\"PlayEpisode('119005016')\"></div>" +
                "<div class=\"haveseen-btn\" data-code=\"season-5\"></div></div>" +
                "<table class=\"movie-parts-list\"><tbody>" +
                "<tr><td class=\"zeta\"><div class=\"external-btn\" onclick=\"PlayEpisode('119005016')\"></div></td>" +
                "<td class=\"delta\" onclick=\"goTo('/series/Breaking_Bad/season_5/episode_16/',false)\"><span class=\"small-text\">Eng: 23.05.2017</span></td></tr>" +
                "<tr><td class=\"gamma\"><div>Crystal Blue</div></td>" +
                "<td class=\"zeta\"><div class=\"external-btn\" onclick=\"PlayEpisode('119005016')\"></div></td>" +
                "<td class=\"delta\" onclick=\"goTo('/series/Breaking_Bad/season_5/episode_16/',false)\"><span class=\"small-text\">Eng: 23.05.2017</span></td></tr>" +
                "</tbody></table></div></body></html>";
            MockResponse(HttpMethod.Get, "/series/Breaking_Bad/season_5", seasonPage);
            MockResponse(HttpMethod.Get, "/v_search.php", ReadAllText(@"Files/Indexers/LostFilm/vsearch_bb.html"));
            MockResponse(HttpMethod.Get, "/V/", ReadAllText(@"Files/Indexers/LostFilm/tracker_bb.html"));

            var result = await Subject.Fetch(new TvSearchCriteria { SearchTerm = "breaking bad crystal", Season = 5, Categories = new[] { 5000 } });

            result.Releases.Should().NotBeEmpty();
            result.Releases.Should().OnlyContain(r => r.Title.Contains("Breaking Bad - S5E16"));
        }

        [Test]
        public async Task should_fallback_to_mirror_when_primary_is_geo_blocked()
        {
            // Regression: www.lostfilm.tv serves the search API but Cloudflare geo-blocks its
            // content pages with HTTP 451, which used to yield an empty result set. The content
            // request must be transparently retried on the next working mirror and all follow-up
            // requests (tracker, redirection, re-login) must stay on that mirror.
            const string geoBlockedPage = "<html><body>Unavailable For Legal Reasons</body></html>";

            var client = Mocker.GetMock<IIndexerHttpClient>();

            // Fallback to an empty page so unmatched requests do not fail the test (first wins
            // nothing here: Moq uses the last matching setup, so register it first).
            client.Setup(o => o.ExecuteProxiedAsync(It.IsAny<HttpRequest>(), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) =>
                    Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "text/html" } }, new CookieCollection(), "<html><body></body></html>")));

            // The search API lives on the primary host and still works there; the same endpoint
            // handles the login submission (the anonymous mirror page triggers a re-login).
            client.Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Post && v.Url.Path == "/ajaxik.php"), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) =>
                {
                    var body = r.GetContent() ?? string.Empty;

                    if (body.Contains("type=login"))
                    {
                        var loginCookies = new CookieCollection
                        {
                            new Cookie("lf_session", "MIRROR_SESSION", "/") { Expires = new DateTime(2027, 6, 1) },
                            new Cookie("PHPSESSID", "MIRROR_PHP", "/")
                        };
                        return Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "application/json" } }, loginCookies, "{\"success\":true,\"result\":\"ok\"}"));
                    }

                    return Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), ReadAllText(@"Files/Indexers/LostFilm/search_shogun.json")));
                });

            MockResponse(HttpMethod.Get, "/v_search.php", ReadAllText(@"Files/Indexers/LostFilm/vsearch_shogun.html"));
            MockResponse(HttpMethod.Get, "/V/", ReadAllText(@"Files/Indexers/LostFilm/tracker_shogun.html"));

            // Host-specific setups must be registered after the path-based ones (Moq: last wins).
            client.Setup(o => o.ExecuteProxiedAsync(
                    It.Is<HttpRequest>(v => v.Method == HttpMethod.Get && v.Url.Host == "www.lostfilm.tv" && v.Url.Path == "/series/Shogun/seasons"),
                    Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) =>
                    Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "text/html" } }, new CookieCollection(), geoBlockedPage, statusCode: HttpStatusCode.UnavailableForLegalReasons)));
            client.Setup(o => o.ExecuteProxiedAsync(
                    It.Is<HttpRequest>(v => v.Method == HttpMethod.Get && v.Url.Host == "www.lostfilmtv5.site" && v.Url.Path == "/series/Shogun/seasons"),
                    Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) =>
                    Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "text/html" } }, new CookieCollection(), ReadAllText(@"Files/Indexers/LostFilm/shogun_seasons.html"))));

            var result = await Subject.Fetch(new TvSearchCriteria { SearchTerm = "shogun", Categories = new[] { 5000 } });

            // The geo-blocked primary must not swallow the search: the mirror serves the seasons
            // page and the season-pack release is parsed from it, keeping InfoUrl on the mirror.
            result.Releases.Should().HaveCount(3);
            result.Releases.Should().OnlyContain(r => r.InfoUrl == "https://www.lostfilmtv5.site/series/Shogun/seasons");
            result.Releases.Should().OnlyContain(r => r.Title.StartsWith("Сёгун - S1"));

            client.Verify(
                o => o.ExecuteProxiedAsync(
                    It.Is<HttpRequest>(v => v.Method == HttpMethod.Get && v.Url.Host == "www.lostfilm.tv" && v.Url.Path == "/series/Shogun/seasons"),
                    Subject.Definition),
                Times.Once());

            // The mirror is hit twice: once for the fallback, once for the re-login replay.
            client.Verify(
                o => o.ExecuteProxiedAsync(
                    It.Is<HttpRequest>(v => v.Method == HttpMethod.Get && v.Url.Host == "www.lostfilmtv5.site" && v.Url.Path == "/series/Shogun/seasons"),
                    Subject.Definition),
                Times.Exactly(2));
        }

        [Test]
        public void should_advertise_tv_and_movie_categories()
        {
            Subject.Capabilities.Categories.GetTrackerCategories().Should().Contain(new[] { "1", "2" });

            Subject.Capabilities.Categories.MapTrackerCatToNewznab("1").Should().Contain(NewznabStandardCategory.TV);
            Subject.Capabilities.Categories.MapTrackerCatToNewznab("2").Should().Contain(NewznabStandardCategory.Movies);
        }

        [Test]
        public void should_search_with_basic_search_criteria()
        {
            var chain = Subject.GetRequestGenerator().GetSearchRequests(new BasicSearchCriteria { SearchTerm = "breaking bad" });

            var paths = chain.GetAllTiers().SelectMany(x => x).Select(x => x.HttpRequest.Url.Path).ToList();

            paths.Should().Contain("/ajaxik.php");
            paths.Should().NotContain("/new");
        }

        [Test]
        public void should_return_rss_for_empty_basic_search_criteria()
        {
            var chain = Subject.GetRequestGenerator().GetSearchRequests(new BasicSearchCriteria());

            var paths = chain.GetAllTiers().SelectMany(x => x).Select(x => x.HttpRequest.Url.Path).ToList();

            paths.Should().Contain("/new");
            paths.Should().NotContain("/ajaxik.php");
        }

        [Test]
        public async Task should_return_no_releases_when_search_degenerates_to_stopword()
        {
            // Regression: "The Cuphead Show! Шоу Чашека! 2022" has no matching series, so the search
            // loop drops keywords until only the stopword "the" remains. Without a relevance filter
            // that matches dozens of unrelated series and fetches their episodes (the original flood).
            const string decoySeriesJson = "{\"data\":{\"series\":[" +
                "{\"id\":\"1\",\"title\":\"4400\",\"title_orig\":\"The 4400\",\"link\":\"/series/The_4400\"}," +
                "{\"id\":\"2\",\"title\":\"Сотня\",\"title_orig\":\"The 100\",\"link\":\"/series/The_100\"}," +
                "{\"id\":\"3\",\"title\":\"Офис\",\"title_orig\":\"The Office\",\"link\":\"/series/The_Office\"}]," +
                "\"result\":\"ok\"}";

            var client = Mocker.GetMock<IIndexerHttpClient>();

            // Fallback to an empty page so unmatched requests do not fail the test (first wins
            // nothing here: Moq uses the last matching setup, so register it first).
            client.Setup(o => o.ExecuteProxiedAsync(It.IsAny<HttpRequest>(), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) =>
                    Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "text/html" } }, new CookieCollection(), "<html><body></body></html>")));

            client.Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Post && v.Url.Path == "/ajaxik.php"), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) =>
                {
                    var body = r.GetContent() ?? string.Empty;
                    var content = body.Contains("cuphead", StringComparison.OrdinalIgnoreCase) ? "{\"data\":[],\"result\":\"ok\"}" : decoySeriesJson;
                    return Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), content));
                });

            var result = await Subject.Fetch(new TvSearchCriteria { SearchTerm = "The Cuphead Show! Шоу Чашека! 2022", Categories = new[] { 5000 } });

            result.Releases.Should().BeEmpty();

            client.Verify(
                o => o.ExecuteProxiedAsync(
                    It.Is<HttpRequest>(v => v.Method == HttpMethod.Get && (v.Url.Path.StartsWith("/series/") || v.Url.Path.StartsWith("/movies/"))),
                    Subject.Definition),
                Times.Never());
        }

        [Test]
        public async Task should_fetch_only_highest_scoring_series_on_tie()
        {
            // Regression: the api returns Breaking Bad and El Camino for "breaking bad"; both match
            // the informative tokens equally. Only the highest-scoring series must be fetched, the
            // other must not trigger any episode downloads (the tie resolves to API order via the
            // stable sort).
            MockAllResponses();

            var result = await Subject.Fetch(new TvSearchCriteria { SearchTerm = "breaking bad", Season = 5, Episode = "16", Categories = new[] { 5000 } });

            result.Releases.Should().HaveCount(3);
            result.Releases.Should().OnlyContain(r => r.InfoUrl == $"{BaseUrl}/series/Breaking_Bad/season_5/episode_16");

            var client = Mocker.GetMock<IIndexerHttpClient>();
            client.Verify(
                o => o.ExecuteProxiedAsync(
                    It.Is<HttpRequest>(v => v.Method == HttpMethod.Get && v.Url.Path == "/series/Breaking_Bad/season_5/episode_16"),
                    Subject.Definition),
                Times.Once());
            client.Verify(
                o => o.ExecuteProxiedAsync(
                    It.Is<HttpRequest>(v => v.Method == HttpMethod.Get && v.Url.Path == "/movies/El_Camino_A_Breaking_Bad_Movie/season_5/episode_16"),
                    Subject.Definition),
                Times.Never());
        }

        [Test]
        public async Task should_keep_single_series_that_matches_only_via_link()
        {
            // Regression: a single series result may carry readable titles that do not contain the
            // search tokens (e.g. transliteration) and a missing title_orig. It must still be accepted
            // when its link slug matches, instead of being pruned by the relevance filter.
            const string singleSeriesJson = "{\"data\":{\"series\":[{\"id\":\"710\",\"title\":\"Эль Камино\",\"link\":\"/movies/El_Camino_A_Breaking_Bad_Movie\"}]},\"result\":\"ok\"}";

            MockResponse(HttpMethod.Post, "/ajaxik.php", singleSeriesJson, "application/json");
            MockResponse(HttpMethod.Get, "/movies/El_Camino_A_Breaking_Bad_Movie/seasons", ReadAllText(@"Files/Indexers/LostFilm/episode_auth.html"));
            MockResponse(HttpMethod.Get, "/movies/El_Camino_A_Breaking_Bad_Movie", ReadAllText(@"Files/Indexers/LostFilm/episode_auth.html"));
            MockResponse(HttpMethod.Get, "/v_search.php", ReadAllText(@"Files/Indexers/LostFilm/vsearch_bb.html"));
            MockResponse(HttpMethod.Get, "/V/", ReadAllText(@"Files/Indexers/LostFilm/tracker_bb.html"));

            var result = await Subject.Fetch(new TvSearchCriteria { SearchTerm = "camino", Categories = new[] { 5000 } });

            result.Releases.Should().NotBeEmpty();
            result.Releases.Should().OnlyContain(r => r.InfoUrl == $"{BaseUrl}/movies/El_Camino_A_Breaking_Bad_Movie");
        }
    }
}
