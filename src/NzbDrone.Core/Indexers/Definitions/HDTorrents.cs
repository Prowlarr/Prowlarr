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
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class HDTorrents : TorrentIndexerBase<UserPassTorrentBaseSettings>
    {
        public override string Name => "HD-Torrents";

        public override string[] IndexerUrls => new[]
        {
            "https://hdts.ru/",
            "https://hd-torrents.org/",
            "https://hd-torrents.net/",
            "https://hd-torrents.me/",
        };
        public override string Description => "HD-Torrents is a private torrent website with HD torrents and strict rules on their content.";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();
        private string LoginUrl => Settings.BaseUrl + "login.php";

        public HDTorrents(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new HDTorrentsRequestGenerator { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new HDTorrentsParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true,
                Method = HttpMethod.Post
            };

            requestBuilder.PostProcess += r => r.RequestTimeout = TimeSpan.FromSeconds(15);

            var cookies = Cookies;

            Cookies = null;
            var authLoginRequest = requestBuilder
                .AddFormParameter("uid", Settings.Username)
                .AddFormParameter("pwd", Settings.Password)
                .SetHeader("Content-Type", "application/x-www-form-urlencoded")
                .SetHeader("Referer", LoginUrl)
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            cookies = response.GetCookies();
            UpdateCookies(cookies, DateTime.Now + TimeSpan.FromDays(30));

            _logger.Debug("HDTorrents authentication succeeded.");
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            return httpResponse.Content.Contains("Error:You're not authorized");
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
                Flags = new List<IndexerFlag>
                {
                    IndexerFlag.Internal
                }
            };

            caps.Categories.AddCategoryMapping("70", NewznabStandardCategory.MoviesBluRay, "Movie/UHD/Blu-Ray");
            caps.Categories.AddCategoryMapping("1", NewznabStandardCategory.MoviesBluRay, "Movie/Blu-Ray");
            caps.Categories.AddCategoryMapping("71", NewznabStandardCategory.MoviesUHD, "Movie/UHD/Remux");
            caps.Categories.AddCategoryMapping("2", NewznabStandardCategory.MoviesHD, "Movie/Remux");
            caps.Categories.AddCategoryMapping("5", NewznabStandardCategory.MoviesHD, "Movie/1080p/i");
            caps.Categories.AddCategoryMapping("3", NewznabStandardCategory.MoviesHD, "Movie/720p");
            caps.Categories.AddCategoryMapping("64", NewznabStandardCategory.MoviesUHD, "Movie/2160p");
            caps.Categories.AddCategoryMapping("63", NewznabStandardCategory.Audio, "Movie/Audio Track");

            // TV Show
            caps.Categories.AddCategoryMapping("72", NewznabStandardCategory.TVUHD, "TV Show/UHD/Blu-ray");
            caps.Categories.AddCategoryMapping("59", NewznabStandardCategory.TVHD, "TV Show/Blu-ray");
            caps.Categories.AddCategoryMapping("73", NewznabStandardCategory.TVUHD, "TV Show/UHD/Remux");
            caps.Categories.AddCategoryMapping("60", NewznabStandardCategory.TVHD, "TV Show/Remux");
            caps.Categories.AddCategoryMapping("30", NewznabStandardCategory.TVHD, "TV Show/1080p/i");
            caps.Categories.AddCategoryMapping("38", NewznabStandardCategory.TVHD, "TV Show/720p");
            caps.Categories.AddCategoryMapping("65", NewznabStandardCategory.TVUHD, "TV Show/2160p");

            // Music
            caps.Categories.AddCategoryMapping("44", NewznabStandardCategory.Audio, "Music/Album");
            caps.Categories.AddCategoryMapping("61", NewznabStandardCategory.AudioVideo, "Music/Blu-Ray");
            caps.Categories.AddCategoryMapping("62", NewznabStandardCategory.AudioVideo, "Music/Remux");
            caps.Categories.AddCategoryMapping("57", NewznabStandardCategory.AudioVideo, "Music/1080p/i");
            caps.Categories.AddCategoryMapping("45", NewznabStandardCategory.AudioVideo, "Music/720p");
            caps.Categories.AddCategoryMapping("66", NewznabStandardCategory.AudioVideo, "Music/2160p");

            // XXX
            caps.Categories.AddCategoryMapping("58", NewznabStandardCategory.XXX, "XXX/Blu-ray");
            caps.Categories.AddCategoryMapping("74", NewznabStandardCategory.XXX, "XXX/UHD/Blu-ray");
            caps.Categories.AddCategoryMapping("48", NewznabStandardCategory.XXX, "XXX/1080p/i");
            caps.Categories.AddCategoryMapping("47", NewznabStandardCategory.XXX, "XXX/720p");
            caps.Categories.AddCategoryMapping("67", NewznabStandardCategory.XXX, "XXX/2160p");

            return caps;
        }
    }

    public class HDTorrentsRequestGenerator : IIndexerRequestGenerator
    {
        public UserPassTorrentBaseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var searchUrl = Settings.BaseUrl + "torrents.php?" + string.Join(string.Empty, Capabilities.Categories.MapTorznabCapsToTrackers(categories).Select(cat => $"category[]={cat}&"));

            var search = new[] { imdbId, term };

            var queryCollection = new NameValueCollection
            {
                { "search", string.Join(" ", search.Where(s => !string.IsNullOrEmpty(s))) },
                { "active", "0" },
                { "options", "0" }
            };

            // manually url encode parenthesis to prevent "hacking" detection
            searchUrl += queryCollection.GetQueryString().Replace("(", "%28").Replace(")", "%29").Replace(".", " ");

            var request = new IndexerRequest(searchUrl, HttpAccept.Rss);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SearchTerm), searchCriteria.Categories, searchCriteria.FullImdbId));

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

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

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

    public class HDTorrentsParser : IParseIndexerResponse
    {
        private readonly UserPassTorrentBaseSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        private readonly Regex _posterRegex = new Regex(@"src=\\'./([^']+)\\'", RegexOptions.IgnoreCase);
        private readonly HashSet<string> _freeleechRanks = new (StringComparer.OrdinalIgnoreCase)
        {
            "VIP",
            "Uploader",
            "HD Internal",
            "Moderator",
            "Administrator",
            "Owner"
        };

        public HDTorrentsParser(UserPassTorrentBaseSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);

            var userInfo = dom.QuerySelector("table.navus tr");
            var userRank = userInfo?.Children[1].TextContent.Replace("Rank:", string.Empty).Trim();
            var hasFreeleech = _freeleechRanks.Contains(userRank);

            var rows = dom.QuerySelectorAll("table.mainblockcontenttt tr:has(td.mainblockcontent)");
            foreach (var row in rows.Skip(1))
            {
                var mainLink = row.Children[2].QuerySelector("a");
                var title = mainLink.TextContent;
                var details = new Uri(_settings.BaseUrl + mainLink.GetAttribute("href"));

                var posterMatch = _posterRegex.Match(mainLink.GetAttribute("onmouseover"));
                var poster = posterMatch.Success
                    ? _settings.BaseUrl + posterMatch.Groups[1].Value.Replace("\\", "/")
                    : null;

                var link = new Uri(_settings.BaseUrl + row.Children[4].FirstElementChild.GetAttribute("href"));
                var description = row.Children[2].QuerySelector("span").TextContent;
                var size = ParseUtil.GetBytes(row.Children[7].TextContent);

                var dateTag = row.Children[6].FirstElementChild;
                var dateString = string.Join(" ", dateTag.Attributes.Select(attr => attr.Name));
                var publishDate = DateTime.ParseExact(dateString, "dd MMM yyyy HH:mm:ss zz00", CultureInfo.InvariantCulture).ToLocalTime();

                var catStr = row.FirstElementChild.FirstElementChild.GetAttribute("href").Split('=')[1];
                var cat = _categories.MapTrackerCatToNewznab(catStr);

                // Sometimes the uploader column is missing, so seeders, leechers, and grabs may be at a different index.
                // There's room for improvement, but this works for now.
                var endIndex = row.Children.Length;

                if (row.Children[endIndex - 1].TextContent == "Edit")
                {
                    //Maybe use row.Children.Index(Node) after searching for an element instead?
                    endIndex -= 1;
                }
                else if (row.Children[endIndex - 4].TextContent == "Edit")
                {
                    // moderators get additional delete, recommend and like links
                    endIndex -= 4;
                }

                int? seeders = null;
                int? peers = null;
                if (ParseUtil.TryCoerceInt(row.Children[endIndex - 3].TextContent, out var rSeeders))
                {
                    seeders = rSeeders;
                    if (ParseUtil.TryCoerceInt(row.Children[endIndex - 2].TextContent, out var rLeechers))
                    {
                        peers = rLeechers + rSeeders;
                    }
                }

                var grabs = ParseUtil.TryCoerceInt(row.Children[endIndex - 1].TextContent, out var rGrabs)
                    ? (int?)rGrabs
                    : null;

                var dlVolumeFactor = 1.0;
                var upVolumeFactor = 1.0;
                if (row.QuerySelector("img[src$=\"no_ratio.png\"]") != null)
                {
                    dlVolumeFactor = 0;
                    upVolumeFactor = 0;
                }
                else if (hasFreeleech || row.QuerySelector("img[src$=\"free.png\"]") != null)
                {
                    dlVolumeFactor = 0;
                }
                else if (row.QuerySelector("img[src$=\"50.png\"]") != null)
                {
                    dlVolumeFactor = 0.5;
                }
                else if (row.QuerySelector("img[src$=\"25.png\"]") != null)
                {
                    dlVolumeFactor = 0.75;
                }
                else if (row.QuerySelector("img[src$=\"75.png\"]") != null)
                {
                    dlVolumeFactor = 0.25;
                }

                var imdbLink = row.QuerySelector("a[href*=\"www.imdb.com/title/\"]")?.GetAttribute("href");
                var imdb = !string.IsNullOrWhiteSpace(imdbLink) ? ParseUtil.GetImdbID(imdbLink) : null;

                var flags = new HashSet<IndexerFlag>();

                if (row.QuerySelector("img[src$=\"internal.png\"]") != null)
                {
                    flags.Add(IndexerFlag.Internal);
                }

                var release = new TorrentInfo
                {
                    Title = title,
                    Guid = details.AbsoluteUri,
                    DownloadUrl = link.AbsoluteUri,
                    InfoUrl = details.AbsoluteUri,
                    PosterUrl = poster,
                    PublishDate = publishDate,
                    Categories = cat,
                    ImdbId = imdb ?? 0,
                    Size = size,
                    Grabs = grabs,
                    Seeders = seeders,
                    Peers = peers,
                    IndexerFlags = flags,
                    DownloadVolumeFactor = dlVolumeFactor,
                    UploadVolumeFactor = upVolumeFactor,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800 // 48 hours
                };

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
