using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using NLog;
using NzbDrone.Common.Http;
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
    [Obsolete("Site is unusable due to a mix of HTTP errors")]
    public class Animedia : TorrentIndexerBase<NoAuthTorrentBaseSettings>
    {
        public override string Name => "Animedia";
        public override string[] IndexerUrls => new[] { "https://tt.animedia.tv/" };
        public override string Description => "Animedia is RUSSIAN anime voiceover group and eponymous anime tracker.";
        public override string Language => "ru-RU";
        public override Encoding Encoding => Encoding.UTF8;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public Animedia(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AnimediaRequestGenerator(Settings);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new AnimediaParser(Definition, Settings, Capabilities.Categories, RateLimit, _httpClient);
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

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.TVAnime, "TV Anime");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVAnime, "OVA/ONA/Special");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.TV, "Dorama");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.Movies, "Movies");

            return caps;
        }
    }

    public class AnimediaRequestGenerator : IIndexerRequestGenerator
    {
        private readonly NoAuthTorrentBaseSettings _settings;

        public AnimediaRequestGenerator(NoAuthTorrentBaseSettings settings)
        {
            _settings = settings;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term)
        {
            string requestUrl;

            if (string.IsNullOrWhiteSpace(term))
            {
                requestUrl = _settings.BaseUrl;
            }
            else
            {
                var queryCollection = new NameValueCollection
                {
                    // Remove season and episode info from search term cause it breaks search
                    { "keywords", Regex.Replace(term, @"(?:[SsEe]?\d{1,4}){1,2}$", "").TrimEnd() },
                    { "limit", "20" },
                    { "orderby_sort", "entry_date|desc" }
                };

                requestUrl = $"{_settings.BaseUrl.TrimEnd('/')}/ajax/search_result/P0?{queryCollection.GetQueryString()}";
            }

            yield return new IndexerRequest(requestUrl, HttpAccept.Html);
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

        // Animedia doesn't support music, but this function required by interface
        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        // Animedia doesn't support books, but this function required by interface
        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class AnimediaParser : IParseIndexerResponse
    {
        private readonly ProviderDefinition _definition;
        private readonly NoAuthTorrentBaseSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        private readonly TimeSpan _rateLimit;
        private readonly IIndexerHttpClient _httpClient;

        private static readonly Regex EpisodesInfoQueryRegex = new Regex(@"сери[ия] (\d+)(?:-(\d+))? из.*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ResolutionInfoQueryRegex = new Regex(@"качество (\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SizeInfoQueryRegex = new Regex(@"размер:(.*)\n", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ReleaseDateInfoQueryRegex = new Regex(@"добавлен:(.*)\n", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CategorieMovieRegex = new Regex(@"Фильм", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CategorieOVARegex = new Regex(@"ОВА|OVA|ОНА|ONA|Special", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CategorieDoramaRegex = new Regex(@"Дорама", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public AnimediaParser(ProviderDefinition definition, NoAuthTorrentBaseSettings settings, IndexerCapabilitiesCategories categories, TimeSpan rateLimit, IIndexerHttpClient httpClient)
        {
            _definition = definition;
            _settings = settings;
            _categories = categories;
            _rateLimit = rateLimit;
            _httpClient = httpClient;
        }

        private string ComposeTitle(AngleSharp.Html.Dom.IHtmlDocument dom, AngleSharp.Dom.IElement t, AngleSharp.Dom.IElement tr)
        {
            var nameRu = dom.QuerySelector("div.media__post__header > h1")?.TextContent.Trim() ?? string.Empty;
            var nameEn = dom.QuerySelector("div.media__panel > div:nth-of-type(1) > div.col-l:nth-of-type(1) > div > span")?.TextContent.Trim() ?? string.Empty;
            var nameOrig = dom.QuerySelector("div.media__panel > div:nth-of-type(1) > div.col-l:nth-of-type(2) > div > span")?.TextContent.Trim() ?? string.Empty;

            var title = nameRu + " / " + nameEn;
            if (nameEn != nameOrig)
            {
                title += " / " + nameOrig;
            }

            var tabName = t.TextContent;
            tabName = tabName.Replace("Сезон", "Season");
            if (tabName.Contains("Серии"))
            {
                tabName = "";
            }

            var heading = tr.QuerySelector("h3.tracker_info_bold")?.TextContent.Trim() ?? string.Empty;

            // Parse episodes info from heading if episods info present
            var match = EpisodesInfoQueryRegex.Match(heading);
            heading = tabName;
            if (match.Success)
            {
                if (string.IsNullOrEmpty(match.Groups[2].Value))
                {
                    heading += $" E{match.Groups[1].Value}";
                }
                else
                {
                    heading += $" E{match.Groups[1].Value}-{match.Groups[2].Value}";
                }
            }

            return title + " - " + heading + " [" + GetResolution(tr) + "p]";
        }

        private string GetResolution(AngleSharp.Dom.IElement tr)
        {
            var resolution = tr.QuerySelector("div.tracker_info_left")?.TextContent.Trim() ?? string.Empty;
            return ResolutionInfoQueryRegex.Match(resolution).Groups[1].Value;
        }

        private long GetReleaseSize(AngleSharp.Dom.IElement tr)
        {
            var sizeStr = tr.QuerySelector("div.tracker_info_left")?.TextContent.Trim() ?? string.Empty;
            return ParseUtil.GetBytes(SizeInfoQueryRegex.Match(sizeStr).Groups[1].Value.Trim());
        }

        private DateTime GetReleaseDate(AngleSharp.Dom.IElement tr)
        {
            var sizeStr = tr.QuerySelector("div.tracker_info_left")?.TextContent.Trim() ?? string.Empty;
            return DateTime.Parse(ReleaseDateInfoQueryRegex.Match(sizeStr).Groups[1].Value.Trim());
        }

        private ICollection<IndexerCategory> MapCategories(AngleSharp.Html.Dom.IHtmlDocument dom, AngleSharp.Dom.IElement t, AngleSharp.Dom.IElement tr)
        {
            var rName = t.TextContent;
            var rDesc = tr.QuerySelector("h3.tracker_info_bold")?.TextContent.Trim() ?? string.Empty;
            var type = dom.QuerySelector("div.releases-date:contains('Тип:')")?.TextContent.Trim() ?? string.Empty;

            // Check OVA first cause OVA looks like anime with OVA in release name or description
            if (CategorieOVARegex.IsMatch(rName) || CategorieOVARegex.IsMatch(rDesc))
            {
                return _categories.MapTrackerCatDescToNewznab("OVA/ONA/Special");
            }

            // Check movies then, cause some of releases could be movies dorama and should go to movies category
            if (CategorieMovieRegex.IsMatch(rName) || CategorieMovieRegex.IsMatch(rDesc))
            {
                return _categories.MapTrackerCatDescToNewznab("Movies");
            }

            // Check dorama. Most of doramas are flaged as doramas in type info, but type info could have a lot of types at same time (movie, etc)
            if (CategorieDoramaRegex.IsMatch(rName) || CategorieDoramaRegex.IsMatch(type))
            {
                return _categories.MapTrackerCatDescToNewznab("Dorama");
            }

            return _categories.MapTrackerCatDescToNewznab("TV Anime");
        }

        private IList<TorrentInfo> ParseRelease(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();
            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(indexerResponse.Content);

            foreach (var t in dom.QuerySelectorAll("ul.media__tabs__nav > li > a"))
            {
                var trId = t.GetAttribute("href");
                var tr = dom.QuerySelector("div" + trId);
                var seeders = int.Parse(tr.QuerySelector("div.circle_green_text_top").TextContent);
                var url = indexerResponse.HttpRequest.Url.FullUri;

                var release = new TorrentInfo
                {
                    Title = ComposeTitle(dom, t, tr),
                    InfoUrl = url,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1,

                    Guid = url + trId,
                    Seeders = seeders,
                    Peers = seeders + int.Parse(tr.QuerySelector("div.circle_red_text_top").TextContent),
                    Grabs = int.Parse(tr.QuerySelector("div.circle_grey_text_top").TextContent),
                    Categories = MapCategories(dom, t, tr),
                    PublishDate = GetReleaseDate(tr),
                    DownloadUrl = tr.QuerySelector("div.download_tracker > a.btn__green").GetAttribute("href"),
                    MagnetUrl = tr.QuerySelector("div.download_tracker > a.btn__d-gray").GetAttribute("href"),
                    Size = GetReleaseSize(tr),
                    Resolution = GetResolution(tr)
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

            var links = dom.QuerySelectorAll("a.ads-list__item__title");
            foreach (var link in links)
            {
                var url = link.GetAttribute("href");

                // Some URLs in search are broken
                if (url.StartsWith("//"))
                {
                    url = "https:" + url;
                }

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
}
