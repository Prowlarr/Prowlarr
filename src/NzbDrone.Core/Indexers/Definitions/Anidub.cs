using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
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

namespace NzbDrone.Core.Indexers.Definitions
{
    public class Anidub : TorrentIndexerBase<AnidubSettings>
    {
        public override string Name => "Anidub";
        public override string[] IndexerUrls => new[] { "https://tr.anidub.com/" };
        public override string Description => "Anidub is RUSSIAN anime voiceover group and eponymous anime tracker.";
        public override string Language => "ru-RU";
        public override Encoding Encoding => Encoding.UTF8;
        public override IndexerPrivacy Privacy => IndexerPrivacy.SemiPrivate;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public Anidub(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AnidubRequestGenerator(Settings);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new AnidubParser(Definition, Settings, Capabilities.Categories, RateLimit, _httpClient, _logger);
        }

        protected override async Task DoLogin()
        {
            UpdateCookies(null, null);

            var mainPage = await ExecuteAuth(new HttpRequest(Settings.BaseUrl));

            var requestBuilder = new HttpRequestBuilder(Settings.BaseUrl + "index.php")
            {
                LogResponseContent = true,
                AllowAutoRedirect = true,
                Method = HttpMethod.Post
            };

            var authLoginRequest = requestBuilder
                .SetCookies(mainPage.GetCookies())
                .AddFormParameter("login_name", Settings.Username)
                .AddFormParameter("login_password", Settings.Password)
                .AddFormParameter("login", "submit")
                .SetHeader("Content-Type", "application/x-www-form-urlencoded")
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            if (response.Content != null && !CheckIfLoginNeeded(response))
            {
                UpdateCookies(response.GetCookies(), DateTime.Now.AddDays(30));
                _logger.Debug("Anidub authentication succeeded");
            }
            else
            {
                var parser = new HtmlParser();
                using var document = await parser.ParseDocumentAsync(response.Content);
                var errorMessage = document.QuerySelector("#content .berror .berror_c")?.TextContent.Trim();

                throw new IndexerAuthException(errorMessage ?? "Unknown error message, please report.");
            }
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            return !httpResponse.Content.Contains("index.php?action=logout");
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
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVAnime, "Аниме TV");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.Movies, "Аниме Фильмы");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.TVAnime, "Аниме OVA");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.TVAnime, "Аниме OVA |- Аниме ONA");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.TV, "Дорамы / Японские Сериалы и Фильмы");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.TV, "Дорамы / Корейские Сериалы и Фильмы");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.TV, "Дорамы / Китайские Сериалы и Фильмы");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.TV, "Дорамы");
            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.TVAnime, "Аниме TV / Аниме Ongoing");
            caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.TVAnime, "Аниме TV / Многосерийный сёнэн");
            caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.Other, "Аниме Ongoing Анонсы");
            caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.XXX, "18+");
            caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.TVAnime, "Аниме TV / Законченные сериалы");
            caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.BooksComics, "Манга");
            caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.Audio, "OST");
            caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.Audio, "Подкасты");

            return caps;
        }
    }

    public class AnidubRequestGenerator : IIndexerRequestGenerator
    {
        private readonly UserPassTorrentBaseSettings _settings;

        public AnidubRequestGenerator(UserPassTorrentBaseSettings settings)
        {
            _settings = settings;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term)
        {
            string requestUrl;
            var isSearch = !string.IsNullOrWhiteSpace(term);

            if (isSearch)
            {
                requestUrl = $"{_settings.BaseUrl.TrimEnd('/')}/index.php?do=search";
            }
            else
            {
                requestUrl = _settings.BaseUrl;
            }

            var request = new IndexerRequest(requestUrl, HttpAccept.Html);

            if (isSearch)
            {
                request.HttpRequest.Method = HttpMethod.Post;
                var postData = new NameValueCollection
                {
                    { "do", "search" },
                    { "subaction", "search" },
                    { "search_start", "1" },
                    { "full_search", "1" },
                    { "result_from", "1" },

                    // Remove season and episode info from search term cause it breaks search
                    { "story", Regex.Replace(term, @"(?:[SsEe]?\d{1,4}){1,2}$", "").TrimEnd() },
                    { "titleonly", "3" },
                    { "searchuser", "" },
                    { "replyless", "0" },
                    { "replylimit", "0" },
                    { "searchdate", "0" },
                    { "beforeafter", "after" },
                    { "sortby", "" },
                    { "resorder", "desc" },
                    { "showposts", "1" },
                    { "catlist[]", "0" }
                };
                var headers = new NameValueCollection
                {
                    { "Content-Type", "application/x-www-form-urlencoded" }
                };

                request.HttpRequest.SetContent(postData.GetQueryString());
                request.HttpRequest.Headers.Add(headers);
            }

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}"));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedTvSearchString}"));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}"));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}"));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}"));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class AnidubParser : IParseIndexerResponse
    {
        private readonly ProviderDefinition _definition;
        private readonly AnidubSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        private readonly TimeSpan _rateLimit;
        private readonly IIndexerHttpClient _httpClient;
        private readonly Logger _logger;
        private readonly AnidubTitleParser _titleParser = new();

        private static Dictionary<string, string> CategoriesMap => new()
        {
            { "/anime_tv/full", "14" },
            { "/anime_tv/anime_ongoing", "10" },
            { "/anime_tv/shonen", "11" },
            { "/anime_tv", "2" },
            { "/xxx", "13" },
            { "/manga", "15" },
            { "/ost", "16" },
            { "/podcast", "17" },
            { "/anime_movie", "3" },
            { "/anime_ova/anime_ona", "5" },
            { "/anime_ova", "4" },
            { "/dorama/japan_dorama", "6" },
            { "/dorama/korea_dorama", "7" },
            { "/dorama/china_dorama", "8" },
            { "/dorama", "9" },
            { "/anons_ongoing", "12" }
        };

        public AnidubParser(ProviderDefinition definition, AnidubSettings settings, IndexerCapabilitiesCategories categories, TimeSpan rateLimit, IIndexerHttpClient httpClient, Logger logger)
        {
            _definition = definition;
            _settings = settings;
            _categories = categories;
            _rateLimit = rateLimit;
            _httpClient = httpClient;
            _logger = logger;
        }

        private string GetTitle(AngleSharp.Html.Dom.IHtmlDocument content, AngleSharp.Dom.IElement tabNode, ICollection<IndexerCategory> categories)
        {
            var domTitle = content.QuerySelector("#news-title");
            var baseTitle = _titleParser.Parse(domTitle.TextContent.Trim(), categories, _settings.StripCyrillicLetters, _settings.AddRussianToTitle);
            var quality = GetQuality(tabNode.ParentElement);

            if (!string.IsNullOrWhiteSpace(quality))
            {
                return $"{baseTitle} [{quality}]";
            }

            return baseTitle;
        }

        private static string GetQuality(AngleSharp.Dom.IElement releaseNode)
        {
            // For some releases there's no block with quality
            if (string.IsNullOrWhiteSpace(releaseNode.Id))
            {
                return null;
            }

            var quality = releaseNode.Id.Trim();
            switch (quality.ToLowerInvariant())
            {
                case "tv720":
                    return "HDTV 720p";
                case "tv1080":
                    return "HDTV 1080p";
                case "bd720":
                    return "BDRip 720p";
                case "bd1080":
                    return "BDRip 1080p";
                case "hwp":
                    return "SDTV";
                default:
                    return quality.ToUpperInvariant();
            }
        }

        private static int GetReleaseLeechers(AngleSharp.Dom.IElement tabNode)
        {
            const string leechersSelector = ".list.down > .li_swing_m";

            var leechersStr = tabNode.QuerySelector(leechersSelector).TextContent;
            int.TryParse(leechersStr, out var leechers);
            return leechers;
        }

        private static int GetReleaseSeeders(AngleSharp.Dom.IElement tabNode)
        {
            const string SeedersSelector = ".list.down > .li_distribute_m";

            var seedersStr = tabNode.QuerySelector(SeedersSelector).TextContent;
            int.TryParse(seedersStr, out var seeders);
            return seeders;
        }

        private static int GetReleaseGrabs(AngleSharp.Dom.IElement tabNode)
        {
            const string grabsSelector = ".list.down > .li_download_m";

            var grabsStr = tabNode.QuerySelector(grabsSelector).TextContent;
            int.TryParse(grabsStr, out var grabs);
            return grabs;
        }

        private static string GetDateFromDocument(AngleSharp.Html.Dom.IHtmlDocument content)
        {
            const string dateSelector = ".story_inf > li:nth-child(2)";

            var domDate = content.QuerySelector(dateSelector).LastChild;

            if (domDate?.NodeName != "#text")
            {
                return string.Empty;
            }

            return domDate.NodeValue.Trim();
        }

        private DateTime GetDateFromShowPage(AngleSharp.Html.Dom.IHtmlDocument content)
        {
            const string dateFormat = "d-MM-yyyy";
            const string dateTimeFormat = dateFormat + ", HH:mm";

            // Would be better to use AssumeLocal and provide "ru-RU" culture,
            // but doesn't work cross-platform
            const DateTimeStyles style = DateTimeStyles.AssumeUniversal;

            var culture = CultureInfo.InvariantCulture;

            var dateText = GetDateFromDocument(content);

            //Correct way but will not always work on cross-platform
            //var localTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
            //var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, localTimeZone);

            // Russian Standard Time is +03:00, no DST
            const int russianStandardTimeDiff = 3;
            var nowLocal = DateTime.UtcNow.AddHours(russianStandardTimeDiff);

            dateText = dateText
                .Replace("Вчера", nowLocal.AddDays(-1).ToString(dateFormat))
                .Replace("Сегодня", nowLocal.ToString(dateFormat));

            if (DateTime.TryParseExact(dateText, dateTimeFormat, culture, style, out var date))
            {
                var utcDate = date.ToUniversalTime();
                return utcDate.AddHours(-russianStandardTimeDiff);
            }

            _logger.Warn($"[AniDub] Date time couldn't be parsed on. Date text: {dateText}");

            return DateTime.UtcNow;
        }

        private static long GetReleaseSize(AngleSharp.Dom.IElement tabNode)
        {
            const string sizeSelector = ".list.down > .red";

            var sizeStr = tabNode.QuerySelector(sizeSelector).TextContent;
            return ParseUtil.GetBytes(sizeStr);
        }

        private string GetReleaseLink(AngleSharp.Dom.IElement tabNode)
        {
            return $"{_settings.BaseUrl}engine/download.php?id={GetTorrentId(tabNode)}";
        }

        private static string GetTorrentId(AngleSharp.Dom.IElement tabNode)
        {
            var nodeId = tabNode.Id;

            // Format is "torrent_{id}_info"
            return nodeId
                .Replace("torrent_", string.Empty)
                .Replace("_info", string.Empty);
        }

        private ICollection<IndexerCategory> ParseCategories(string uriPath)
        {
            var categoriesMap = CategoriesMap;

            return categoriesMap
                .Where(categoryMap => uriPath.StartsWith(categoryMap.Key))
                .Select(categoryMap => _categories.MapTrackerCatToNewznab(categoryMap.Value))
                .FirstOrDefault();
        }

        private IList<TorrentInfo> ParseRelease(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();
            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(indexerResponse.Content);

            var categories = ParseCategories(indexerResponse.Request.Url.Path);

            foreach (var t in dom.QuerySelectorAll("#tabs .torrent_c > div"))
            {
                var release = new TorrentInfo
                {
                    Title = GetTitle(dom, t, categories),
                    InfoUrl = indexerResponse.Request.Url.FullUri,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1,

                    Guid = indexerResponse.Request.Url.FullUri + t.Id,
                    Seeders = GetReleaseSeeders(t),
                    Peers = GetReleaseSeeders(t) + GetReleaseLeechers(t),
                    Grabs = GetReleaseGrabs(t),
                    Categories = categories,
                    PublishDate = GetDateFromShowPage(dom),
                    DownloadUrl = GetReleaseLink(t),
                    Size = GetReleaseSize(t),
                    Resolution = GetQuality(t)
                };
                torrentInfos.Add(release);
            }

            return torrentInfos;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(indexerResponse.Content);

            var links = dom.QuerySelectorAll(".searchitem > h3 > a[href], #dle-content > .story > .story_h > .lcol > h2 > a[href]");
            foreach (var link in links)
            {
                var url = link.GetAttribute("href");

                var releaseRequest = new HttpRequestBuilder(url)
                    .WithRateLimit(_rateLimit.TotalSeconds)
                    .SetHeader("Referer", _settings.BaseUrl)
                    .Accept(HttpAccept.Html)
                    .Build();

                var releaseIndexerRequest = new IndexerRequest(releaseRequest);
                var releaseResponse = new IndexerResponse(releaseIndexerRequest, _httpClient.ExecuteProxied(releaseIndexerRequest.HttpRequest, _definition));

                // Throw common http errors here before we try to parse
                if (releaseResponse.HttpResponse.HasHttpError)
                {
                    if (releaseResponse.HttpResponse.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        throw new TooManyRequestsException(releaseResponse.HttpRequest, releaseResponse.HttpResponse);
                    }

                    throw new IndexerException(releaseResponse, $"HTTP Error - {releaseResponse.HttpResponse.StatusCode}. {url}");
                }

                torrentInfos.AddRange(ParseRelease(releaseResponse));
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class AnidubTitleParser
    {
        // Season markers: "ТВ-2", "TV-2", "TV 3" and roman numerals ("TV-IV")
        private static readonly Regex SeasonMarkerRegex = new(@"\b(?:ТВ|TV)[-\s]?(\d{1,3}|[IVXLC]+)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Specials appended after the main batch: "+ Specials [6 из 6]", "+ SP"
        private static readonly Regex SpecialsSuffixRegex = new(@"\s*\+\s*(?:Specials?|SP)\s*(?:\[[^\]]*\])?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Batch episode counter: "[28 из 28]", "[10 из 24]", "[151-366 из 366]", "[293 из ххх]", "[51 из 51 + SP]"
        private static readonly Regex BatchBracketRegex = new(@"\s*\[(\d+)(?:-(\d+))?\s+из\s+(\d+|[xх]+|\?+)(?:\s*\+\s*\d*\s*SP)?\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex MovieBracketRegex = new(@"\s*\[Movie\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex SeasonTokenRegex = new(@"\bS\d+", RegexOptions.Compiled);

        private static readonly Regex TitlePartSplitRegex = new(@"\s*/\s*", RegexOptions.Compiled);

        private static readonly Regex StripCyrillicRegex = new(@"(\([\p{IsCyrillic}\W]+\))|(^[\p{IsCyrillic}\W\d]+\/ )|([\p{IsCyrillic} \-]+,+)|([\p{IsCyrillic}]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Parse(string title, ICollection<IndexerCategory> categories, bool stripCyrillicLetters = false, bool addRussianToTitle = false)
        {
            categories ??= Array.Empty<IndexerCategory>();

            // https://www.fileformat.info/info/unicode/category/Pd/list.htm
            title = Regex.Replace(title, @"\p{Pd}", "-", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            if (IsAnyTvCategory(categories))
            {
                title = SpecialsSuffixRegex.Replace(title, string.Empty);

                // Both the season marker and the batch counter are removed here
                // and re-appended below in a canonical Sonarr-parseable form.
                var seasonNumber = 1;
                var seasonMatch = SeasonMarkerRegex.Match(title);
                var hasExplicitMarkers = seasonMatch.Success;

                if (seasonMatch.Success)
                {
                    seasonNumber = ParseSeasonNumber(seasonMatch.Groups[1].Value);
                    title = SeasonMarkerRegex.Replace(title, string.Empty);
                }

                var episodes = string.Empty;
                var isCrossSeasonRange = false;

                var batchMatch = BatchBracketRegex.Match(title);
                if (batchMatch.Success)
                {
                    hasExplicitMarkers = true;

                    var from = batchMatch.Groups[1].Value;
                    var to = batchMatch.Groups[2].Value;
                    var total = batchMatch.Groups[3].Value;
                    var totalSuffix = total.All(char.IsDigit) ? $" of {total}" : string.Empty;

                    if (!string.IsNullOrEmpty(to))
                    {
                        // Explicit ranges ("151-366 из 366") may span seasons in absolute numbering, keep them season-less
                        episodes = $"E{from}-{to}{totalSuffix}";
                        isCrossSeasonRange = true;
                    }
                    else if (from == total)
                    {
                        // Complete batch is a full season pack
                        episodes = string.Empty;
                    }
                    else if (int.TryParse(from, out var fromNumber) && fromNumber == 1)
                    {
                        episodes = $"E01{totalSuffix}";
                    }
                    else
                    {
                        // Anidub batches are cumulative and always start from the first episode
                        episodes = $"E01-{from}{totalSuffix}";
                    }

                    title = BatchBracketRegex.Replace(title, string.Empty);
                }

                if (stripCyrillicLetters)
                {
                    title = SelectForeignTitleParts(title);
                }

                if (hasExplicitMarkers)
                {
                    var suffix = isCrossSeasonRange || SeasonTokenRegex.IsMatch(title) ? episodes : $"S{seasonNumber}{episodes}";

                    if (!string.IsNullOrWhiteSpace(suffix))
                    {
                        title = $"{title.TrimEnd()} {suffix}";
                    }
                }
            }
            else
            {
                title = MovieBracketRegex.Replace(title, string.Empty);

                if (stripCyrillicLetters)
                {
                    title = SelectForeignTitleParts(title);
                }
            }

            if (addRussianToTitle && (IsAnyTvCategory(categories) || IsAnyMovieCategory(categories)) && !Regex.IsMatch(title, @"\bRUS\b", RegexOptions.IgnoreCase))
            {
                title += " RUS";
            }

            title = Regex.Replace(title, @"[\[\(]\s*[\)\]]", string.Empty, RegexOptions.Compiled);
            title = title.Trim(' ', ',', '-', '_', '|', '/', '\\', ':', ';');

            // replace multiple spaces with a single space
            title = Regex.Replace(title, @"\s+", " ");

            return title.Trim();
        }

        private static string SelectForeignTitleParts(string title)
        {
            // Anidub titles are "<russian title> / <romaji title> [batch]", keep the romaji part(s)
            var parts = TitlePartSplitRegex.Split(title);

            if (parts.Length > 1)
            {
                var foreignParts = parts.Where(part => !IsMostlyCyrillic(part)).ToList();

                if (foreignParts.Count > 0 && foreignParts.Count < parts.Length)
                {
                    return string.Join(" / ", foreignParts);
                }
            }

            return StripCyrillicRegex.Replace(title, string.Empty).Trim(' ', '-');
        }

        private static bool IsMostlyCyrillic(string part)
        {
            var cyrillicLetters = part.Count(c => c is >= 'А' and <= 'я');
            var latinLetters = part.Count(c => c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z'));

            return cyrillicLetters > latinLetters;
        }

        private static int ParseSeasonNumber(string value)
        {
            if (int.TryParse(value, out var number))
            {
                return number;
            }

            // Roman numerals (e.g. "Overlord TV-IV")
            var result = 0;
            var previous = 0;

            foreach (var letter in value.ToUpperInvariant().Reverse())
            {
                var current = letter switch
                {
                    'I' => 1,
                    'V' => 5,
                    'X' => 10,
                    'L' => 50,
                    'C' => 100,
                    _ => 0
                };

                result += current < previous ? -current : current;
                previous = current;
            }

            return result;
        }

        private static bool IsAnyTvCategory(ICollection<IndexerCategory> categories)
        {
            return categories.Contains(NewznabStandardCategory.TV) || NewznabStandardCategory.TV.SubCategories.Any(subCategory => categories.Contains(subCategory));
        }

        private static bool IsAnyMovieCategory(ICollection<IndexerCategory> categories)
        {
            return categories.Contains(NewznabStandardCategory.Movies) || NewznabStandardCategory.Movies.SubCategories.Any(subCategory => categories.Contains(subCategory));
        }
    }

    public class AnidubSettings : UserPassTorrentBaseSettings
    {
        public AnidubSettings()
        {
            StripCyrillicLetters = false;
            AddRussianToTitle = false;
        }

        [FieldDefinition(4, Label = "Strip Cyrillic Letters", HelpText = "Strip the Cyrillic title part from release names to improve parsing by Sonarr and Radarr", Type = FieldType.Checkbox)]
        public bool StripCyrillicLetters { get; set; }

        [FieldDefinition(5, Label = "Add RUS to titles", HelpText = "Add RUS to end of all titles to improve language detection by Sonarr and Radarr. Will cause English-only results to be misidentified.", Type = FieldType.Checkbox)]
        public bool AddRussianToTitle { get; set; }
    }
}
