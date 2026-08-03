using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using FluentValidation.Results;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Http.CloudFlare;
using NzbDrone.Core.Indexers.Definitions.Cardigann;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class LostFilm : TorrentIndexerBase<LostFilmSettings>
    {
        private static readonly Regex ParsePlayEpisodeRegex = new(@"PlayEpisode\('(?<id>\d+)(?<season>\d{3})(?<episode>\d{3})'\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ParseReleaseDetailsRegex = new("Видео:\\ (?<quality>.+).\\ Размер:\\ (?<size>.+).\\ Перевод", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");

        public override string Name => "LostFilm.tv";
        public override string[] IndexerUrls => new[]
        {
            "https://www.lostfilm.tv/",
            "https://www.lostfilmtv5.site/",
            "https://www.lostfilmtv2.site/",
            "https://www.lostfilmtv3.site/",
            "https://www.lostfilm.today/",
            "https://www.lostfilm.download/",
            "https://www.lostfilm.run/",
            "https://lostfilm.site/",
            "https://www.lostfilm.life/",
            "https://www.lostfilm.uno/",
            "https://www.lostfilm.tw/"
        };
        public override string[] LegacyUrls => new[]
        {
            "https://lostfilm.tw/",
            "https://www.lostfilm.win/",
            "https://www.lostfilmtv.site/"
        };
        public override string Language => "ru-RU";
        public override string Description => "LostFilm is a RUSSIAN Semi-Private site. Unique portal for foreign series";
        public override Encoding Encoding => Encoding.UTF8;
        public override IndexerPrivacy Privacy => IndexerPrivacy.SemiPrivate;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public override TimeSpan RateLimit => TimeSpan.FromSeconds(0.5);

        private string BaseUrl => Settings.BaseUrl.TrimEnd('/');

        private readonly object _cookieSync = new object();

        private DateTime? _cookiesExpiration;

        private IDictionary<string, string> _persistedCookies;
        private DateTime? _persistedExpiration;

        public LostFilm(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new LostFilmRequestGenerator(Settings);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new LostFilmParser();
        }

        public override IEnumerable<ProviderDefinition> DefaultDefinitions
        {
            get
            {
                foreach (var definition in base.DefaultDefinitions)
                {
                    ((IndexerDefinition)definition).ExtraFields = new List<SettingsField>
                    {
                        new()
                        {
                            Name = "cardigannCaptcha",
                            Type = "cardigannCaptcha",
                            Label = "CAPTCHA"
                        }
                    };

                    yield return definition;
                }
            }
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            if (action == "checkCaptcha")
            {
                return new
                {
                    captchaRequest = GetLoginPageAsync().GetAwaiter().GetResult()
                };
            }

            return base.RequestAction(action, query);
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            return httpResponse.Content?.Contains("href=\"/login\"") == true;
        }

        protected override async Task DoLogin()
        {
            _logger.Debug("Logging in to LostFilm.tv");

            // Performing Logout is required to invalidate previous session otherwise the `{"error":1,"result":"ok"}` will be returned.
            await Logout();

            var requestBuilder = new HttpRequestBuilder(BaseUrl + "/ajaxik.php")
                .Post()
                .Accept(HttpAccept.Html)
                .AddFormParameter("act", "users")
                .AddFormParameter("type", "login")
                .AddFormParameter("mail", Settings.Username)
                .AddFormParameter("pass", Settings.Password)
                .AddFormParameter("rem", "1");

            if (Settings.Captcha.IsNotNullOrWhiteSpace())
            {
                requestBuilder.AddFormParameter("need_captcha", "1");
                requestBuilder.AddFormParameter("captcha", Settings.Captcha);
            }

            var request = BuildRequest(requestBuilder);
            request.Cookies.Remove("PHPSESSID");
            var response = await ExecuteAuth(request);

            var content = response.Content ?? string.Empty;

            if (content.Contains("need_captcha"))
            {
                // A captcha is required but the one we hold is stale or missing entirely.
                // Clear it so it is not reused and ask the user to fetch a fresh one.
                Settings.Captcha = null;

                throw new IndexerAuthException("LostFilm.tv requires a captcha. Open the indexer settings to fetch a new one.");
            }

            if (content.Contains("error\":1") || content.Contains("error\":2") || content.Contains("error\":4"))
            {
                // LostFilm captchas are single-use and bound to the login session, so a
                // failed captcha can never succeed again. Clear it to stop the retry loop.
                if (content.Contains("error\":4"))
                {
                    Settings.Captcha = null;
                }

                throw new IndexerAuthException("Captcha is incorrect");
            }

            if (content.Contains("error\":3"))
            {
                throw new IndexerAuthException("E-mail or password is incorrect");
            }

            if (!content.Contains("success\":true"))
            {
                throw new IndexerAuthException("LostFilm.tv authentication failed: " + content);
            }

            // The captcha was consumed by this successful login; never reuse it.
            Settings.Captcha = null;

            var cookies = response.GetCookies();

            // lf_session is a durable cookie (expires years out); keep it in the DB for as long
            // as the server says it is valid instead of forcing a re-login every 30 days.
            DateTime? expiration = null;
            foreach (Cookie cookie in response.Cookies)
            {
                if (cookie.Expires > DateTime.Now && (expiration == null || cookie.Expires > expiration.Value))
                {
                    expiration = cookie.Expires;
                }
            }

            _cookiesExpiration = expiration ?? DateTime.Now.AddDays(30);
            PersistCookies(cookies, _cookiesExpiration);

            _logger.Debug("LostFilm.tv authentication succeeded");
        }

        protected override async Task<ValidationFailure> TestConnection()
        {
            try
            {
                // The default implementation scrapes every new episode, which is slow.
                // Verify connectivity and authentication with a single feed page request instead.
                await GetResponse(new HttpRequestBuilder(BaseUrl + "/new").Accept(HttpAccept.Html));

                return null;
            }
            catch (IndexerAuthException ex)
            {
                _logger.Warn(ex, "Unable to authenticate with LostFilm.tv");

                return new ValidationFailure(string.Empty, "Unable to authenticate with LostFilm.tv. " + ex.Message);
            }
            catch (CloudFlareProtectionException ex)
            {
                return new ValidationFailure(string.Empty, ex.Message);
            }
            catch (HttpRequestException ex)
            {
                _logger.Warn(ex, "Unable to connect to LostFilm.tv");

                return new NzbDroneValidationFailure(string.Empty, "Unable to connect to LostFilm.tv. This is typically caused by DNS/SSL issues. Check DNS settings, ensure IPv6 is working or disabled, consider using different DNS servers, or try a VPN/proxy if needed. See: 'https://wiki.servarr.com/prowlarr/troubleshooting#dns-ssl-connection-issues' " + ex.Message)
                {
                    DetailedDescription = ex.InnerException?.Message
                };
            }
            catch (TaskCanceledException ex)
            {
                _logger.Warn(ex, "Unable to connect to LostFilm.tv");

                return new ValidationFailure(string.Empty, "Unable to connect to LostFilm.tv, possibly due to a timeout. Try again or check your network settings. " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to connect to LostFilm.tv");

                return new ValidationFailure(string.Empty, "Unable to connect to LostFilm.tv, check the log above the ValidationFailure for more details. " + ex.Message);
            }
        }

        public override async Task<IndexerDownloadResponse> Download(Uri link)
        {
            var response = await GetResponse(new HttpRequestBuilder(link.AbsoluteUri).Accept(HttpAccept.Html));

            if (CheckIfLoginNeeded(response))
            {
                throw new IndexerAuthException("LostFilm.tv login required to download torrent");
            }

            ValidateDownloadData(response.ResponseData);

            return new IndexerDownloadResponse(response.ResponseData);
        }

        protected override async Task<IndexerQueryResult> FetchPage(IndexerRequest request, IParseIndexerResponse parser)
        {
            var urlQuery = HttpUtility.ParseQueryString(request.HttpRequest.Url.Query);
            var isRss = request.HttpRequest.Url.Path.Equals("/new", StringComparison.OrdinalIgnoreCase);

            IList<ReleaseInfo> releases;

            if (isRss)
            {
                releases = await FetchNewReleases();
            }
            else
            {
                releases = await PerformSearch(urlQuery["val"], urlQuery["season"].ParseInt32(), urlQuery["episode"]);
            }

            return new IndexerQueryResult
            {
                Releases = releases
            };
        }

        private HttpRequest BuildRequest(HttpRequestBuilder builder)
        {
            var request = builder.Build();
            request.SuppressHttpError = true;
            request.Encoding = Encoding;
            request.RateLimit = RateLimit;

            // LostFilm's session/captcha are bound to a server-side PHPSESSID.
            // Persist cookies in the shared cookie container so the session created
            // while fetching the captcha is reused for the login and search requests.
            request.StoreRequestCookie = true;
            request.StoreResponseCookie = true;

            Cookies ??= LoadCookies();

            if (Cookies != null)
            {
                foreach (var cookie in Cookies)
                {
                    request.Cookies[cookie.Key] = cookie.Value;
                }
            }

            return request;
        }

        private IDictionary<string, string> LoadCookies()
        {
            // Mirrors HttpIndexerBase.GetCookies but also keeps the stored expiration so it
            // is not reset to a shorter TTL when cookies are re-persisted after each request.
            Cookies = _indexerStatusService.GetIndexerCookies(Definition.Id);
            _cookiesExpiration = _indexerStatusService.GetIndexerCookiesExpirationDate(Definition.Id);

            if (_cookiesExpiration < DateTime.Now)
            {
                Cookies = null;
                _cookiesExpiration = null;
            }

            return Cookies;
        }

        private void PersistCookies(IDictionary<string, string> cookies, DateTime? expiration)
        {
            lock (_cookieSync)
            {
                if (CookiesEqual(_persistedCookies, _persistedExpiration, cookies, expiration))
                {
                    return;
                }

                UpdateCookies(cookies, expiration);
                _persistedCookies = new Dictionary<string, string>(cookies);
                _persistedExpiration = expiration;
            }
        }

        private static bool CookiesEqual(IDictionary<string, string> previous, DateTime? previousExpiration, IDictionary<string, string> current, DateTime? expiration)
        {
            if (previous == null || previousExpiration != expiration || previous.Count != current.Count)
            {
                return false;
            }

            foreach (var pair in current)
            {
                if (!previous.TryGetValue(pair.Key, out var value) || value != pair.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private async Task<HttpResponse> ExecuteAsync(HttpRequest request)
        {
            return await RetryStrategy.ExecuteAsync(
                static async (state, _) => await state._httpClient.ExecuteProxiedAsync(state.HttpRequest, state.Definition),
                (_httpClient, HttpRequest: request, Definition));
        }

        private async Task<HttpResponse> GetResponse(HttpRequestBuilder builder, bool checkLogin = true)
        {
            return await GetResponse(BuildRequest(builder), checkLogin);
        }

        private async Task<HttpResponse> GetResponse(HttpRequest request, bool checkLogin = true)
        {
            var response = await ExecuteAsync(request);

            if (checkLogin && CheckIfLoginNeeded(response))
            {
                _logger.Trace("LostFilm.tv: attempting to re-auth based on indexer search response");

                await DoLogin();

                // DoLogin refreshed the in-memory session cookies; the request snapshot still
                // carries the stale pre-login cookies, so re-apply the fresh session to the
                // replayed request (mirrors HttpIndexerBase.ModifyRequest).
                request.Cookies.Clear();

                if (Cookies != null)
                {
                    foreach (var cookie in Cookies)
                    {
                        request.Cookies[cookie.Key] = cookie.Value;
                    }
                }

                response = await ExecuteAsync(request);
            }

            // Persist the current in-memory cookies (which a DoLogin above may have just
            // refreshed with a new session) instead of the stale request snapshot, so the
            // durable lf_session cookie survives across restarts. Only write to the DB when
            // the cookie set or its expiry actually changed.
            if (Cookies != null)
            {
                PersistCookies(Cookies, _cookiesExpiration ?? DateTime.Now.AddDays(30));
            }

            if (CloudFlareDetectionService.IsCloudflareProtected(response))
            {
                throw new CloudFlareProtectionException(response);
            }

            // Surface HTTP errors instead of silently returning empty results, so rate limiting
            // and server errors trigger the usual Prowlarr backoff/handling.
            if (response.HasHttpError && (request.SuppressHttpErrorStatusCodes == null || !request.SuppressHttpErrorStatusCodes.Contains(response.StatusCode)))
            {
                if (request.LogHttpError)
                {
                    _logger.Warn("HTTP Error - {0}", response);
                }

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    throw new TooManyRequestsException(request, response);
                }

                if (response.HasHttpServerError)
                {
                    throw new HttpException(request, response);
                }
            }

            return response;
        }

        private async Task<Captcha> GetLoginPageAsync()
        {
            var request = BuildRequest(new HttpRequestBuilder(BaseUrl + "/login").Accept(HttpAccept.Html));

            // LostFilm only renders the captcha form for guests; when a valid lf_session is
            // present the login page is served with the authenticated layout and no captcha.
            // Drop the session cookie for this fetch (the null value replaces the cookie in
            // the shared container without touching the persisted session). PHPSESSID is kept
            // because the captcha image is bound to it for the subsequent login submission.
            request.Cookies["lf_session"] = null;

            var response = await GetResponse(request, checkLogin: false);

            var parser = new HtmlParser();
            using var document = parser.ParseDocument(response.Content);
            var qCaptchaImg = document.QuerySelector("img#captcha_pictcha");

            if (qCaptchaImg == null)
            {
                return new Captcha { ImageData = Array.Empty<byte>() };
            }

            var captchaSrc = qCaptchaImg.GetAttribute("src");

            if (string.IsNullOrWhiteSpace(captchaSrc))
            {
                return new Captcha { ImageData = Array.Empty<byte>() };
            }

            var captchaUrl = new Uri(new Uri(BaseUrl + "/"), captchaSrc);
            var captchaResponse = await GetResponse(new HttpRequestBuilder(captchaUrl.AbsoluteUri), checkLogin: false);

            return new Captcha
            {
                ContentType = captchaResponse.Headers.ContentType,
                ImageData = captchaResponse.ResponseData
            };
        }

        private async Task Logout()
        {
            _logger.Debug("LostFilm.tv: performing logout");

            var requestBuilder = new HttpRequestBuilder(BaseUrl + "/ajaxik.php")
                .Post()
                .Accept(HttpAccept.Html)
                .AddFormParameter("act", "users")
                .AddFormParameter("type", "logout");

            var response = await ExecuteAuth(BuildRequest(requestBuilder));
            _logger.Debug("LostFilm.tv logout result: " + (response.Content ?? string.Empty));
        }

        private async Task<IList<ReleaseInfo>> PerformSearch(string searchTerm, int? season, string episode)
        {
            if (searchTerm.IsNullOrWhiteSpace())
            {
                return await FetchNewReleases();
            }

            _logger.Debug("PerformSearch: {0} [Season: {1}, Episode: {2}]", searchTerm, season, episode);
            var releases = new List<ReleaseInfo>();

            // Search query words. Consists of Series keywords that will be used for series search request,
            // and Episode keywords that will be used for episode filtering.
            var keywords = searchTerm.Split(' ').ToList();

            // Keywords count related to Series Search.
            var searchKeywords = keywords.Count;

            // Keywords count related to Series Filter.
            var serieFilterKeywords = 0;

            do
            {
                var searchString = string.Join(" ", keywords.Take(searchKeywords));
                _logger.Debug("LostFilm.tv: searching: " + searchString);

                var requestBuilder = new HttpRequestBuilder(BaseUrl + "/ajaxik.php")
                    .Post()
                    .Accept(HttpAccept.Html)
                    .AddFormParameter("act", "common")
                    .AddFormParameter("type", "search")
                    .AddFormParameter("val", searchString);

                var response = await GetResponse(requestBuilder);

                if (response.Content == null)
                {
                    _logger.Debug("LostFilm.tv: empty series response for query: " + searchString);
                    continue;
                }

                try
                {
                    var json = JToken.Parse(response.Content);
                    if (json == null || json.Type == JTokenType.Array)
                    {
                        _logger.Debug("LostFilm.tv: invalid response for query: " + searchString);
                        continue; // Search loop
                    }

                    // Protect from {"data":false,"result":"ok"}
                    var jsonData = json["data"];
                    if (jsonData?.Type != JTokenType.Object)
                    {
                        continue; // Search loop
                    }

                    var jsonSeries = jsonData["series"];
                    if (jsonSeries == null || !jsonSeries.HasValues)
                    {
                        continue; // Search loop
                    }

                    var series = jsonSeries.ToList();
                    _logger.Debug("LostFilm.tv: found {0} series: [{1}]", series.Count, string.Join(", ", series.Select(s => s["title_orig"].Value<string>())));

                    // Filter found series
                    if (series.Count > 1)
                    {
                        serieFilterKeywords = keywords.Count - searchKeywords;

                        do
                        {
                            var serieFilter = string.Join(" ", keywords.GetRange(searchKeywords, serieFilterKeywords));
                            _logger.Debug("LostFilm.tv: filtering: " + serieFilter);
                            var filteredSeries = series.Where(s => (s["title_orig"]?.Value<string>() ?? string.Empty).Contains(serieFilter, StringComparison.OrdinalIgnoreCase)).ToList();

                            if (filteredSeries.Count > 0)
                            {
                                _logger.Debug("LostFilm.tv: series filtered: [{0}]", string.Join(", ", filteredSeries.Select(s => s["title_orig"].Value<string>())));
                                series = filteredSeries;
                                break; // Serie Filter loop
                            }
                        }
                        while (--serieFilterKeywords > 0);
                    }

                    foreach (var serie in series)
                    {
                        var link = serie["link"].ToString();
                        var seasonPath = season is > 0 ? $"/season_{season}" : "/seasons";
                        var url = BaseUrl + link + seasonPath;

                        // Fetch single episode releases
                        if (!string.IsNullOrEmpty(episode))
                        {
                            url += "/episode_" + episode;
                            releases.AddRange(await FetchEpisodeReleases(url));
                        }

                        // Fetch the whole series OR episode with filter applied
                        else
                        {
                            var episodeKeywords = keywords.Skip(searchKeywords + serieFilterKeywords);
                            var episodeFilterKeywords = episodeKeywords.Count();

                            // Search for episodes dropping 1 filter word each time when no results has found.
                            // Last search will be performed with empty filter
                            do
                            {
                                var filter = string.Join(" ", episodeKeywords.Take(episodeFilterKeywords));
                                _logger.Debug("LostFilm.tv: searching episodes with filter [" + filter + "]");
                                var taskReleases = await FetchSeriesReleases(url, season, episode, filter);

                                if (taskReleases.Count > 0)
                                {
                                    _logger.Debug("LostFilm.tv: found {0} episodes", taskReleases.Count);
                                    releases.AddRange(taskReleases);
                                    break; // Episodes Filter loop
                                }
                            }
                            while (--episodeFilterKeywords >= 0);
                        }
                    }

                    break; // Search loop
                }
                catch (Exception ex) when (ex is not IndexerAuthException and not HttpException and not TooManyRequestsException and not CloudFlareProtectionException)
                {
                    _logger.Warn(ex, "LostFilm.tv: error parsing search response for query: " + searchString);
                }
            }
            while (--searchKeywords > 0);

            return releases;
        }

        private async Task<IList<ReleaseInfo>> FetchNewReleases()
        {
            var url = BaseUrl + "/new";
            _logger.Debug("FetchNewReleases: " + url);

            var response = await GetResponse(new HttpRequestBuilder(url).Accept(HttpAccept.Html));
            var releases = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            using var document = parser.ParseDocument(response.Content);
            var rows = document.QuerySelectorAll("div.row");

            foreach (var row in rows)
            {
                var link = row.QuerySelector("a")?.GetAttribute("href");

                if (link.IsNullOrWhiteSpace())
                {
                    continue;
                }

                var episodeUrl = BaseUrl + link;
                releases.AddRange(await FetchEpisodeReleases(episodeUrl));
            }

            return releases;
        }

        private async Task<List<ReleaseInfo>> FetchEpisodeReleases(string url)
        {
            _logger.Debug("FetchEpisodeReleases: " + url);
            var response = await GetResponse(new HttpRequestBuilder(url).Accept(HttpAccept.Html));
            var releases = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            using var document = parser.ParseDocument(response.Content);

            var playButton = document.QuerySelector("div.external-btn");
            if (playButton == null || playButton.ClassList.Contains("inactive"))
            {
                return releases;
            }

            var leftBox = document.QuerySelector("div.details-pane > div.left-box");
            if (leftBox == null)
            {
                return releases;
            }

            var dateString = leftBox.TextContent;
            var key = dateString.Contains("TBA") ? "ru: " : "eng: ";
            dateString = TrimString(dateString, key, " г.");

            // Only take the year from a date that actually parsed; a malformed date must
            // never silently produce a wrong ReleaseYear or PublishDate.
            DateTime? date = null;
            if (dateString != null)
            {
                if (dateString.Length == 4)
                {
                    if (DateTime.TryParseExact(dateString, "yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsedDate))
                    {
                        date = parsedDate;
                    }
                }
                else if (DateTime.TryParse(dateString, RuCulture, DateTimeStyles.AssumeLocal, out var parsedDate))
                {
                    date = parsedDate;
                }
            }

            var isMovie = url.Contains("/movies/", StringComparison.OrdinalIgnoreCase);

            var urlDetails = new TrackerUrlDetails(playButton)
            {
                IsMovie = isMovie,
                ReleaseYear = isMovie && date.HasValue ? date.Value.Year : (int?)null
            };
            var episodeReleases = await FetchTrackerReleases(urlDetails);

            foreach (var release in episodeReleases)
            {
                release.InfoUrl = url;
                release.PublishDate = date ?? DateTime.Now;

                if (isMovie)
                {
                    release.Categories = new List<IndexerCategory> { NewznabStandardCategory.Movies };
                }
            }

            releases.AddRange(episodeReleases);

            return releases;
        }

        private async Task<List<ReleaseInfo>> FetchSeriesReleases(string url, int? season, string episode, string filter)
        {
            _logger.Debug("FetchSeriesReleases: {0} S: {1} E: {2} Filter: {3}", url, season, episode, filter);

            var response = await GetResponse(new HttpRequestBuilder(url).Accept(HttpAccept.Html));
            var releases = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            using var document = parser.ParseDocument(response.Content);
            var seasons = document.QuerySelectorAll("div.serie-block");
            const string rowSelector = "table.movie-parts-list > tbody > tr";

            if (seasons.Length == 0 && url.Contains("/movies/", StringComparison.OrdinalIgnoreCase))
            {
                // Movie pages have no `div.serie-block` season blocks. Fall back to the
                // episode parser which handles the movie page layout (see FetchEpisodeReleases).
                _logger.Debug("LostFilm.tv: movie page detected, parsing as a movie: " + url);

                return await FetchEpisodeReleases(TrimSeasonSuffix(url));
            }

            foreach (var seasonBlock in seasons)
            {
                // Could be null if serie-block is for Extras
                var seasonButton = seasonBlock.QuerySelector("div.movie-details-block > div.external-btn");

                // Process only season we're searching for
                if (seasonButton != null && season is > 0)
                {
                    // If seasonButton in "inactive" it will not contain "onClick" handler. Better to parse element which always exists.
                    var watchedButton = seasonBlock.QuerySelector("div.movie-details-block > div.haveseen-btn");
                    var buttonCode = watchedButton?.GetAttribute("data-code");
                    var dashIndex = buttonCode == null ? -1 : buttonCode.IndexOf('-');

                    if (dashIndex == -1)
                    {
                        _logger.Debug("LostFilm.tv: cannot determine season from haveseen-btn data-code");
                        continue;
                    }

                    if (buttonCode.Substring(dashIndex + 1) != season.ToString())
                    {
                        continue; // Can't match season by regex OR season not matches to a searched one
                    }

                    // Stop parsing season episodes if season pack was required but it's not available yet.
                    if (seasonButton.ClassList.Contains("inactive"))
                    {
                        _logger.Debug("LostFilm.tv: no season pack is found for S{0}", season);
                        break;
                    }
                }

                // Fetch season pack releases if no episode filtering is required.
                // If seasonButton implements "inactive" class there are no season pack available and each episode should be fetched separately.
                if (string.IsNullOrEmpty(episode) && string.IsNullOrEmpty(filter) && seasonButton != null && !seasonButton.ClassList.Contains("inactive"))
                {
                    var lastEpisode = seasonBlock.QuerySelector(rowSelector);
                    var dateColumn = lastEpisode?.QuerySelector("td.delta");
                    var date = DateFromEpisodeColumn(dateColumn);

                    var urlDetails = new TrackerUrlDetails(seasonButton);
                    var seasonReleases = await FetchTrackerReleases(urlDetails);

                    foreach (var release in seasonReleases)
                    {
                        release.InfoUrl = url;
                        release.PublishDate = date;
                    }

                    releases.AddRange(seasonReleases);

                    if (season is > 0)
                    {
                        break; // Searched season was processed
                    }

                    // Skip parsing separate episodes if season pack was added
                    if (seasonReleases.Count > 0)
                    {
                        continue;
                    }
                }

                // No season filtering was applied OR season pack in not available
                var rows = seasonBlock.QuerySelectorAll(rowSelector).Where(s => !s.ClassList.Contains("not-available"));

                foreach (var row in rows)
                {
                    var couldBreak = false; // Set to `true` if searched episode was found

                    if (!string.IsNullOrEmpty(filter))
                    {
                        var titles = row.QuerySelector("td.gamma > div");
                        if (titles == null || !titles.TextContent.Contains(filter, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                    }

                    var playButton = row.QuerySelector("td.zeta > div.external-btn");
                    if (playButton == null)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(episode))
                    {
                        var match = ParsePlayEpisodeRegex.Match(playButton.GetAttribute("onclick") ?? string.Empty);
                        var episodeNumber = match.Groups["episode"];

                        if (episodeNumber == null || episodeNumber.Value.TrimStart('0') != episode.TrimStart('0'))
                        {
                            continue;
                        }

                        couldBreak = true;
                    }

                    var dateColumn = row.QuerySelector("td.delta"); // Contains both Date and EpisodeURL
                    var date = DateFromEpisodeColumn(dateColumn);

                    var link = dateColumn.GetAttribute("onclick"); // goTo('/series/Prison_Break/season_5/episode_9/',false)
                    link = TrimString(link, '\'', '\'');
                    var episodeUrl = BaseUrl + link;
                    var urlDetails = new TrackerUrlDetails(playButton);

                    var episodeReleases = await FetchTrackerReleases(urlDetails);

                    foreach (var release in episodeReleases)
                    {
                        release.InfoUrl = episodeUrl;
                        release.PublishDate = date;
                    }

                    releases.AddRange(episodeReleases);

                    if (couldBreak)
                    {
                        break;
                    }
                }
            }

            return releases;
        }

        private async Task<IReadOnlyList<ReleaseInfo>> FetchTrackerReleases(TrackerUrlDetails details)
        {
            var url = $"{BaseUrl}/v_search.php?c={details.SeriesId}&s={details.Season}&e={(string.IsNullOrEmpty(details.Episode) ? "999" : details.Episode)}";
            _logger.Debug("FetchTrackerReleases: " + url);

            // Get redirection page with generated link on it. This link can't be constructed manually as it contains Hash field and hashing algo is unknown.
            // The page redirects to /login when not authenticated, so follow redirects to let CheckIfLoginNeeded re-auth.
            var request = BuildRequest(new HttpRequestBuilder(url).Accept(HttpAccept.Html));
            request.AllowAutoRedirect = true;
            var response = await GetResponse(request);

            if (response.Content == null)
            {
                throw new IndexerAuthException("Empty response from " + url);
            }

            if (response.Content == "log in first")
            {
                throw new IndexerAuthException("Log in first");
            }

            var parser = new HtmlParser();
            using var document = parser.ParseDocument(response.Content);
            var refreshMeta = document.QuerySelector("meta[http-equiv=\"refresh\"]");
            var metaContent = refreshMeta?.GetAttribute("content");
            var urlIndex = metaContent?.IndexOf("url=") ?? -1;

            if (refreshMeta == null || metaContent == null || urlIndex == -1)
            {
                _logger.Debug("LostFilm.tv: no refresh redirect meta found in response");
                return Array.Empty<ReleaseInfo>();
            }

            // Follow redirection defined by async url.replace and prepend sitelink
            var redirectionUrl = BaseUrl + metaContent.Substring(urlIndex + 4);
            return await FollowTrackerRedirection(redirectionUrl, details);
        }

        private async Task<List<ReleaseInfo>> FollowTrackerRedirection(string url, TrackerUrlDetails details)
        {
            _logger.Debug("FollowTrackerRedirection: " + url);
            var response = await GetResponse(new HttpRequestBuilder(url).Accept(HttpAccept.Html));
            var releases = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            using var document = parser.ParseDocument(response.Content);
            var rows = document.QuerySelectorAll("div.inner-box--item");

            if (rows.Length == 0)
            {
                return releases;
            }

            _logger.Debug("LostFilm.tv: parsing {0} releases", rows.Length);

            var subtitleElement = document.QuerySelector("div.inner-box--subtitle");
            if (subtitleElement == null)
            {
                _logger.Debug("LostFilm.tv: no series subtitle found on tracker page");
                return releases;
            }

            var serieTitle = subtitleElement.TextContent;
            var lastComma = serieTitle.LastIndexOf(',');
            serieTitle = lastComma > 0 ? serieTitle.Substring(0, lastComma) : serieTitle;

            var episodeInfo = document.QuerySelector("div.inner-box--text")?.TextContent;
            var episodeName = episodeInfo == null ? null : TrimString(episodeInfo, '(', ')');

            foreach (var row in rows)
            {
                var descElement = row.QuerySelector("div.inner-box--desc");
                var detailsInfo = descElement?.TextContent;

                if (string.IsNullOrEmpty(detailsInfo))
                {
                    _logger.Debug("LostFilm.tv: release row has no description");
                    continue;
                }

                var releaseDetails = ParseReleaseDetailsRegex.Match(detailsInfo);

                if (!releaseDetails.Success)
                {
                    _logger.Debug("LostFilm.tv: failed to map release details string: {0}", detailsInfo);
                    continue;
                }

                // For supported qualities see TvCategoryParser.cs
                var quality = releaseDetails.Groups["quality"].Value.Trim();

                // Adapt shitty quality format for common algorithms
                quality = Regex.Replace(quality, "-Rip", "Rip", RegexOptions.IgnoreCase);
                quality = Regex.Replace(quality, "WEB-DLRip", "WEBDL", RegexOptions.IgnoreCase);
                quality = Regex.Replace(quality, "WEB-DL", "WEBDL", RegexOptions.IgnoreCase);
                quality = Regex.Replace(quality, "HDTVRip", "HDTV", RegexOptions.IgnoreCase);

                // Fix forgotten p-Progressive suffix in resolution index
                quality = Regex.Replace(quality, "1080 ", "1080p ", RegexOptions.IgnoreCase);
                quality = Regex.Replace(quality, "720 ", "720p ", RegexOptions.IgnoreCase);

                var techComponents = new[]
                {
                    "rus",
                    quality,
                    "(LostFilm)"
                };
                var techInfo = string.Join(" ", techComponents.Where(s => !string.IsNullOrEmpty(s)));

                // Ru title: downloadLink.TextContent.Replace("\n", "")
                // En title should be manually constructed.
                // For movies there is no season/episode to show; mark them and append the release year instead.
                var titleComponents = details.IsMovie
                    ? new[]
                    {
                        serieTitle,
                        "Movie",
                        details.ReleaseYear?.ToString(),
                        techInfo
                    }
                    : new[]
                    {
                        serieTitle,
                        details.GetEpisodeString(),
                        episodeName,
                        techInfo
                    };
                var downloadLink = row.QuerySelector("div.inner-box--link > a");
                if (downloadLink == null)
                {
                    _logger.Debug("LostFilm.tv: release row has no download link");
                    continue;
                }

                var sizeString = releaseDetails.Groups["size"].Value.ToUpper();
                sizeString = sizeString.Replace("ТБ", "TB");
                sizeString = sizeString.Replace("ГБ", "GB");
                sizeString = sizeString.Replace("МБ", "MB");
                sizeString = sizeString.Replace("КБ", "KB");
                var href = downloadLink.GetAttribute("href");

                if (string.IsNullOrWhiteSpace(href))
                {
                    _logger.Debug("LostFilm.tv: release row has no download link");
                    continue;
                }

                var link = new Uri(new Uri(BaseUrl + "/"), href);

                var release = new TorrentInfo
                {
                    Title = string.Join(" - ", titleComponents.Where(s => !string.IsNullOrEmpty(s))),
                    DownloadUrl = link.AbsoluteUri,
                    Guid = link.AbsoluteUri,
                    Size = ParseUtil.GetBytes(sizeString),

                    // add missing torznab fields not available from results
                    Seeders = 1,
                    Peers = 2,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800 // 48 hours
                };

                release.Categories = new List<IndexerCategory> { NewznabStandardCategory.TV };

                _logger.Debug("LostFilm.tv: add: " + release.Title);
                releases.Add(release);
            }

            return releases;
        }

        private static string TrimSeasonSuffix(string url)
        {
            return Regex.Replace(url, @"/(season_\d+|seasons)/?$", string.Empty, RegexOptions.IgnoreCase);
        }

        private static string TrimString(string s, char startChar, char endChar)
        {
            var start = s.IndexOf(startChar);
            var end = s.LastIndexOf(endChar);
            return (start != -1 && end != -1) ? s.Substring(start + 1, end - start - 1) : null;
        }

        private static string TrimString(string s, string startString, string endString)
        {
            var start = s.IndexOf(startString);
            var end = s.LastIndexOf(endString);
            return (start != -1 && end != -1) ? s.Substring(start + startString.Length, end - start - startString.Length) : null;
        }

        private static DateTime DateFromEpisodeColumn(IElement dateColumn)
        {
            if (dateColumn == null)
            {
                return DateTime.Now;
            }

            var dateString = dateColumn.QuerySelector("span.small-text")?.TextContent;

            // 'Eng: 23.05.2017' -> '23.05.2017' OR '23.05.2017' -> '23.05.2017'
            if (!string.IsNullOrEmpty(dateString))
            {
                var separator = dateString.IndexOf(':');
                dateString = separator > 0 ? dateString.Substring(separator + 2) : dateString;
            }
            else
            {
                dateString = dateColumn.QuerySelector("span")?.TextContent;
            }

            // dd.mm.yyyy
            return DateTime.TryParse(dateString, RuCulture, DateTimeStyles.AssumeLocal, out var parsedDate) ? parsedDate : DateTime.Now;
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.TV);
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.Movies);

            return caps;
        }

        private class TrackerUrlDetails
        {
            public string SeriesId { get; private set; }
            public string Season { get; private set; }
            public string Episode { get; private set; }
            public bool IsMovie { get; set; }
            public int? ReleaseYear { get; set; }

            public TrackerUrlDetails(string seriesId, string season, string episode)
            {
                SeriesId = seriesId;
                Season = season;
                Episode = episode;
            }

            public TrackerUrlDetails(IElement button)
            {
                var trigger = button.GetAttribute("onclick") ?? string.Empty;
                var match = ParsePlayEpisodeRegex.Match(trigger);

                SeriesId = match.Groups["id"].Value.TrimStart('0');
                Season = match.Groups["season"].Value.TrimStart('0');
                Episode = match.Groups["episode"].Value.TrimStart('0');
            }

            public string GetEpisodeString()
            {
                var result = string.Empty;

                if (!string.IsNullOrEmpty(Season) && Season != "0" && Season != "999")
                {
                    result += "S" + Season;

                    if (!string.IsNullOrEmpty(Episode) && Episode != "0" && Episode != "999")
                    {
                        result += "E" + Episode;
                    }
                }

                return result;
            }
        }
    }

    public class LostFilmRequestGenerator : IIndexerRequestGenerator
    {
        private readonly LostFilmSettings _settings;

        public LostFilmRequestGenerator(LostFilmSettings settings)
        {
            _settings = settings;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            if (searchCriteria.IsRssSearch || searchCriteria.SearchTerm.IsNullOrWhiteSpace())
            {
                pageableRequests.Add(GetRssRequests());
            }
            else
            {
                pageableRequests.Add(GetSearchRequests(searchCriteria.SanitizedSearchTerm, null, null));
            }

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            if (searchCriteria.IsRssSearch || searchCriteria.SearchTerm.IsNullOrWhiteSpace())
            {
                pageableRequests.Add(GetRssRequests());
            }
            else
            {
                pageableRequests.Add(GetSearchRequests(searchCriteria.SanitizedSearchTerm, searchCriteria.Season, searchCriteria.Episode));
            }

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            if (searchCriteria.IsRssSearch || searchCriteria.SearchTerm.IsNullOrWhiteSpace())
            {
                pageableRequests.Add(GetRssRequests());
            }
            else
            {
                pageableRequests.Add(GetSearchRequests(searchCriteria.SanitizedSearchTerm, null, null));
            }

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        private IEnumerable<IndexerRequest> GetRssRequests()
        {
            yield return new IndexerRequest(new HttpRequestBuilder(_settings.BaseUrl).Resource("new").Build());
        }

        private IEnumerable<IndexerRequest> GetSearchRequests(string term, int? season, string episode)
        {
            var requestBuilder = new HttpRequestBuilder(_settings.BaseUrl)
                .Resource("ajaxik.php")
                .Post()
                .Accept(HttpAccept.Html)
                .AddFormParameter("act", "common")
                .AddFormParameter("type", "search")
                .AddFormParameter("val", term)
                .AddQueryParam("val", term);

            if (season is > 0)
            {
                requestBuilder.AddQueryParam("season", season.Value);
            }

            if (episode.IsNotNullOrWhiteSpace())
            {
                requestBuilder.AddQueryParam("episode", episode);
            }

            yield return new IndexerRequest(requestBuilder.Build());
        }
    }

    public class LostFilmParser : IParseIndexerResponse
    {
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            // Multi-step flow is handled in LostFilm.FetchPage, no response parsing is required here.
            return Array.Empty<ReleaseInfo>();
        }
    }

    public class LostFilmSettings : UserPassTorrentBaseSettings, ICaptchaProvider
    {
        public string Captcha { get; set; }
    }
}
