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
using Newtonsoft.Json.Linq;
using NLog;
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
    public class ZonaQ : TorrentIndexerBase<UserPassTorrentBaseSettings>
    {
        public override string Name => "ZonaQ";
        public override string[] IndexerUrls => new[] { "https://www.zonaq.pw/" };
        private string Login1Url => Settings.BaseUrl + "index.php";
        private string Login2Url => Settings.BaseUrl + "paDentro.php";
        private string Login3Url => Settings.BaseUrl + "retorno/include/puerta_8_ajax.php";
        private string Login4Url => Settings.BaseUrl + "retorno/index.php";
        public override string Description => "ZonaQ is a SPANISH Private Torrent Tracker for MOVIES / TV";
        public override string Language => "es-ES";
        public override Encoding Encoding => Encoding.UTF8;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public ZonaQ(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new ZonaQRequestGenerator { Settings = Settings, Capabilities = Capabilities };
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
            using var dom = parser.ParseDocument(loginPage.Content);
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
                LogResponseContent = true,
                Method = HttpMethod.Post
            };

            var authLoginRequest = requestBuilder
                .SetCookies(loginPage.GetCookies())
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
                    LogResponseContent = true,
                    Method = HttpMethod.Post
                };

                var authLoginRequest2 = requestBuilder2
                    .SetCookies(response.GetCookies())
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

            var authLoginRequest3 = requestBuilder4
                .SetCookies(response.GetCookies())
                .Build();

            response = await ExecuteAuth(authLoginRequest3);

            UpdateCookies(response.GetCookies(), DateTime.Now.AddDays(30));
        }

        private static string Sha1Hash(string input)
        {
            var hash = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(input));
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            return httpResponse.Content == null || !httpResponse.Content.Contains("/index.php?action=logout;");
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
                }
            };

            caps.Categories.AddCategoryMapping("cat[]=1&subcat[]=1", NewznabStandardCategory.MoviesDVD, "Películas/DVD");
            caps.Categories.AddCategoryMapping("cat[]=1&subcat[]=2", NewznabStandardCategory.MoviesDVD, "Películas/BDVD + Autorías");
            caps.Categories.AddCategoryMapping("cat[]=1&subcat[]=3", NewznabStandardCategory.MoviesBluRay, "Películas/BD");
            caps.Categories.AddCategoryMapping("cat[]=1&subcat[]=4", NewznabStandardCategory.MoviesUHD, "Películas/BD 4K");
            caps.Categories.AddCategoryMapping("cat[]=1&subcat[]=5", NewznabStandardCategory.Movies3D, "Películas/BD 3D");
            caps.Categories.AddCategoryMapping("cat[]=1&subcat[]=6", NewznabStandardCategory.MoviesBluRay, "Películas/BD Remux");
            caps.Categories.AddCategoryMapping("cat[]=1&subcat[]=7", NewznabStandardCategory.MoviesHD, "Películas/MKV");
            caps.Categories.AddCategoryMapping("cat[]=1&subcat[]=8", NewznabStandardCategory.MoviesUHD, "Películas/MKV 4K");
            caps.Categories.AddCategoryMapping("cat[]=1&subcat[]=9", NewznabStandardCategory.MoviesUHD, "Películas/BD Remux 4K");

            caps.Categories.AddCategoryMapping("cat[]=2&subcat[]=1", NewznabStandardCategory.MoviesDVD, "Animación/DVD");
            caps.Categories.AddCategoryMapping("cat[]=2&subcat[]=2", NewznabStandardCategory.MoviesDVD, "Animación/BDVD + Autorías");
            caps.Categories.AddCategoryMapping("cat[]=2&subcat[]=3", NewznabStandardCategory.MoviesBluRay, "Animación/BD");
            caps.Categories.AddCategoryMapping("cat[]=2&subcat[]=4", NewznabStandardCategory.MoviesUHD, "Animación/BD 4K");
            caps.Categories.AddCategoryMapping("cat[]=2&subcat[]=5", NewznabStandardCategory.Movies3D, "Animación/BD 3D");
            caps.Categories.AddCategoryMapping("cat[]=2&subcat[]=6", NewznabStandardCategory.MoviesBluRay, "Animación/BD Remux");
            caps.Categories.AddCategoryMapping("cat[]=2&subcat[]=7", NewznabStandardCategory.MoviesHD, "Animación/MKV");
            caps.Categories.AddCategoryMapping("cat[]=2&subcat[]=8", NewznabStandardCategory.MoviesUHD, "Animación/MKV 4K");
            caps.Categories.AddCategoryMapping("cat[]=2&subcat[]=9", NewznabStandardCategory.MoviesUHD, "Animación/BD Remux 4K");

            caps.Categories.AddCategoryMapping("cat[]=3&subcat[]=1", NewznabStandardCategory.AudioVideo, "Música/DVD");
            caps.Categories.AddCategoryMapping("cat[]=3&subcat[]=2", NewznabStandardCategory.AudioVideo, "Música/BDVD + Autorías");
            caps.Categories.AddCategoryMapping("cat[]=3&subcat[]=3", NewznabStandardCategory.AudioVideo, "Música/BD");
            caps.Categories.AddCategoryMapping("cat[]=3&subcat[]=4", NewznabStandardCategory.AudioVideo, "Música/BD 4K");
            caps.Categories.AddCategoryMapping("cat[]=3&subcat[]=5", NewznabStandardCategory.AudioVideo, "Música/BD 3D");
            caps.Categories.AddCategoryMapping("cat[]=3&subcat[]=6", NewznabStandardCategory.AudioVideo, "Música/BD Remux");
            caps.Categories.AddCategoryMapping("cat[]=3&subcat[]=7", NewznabStandardCategory.AudioVideo, "Música/MKV");
            caps.Categories.AddCategoryMapping("cat[]=3&subcat[]=8", NewznabStandardCategory.AudioVideo, "Música/MKV 4K");
            caps.Categories.AddCategoryMapping("cat[]=3&subcat[]=9", NewznabStandardCategory.AudioVideo, "Música/BD Remux 4K");

            caps.Categories.AddCategoryMapping("cat[]=4&subcat[]=1", NewznabStandardCategory.TVSD, "Series/DVD");
            caps.Categories.AddCategoryMapping("cat[]=4&subcat[]=2", NewznabStandardCategory.TVSD, "Series/BDVD + Autorías");
            caps.Categories.AddCategoryMapping("cat[]=4&subcat[]=3", NewznabStandardCategory.TVHD, "Series/BD");
            caps.Categories.AddCategoryMapping("cat[]=4&subcat[]=4", NewznabStandardCategory.TVUHD, "Series/BD 4K");
            caps.Categories.AddCategoryMapping("cat[]=4&subcat[]=5", NewznabStandardCategory.TVOther, "Series/BD 3D");
            caps.Categories.AddCategoryMapping("cat[]=4&subcat[]=6", NewznabStandardCategory.TVHD, "Series/BD Remux");
            caps.Categories.AddCategoryMapping("cat[]=4&subcat[]=7", NewznabStandardCategory.TVHD, "Series/MKV");
            caps.Categories.AddCategoryMapping("cat[]=4&subcat[]=8", NewznabStandardCategory.TVUHD, "Series/MKV 4K");
            caps.Categories.AddCategoryMapping("cat[]=4&subcat[]=9", NewznabStandardCategory.TVUHD, "Series/BD Remux 4K");

            caps.Categories.AddCategoryMapping("cat[]=5&subcat[]=1", NewznabStandardCategory.TVDocumentary, "Docus/DVD");
            caps.Categories.AddCategoryMapping("cat[]=5&subcat[]=2", NewznabStandardCategory.TVDocumentary, "Docus/BDVD + Autorías");
            caps.Categories.AddCategoryMapping("cat[]=5&subcat[]=3", NewznabStandardCategory.TVDocumentary, "Docus/BD");
            caps.Categories.AddCategoryMapping("cat[]=5&subcat[]=4", NewznabStandardCategory.TVDocumentary, "Docus/BD 4K");
            caps.Categories.AddCategoryMapping("cat[]=5&subcat[]=5", NewznabStandardCategory.TVDocumentary, "Docus/BD 3D");
            caps.Categories.AddCategoryMapping("cat[]=5&subcat[]=6", NewznabStandardCategory.TVDocumentary, "Docus/BD Remux");
            caps.Categories.AddCategoryMapping("cat[]=5&subcat[]=7", NewznabStandardCategory.TVDocumentary, "Docus/MKV");
            caps.Categories.AddCategoryMapping("cat[]=5&subcat[]=8", NewznabStandardCategory.TVDocumentary, "Docus/MKV 4K");
            caps.Categories.AddCategoryMapping("cat[]=5&subcat[]=9", NewznabStandardCategory.TVDocumentary, "Docus/BD Remux 4K");

            caps.Categories.AddCategoryMapping("cat[]=6&subcat[]=1", NewznabStandardCategory.OtherMisc, "Deportes y Otros/DVD");
            caps.Categories.AddCategoryMapping("cat[]=6&subcat[]=2", NewznabStandardCategory.OtherMisc, "Deportes y Otros/BDVD + Autorías");
            caps.Categories.AddCategoryMapping("cat[]=6&subcat[]=3", NewznabStandardCategory.OtherMisc, "Deportes y Otros/BD");
            caps.Categories.AddCategoryMapping("cat[]=6&subcat[]=4", NewznabStandardCategory.OtherMisc, "Deportes y Otros/BD 4K");
            caps.Categories.AddCategoryMapping("cat[]=6&subcat[]=5", NewznabStandardCategory.OtherMisc, "Deportes y Otros/BD 3D");
            caps.Categories.AddCategoryMapping("cat[]=6&subcat[]=6", NewznabStandardCategory.OtherMisc, "Deportes y Otros/BD Remux");
            caps.Categories.AddCategoryMapping("cat[]=6&subcat[]=7", NewznabStandardCategory.OtherMisc, "Deportes y Otros/MKV");
            caps.Categories.AddCategoryMapping("cat[]=6&subcat[]=8", NewznabStandardCategory.OtherMisc, "Deportes y Otros/MKV 4K");
            caps.Categories.AddCategoryMapping("cat[]=6&subcat[]=9", NewznabStandardCategory.OtherMisc, "Deportes y Otros/BD Remux 4K");

            return caps;
        }
    }

    public class ZonaQRequestGenerator : IIndexerRequestGenerator
    {
        public UserPassTorrentBaseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

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
            using var doc = parser.ParseDocument(indexerResponse.Content);

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
