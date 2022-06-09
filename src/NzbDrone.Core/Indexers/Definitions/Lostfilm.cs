using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using FluentValidation;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
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
    public class Lostfilm : TorrentIndexerBase<UserPassCaptchaTorrentBaseSettings>
    {
        public override string Name => "Lostfilm";
        public override string[] IndexerUrls => new string[]
        {
            "https://www.lostfilm.tv/",
            "https://www.lostfilm.run/",
            "https://www.lostfilmtv.site/",
            "https://www.lostfilm.tv/",
            "https://www.lostfilm.win/",
            "https://www.lostfilm.tw/",
            "https://www.lostfilmtv2.site/",
            "https://www.lostfilm.uno/"
        };
        public override string Description => "Lostfilm is russian tv shows voiceover group publishing their releases.";
        public override string Language => "ru-RU";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.SemiPrivate;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public Lostfilm(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new LostfilmRequestGenerator() { Settings = Settings, Capabilities = Capabilities, HttpClient = _httpClient, Logger = _logger, Definition = Definition, Indexer = this };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new LostfilmParser(Settings, Capabilities.Categories) { HttpClient = _httpClient, Logger = _logger, Definition = Definition, Indexer = this };
        }

        public IDictionary<string, string> GetCookiesPublic()
        {
            return GetCookies();
        }

        protected override async Task DoLogin()
        {
            UpdateCookies(null, null);
            var loginPage = await ExecuteAuth(new HttpRequest(Settings.BaseUrl + "login"));
            UpdateCookies(loginPage.GetCookies(), DateTime.Now + TimeSpan.FromDays(30));

            // TODO: Finish captcha
            // var parser = new HtmlParser();
            // var document = parser.ParseDocument(loginPage.Content);
            // var qCaptchaImg = document.QuerySelector("img#captcha_pictcha");
            // if (qCaptchaImg != null)
            // {
            //     // var captchaUrl = Settings.BaseUrl + qCaptchaImg.GetAttribute("src");
            //     // var captchaImage = await ExecuteAuth(new HttpRequest(captchaUrl));
            //     // Settings.ExtraFieldData["CAPTCHA"] = captchaImage.Content;
            //     throw new IndexerAuthException("Captcha is not supported yet");
            // }
            // else
            // {
                // configData.CaptchaImage.Value = new byte[0];
            // }
            if (!Settings.Username.Contains("@"))
            {
                throw new IndexerAuthException("Username must be an e-mail address");
            }

            var data = new Dictionary<string, string>
            {
                { "act", "users" },
                { "type", "login" },
                { "mail", Settings.Username },
                { "pass", Settings.Password },
                { "rem", "1" }
            };

            if (!string.IsNullOrWhiteSpace(Settings.Captcha))
            {
                data.Add("need_captcha", "1");
                data.Add("captcha", Settings.Captcha);
            }

            var requestBuilder = new HttpRequestBuilder(Settings.BaseUrl + "ajaxik.php")
            {
                LogResponseContent = true,
                AllowAutoRedirect = true,
                Method = HttpMethod.Post
            };
            requestBuilder.PostProcess += r => r.RequestTimeout = TimeSpan.FromSeconds(15);
            requestBuilder.SetCookies(loginPage.GetCookies());
            requestBuilder.SetHeader("Content-Type", "application/x-www-form-urlencoded");
            foreach (var item in data)
            {
                requestBuilder.AddFormParameter(item.Key, item.Value);
            }

            var authLoginRequest = requestBuilder.Build();
            var response = await ExecuteAuth(authLoginRequest);

            if (response.Content != null && response.Content.Contains("\"success\":true"))
            {
                UpdateCookies(response.GetCookies(), DateTime.Now + TimeSpan.FromDays(30));
            }
            else
            {
                var errorMessage = response.Content;
                if (errorMessage.Contains("\"error\":2"))
                {
                    errorMessage = "Captcha is incorrect";
                }

                if (errorMessage.Contains("\"error\":3"))
                {
                    errorMessage = "E-mail or password is incorrect";
                }

                throw new IndexerAuthException(errorMessage);
            }
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            if (httpResponse.Content.Contains("href=\"/my\""))
            {
                return false;
            }

            return true;
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                                   {
                                       TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                                   }
            };
            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.TV, "TV Shows");
            return caps;
        }
    }

    public class LostfilmRequestGenerator : IIndexerRequestGenerator
    {
        public UserPassCaptchaTorrentBaseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }
        public IIndexerHttpClient HttpClient { get; set; }
        public ProviderDefinition Definition { get; set; }
        public Lostfilm Indexer { get; set; }
        public Logger Logger { get; set; }

        public LostfilmRequestGenerator()
        {
        }

        private IList<string> GetSearchPageURLs(string term, int? season, string episode)
        {
            var urls = new List<string>();
            /*
            Torznab query for some series could contains sanitized title. E.g. "Star Wars: The Clone Wars" will become "Star Wars The Clone Wars".
            Search API on LostFilm.tv doesn't return anything on such search query so the query should be "morphed" even for "tvsearch" queries.
            Also the queries to Specials is a union of Series and Episode titles. E.g.: "Breaking Bad - El Camino: A Breaking Bad Movie".
            The algorythm works in the following way:
                1. Search with the full SearchTerm. Just for example, let's search for episode by it's name
                    - {Star Wars The Clone Wars To Catch a Jedi}
                2. [loop] If none were found, repeat search with SearchTerm reduced by 1 word from the end. Fail search if no words left and no results were obtained
                    - {Star Wars The Clone Wars To Catch a} Jedi
                    - {Star Wars The Clone Wars To Catch} a Jedi
                    - ...
                    - {Star Wars} The Clone Wars To Catch a Jedi
                3. When we got few results, try to filter them with the words excluded before
                    - [Star Wars: The Clone Wars, Star Wars Rebels, Star Wars: Forces of Destiny]
                        .filterBy(The Clone Wars To Catch a Jedi)
                4. [loop] Reduce filterTerm by 1 word from the end. Fail search if no words left and no results were obtained
                        .filterBy(The Clone Wars To Catch a) / Jedi
                        .filterBy(The Clone Wars To Catch) / a Jedi
                        ...
                        .filterBy(The Clone Wars) / To Catch a Jedi
                5. [loop] Now we know that series we're looking for is called "Star Wars The Clone Wars". Fetch series detail page for it and try to apply remaining words as episode filter, reducing filter by 1 word each time we get no results:
                    - .episodes().filteredBy(To Catch a Jedi)
                    - .episodes().filteredBy(To Catch a) / Jedi
                    - ...
                    - .episodes() / To Catch a Jedi
            Test queries:
                - "Star Wars The Clone Wars To Catch a Jedi"    -> S05E19
                - "Breaking Bad El Camino A Breaking Bad Movie" -> Special
                - "The Magicians (2015)"                        -> Year should be ignored
            */

            // Search query words. Consists of Series keywords that will be used for series search request, and Episode keywords that will be used for episode filtering.
            var keywords = new List<string>(term.Split(' '));

            // Keywords count related to Series Search.
            var searchKeywords = keywords.Count;

            // Keywords count related to Series Filter.
            var serieFilterKeywords = 0;

            // Overall (keywords.count - searchKeywords - serieFilterKeywords) are related to episode filter
            do
            {
                var searchString = string.Join(" ", keywords.Take(searchKeywords));
                var data = new Dictionary<string, string>
                {
                    { "act", "common" },
                    { "type", "search" },
                    { "val", searchString }
                };

                var requestBuilder = new HttpRequestBuilder(Settings.BaseUrl + "ajaxik.php")
                {
                    Method = HttpMethod.Post
                };
                foreach (var item in data)
                {
                    requestBuilder.AddFormParameter(item.Key, item.Value);
                }

                requestBuilder.PostProcess += r => r.RequestTimeout = TimeSpan.FromSeconds(15);
                requestBuilder.SetCookies(Indexer.GetCookiesPublic());
                var req = new IndexerRequest(requestBuilder.Build());
                var response = new IndexerResponse(req, HttpClient.ExecuteProxied(req.HttpRequest, Definition));

                if (response.Content == null)
                {
                    continue;
                }

                var json = JToken.Parse(response.Content);
                if (json == null || json.Type == JTokenType.Array)
                {
                    continue; // Search loop
                }

                // Protect from {"data":false,"result":"ok"}
                var jsonData = json["data"];
                if (jsonData.Type != JTokenType.Object)
                {
                    continue; // Search loop
                }

                var jsonSeries = jsonData["series"];
                if (jsonSeries == null || !jsonSeries.HasValues)
                {
                    continue; // Search loop
                }

                var series = jsonSeries.ToList();

                // Filter found series
                if (series.Count() > 1)
                {
                    serieFilterKeywords = keywords.Count - searchKeywords;

                    do
                    {
                        var serieFilter = string.Join(" ", keywords.GetRange(searchKeywords, serieFilterKeywords));
                        var filteredSeries = series.Where(s => s["title_orig"].Value<string>().Contains(serieFilter)).ToList();

                        if (filteredSeries.Count() > 0)
                        {
                            series = filteredSeries;
                            break; // Serie Filter loop
                        }
                    }
                    while (--serieFilterKeywords > 0);
                }

                foreach (var serie in series)
                {
                    var link = serie["link"].ToString();
                    var season_url = (season == null) || (season == 0) ? "/seasons" : "/season_" + season.ToString();
                    var url = Settings.BaseUrl + link.TrimStart('/') + season_url;

                    if (!string.IsNullOrEmpty(episode))
                    {
                        // Fetch single episode releases
                        // TODO: Add a togglable Quick Path via v_search.php in Indexer Settings
                        url += "/episode_" + episode;
                    }

                    urls.Add(url);
                }
            }
            while (--searchKeywords > 0);

            return urls;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, int? season, string episode)
        {
            var requestUrls = new List<string>();

            if (string.IsNullOrWhiteSpace(term))
            {
                requestUrls.Add(Settings.BaseUrl + "new");
            }
            else
            {
                requestUrls.AddRange(GetSearchPageURLs(term, season, episode));
            }

            foreach (var url in requestUrls)
            {
                yield return new IndexerRequest(url, HttpAccept.Html);
            }
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories, null, ""));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories, searchCriteria.Season, searchCriteria.Episode));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories, null, ""));

            return pageableRequests;
        }

        // Lostfilm doesn't support music, but this function required by interface
        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        // Lostfilm doesn't support books, but this function required by interface
        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class LostfilmParser : IParseIndexerResponse
    {
        private readonly UserPassCaptchaTorrentBaseSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        private static readonly Regex ParsePlayEpisodeRegex = new Regex("PlayEpisode\\('(?<id>\\d{1,3})(?<season>\\d{3})(?<episode>\\d{3})'\\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ParseReleaseDetailsRegex = new Regex("Видео:\\ (?<quality>.+).\\ Размер:\\ (?<size>.+).\\ Перевод", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public IIndexerHttpClient HttpClient { get; set; }
        public ProviderDefinition Definition { get; set; }
        public Lostfilm Indexer { get; set; }
        public Logger Logger { get; set; }

        public LostfilmParser(UserPassCaptchaTorrentBaseSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        internal class TrackerUrlDetails
        {
            internal string seriesId { get; private set; }
            internal string season { get; private set; }
            internal string episode { get; private set; }

            internal TrackerUrlDetails(string seriesId, string season, string episode)
            {
                this.seriesId = seriesId;
                this.season = season;
                this.episode = episode;
            }

            internal TrackerUrlDetails(AngleSharp.Dom.IElement button)
            {
                var trigger = button.GetAttribute("onclick");
                var match = ParsePlayEpisodeRegex.Match(trigger);

                seriesId = match.Groups["id"].Value.TrimStart('0');
                season = match.Groups["season"].Value.TrimStart('0');
                episode = match.Groups["episode"].Value.TrimStart('0');
            }

            // TODO: see if query.GetEpisodeString() is sufficient
            internal string GetEpisodeString()
            {
                var result = string.Empty;

                if (!string.IsNullOrEmpty(season) && season != "0" && season != "999")
                {
                    result += "S" + season;

                    if (!string.IsNullOrEmpty(episode) && episode != "0" && episode != "999")
                    {
                        result += "E" + episode;
                    }
                }

                return result;
            }
        }

        private IList<TorrentInfo> FollowTrackerRedirection(string url, TrackerUrlDetails details)
        {
            var req = new IndexerRequest(url, HttpAccept.Html);
            var results = new IndexerResponse(req, HttpClient.ExecuteProxied(req.HttpRequest, Definition));
            var releases = new List<TorrentInfo>();

            var parser = new HtmlParser();
            var document = parser.ParseDocument(results.Content);
            var rows = document.QuerySelectorAll("div.inner-box--item");

            var serieTitle = document.QuerySelector("div.inner-box--subtitle").TextContent;
            serieTitle = serieTitle.Substring(0, serieTitle.LastIndexOf(','));

            var episodeInfo = document.QuerySelector("div.inner-box--text").TextContent;
            var episodeName = TrimString(episodeInfo, '(', ')');

            foreach (var row in rows)
            {
                var detailsInfo = row.QuerySelector("div.inner-box--desc").TextContent;
                var releaseDetails = ParseReleaseDetailsRegex.Match(detailsInfo);

                // ReSharper states "Expression is always false"
                // TODO Refactor to get the intended operation
                if (releaseDetails == null)
                {
                    throw new FormatException("Failed to map release details string: " + detailsInfo);
                }

                /*
                    * For supported qualities see:
                    *  - TvCategoryParser.cs
                    *  - https://github.com/SickRage/SickRage/wiki/Quality-Settings#quality-names-to-recognize-the-quality-of-a-file
                    */
                var quality = releaseDetails.Groups["quality"].Value.Trim();

                // Adapt shitty quality format for common algorythms
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
                var techInfo = string.Join(" ", techComponents);

                // Ru title: downloadLink.TextContent.Replace("\n", "");
                // En title should be manually constructed.
                var titleComponents = new[]
                {
                    serieTitle, details.GetEpisodeString(), episodeName, techInfo
                };
                var downloadLink = row.QuerySelector("div.inner-box--link > a");
                var sizeString = releaseDetails.Groups["size"].Value.ToUpper();
                sizeString = sizeString.Replace("ТБ", "TB"); // untested
                sizeString = sizeString.Replace("ГБ", "GB");
                sizeString = sizeString.Replace("МБ", "MB");
                sizeString = sizeString.Replace("КБ", "KB"); // untested
                var link = downloadLink.GetAttribute("href");

                // TODO this feels sparse compared to other trackers. Expand later
                var release = new TorrentInfo
                {
                    Categories = _categories.MapTrackerCatToNewznab("1"),
                    Title = string.Join(" - ", titleComponents),
                    DownloadUrl = link,
                    Guid = link,
                    Size = ParseUtil.GetBytes(sizeString),

                    // add missing torznab fields not available from results
                    Seeders = 1,
                    Peers = 2,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800 // 48 hours
                };

                releases.Add(release);
            }

            return releases;
        }

        private IList<TorrentInfo> FetchTrackerReleases(TrackerUrlDetails details)
        {
            var queryCollection = new NameValueCollection
            {
                { "c", details.seriesId },
                { "s", details.season },
                { "e", string.IsNullOrEmpty(details.episode) ? "999" : details.episode } // 999 is a synonym for the whole serie
            };
            var url = _settings.BaseUrl + "v_search.php" + "?" + queryCollection.GetQueryString();

            // Get redirection page with generated link on it. This link can't be constructed manually as it contains Hash field and hashing algo is unknown.
            var requestBuilder = new HttpRequestBuilder(url)
            {
                AllowAutoRedirect = true,
            };
            requestBuilder.PostProcess += r => r.RequestTimeout = TimeSpan.FromSeconds(15);
            requestBuilder.SetCookies(Indexer.GetCookiesPublic());
            var req = new IndexerRequest(requestBuilder.Build());
            var results = new IndexerResponse(req, HttpClient.ExecuteProxied(req.HttpRequest, Definition));

            if (results.Content == null)
            {
                throw new Exception("Empty response from " + url);
            }

            if (results.Content == "log in first")
            {
                throw new Exception(results.Content);
            }

            var parser = new HtmlParser();
            var document = parser.ParseDocument(results.Content);
            var meta = document.QuerySelector("meta");
            var metaContent = meta.GetAttribute("content");

            // Follow redirection defined by async url.replace
            var redirectionUrl = metaContent.Substring(metaContent.IndexOf("http"));
            return FollowTrackerRedirection(redirectionUrl, details);
        }

        private IList<TorrentInfo> ParseRelease(IndexerResponse indexerResponse)
        {
            var releases = new List<TorrentInfo>();
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);

            var playButton = dom.QuerySelector("div.external-btn");
            if (playButton != null && !playButton.ClassList.Contains("inactive"))
            {
                    // var details = new Uri(url);
                    var dateString = dom.QuerySelector("div.title-block > div.details-pane > div.left-box").TextContent;
                    var key = dateString.Contains("TBA") ? "ru: " : "eng: ";
                    dateString = TrimString(dateString, key, " г."); // '... Дата выхода eng: 09 марта 2012 г. ...' -> '09 марта 2012'
                    DateTime date;

                    //dateString might be just a year, e.g. https://www.lostfilm.tv/series/Ghosted/season_1/episode_14/
                    if (dateString.Length == 4)
                    {
                        date = DateTime.ParseExact(dateString, "yyyy", CultureInfo.InvariantCulture).ToLocalTime();
                    }
                    else
                    {
                        date = DateTime.Parse(dateString, new CultureInfo("ru-RU")); // dd mmmm yyyy
                    }

                    var urlDetails = new TrackerUrlDetails(playButton);
                    var episodeReleases = FetchTrackerReleases(urlDetails);

                    foreach (var release in episodeReleases)
                    {
                        release.InfoUrl = indexerResponse.Request.Url.ToString();
                        release.PublishDate = date;
                    }

                    releases.AddRange(episodeReleases);
            }

            return releases;
        }

        private IList<ReleaseInfo> ParseNewResponse(IndexerResponse indexerResponse)
        {
            var releases = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);
            var rows = dom.QuerySelectorAll("div.row");
            foreach (var r in rows)
            {
                var link = r.QuerySelector("a").GetAttribute("href");
                var releaseRequest = new IndexerRequest(_settings.BaseUrl + link.TrimStart('/'), HttpAccept.Html);
                var releaseResponse = new IndexerResponse(releaseRequest, HttpClient.ExecuteProxied(releaseRequest.HttpRequest, Definition));

                // Throw common http errors here before we try to parse
                if (releaseResponse.HttpResponse.HasHttpError)
                {
                    if ((int)releaseResponse.HttpResponse.StatusCode == 429)
                    {
                        throw new TooManyRequestsException(releaseRequest.HttpRequest, releaseResponse.HttpResponse);
                    }
                    else
                    {
                        throw new IndexerException(releaseResponse, "Http error code: " + releaseResponse.HttpResponse.StatusCode);
                    }
                }

                releases.AddRange(ParseRelease(releaseResponse));
            }

            return releases.ToArray();
        }

        private IList<ReleaseInfo> ParseSearchResponse(IndexerResponse indexerResponse)
        {
            var releases = new List<ReleaseInfo>();
            return releases.ToArray();
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            if (indexerResponse.Request.Url.Path == "/new")
            {
                return ParseNewResponse(indexerResponse);
            }

            if (indexerResponse.Request.Url.Path.Contains("/episode_"))
            {
                return ParseNewResponse(indexerResponse);
            }

            if (indexerResponse.Request.Url.Path == "/new")
            {
                return ParseNewResponse(indexerResponse);
            }

            return new List<ReleaseInfo>().ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        private string TrimString(string s, char startChar, char endChar)
        {
            var start = s.IndexOf(startChar);
            var end = s.LastIndexOf(endChar);
            return (start != -1 && end != -1) ? s.Substring(start + 1, end - start - 1) : null;
        }

        private string TrimString(string s, string startString, string endString)
        {
            var start = s.IndexOf(startString);
            var end = s.LastIndexOf(endString);
            return (start != -1 && end != -1) ? s.Substring(start + startString.Length, end - start - startString.Length) : null;
        }

        // private DateTime DateFromEpisodeColumn(AngleSharp.Dom.IElement dateColumn)
        // {
        //     var dateString = dateColumn.QuerySelector("span.small-text")?.TextContent;

        //     // 'Eng: 23.05.2017' -> '23.05.2017' OR '23.05.2017' -> '23.05.2017'
        //     dateString = string.IsNullOrEmpty(dateString) ? dateColumn.QuerySelector("span")?.TextContent : dateString.Substring(dateString.IndexOf(":") + 2);
        //     var date = DateTime.Parse(dateString, new CultureInfo("RU-ru")); // dd.mm.yyyy
        //     return date;
        // }
    }
}
