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
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class AnimeTorrents : TorrentIndexerBase<UserPassTorrentBaseSettings>
    {
        public override string Name => "AnimeTorrents";

        public override string[] IndexerUrls => new[] { "https://animetorrents.me/" };
        public override string Description => "Definitive source for anime and manga";
        private string LoginUrl => Settings.BaseUrl + "login.php";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public AnimeTorrents(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AnimeTorrentsRequestGenerator { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new AnimeTorrentsParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            UpdateCookies(null, null);

            var loginPage = await ExecuteAuth(new HttpRequest(LoginUrl));

            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true,
                AllowAutoRedirect = true,
                Method = HttpMethod.Post
            };

            var authLoginRequest = requestBuilder
                .SetCookies(loginPage.GetCookies())
                .AddFormParameter("username", Settings.Username)
                .AddFormParameter("password", Settings.Password)
                .AddFormParameter("form", "login")
                .AddFormParameter("rememberme[]", "1")
                .SetHeader("Content-Type", "application/x-www-form-urlencoded")
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            if (response.Content != null && response.Content.Contains("logout.php"))
            {
                UpdateCookies(response.GetCookies(), DateTime.Now.AddDays(30));

                _logger.Debug("AnimeTorrents authentication succeeded");
            }
            else
            {
                throw new IndexerAuthException("AnimeTorrents authentication failed");
            }
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            return httpResponse.Content.Contains("Access Denied!") || httpResponse.Content.Contains("login.php");
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

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.MoviesSD, "Anime Movie");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.MoviesHD, "Anime Movie HD");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVAnime, "Anime Series");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.TVAnime, "Anime Series HD");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.XXXDVD, "Hentai (censored)");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.XXXDVD, "Hentai (censored) HD");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.XXXDVD, "Hentai (un-censored)");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.XXXDVD, "Hentai (un-censored) HD");
            caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.BooksForeign, "Light Novel");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.BooksComics, "Manga");
            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.BooksComics, "Manga 18+");
            caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.TVAnime, "OVA");
            caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.TVAnime, "OVA HD");
            caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.BooksComics, "Doujin Anime");
            caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.XXXDVD, "Doujin Anime 18+");
            caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.AudioForeign, "Doujin Music");
            caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.BooksComics, "Doujinshi");
            caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.BooksComics, "Doujinshi 18+");
            caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.Audio, "OST");

            return caps;
        }
    }

    public class AnimeTorrentsRequestGenerator : IIndexerRequestGenerator
    {
        public UserPassTorrentBaseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
        {
            var searchString = term;

            //  replace any space, special char, etc. with % (wildcard)
            var replaceRegex = new Regex("[^a-zA-Z0-9]+");
            searchString = replaceRegex.Replace(searchString, "%");
            var searchUrl = Settings.BaseUrl + "ajax/torrents_data.php";
            var searchUrlReferer = Settings.BaseUrl + "torrents.php?cat=0&searchin=filename&search=";

            var trackerCats = Capabilities.Categories.MapTorznabCapsToTrackers(categories) ?? new List<string>();

            var queryCollection = new NameValueCollection
            {
                { "total", "146" }, // Not sure what this is about but its required!
                { "cat", trackerCats.Count == 1 ? trackerCats.First() : "0" },
                { "page", "1" },
                { "searchin", "filename" },
                { "search", searchString }
            };

            searchUrl += "?" + queryCollection.GetQueryString();

            var extraHeaders = new NameValueCollection
            {
                { "X-Requested-With", "XMLHttpRequest" },
                { "Referer", searchUrlReferer }
            };

            var request = new IndexerRequest(searchUrl, null);
            request.HttpRequest.Headers.Add(extraHeaders);

            yield return request;
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            return GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories);
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories);
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            return GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories);
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories);
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            return GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories);
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class AnimeTorrentsParser : IParseIndexerResponse
    {
        private readonly UserPassTorrentBaseSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public AnimeTorrentsParser(UserPassTorrentBaseSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);

            var rows = dom.QuerySelectorAll("tr");
            foreach (var row in rows.Skip(1))
            {
                var release = new TorrentInfo();
                var qTitleLink = row.QuerySelector("td:nth-of-type(2) a:nth-of-type(1)");
                release.Title = qTitleLink.TextContent.Trim();

                // If we search an get no results, we still get a table just with no info.
                if (string.IsNullOrWhiteSpace(release.Title))
                {
                    break;
                }

                release.Guid = qTitleLink.GetAttribute("href");
                release.InfoUrl = release.Guid;

                var dateString = row.QuerySelector("td:nth-of-type(5)").TextContent;
                release.PublishDate = DateTime.ParseExact(dateString, "dd MMM yy", CultureInfo.InvariantCulture);

                // newbie users don't see DL links
                var qLink = row.QuerySelector("td:nth-of-type(3) a");
                if (qLink != null)
                {
                    release.DownloadUrl = qLink.GetAttribute("href");
                }
                else
                {
                    // use details link as placeholder
                    // null causes errors during export to torznab
                    // skipping the release prevents newbie users from adding the tracker (empty result)
                    release.DownloadUrl = release.InfoUrl;
                }

                var sizeStr = row.QuerySelector("td:nth-of-type(6)").TextContent;
                release.Size = ParseUtil.GetBytes(sizeStr);

                var connections = row.QuerySelector("td:nth-of-type(8)").TextContent.Trim().Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                release.Seeders = ParseUtil.CoerceInt(connections[0].Trim());
                release.Peers = ParseUtil.CoerceInt(connections[1].Trim()) + release.Seeders;
                release.Grabs = ParseUtil.CoerceInt(connections[2].Trim());

                var rCat = row.QuerySelector("td:nth-of-type(1) a").GetAttribute("href");
                var rCatIdx = rCat.IndexOf("cat=");
                if (rCatIdx > -1)
                {
                    rCat = rCat.Substring(rCatIdx + 4);
                }

                release.Categories = _categories.MapTrackerCatToNewznab(rCat);

                if (row.QuerySelector("img[alt=\"Gold Torrent\"]") != null)
                {
                    release.DownloadVolumeFactor = 0;
                }
                else if (row.QuerySelector("img[alt=\"Silver Torrent\"]") != null)
                {
                    release.DownloadVolumeFactor = 0.5;
                }
                else
                {
                    release.DownloadVolumeFactor = 1;
                }

                var uLFactorImg = row.QuerySelector("img[alt*=\"x Multiplier Torrent\"]");
                if (uLFactorImg != null)
                {
                    release.UploadVolumeFactor = ParseUtil.CoerceDouble(uLFactorImg.GetAttribute("alt").Split('x')[0]);
                }
                else
                {
                    release.UploadVolumeFactor = 1;
                }

                qTitleLink.Remove();

                //release.Description = row.QuerySelector("td:nth-of-type(2)").TextContent;
                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
