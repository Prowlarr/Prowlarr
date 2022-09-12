using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
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
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class Nebulance : TorrentIndexerBase<NebulanceSettings>
    {
        public override string Name => "Nebulance";
        private string LoginUrl => Settings.BaseUrl + "login.php";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public Nebulance(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new NebulanceRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new NebulanceParser(Settings, Capabilities.Categories);
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
                .AddFormParameter("twofa", Settings.TwoFactorAuth)
                .AddFormParameter("keeplogged", "on")
                .AddFormParameter("login", "Login")
                .SetHeader("Content-Type", "multipart/form-data")
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            cookies = response.GetCookies();
            UpdateCookies(cookies, DateTime.Now + TimeSpan.FromDays(30));

            _logger.Debug("Nebulance authentication succeeded.");
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

    public class NebulanceRequestGenerator : IIndexerRequestGenerator
    {
        public NebulanceSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public NebulanceRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
        {
            var searchUrl = string.Format("{0}/torrents.php", Settings.BaseUrl.TrimEnd('/'));

            var searchTerm = term;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = Regex.Replace(searchTerm, @"[-._]", " ");
            }

            var qc = new NameValueCollection
            {
                { "action", "basic" },
                { "order_by", "time" },
                { "order_way", "desc" },
                { "searchtext", searchTerm }
            };

            searchUrl = searchUrl + "?" + qc.GetQueryString();

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories));

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

    public class NebulanceParser : IParseIndexerResponse
    {
        private readonly NebulanceSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public NebulanceParser(NebulanceSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var document = parser.ParseDocument(indexerResponse.Content);
            var rows = document.QuerySelectorAll(".torrent_table > tbody > tr[class^='torrent row']");

            foreach (var row in rows)
            {
                var title = row.QuerySelector("a[data-src]").GetAttribute("data-src");
                if (string.IsNullOrEmpty(title) || title == "0")
                {
                    title = row.QuerySelector("a[data-src]").TextContent;
                    title = Regex.Replace(title, @"[\[\]\/]", "");
                }
                else
                {
                    if (title.Length > 5 && title.Substring(title.Length - 5).Contains("."))
                    {
                        title = title.Remove(title.LastIndexOf(".", StringComparison.Ordinal));
                    }
                }

                var posterStr = row.QuerySelector("img")?.GetAttribute("src");
                Uri.TryCreate(posterStr, UriKind.Absolute, out var poster);

                var details = _settings.BaseUrl + row.QuerySelector("a[data-src]").GetAttribute("href");
                var link = _settings.BaseUrl + row.QuerySelector("a[href*='action=download']").GetAttribute("href");

                var qColSize = row.QuerySelector("td:nth-child(3)");
                var size = ParseUtil.GetBytes(qColSize.Children[0].TextContent);
                var files = ParseUtil.CoerceInt(qColSize.Children[1].TextContent.Split(':')[1].Trim());

                var qPublishdate = row.QuerySelector("td:nth-child(4) span");
                var publishDateStr = qPublishdate.GetAttribute("title");
                var publishDate = !string.IsNullOrEmpty(publishDateStr) && publishDateStr.Contains(",")
                    ? DateTime.ParseExact(publishDateStr, "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture)
                    : DateTime.ParseExact(qPublishdate.TextContent.Trim(), "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture);

                var grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(5)").TextContent);
                var seeds = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(6)").TextContent);
                var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(7)").TextContent);

                var release = new TorrentInfo
                {
                    Title = title,
                    Guid = details,
                    InfoUrl = details,
                    PosterUrl = poster?.AbsoluteUri ?? null,
                    DownloadUrl = link,
                    Categories = new List<IndexerCategory> { TvCategoryFromQualityParser.ParseTvShowQuality(title) },
                    Size = size,
                    Files = files,
                    PublishDate = publishDate,
                    Grabs = grabs,
                    Seeders = seeds,
                    Peers = seeds + leechers,
                    MinimumRatio = 0, // ratioless
                    MinimumSeedTime = 86400, // 24 hours
                    DownloadVolumeFactor = 0, // ratioless tracker
                    UploadVolumeFactor = 1
                };

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class NebulanceSettings : UserPassTorrentBaseSettings
    {
        public NebulanceSettings()
        {
            TwoFactorAuth = "";
        }

        [FieldDefinition(4, Label = "Two Factor Auth", HelpText = "Two-Factor Auth")]
        public string TwoFactorAuth { get; set; }
    }
}
