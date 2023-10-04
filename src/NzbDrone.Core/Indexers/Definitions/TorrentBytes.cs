using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net.Http;
using System.Text;
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
    public class TorrentBytes : TorrentIndexerBase<UserPassTorrentBaseSettings>
    {
        public override string Name => "Torrent Bytes";
        public override string[] IndexerUrls => new[] { "https://www.torrentbytes.net/" };
        private string LoginUrl => Settings.BaseUrl + "takelogin.php";
        public override string Description => "A decade of TorrentBytes";
        public override string Language => "en-US";
        public override Encoding Encoding => Encoding.GetEncoding("iso-8859-1");
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public TorrentBytes(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new TorrentBytesRequestGenerator { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new TorrentBytesParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true,
                AllowAutoRedirect = true,
                Method = HttpMethod.Post
            };

            var cookies = Cookies;
            Cookies = null;

            var authLoginRequest = requestBuilder
                .AddFormParameter("username", Settings.Username)
                .AddFormParameter("password", Settings.Password)
                .AddFormParameter("returnto", "/")
                .AddFormParameter("login", "Log in!")
                .SetHeader("Content-Type", "application/x-www-form-urlencoded")
                .SetHeader("Referer", LoginUrl)
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            if (CheckIfLoginNeeded(response))
            {
                var parser = new HtmlParser();
                using var dom = parser.ParseDocument(response.Content);
                var errorMessage = dom.QuerySelector("td.embedded")?.TextContent.Trim();

                throw new IndexerAuthException(errorMessage ?? "Unknown error message, please report.");
            }

            cookies = response.GetCookies();
            UpdateCookies(cookies, DateTime.Now.AddDays(30));

            _logger.Debug("TorrentBytes authentication succeeded.");
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            return !httpResponse.Content.Contains("my.php");
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
                }
            };

            caps.Categories.AddCategoryMapping(23, NewznabStandardCategory.TVAnime, "Anime");
            caps.Categories.AddCategoryMapping(52, NewznabStandardCategory.PCMac, "Apple/All");
            caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.PC, "Apps/misc");
            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.PC, "Apps/PC");
            caps.Categories.AddCategoryMapping(28, NewznabStandardCategory.TVForeign, "Foreign Titles");
            caps.Categories.AddCategoryMapping(50, NewznabStandardCategory.Console, "Games/Consoles");
            caps.Categories.AddCategoryMapping(42, NewznabStandardCategory.PCGames, "Games/Pack");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.PCGames, "Games/PC");
            caps.Categories.AddCategoryMapping(51, NewznabStandardCategory.PC, "Linux/All");
            caps.Categories.AddCategoryMapping(31, NewznabStandardCategory.OtherMisc, "Misc");
            caps.Categories.AddCategoryMapping(20, NewznabStandardCategory.MoviesDVD, "Movies/DVD-R");
            caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.MoviesBluRay, "Movies/Full Blu-ray");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.MoviesHD, "Movies/HD");
            caps.Categories.AddCategoryMapping(40, NewznabStandardCategory.Movies, "Movies/Pack");
            caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.MoviesSD, "Movies/SD");
            caps.Categories.AddCategoryMapping(49, NewznabStandardCategory.MoviesUHD, "Movies/UHD");
            caps.Categories.AddCategoryMapping(25, NewznabStandardCategory.Audio, "Music/DVDR");
            caps.Categories.AddCategoryMapping(48, NewznabStandardCategory.AudioLossless, "Music/Flac");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.AudioMP3, "Music/MP3");
            caps.Categories.AddCategoryMapping(43, NewznabStandardCategory.Audio, "Music/Pack");
            caps.Categories.AddCategoryMapping(34, NewznabStandardCategory.AudioVideo, "Music/Videos");
            caps.Categories.AddCategoryMapping(45, NewznabStandardCategory.MoviesBluRay, "NonScene/BRrip");
            caps.Categories.AddCategoryMapping(46, NewznabStandardCategory.MoviesHD, "NonScene/x264");
            caps.Categories.AddCategoryMapping(44, NewznabStandardCategory.MoviesSD, "NonScene/Xvid");
            caps.Categories.AddCategoryMapping(37, NewznabStandardCategory.TVHD, "TV/BRrip");
            caps.Categories.AddCategoryMapping(38, NewznabStandardCategory.TVHD, "TV/HD");
            caps.Categories.AddCategoryMapping(41, NewznabStandardCategory.TV, "TV/Pack");
            caps.Categories.AddCategoryMapping(33, NewznabStandardCategory.TVSD, "TV/SD");
            caps.Categories.AddCategoryMapping(32, NewznabStandardCategory.TVUHD, "TV/UHD");
            caps.Categories.AddCategoryMapping(39, NewznabStandardCategory.XXXx264, "XXX/HD");
            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.XXXImageSet, "XXX/IMGSET");
            caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.XXXPack, "XXX/Pack");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.XXXXviD, "XXX/SD");
            caps.Categories.AddCategoryMapping(29, NewznabStandardCategory.XXX, "XXX/Web");

            return caps;
        }
    }

    public class TorrentBytesRequestGenerator : IIndexerRequestGenerator
    {
        public UserPassTorrentBaseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var searchUrl = string.Format("{0}/browse.php", Settings.BaseUrl.TrimEnd('/'));

            var qc = new NameValueCollection
            {
                { "incldead", "1" }
            };

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                qc.Add("search", imdbId);
                qc.Add("sc", "2"); // search in description
            }
            else
            {
                qc.Add("search", term);
                qc.Add("sc", "1"); // search in title
            }

            foreach (var cat in Capabilities.Categories.MapTorznabCapsToTrackers(categories))
            {
                qc.Add("c" + cat, "1");
            }

            searchUrl = searchUrl + "?" + qc.GetQueryString();

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories, searchCriteria.FullImdbId));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories, searchCriteria.FullImdbId));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class TorrentBytesParser : IParseIndexerResponse
    {
        private readonly UserPassTorrentBaseSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public TorrentBytesParser(UserPassTorrentBaseSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            using var doc = parser.ParseDocument(indexerResponse.Content);
            var rows = doc.QuerySelectorAll("table > tbody:has(tr > td.colhead) > tr:not(:has(td.colhead))");
            foreach (var row in rows)
            {
                var release = new TorrentInfo();
                var link = row.QuerySelector("td:nth-of-type(2) a:nth-of-type(2)");
                release.Guid = _settings.BaseUrl + link.GetAttribute("href");
                release.InfoUrl = release.Guid;
                release.Title = link.GetAttribute("title");

                // There isn't a title attribute if the release name isn't truncated.
                if (string.IsNullOrWhiteSpace(release.Title))
                {
                    release.Title = link.FirstChild.TextContent.Trim();
                }

                release.Description = release.Title;

                // If we search an get no results, we still get a table just with no info.
                if (string.IsNullOrWhiteSpace(release.Title))
                {
                    break;
                }

                // Check if the release has been assigned a category
                var qCat = row.QuerySelector("td:nth-of-type(1) a");
                if (qCat != null)
                {
                    var cat = qCat.GetAttribute("href").Substring(15);
                    release.Categories = _categories.MapTrackerCatToNewznab(cat);
                }

                var qLink = row.QuerySelector("td:nth-of-type(2) a");
                release.DownloadUrl = _settings.BaseUrl + qLink.GetAttribute("href");

                var added = row.QuerySelector("td:nth-of-type(5)").TextContent.Trim();
                release.PublishDate = DateTime.ParseExact(added, "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture);

                var sizeStr = row.QuerySelector("td:nth-of-type(7)").TextContent.Trim();
                release.Size = ParseUtil.GetBytes(sizeStr);
                release.Seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(9)").TextContent.Trim());
                release.Peers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(10)").TextContent.Trim()) +
                                release.Seeders;

                var files = row.QuerySelector("td:nth-child(3)").TextContent;
                release.Files = ParseUtil.CoerceInt(files);

                var grabs = row.QuerySelector("td:nth-child(8)").TextContent;
                if (grabs != "----")
                {
                    release.Grabs = ParseUtil.CoerceInt(grabs);
                }

                release.DownloadVolumeFactor = row.QuerySelector("font[color=\"green\"]:contains(\"F\"):contains(\"L\")") != null ? 0 : 1;
                release.UploadVolumeFactor = 1;

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
