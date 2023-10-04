using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
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
    public class SceneTime : TorrentIndexerBase<SceneTimeSettings>
    {
        public override string Name => "SceneTime";
        public override string[] IndexerUrls => new[] { "https://www.scenetime.com/" };
        public override string Description => "Always on time";
        public override string Language => "en-US";
        public override Encoding Encoding => Encoding.UTF8;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public SceneTime(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new SceneTimeRequestGenerator(Settings, Capabilities);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new SceneTimeParser(Settings, Capabilities.Categories);
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            if (!httpResponse.Content.Contains("logout.php"))
            {
                throw new IndexerAuthException("SceneTime authentication with cookies failed.");
            }

            return false;
        }

        protected override IDictionary<string, string> GetCookies()
        {
            return CookieUtil.CookieHeaderToDictionary(Settings.Cookie);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.XXX, "Movies Adult");
            caps.Categories.AddCategoryMapping(47, NewznabStandardCategory.Movies, "Movie Packs");
            caps.Categories.AddCategoryMapping(57, NewznabStandardCategory.MoviesSD, "Movies SD");
            caps.Categories.AddCategoryMapping(59, NewznabStandardCategory.MoviesHD, "Movies HD");
            caps.Categories.AddCategoryMapping(64, NewznabStandardCategory.Movies3D, "Movies 3D");
            caps.Categories.AddCategoryMapping(82, NewznabStandardCategory.MoviesOther, "Movies CAM/TS");
            caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.MoviesUHD, "Movies UHD");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVUHD, "TV UHD");
            caps.Categories.AddCategoryMapping(43, NewznabStandardCategory.TV, "TV Packs");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.TVHD, "TV HD");
            caps.Categories.AddCategoryMapping(77, NewznabStandardCategory.TVSD, "TV SD");
            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.TVAnime, "TV ANIME");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.PCGames, "Games PC-ISO");
            caps.Categories.AddCategoryMapping(48, NewznabStandardCategory.ConsoleXBox, "Games XBOX");
            caps.Categories.AddCategoryMapping(51, NewznabStandardCategory.ConsoleWii, "Games Wii");
            caps.Categories.AddCategoryMapping(55, NewznabStandardCategory.ConsoleNDS, "Games Nintendo");
            caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.ConsolePS4, "Games PS");
            caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.ConsoleOther, "Games Dreamcast");
            caps.Categories.AddCategoryMapping(52, NewznabStandardCategory.PCMac, "Mac/Linux");
            caps.Categories.AddCategoryMapping(53, NewznabStandardCategory.PC0day, "Apps");
            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.PCMobileOther, "Mobile Apps");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.Books, "Books and Magazines");
            caps.Categories.AddCategoryMapping(65, NewznabStandardCategory.BooksComics, "Books Comics");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.Audio, "Music");
            caps.Categories.AddCategoryMapping(116, NewznabStandardCategory.Audio, "Music Packs");

            caps.Flags = new List<IndexerFlag>
            {
                IndexerFlag.FreeLeech
            };

            return caps;
        }
    }

    public class SceneTimeRequestGenerator : IIndexerRequestGenerator
    {
        private readonly SceneTimeSettings _settings;
        private readonly IndexerCapabilities _capabilities;

        public SceneTimeRequestGenerator(SceneTimeSettings settings, IndexerCapabilities capabilities)
        {
            _settings = settings;
            _capabilities = capabilities;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories, searchCriteria.FullImdbId));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedTvSearchString}", searchCriteria.Categories, searchCriteria.FullImdbId));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var parameters = new NameValueCollection
            {
                { "cata", "yes" }
            };

            var catList = _capabilities.Categories.MapTorznabCapsToTrackers(categories);
            foreach (var cat in catList)
            {
                parameters.Set($"c{cat}", "1");
            }

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                parameters.Set("imdb", imdbId);
            }

            if (term.IsNotNullOrWhiteSpace())
            {
                parameters.Set("search", term);
            }

            if (_settings.FreeLeechOnly)
            {
                parameters.Set("freeleech", "on");
            }

            var searchUrl = $"{_settings.BaseUrl.TrimEnd('/')}/browse.php?{parameters.GetQueryString()}";

            yield return new IndexerRequest(searchUrl, HttpAccept.Html);
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class SceneTimeParser : IParseIndexerResponse
    {
        private readonly SceneTimeSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public SceneTimeParser(SceneTimeSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releaseInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(indexerResponse.Content);

            var table = dom.QuerySelector("table.movehere");
            if (table == null)
            {
                return releaseInfos; // no results
            }

            var headerColumns = table.QuerySelectorAll("tbody > tr > td.cat_Head").Select(x => x.TextContent).ToList();
            var categoryIndex = headerColumns.FindIndex(x => x.Equals("Type"));
            var nameIndex = headerColumns.FindIndex(x => x.Equals("Name"));
            var sizeIndex = headerColumns.FindIndex(x => x.Equals("Size"));
            var seedersIndex = headerColumns.FindIndex(x => x.Equals("Seeders"));
            var leechersIndex = headerColumns.FindIndex(x => x.Equals("Leechers"));

            var rows = dom.QuerySelectorAll("tr.browse");

            foreach (var row in rows)
            {
                var qDescCol = row.Children[nameIndex];
                var qLink = qDescCol.QuerySelector("a");

                // Clean up title
                qLink.QuerySelectorAll("font[color=\"green\"]").ToList().ForEach(e => e.Remove());
                var title = qLink.TextContent.Trim();

                var infoUrl = _settings.BaseUrl + qLink.GetAttribute("href")?.TrimStart('/');
                var torrentId = ParseUtil.GetArgumentFromQueryString(infoUrl, "id");
                var seeders = ParseUtil.CoerceInt(row.Children[seedersIndex].TextContent.Trim());

                var categoryLink = row.Children[categoryIndex].QuerySelector("a")?.GetAttribute("href");
                var cat = categoryLink != null ? ParseUtil.GetArgumentFromQueryString(categoryLink, "cat") : "82"; // default

                var dateAdded = qDescCol.QuerySelector("span[class=\"elapsedDate\"]")?.GetAttribute("title")?.Trim();
                var publishDate = DateTime.TryParseExact(dateAdded, "dddd, MMMM d, yyyy \\a\\t h:mmtt", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)
                    ? date
                    : DateTimeUtil.FromTimeAgo(qDescCol.QuerySelector("span[class=\"elapsedDate\"]").TextContent.Trim());

                var release = new TorrentInfo
                {
                    Guid = infoUrl,
                    InfoUrl = infoUrl,
                    DownloadUrl = $"{_settings.BaseUrl}download.php/{torrentId}/download.torrent",
                    Title = title,
                    Categories = _categories.MapTrackerCatToNewznab(cat),
                    PublishDate = publishDate,
                    Size = ParseUtil.GetBytes(row.Children[sizeIndex].TextContent),
                    Seeders = seeders,
                    Peers = ParseUtil.CoerceInt(row.Children[leechersIndex].TextContent.Trim()) + seeders,
                    DownloadVolumeFactor = row.QuerySelector("font > b:contains(Freeleech)") != null ? 0 : 1,
                    UploadVolumeFactor = 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 259200 // 72 hours
                };

                //TODO Do something with this filtering
                //if (!query.MatchQueryStringAND(release.Title))
                //    continue;
                releaseInfos.Add(release);
            }

            return releaseInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class SceneTimeSettings : CookieTorrentBaseSettings
    {
        [FieldDefinition(3, Label = "FreeLeech Only", Type = FieldType.Checkbox,  HelpText = "Search FreeLeech torrents only")]
        public bool FreeLeechOnly { get; set; }
    }
}
