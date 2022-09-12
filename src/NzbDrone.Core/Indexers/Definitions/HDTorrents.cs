using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using FluentValidation;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class HDTorrents : TorrentIndexerBase<UserPassTorrentBaseSettings>
    {
        public override string Name => "HD-Torrents";
        private string LoginUrl => Settings.BaseUrl + "login.php";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public HDTorrents(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new HDTorrentsRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new HDTorrentsParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true
            };

            requestBuilder.Method = HttpMethod.Post;
            requestBuilder.PostProcess += r => r.RequestTimeout = TimeSpan.FromSeconds(15);

            var cookies = Cookies;

            Cookies = null;
            var authLoginRequest = requestBuilder
                .AddFormParameter("uid", Settings.Username)
                .AddFormParameter("pwd", Settings.Password)
                .SetHeader("Content-Type", "multipart/form-data")
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            cookies = response.GetCookies();
            UpdateCookies(cookies, DateTime.Now + TimeSpan.FromDays(30));

            _logger.Debug("HDTorrents authentication succeeded.");
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            if (httpResponse.Content.Contains("Error:You're not authorized"))
            {
                return true;
            }

            return false;
        }
    }

    public class HDTorrentsRequestGenerator : IIndexerRequestGenerator
    {
        public UserPassTorrentBaseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public HDTorrentsRequestGenerator()
        {
        }

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
        private readonly HashSet<string> _freeleechRanks = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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
            var userRank = userInfo.Children[1].TextContent.Replace("Rank:", string.Empty).Trim();
            var hasFreeleech = _freeleechRanks.Contains(userRank);

            var rows = dom.QuerySelectorAll("table.mainblockcontenttt tr:has(td.mainblockcontent)");
            foreach (var row in rows.Skip(1))
            {
                var mainLink = row.Children[2].QuerySelector("a");
                var title = mainLink.TextContent;
                var details = new Uri(_settings.BaseUrl + mainLink.GetAttribute("href"));

                var posterMatch = _posterRegex.Match(mainLink.GetAttribute("onmouseover"));
                var poster = posterMatch.Success ? _settings.BaseUrl + posterMatch.Groups[1].Value.Replace("\\", "/") : null;

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
