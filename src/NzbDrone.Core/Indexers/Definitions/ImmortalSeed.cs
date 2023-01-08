using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Http;
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
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();
        private string LoginUrl => Settings.BaseUrl + "takelogin.php";

        public ImmortalSeed(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new ImmortalSeedRequestGenerator { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new ImmortalSeedParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true,
                AllowAutoRedirect = true
            };

            requestBuilder.Method = HttpMethod.Post;
            requestBuilder.PostProcess += r => r.RequestTimeout = TimeSpan.FromSeconds(15);

            var cookies = Cookies;

            Cookies = null;
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

            cookies = response.GetCookies();
            UpdateCookies(cookies, DateTime.Now + TimeSpan.FromDays(30));

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
        public UserPassTorrentBaseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        private IEnumerable<IndexerRequest> GetPagedRequests(SearchCriteriaBase searchCriteria)
        {
            var parameters = new NameValueCollection();

            var term = searchCriteria.SanitizedSearchTerm;

            if (term.IsNotNullOrWhiteSpace())
            {
                parameters.Add("do", "search");
                parameters.Add("keywords", term.Trim());
                parameters.Add("search_type", "t_name");
                parameters.Add("category", "0");
                parameters.Add("include_dead_torrents", "no");
            }

            var queryCats = Capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories);

            if (queryCats.Count > 0)
            {
                parameters.Add("selectedcats2", string.Join(",", queryCats));
            }

            var searchUrl = Settings.BaseUrl + "browse.php";

            if (parameters.Count > 0)
            {
                searchUrl += $"?{parameters.GetQueryString()}";
            }

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetPagedRequests(searchCriteria));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetPagedRequests(searchCriteria));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetPagedRequests(searchCriteria));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetPagedRequests(searchCriteria));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetPagedRequests(searchCriteria));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class ImmortalSeedParser : IParseIndexerResponse
    {
        private readonly UserPassTorrentBaseSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public ImmortalSeedParser(UserPassTorrentBaseSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);

            var rows = dom.QuerySelectorAll("#sortabletable tr:has(a[href*=\"details.php?id=\"])");
            foreach (var row in rows)
            {
                var release = new TorrentInfo();

                var qDetails = row.QuerySelector("div > a[href*=\"details.php?id=\"]"); // details link, release name get's shortened if it's to long

                // use Title from tooltip or fallback to Details link if there's no tooltip
                var qTitle = row.QuerySelector(".tooltip-content > div:nth-of-type(1)") ?? qDetails;
                release.Title = qTitle.TextContent;

                var qDesciption = row.QuerySelectorAll(".tooltip-content > div");
                if (qDesciption.Any())
                {
                    release.Description = qDesciption[1].TextContent.Trim();
                }

                var qLink = row.QuerySelector("a[href*=\"download.php\"]");
                release.DownloadUrl = qLink.GetAttribute("href");
                release.Guid = release.DownloadUrl;
                release.InfoUrl = qDetails.GetAttribute("href");

                // 2021-03-17 03:39 AM
                var dateString = row.QuerySelectorAll("td:nth-of-type(2) div").Last().LastChild.TextContent.Trim();
                release.PublishDate = DateTime.ParseExact(dateString, "yyyy-MM-dd hh:mm tt", CultureInfo.InvariantCulture);

                var sizeStr = row.QuerySelector("td:nth-of-type(5)").TextContent.Trim();
                release.Size = ParseUtil.GetBytes(sizeStr);

                release.Seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(7)").TextContent.Trim());
                release.Peers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(8)").TextContent.Trim()) + release.Seeders;

                var catLink = row.QuerySelector("td:nth-of-type(1) a").GetAttribute("href");
                var catSplit = catLink.IndexOf("category=");
                if (catSplit > -1)
                {
                    catLink = catLink.Substring(catSplit + 9);
                }

                release.Categories = _categories.MapTrackerCatToNewznab(catLink);

                var grabs = row.QuerySelector("td:nth-child(6)").TextContent;
                release.Grabs = ParseUtil.CoerceInt(grabs);

                if (row.QuerySelector("img[title^=\"Free Torrent\"]") != null)
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

                if (row.QuerySelector("img[title^=\"x2 Torrent\"]") != null)
                {
                    release.UploadVolumeFactor = 2;
                }
                else
                {
                    release.UploadVolumeFactor = 1;
                }

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
