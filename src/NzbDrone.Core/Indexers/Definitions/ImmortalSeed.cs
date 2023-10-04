using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class ImmortalSeed : TorrentIndexerBase<UserPassTorrentBaseSettings>
    {
        public override string Name => "ImmortalSeed";
        public override string[] IndexerUrls => new[] { "https://immortalseed.me/" };
        public override string Description => "ImmortalSeed (iS) is a Private Torrent Tracker for MOVIES / TV / GENERAL";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();
        public override TimeSpan RateLimit => TimeSpan.FromSeconds(5);
        private string LoginUrl => Settings.BaseUrl + "takelogin.php";

        public ImmortalSeed(IIndexerHttpClient httpClient,
                            IEventAggregator eventAggregator,
                            IIndexerStatusService indexerStatusService,
                            IConfigService configService,
                            Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new ImmortalSeedRequestGenerator(Settings, Capabilities);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new ImmortalSeedParser(Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true,
                AllowAutoRedirect = true,
                Method = HttpMethod.Post
            };

            var authLoginRequest = requestBuilder
                .AddFormParameter("username", Settings.Username)
                .AddFormParameter("password", Settings.Password)
                .SetHeader("Content-Type", "application/x-www-form-urlencoded")
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            if (!response.Content.Contains("logout.php"))
            {
                throw new IndexerAuthException("ImmortalSeed Auth Failed");
            }

            var cookies = response.GetCookies();
            UpdateCookies(cookies, DateTime.Now.AddDays(30));

            _logger.Debug("ImmortalSeed authentication succeeded.");
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            return httpResponse.Content.Contains("You do not have permission to access this page.");
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
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.Other, "Nuked");
            caps.Categories.AddCategoryMapping(32, NewznabStandardCategory.TVAnime, "Anime");
            caps.Categories.AddCategoryMapping(23, NewznabStandardCategory.PC, "Apps");
            caps.Categories.AddCategoryMapping(35, NewznabStandardCategory.AudioAudiobook, "Audiobooks");
            caps.Categories.AddCategoryMapping(31, NewznabStandardCategory.TV, "Childrens/Cartoons");
            caps.Categories.AddCategoryMapping(54, NewznabStandardCategory.TVDocumentary, "Documentary - HD");
            caps.Categories.AddCategoryMapping(53, NewznabStandardCategory.TVDocumentary, "Documentary - SD");
            caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.BooksEBook, "Ebooks");
            caps.Categories.AddCategoryMapping(41, NewznabStandardCategory.BooksComics, "Ebooks -- Comics");
            caps.Categories.AddCategoryMapping(46, NewznabStandardCategory.BooksMags, "Ebooks -- Magazines");
            caps.Categories.AddCategoryMapping(25, NewznabStandardCategory.PCGames, "Games");
            caps.Categories.AddCategoryMapping(61, NewznabStandardCategory.ConsoleNDS, "Games -- Nintendo");
            caps.Categories.AddCategoryMapping(26, NewznabStandardCategory.PCGames, "Games -- PC");
            caps.Categories.AddCategoryMapping(28, NewznabStandardCategory.ConsolePS3, "Games -- Playstation");
            caps.Categories.AddCategoryMapping(29, NewznabStandardCategory.ConsoleXBox, "Games -- Xbox");
            caps.Categories.AddCategoryMapping(49, NewznabStandardCategory.PCMobileOther, "Mobile");
            caps.Categories.AddCategoryMapping(51, NewznabStandardCategory.PCMobileAndroid, "Mobile -- Android");
            caps.Categories.AddCategoryMapping(50, NewznabStandardCategory.PCMobileiOS, "Mobile -- IOS");
            caps.Categories.AddCategoryMapping(52, NewznabStandardCategory.PCMobileOther, "Mobile -- Windows");
            caps.Categories.AddCategoryMapping(59, NewznabStandardCategory.MoviesUHD, "Movies-4k");
            caps.Categories.AddCategoryMapping(60, NewznabStandardCategory.MoviesForeign, "Movies-4k -- Non-English");
            caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.MoviesHD, "Movies-HD");
            caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.MoviesForeign, "Movies-HD -- Non-English");
            caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.MoviesSD, "Movies-Low Def");
            caps.Categories.AddCategoryMapping(34, NewznabStandardCategory.MoviesForeign, "Movies-Low Def -- Non-English");
            caps.Categories.AddCategoryMapping(62, NewznabStandardCategory.Movies, "Movies-Packs");
            caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.MoviesSD, "Movies-SD");
            caps.Categories.AddCategoryMapping(33, NewznabStandardCategory.MoviesForeign, "Movies-SD -- Non-English");
            caps.Categories.AddCategoryMapping(30, NewznabStandardCategory.AudioOther, "Music");
            caps.Categories.AddCategoryMapping(37, NewznabStandardCategory.AudioLossless, "Music -- FLAC");
            caps.Categories.AddCategoryMapping(36, NewznabStandardCategory.AudioMP3, "Music -- MP3");
            caps.Categories.AddCategoryMapping(39, NewznabStandardCategory.AudioOther, "Music -- Other");
            caps.Categories.AddCategoryMapping(38, NewznabStandardCategory.AudioVideo, "Music -- Video");
            caps.Categories.AddCategoryMapping(45, NewznabStandardCategory.Other, "Other");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.TVSport, "Sports Tv");
            caps.Categories.AddCategoryMapping(44, NewznabStandardCategory.TVSport, "Sports Tv -- Fitness-Instructional");
            caps.Categories.AddCategoryMapping(58, NewznabStandardCategory.TVSport, "Sports Tv -- Olympics");
            caps.Categories.AddCategoryMapping(47, NewznabStandardCategory.TVSD, "TV - 480p");
            caps.Categories.AddCategoryMapping(64, NewznabStandardCategory.TVUHD, "TV - 4K");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.TVHD, "TV - High Definition");
            caps.Categories.AddCategoryMapping(48, NewznabStandardCategory.TVSD, "TV SD - x264");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.TVSD, "TV SD - XviD");
            caps.Categories.AddCategoryMapping(63, NewznabStandardCategory.TVUHD, "TV Season Packs - 4K");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.TVHD, "TV Season Packs - HD");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.TVSD, "TV Season Packs - SD");

            return caps;
        }
    }

    public class ImmortalSeedRequestGenerator : IIndexerRequestGenerator
    {
        private readonly UserPassTorrentBaseSettings _settings;
        private readonly IndexerCapabilities _capabilities;

        public ImmortalSeedRequestGenerator(UserPassTorrentBaseSettings settings, IndexerCapabilities capabilities)
        {
            _settings = settings;
            _capabilities = capabilities;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedTvSearchString}", searchCriteria.Categories));

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

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
        {
            var parameters = new NameValueCollection
            {
                { "category", "0" },
                { "include_dead_torrents", "yes" },
                { "sort", "added" },
                { "order", "desc" }
            };

            term = Regex.Replace(term, @"[ -._]+", " ").Trim();

            if (term.IsNotNullOrWhiteSpace())
            {
                parameters.Set("do", "search");
                parameters.Set("keywords", term);
                parameters.Set("search_type", "t_name");
            }

            var queryCats = _capabilities.Categories.MapTorznabCapsToTrackers(categories);
            if (queryCats.Any())
            {
                parameters.Set("selectedcats2", string.Join(",", queryCats));
            }

            var searchUrl = _settings.BaseUrl + "browse.php";

            if (parameters.Count > 0)
            {
                searchUrl += $"?{parameters.GetQueryString()}";
            }

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class ImmortalSeedParser : IParseIndexerResponse
    {
        private readonly IndexerCapabilitiesCategories _categories;
        private readonly Regex _dateAddedRegex = new (@"\d{4}-\d{2}-\d{2} \d{2}:\d{2} [AaPp][Mm]", RegexOptions.Compiled);

        public ImmortalSeedParser(IndexerCapabilitiesCategories categories)
        {
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releaseInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(indexerResponse.Content);

            var rows = dom.QuerySelectorAll("table#sortabletable > tbody > tr:has(a[href*=\"details.php?id=\"])");
            foreach (var row in rows)
            {
                // details link, release name gets shortened if it's to long
                var qDetails = row.QuerySelector("div > a[href*=\"details.php?id=\"]");

                // use Title from tooltip or fallback to Details link if there's no tooltip
                var qTitle = row.QuerySelector(".tooltip-content > div:nth-of-type(1)") ?? qDetails;
                var title = qTitle?.TextContent.Trim();
                var description = row.QuerySelector(".tooltip-content > div:nth-of-type(2)")?.TextContent.Replace("|", ",").Replace(" ", "").Trim();

                var infoUrl = qDetails?.GetAttribute("href");
                var downloadUrl = row.QuerySelector("a[href*=\"download.php\"]")?.GetAttribute("href");

                var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(7)")?.TextContent);
                var peers = seeders + ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(8)")?.TextContent.Trim());

                var categoryLink = row.QuerySelector("td:nth-of-type(1) a").GetAttribute("href");
                var cat = ParseUtil.GetArgumentFromQueryString(categoryLink, "category");

                var release = new TorrentInfo
                {
                    Guid = infoUrl,
                    InfoUrl = infoUrl,
                    DownloadUrl = downloadUrl,
                    Title = title,
                    Description = description,
                    Categories = _categories.MapTrackerCatToNewznab(cat),
                    Seeders = seeders,
                    Peers = peers,
                    Size =  ParseUtil.GetBytes(row.QuerySelector("td:nth-of-type(5)")?.TextContent.Trim()),
                    Grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(6)")?.TextContent),
                    UploadVolumeFactor = row.QuerySelector("img[title^=\"x2 Torrent\"]") != null ? 2 : 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 86400 // 24 hours
                };

                var dateAddedMatch = _dateAddedRegex.Match(row.QuerySelector("td:nth-of-type(2) > div:last-child").TextContent.Trim());
                if (dateAddedMatch.Success)
                {
                    release.PublishDate = DateTime.ParseExact(dateAddedMatch.Value, "yyyy-MM-dd hh:mm tt", CultureInfo.InvariantCulture);
                }

                if (row.QuerySelector("img[title^=\"Free Torrent\"], img[title^=\"Sitewide Free Torrent\"]") != null)
                {
                    release.DownloadVolumeFactor = 0;
                }
                else if (row.QuerySelector("img[title^=\"Silver Torrent\"]") != null)
                {
                    release.DownloadVolumeFactor = 0.5;
                }
                else
                {
                    release.DownloadVolumeFactor = 1;
                }

                releaseInfos.Add(release);
            }

            return releaseInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
