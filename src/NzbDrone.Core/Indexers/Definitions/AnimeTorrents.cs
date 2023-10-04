using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class AnimeTorrents : TorrentIndexerBase<AnimeTorrentsSettings>
    {
        public override string Name => "AnimeTorrents";
        public override string[] IndexerUrls => new[] { "https://animetorrents.me/" };
        public override string Description => "Definitive source for anime and manga";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override bool SupportsPagination => true;
        public override TimeSpan RateLimit => TimeSpan.FromSeconds(4);
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public AnimeTorrents(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AnimeTorrentsRequestGenerator(Settings, Capabilities);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new AnimeTorrentsParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            UpdateCookies(null, null);

            var loginUrl = Settings.BaseUrl + "login.php";

            var loginPage = await ExecuteAuth(new HttpRequest(loginUrl));

            var requestBuilder = new HttpRequestBuilder(loginUrl)
            {
                LogResponseContent = true,
                AllowAutoRedirect = true
            };

            var authLoginRequest = requestBuilder
                .Post()
                .SetCookies(loginPage.GetCookies())
                .AddFormParameter("username", Settings.Username)
                .AddFormParameter("password", Settings.Password)
                .AddFormParameter("form", "login")
                .AddFormParameter("rememberme[]", "1")
                .SetHeader("Content-Type", "application/x-www-form-urlencoded")
                .SetHeader("Referer", loginUrl)
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            if (response.Content == null || !response.Content.Contains("logout.php"))
            {
                throw new IndexerAuthException("AnimeTorrents authentication failed");
            }

            UpdateCookies(response.GetCookies(), DateTime.Now.AddDays(30));

            _logger.Debug("AnimeTorrents authentication succeeded");
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            return httpResponse.Content.Contains("Access Denied!") || httpResponse.Content.Contains("login.php");
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

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.MoviesSD, "Anime Movie");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.MoviesHD, "Anime Movie HD");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVAnime, "Anime Series");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.TVAnime, "Anime Series HD");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.XXXDVD, "Hentai (censored)");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.XXXDVD, "Hentai (censored) HD");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.XXXDVD, "Hentai (un-censored)");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.XXXDVD, "Hentai (un-censored) HD");
            caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.BooksForeign, "Light Novel");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.BooksComics, "Manga");
            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.BooksComics, "Manga 18+");
            caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.TVAnime, "OVA");
            caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.TVAnime, "OVA HD");
            caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.BooksComics, "Doujin Anime");
            caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.XXXDVD, "Doujin Anime 18+");
            caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.AudioForeign, "Doujin Music");
            caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.BooksComics, "Doujinshi");
            caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.BooksComics, "Doujinshi 18+");
            caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.Audio, "OST");

            return caps;
        }
    }

    public class AnimeTorrentsRequestGenerator : IIndexerRequestGenerator
    {
        private readonly AnimeTorrentsSettings _settings;
        private readonly IndexerCapabilities _capabilities;

        public AnimeTorrentsRequestGenerator(AnimeTorrentsSettings settings, IndexerCapabilities capabilities)
        {
            _settings = settings;
            _capabilities = capabilities;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var searchTerm = $"{searchCriteria.SanitizedSearchTerm}";

            foreach (var category in GetTrackerCategories(searchTerm, searchCriteria))
            {
                pageableRequests.Add(GetPagedRequests(searchTerm, category, searchCriteria));
            }

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var searchTerm = $"{searchCriteria.SanitizedSearchTerm}";

            foreach (var category in GetTrackerCategories(searchTerm, searchCriteria))
            {
                pageableRequests.Add(GetPagedRequests(searchTerm, category, searchCriteria));
            }

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var searchTerm = $"{searchCriteria.SanitizedSearchTerm}";

            foreach (var category in GetTrackerCategories(searchTerm, searchCriteria))
            {
                pageableRequests.Add(GetPagedRequests(searchTerm, category, searchCriteria));
            }

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var searchTerm = $"{searchCriteria.SanitizedSearchTerm}";

            foreach (var category in GetTrackerCategories(searchTerm, searchCriteria))
            {
                pageableRequests.Add(GetPagedRequests(searchTerm, category, searchCriteria));
            }

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var searchTerm = $"{searchCriteria.SanitizedSearchTerm}";

            foreach (var category in GetTrackerCategories(searchTerm, searchCriteria))
            {
                pageableRequests.Add(GetPagedRequests(searchTerm, category, searchCriteria));
            }

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, string category, SearchCriteriaBase searchCriteria)
        {
            var searchUrl = _settings.BaseUrl + "ajax/torrents_data.php";

            // replace non-word characters with % (wildcard)
            var searchString = Regex.Replace(term.Trim(), @"[\W]+", "%");

            var page = searchCriteria.Limit is > 0 && searchCriteria.Offset is > 0 ? (int)(searchCriteria.Offset / searchCriteria.Limit) + 1 : 1;

            var refererUri = new HttpUri(_settings.BaseUrl)
                .CombinePath("/torrents.php")
                .AddQueryParam("cat", $"{category}");

            if (_settings.DownloadableOnly)
            {
                refererUri = refererUri.AddQueryParam("dlable", "1");
            }

            var requestBuilder = new HttpRequestBuilder(searchUrl)
                .AddQueryParam("total", "100") // Assuming the total number of pages
                .AddQueryParam("cat", $"{category}")
                .AddQueryParam("searchin", "filename")
                .AddQueryParam("search", searchString)
                .AddQueryParam("page", page)
                .SetHeader("X-Requested-With", "XMLHttpRequest")
                .SetHeader("Referer", refererUri.FullUri)
                .Accept(HttpAccept.Html);

            if (_settings.DownloadableOnly)
            {
                requestBuilder.AddQueryParam("dlable", "1");
            }

            yield return new IndexerRequest(requestBuilder.Build());
        }

        private List<string> GetTrackerCategories(string term, SearchCriteriaBase searchCriteria)
        {
            var searchTerm = term.Trim();

            var categoryMapping = _capabilities.Categories
                .MapTorznabCapsToTrackers(searchCriteria.Categories)
                .Distinct()
                .ToList();

            return searchTerm.IsNullOrWhiteSpace() && categoryMapping.Count == 2
                ? categoryMapping
                : new List<string> { categoryMapping.FirstIfSingleOrDefault("0") };
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class AnimeTorrentsParser : IParseIndexerResponse
    {
        private readonly AnimeTorrentsSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public AnimeTorrentsParser(AnimeTorrentsSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releaseInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(indexerResponse.Content);

            var rows = dom.QuerySelectorAll("table tr");
            foreach (var (row, index) in rows.Skip(1).Select((v, i) => (v, i)))
            {
                var downloadVolumeFactor = row.QuerySelector("img[alt=\"Gold Torrent\"]") != null ? 0 : row.QuerySelector("img[alt=\"Silver Torrent\"]") != null ? 0.5 : 1;

                // skip non-freeleech results when freeleech only is set
                if (_settings.FreeleechOnly && downloadVolumeFactor != 0)
                {
                    continue;
                }

                var qTitleLink = row.QuerySelector("td:nth-of-type(2) a:nth-of-type(1)");
                var title = qTitleLink?.TextContent.Trim();

                // If we search an get no results, we still get a table just with no info.
                if (title.IsNullOrWhiteSpace())
                {
                    break;
                }

                var infoUrl = qTitleLink?.GetAttribute("href");

                // newbie users don't see DL links
                // use details link as placeholder
                // skipping the release prevents newbie users from adding the tracker (empty result)
                var downloadUrl = row.QuerySelector("td:nth-of-type(3) a")?.GetAttribute("href") ?? infoUrl;

                var connections = row.QuerySelector("td:nth-of-type(8)").TextContent.Trim().Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                var seeders = ParseUtil.CoerceInt(connections[0]);

                var categoryLink = row.QuerySelector("td:nth-of-type(1) a")?.GetAttribute("href") ?? string.Empty;
                var categoryId = ParseUtil.GetArgumentFromQueryString(categoryLink, "cat");

                var publishedDate = DateTime.ParseExact(row.QuerySelector("td:nth-of-type(5)").TextContent, "dd MMM yy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

                if (publishedDate.Date == DateTime.Today)
                {
                    publishedDate = publishedDate.Date + DateTime.Now.TimeOfDay - TimeSpan.FromMinutes(index);
                }

                var release = new TorrentInfo
                {
                    Guid = infoUrl,
                    InfoUrl = infoUrl,
                    DownloadUrl = downloadUrl,
                    Title = title,
                    Categories = _categories.MapTrackerCatToNewznab(categoryId),
                    PublishDate = publishedDate,
                    Size = ParseUtil.GetBytes(row.QuerySelector("td:nth-of-type(6)").TextContent.Trim()),
                    Seeders = seeders,
                    Peers = ParseUtil.CoerceInt(connections[1]) + seeders,
                    Grabs = ParseUtil.CoerceInt(connections[2]),
                    DownloadVolumeFactor = downloadVolumeFactor,
                    UploadVolumeFactor = 1,
                    Genres = row.QuerySelectorAll("td:nth-of-type(2) a.tortags").Select(t => t.TextContent.Trim()).ToList()
                };

                var uLFactorImg = row.QuerySelector("img[alt*=\"x Multiplier Torrent\"]");
                if (uLFactorImg != null)
                {
                    release.UploadVolumeFactor = ParseUtil.CoerceDouble(uLFactorImg.GetAttribute("alt").Split('x')[0]);
                }

                releaseInfos.Add(release);
            }

            return releaseInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class AnimeTorrentsSettings : UserPassTorrentBaseSettings
    {
        public AnimeTorrentsSettings()
        {
            FreeleechOnly = false;
            DownloadableOnly = false;
        }

        [FieldDefinition(4, Label = "Freeleech Only", Type = FieldType.Checkbox, HelpText = "Show freeleech torrents only")]
        public bool FreeleechOnly { get; set; }

        [FieldDefinition(5, Label = "Downloadable Only", Type = FieldType.Checkbox, HelpText = "Search downloadable torrents only (enable this only if your account class is Newbie)")]
        public bool DownloadableOnly { get; set; }
    }
}
