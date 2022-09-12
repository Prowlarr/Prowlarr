using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using FluentValidation;
using Newtonsoft.Json.Linq;
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
    public class ZonaQ : TorrentIndexerBase<UserPassTorrentBaseSettings>
    {
        public override string Name => "ZonaQ";
        private string Login1Url => Settings.BaseUrl + "index.php";
        private string Login2Url => Settings.BaseUrl + "paDentro.php";
        private string Login3Url => Settings.BaseUrl + "retorno/include/puerta_8_ajax.php";
        private string Login4Url => Settings.BaseUrl + "retorno/index.php";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public ZonaQ(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new ZonaQRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new ZonaQParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            _logger.Debug("ZonaQ authentication succeeded.");

            // The first page set the cookies and the session_id
            var loginPage = await ExecuteAuth(new HttpRequest(Login1Url));
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(loginPage.Content);
            var sessionId = dom.QuerySelector("input#session_id")?.GetAttribute("value");
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new IndexerAuthException("Error getting the ZonaQ Session ID");
            }

            // The second page send the login with the hash
            // The hash is reverse engineering from https://www.zonaq.pw/retorno/2/smf/Themes/smf_ZQ/scripts/script.js
            // doForm.hash_passwrd.value = hex_sha1(hex_sha1(doForm.user.value.php_to8bit().php_strtolower() + doForm.passwrd.value.php_to8bit()) + cur_session_id);
            Thread.Sleep(3000);
            var hashPassword = Sha1Hash(Sha1Hash(Settings.Username.ToLower() + Settings.Password) + sessionId);

            var requestBuilder = new HttpRequestBuilder(Login2Url)
            {
                LogResponseContent = true
            };

            requestBuilder.Method = HttpMethod.Post;
            requestBuilder.PostProcess += r => r.RequestTimeout = TimeSpan.FromSeconds(15);
            requestBuilder.SetCookies(loginPage.GetCookies());

            var authLoginRequest = requestBuilder
                .AddFormParameter("user", Settings.Username)
                .AddFormParameter("passwrd", Settings.Password)
                .AddFormParameter("hash_passwrd", hashPassword)
                .SetHeader("X-Requested-With", "XMLHttpRequest")
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            var message = JObject.Parse(response.Content)["msg"]?.ToString();
            if (message == "puerta_2")
            {
                // The third page sets the cookie duration
                Thread.Sleep(3000);
                var requestBuilder2 = new HttpRequestBuilder(Login3Url)
                {
                    LogResponseContent = true
                };

                requestBuilder2.Method = HttpMethod.Post;
                requestBuilder2.PostProcess += r => r.RequestTimeout = TimeSpan.FromSeconds(15);
                requestBuilder2.SetCookies(response.GetCookies());

                var authLoginRequest2 = requestBuilder2
                    .AddFormParameter("passwd", "")
                    .AddFormParameter("cookielength", "43200")
                    .AddFormParameter("respuesta", "")
                    .SetHeader("X-Requested-With", "XMLHttpRequest")
                    .Build();

                response = await ExecuteAuth(authLoginRequest2);
                message = JObject.Parse(response.Content)["msg"]?.ToString();
            }

            if (message != "last_door")
            {
                throw new IndexerAuthException($"Login error: {message}");
            }

            // The forth page sets the last cookie
            Thread.Sleep(3000);
            var requestBuilder4 = new HttpRequestBuilder(Login4Url)
            {
                LogResponseContent = true
            };

            requestBuilder4.SetCookies(response.GetCookies());
            var authLoginRequest3 = requestBuilder4.Build();

            response = await ExecuteAuth(authLoginRequest3);

            UpdateCookies(response.GetCookies(), DateTime.Now + TimeSpan.FromDays(30));
        }

        private static string Sha1Hash(string input)
        {
            var hash = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(input));
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            if (httpResponse.Content == null || !httpResponse.Content.Contains("/index.php?action=logout;"))
            {
                return true;
            }

            return false;
        }
    }

    public class ZonaQRequestGenerator : IIndexerRequestGenerator
    {
        public UserPassTorrentBaseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public ZonaQRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
        {
            var searchUrl = string.Format("{0}/retorno/2/index.php", Settings.BaseUrl.TrimEnd('/'));

            var qc = new NameValueCollection
            {
                { "page", "torrents" },
                { "search", term },
                { "active", "0" }
            };

            searchUrl = searchUrl + "?" + qc.GetQueryString();

            // categories are already encoded
            foreach (var cat in Capabilities.Categories.MapTorznabCapsToTrackers(categories))
            {
                searchUrl += "&" + cat;
            }

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

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

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories));

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

    public class ZonaQParser : IParseIndexerResponse
    {
        private readonly UserPassTorrentBaseSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public ZonaQParser(UserPassTorrentBaseSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var doc = parser.ParseDocument(indexerResponse.Content);

            var rows = doc.QuerySelectorAll("table.torrent_list > tbody > tr");

            foreach (var row in rows.Skip(1))
            {
                var qTitleLink = row.QuerySelector("a[href*=\"?page=torrent-details\"]");

                // no results
                if (qTitleLink == null)
                {
                    continue;
                }

                var title = qTitleLink.TextContent.Trim();
                title += " SPANiSH"; // fix for Radarr
                title = Regex.Replace(title, "4k", "2160p", RegexOptions.IgnoreCase);

                var detailsStr = qTitleLink.GetAttribute("href");
                var details = new Uri(detailsStr);
                var link = new Uri(detailsStr.Replace("/index.php?page=torrent-details&", "/download.php?"));
                var qPoster = qTitleLink.GetAttribute("title");
                var poster = qPoster != null ? new Uri(qPoster) : null;

                var publishDateStr = row.Children[4].InnerHtml.Split('>').Last();
                var publishDate = DateTime.ParseExact(publishDateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var size = ParseUtil.GetBytes(row.Children[5].TextContent.Replace(".", "").Replace(",", "."));
                var seeders = ParseUtil.CoerceInt(row.Children[6].TextContent);
                var leechers = ParseUtil.CoerceInt(row.Children[7].TextContent);
                var grabs = ParseUtil.CoerceInt(row.Children[8].TextContent);

                var cat1 = row.Children[0].FirstElementChild.GetAttribute("href").Split('=').Last();
                var cat2 = row.Children[1].FirstElementChild.GetAttribute("href").Split('=').Last();
                var cat = _categories.MapTrackerCatToNewznab($"cat[]={cat1}&subcat[]={cat2}");

                var dlVolumeFactor = row.QuerySelector("img[src*=\"/gold.png\"]") != null ? 0 :
                    row.QuerySelector("img[src*=\"/silver.png\"]") != null ? 0.5 : 1;
                var ulVolumeFactor = row.QuerySelector("img[src*=\"/por3.gif\"]") != null ? 3 :
                    row.QuerySelector("img[src*=\"/por2.gif\"]") != null ? 2 : 1;

                var release = new TorrentInfo
                {
                    Title = title,
                    InfoUrl = details.AbsoluteUri,
                    Guid = details.AbsoluteUri,
                    DownloadUrl = link.AbsoluteUri,
                    PosterUrl = poster.AbsoluteUri,
                    PublishDate = publishDate,
                    Categories = cat,
                    Size = size,
                    Grabs = grabs,
                    Seeders = seeders,
                    Peers = seeders + leechers,
                    DownloadVolumeFactor = dlVolumeFactor,
                    UploadVolumeFactor = ulVolumeFactor,
                    MinimumRatio = 1,
                    MinimumSeedTime = 259200 // 72 hours
                };

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
