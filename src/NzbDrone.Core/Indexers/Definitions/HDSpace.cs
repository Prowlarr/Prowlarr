using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using FluentValidation;
using NLog;
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
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class HDSpace : TorrentIndexerBase<UserPassTorrentBaseSettings>
    {
        public override string Name => "HD-Space";
        private string LoginUrl => Settings.BaseUrl + "index.php?page=login";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public HDSpace(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new HDSpaceRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new HDSpaceParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            var loginPage = await ExecuteAuth(new HttpRequest(LoginUrl));

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
                .AddFormParameter("uid", Settings.Username)
                .AddFormParameter("pwd", Settings.Password)
                .SetCookies(loginPage.GetCookies())
                .SetHeader("Content-Type", "multipart/form-data")
                .SetHeader("Referer", LoginUrl)
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            if (CheckIfLoginNeeded(response))
            {
                var errorStr = "Login Failed: You have {0} remaining login attempts";
                var remainingAttemptSpan = new Regex(string.Format(errorStr, "(.*?)"))
                                           .Match(loginPage.Content).Groups[1].ToString();
                var attempts = Regex.Replace(remainingAttemptSpan, "<.*?>", string.Empty);
                var errorMessage = string.Format(errorStr, attempts);

                throw new IndexerAuthException(errorMessage);
            }

            cookies = response.GetCookies();
            UpdateCookies(cookies, DateTime.Now + TimeSpan.FromDays(30));

            _logger.Debug("HDSpace authentication succeeded.");
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            if (!httpResponse.Content.Contains("logout.php"))
            {
                return true;
            }

            return false;
        }
    }

    public class HDSpaceRequestGenerator : IIndexerRequestGenerator
    {
        public UserPassTorrentBaseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public HDSpaceRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdb = null)
        {
            var searchUrl = string.Format("{0}/index.php?page=torrents&", Settings.BaseUrl.TrimEnd('/'));

            var queryCollection = new NameValueCollection
            {
                { "active", "0" },
                { "category", string.Join(";", Capabilities.Categories.MapTorznabCapsToTrackers(categories)) }
            };

            if (imdb != null)
            {
                queryCollection.Add("options", "2");
                queryCollection.Add("search", imdb);
            }
            else
            {
                queryCollection.Add("options", "0");
                queryCollection.Add("search", term.Replace(".", " "));
            }

            searchUrl += queryCollection.GetQueryString();

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories, searchCriteria.ImdbId));

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

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories, searchCriteria.ImdbId));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
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

    public class HDSpaceParser : IParseIndexerResponse
    {
        private readonly UserPassTorrentBaseSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public HDSpaceParser(UserPassTorrentBaseSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var resultParser = new HtmlParser();
            var searchResultDocument = resultParser.ParseDocument(indexerResponse.Content);
            var rows = searchResultDocument.QuerySelectorAll("table.lista > tbody > tr");

            foreach (var row in rows)
            {
                // this tracker has horrible markup, find the result rows by looking for the style tag before each one
                var prev = row.PreviousElementSibling;
                if (prev == null || !string.Equals(prev.NodeName, "style", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var release = new TorrentInfo();
                release.MinimumRatio = 1;
                release.MinimumSeedTime = 86400; // 24 hours

                var qLink = row.Children[1].FirstElementChild;
                release.Title = qLink.TextContent.Trim();
                release.InfoUrl = _settings.BaseUrl + qLink.GetAttribute("href");
                release.Guid = release.InfoUrl;

                var imdbLink = row.Children[1].QuerySelector("a[href*=imdb]");
                if (imdbLink != null)
                {
                    release.ImdbId = ParseUtil.GetImdbID(imdbLink.GetAttribute("href").Split('/').Last()).GetValueOrDefault();
                }

                var qDownload = row.Children[3].FirstElementChild;
                release.DownloadUrl = _settings.BaseUrl + qDownload.GetAttribute("href");

                var dateStr = row.Children[4].TextContent.Trim();

                //"July 11, 2015, 13:34:09", "Today|Yesterday at 20:04:23"
                release.PublishDate = DateTimeUtil.FromUnknown(dateStr);
                var sizeStr = row.Children[5].TextContent;
                release.Size = ParseUtil.GetBytes(sizeStr);
                release.Seeders = ParseUtil.CoerceInt(row.Children[7].TextContent);
                release.Peers = ParseUtil.CoerceInt(row.Children[8].TextContent) + release.Seeders;
                var grabs = row.QuerySelector("td:nth-child(10)").TextContent;
                grabs = grabs.Replace("---", "0");
                release.Grabs = ParseUtil.CoerceInt(grabs);

                if (row.QuerySelector("img[title=\"FreeLeech\"]") != null)
                {
                    release.DownloadVolumeFactor = 0;
                }
                else if (row.QuerySelector("img[src=\"images/sf.png\"]") != null)
                {
                    release.DownloadVolumeFactor = 0;
                }
                else if (row.QuerySelector("img[title=\"Half FreeLeech\"]") != null)
                {
                    release.DownloadVolumeFactor = 0.5;
                }
                else
                {
                    release.DownloadVolumeFactor = 1;
                }

                release.UploadVolumeFactor = 1;
                var qCat = row.QuerySelector("a[href^=\"index.php?page=torrents&category=\"]");
                var cat = qCat.GetAttribute("href").Split('=')[2];
                release.Categories = _categories.MapTrackerCatToNewznab(cat);
                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
