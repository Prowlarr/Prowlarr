using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using FluentValidation;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class ImmortalSeed : TorrentIndexerBase<UserPassTorrentBaseSettings>
    {
        public override string Name => "ImmortalSeed";
        private string LoginUrl => Settings.BaseUrl + "takelogin.php";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public ImmortalSeed(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new ImmortalSeedRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new ImmortalSeedParser(Settings, Capabilities.Categories);
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
                .AddFormParameter("username", Settings.Username)
                .AddFormParameter("password", Settings.Password)
                .SetHeader("Content-Type", "multipart/form-data")
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            if (!response.Content.Contains("You have successfully logged in"))
            {
                throw new IndexerAuthException("ImmortalSeed Auth Failed");
            }

            cookies = response.GetCookies();
            UpdateCookies(cookies, DateTime.Now + TimeSpan.FromDays(30));

            _logger.Debug("ImmortalSeed authentication succeeded.");
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            if (httpResponse.Content.Contains("You do not have permission to access this page."))
            {
                return true;
            }

            return false;
        }
    }

    public class ImmortalSeedRequestGenerator : IIndexerRequestGenerator
    {
        public UserPassTorrentBaseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public ImmortalSeedRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var searchUrl = Settings.BaseUrl + "browse.php?";

            if (term.IsNotNullOrWhiteSpace())
            {
                searchUrl += string.Format("do=search&keywords={0}&search_type=t_name&category=0&include_dead_torrents=no", WebUtility.UrlEncode(term));
            }

            if (categories != null && categories.Length > 0)
            {
                if (term.IsNotNullOrWhiteSpace())
                {
                     searchUrl += "&";
                }

                searchUrl += "selectedcats2=" + string.Join(",", Capabilities.Categories.MapTorznabCapsToTrackers(categories));
            }

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
