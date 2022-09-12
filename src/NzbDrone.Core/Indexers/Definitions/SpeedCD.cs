using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class SpeedCD : TorrentIndexerBase<UserPassTorrentBaseSettings>
    {
        public override string Name => "SpeedCD";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public SpeedCD(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new SpeedCDRequestGenerator() { Settings = Settings, Capabilities = Capabilities, Encoding = Encoding };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new SpeedCDParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            var requestBuilder = new HttpRequestBuilder(string.Format("{0}/{1}", Settings.BaseUrl.TrimEnd('/'), "checkpoint/API"))
            {
                Method = HttpMethod.Post,
                LogResponseContent = true,
                AllowAutoRedirect = true
            };

            var loginPage = await ExecuteAuth(requestBuilder.AddFormParameter("username", Settings.Username).Build());

            var tokenRegex = new Regex(@"name=\\""a\\"" value=\\""([^""]+)\\""");
            var matches = tokenRegex.Match(loginPage.Content);
            if (!matches.Success)
            {
                throw new IndexerAuthException("Error parsing the login form");
            }

            var token = matches.Groups[1].Value;

            Cookies = null;

            var requestBuilder2 = new HttpRequestBuilder(string.Format("{0}/{1}", Settings.BaseUrl.TrimEnd('/'), "checkpoint/"))
            {
                Method = HttpMethod.Post,
                LogResponseContent = true,
                AllowAutoRedirect = true
            };

            var authLoginRequest = requestBuilder2
                .AddFormParameter("pwd", Settings.Password)
                .AddFormParameter("a", token)
                .SetCookies(loginPage.GetCookies())
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            if (CheckIfLoginNeeded(response))
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.Content);
                var errorMessage = dom.QuerySelector("h5")?.TextContent;
                if (response.Content.Contains("Wrong Captcha!"))
                {
                    errorMessage = "Captcha required due to a failed login attempt. Login via a browser to whitelist your IP and then reconfigure Prowlarr.";
                }

                throw new IndexerAuthException(errorMessage);
            }

            var cookies = response.GetCookies();
            UpdateCookies(cookies, DateTime.Now + TimeSpan.FromDays(30));

            _logger.Debug("SpeedCD authentication succeeded.");
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            if (!httpResponse.Content.Contains("/browse.php"))
            {
                return true;
            }

            return false;
        }
    }

    public class SpeedCDRequestGenerator : IIndexerRequestGenerator
    {
        public UserPassTorrentBaseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }
        public Encoding Encoding { get; set; }

        public SpeedCDRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var searchUrl = string.Format("{0}/browse/", Settings.BaseUrl.TrimEnd('/'));

            var qc = new List<string>();

            var catList = Capabilities.Categories.MapTorznabCapsToTrackers(categories);
            foreach (var cat in catList)
            {
                qc.Add(cat);
            }

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                qc.Add("deep");
                qc.Add("q");
                qc.Add(imdbId);
            }
            else
            {
                qc.Add("q");
                qc.Add(term.UrlEncode(Encoding));
            }

            searchUrl += string.Join("/", qc);

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

    public class SpeedCDParser : IParseIndexerResponse
    {
        private readonly UserPassTorrentBaseSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public SpeedCDParser(UserPassTorrentBaseSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);
            var rows = dom.QuerySelectorAll("div.boxContent > table > tbody > tr");

            foreach (var row in rows)
            {
                var cells = row.QuerySelectorAll("td");

                var title = row.QuerySelector("td[class='lft'] > div > a").TextContent.Trim();
                var link = new Uri(_settings.BaseUrl + row.QuerySelector("img[title='Download']").ParentElement.GetAttribute("href").TrimStart('/'));
                var details = new Uri(_settings.BaseUrl + row.QuerySelector("td[class='lft'] > div > a").GetAttribute("href").TrimStart('/'));
                var size = ParseUtil.GetBytes(cells[5].TextContent);
                var grabs = ParseUtil.CoerceInt(cells[6].TextContent);
                var seeders = ParseUtil.CoerceInt(cells[7].TextContent);
                var leechers = ParseUtil.CoerceInt(cells[8].TextContent);

                var pubDateStr = row.QuerySelector("span[class^='elapsedDate']").GetAttribute("title").Replace(" at", "");
                var publishDate = DateTime.ParseExact(pubDateStr, "dddd, MMMM d, yyyy h:mmtt", CultureInfo.InvariantCulture);

                var cat = row.QuerySelector("a").GetAttribute("href").Split('/').Last();
                var downloadVolumeFactor = row.QuerySelector("span:contains(\"[Freeleech]\")") != null ? 0 : 1;

                var release = new TorrentInfo
                {
                    Title = title,
                    DownloadUrl = link.AbsoluteUri,
                    Guid = link.AbsoluteUri,
                    InfoUrl = details.AbsoluteUri,
                    PublishDate = publishDate,
                    Categories = _categories.MapTrackerCatToNewznab(cat),
                    Size = size,
                    Grabs = grabs,
                    Seeders = seeders,
                    Peers = seeders + leechers,
                    MinimumRatio = 1,
                    MinimumSeedTime = 259200, // 72 hours
                    DownloadVolumeFactor = downloadVolumeFactor,
                    UploadVolumeFactor = 1
                };

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
