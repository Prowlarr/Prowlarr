using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Http;
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
    public class RevoPeers : TorrentIndexerBase<UserPassTorrentBaseSettings>
    {
        public override string Name => "RevoPeers";

        public override string[] IndexerUrls => new[] { "https://revopeers.me/" };
        public override string[] LegacyUrls => new[] { "https://revott.me/", "https://revolutiontt.me/", "https://www.gimmepeers.com/" };
        public override string Description => "RevoPeers is a Private PAY2DL Torrent Tracker for MOVIES / TV / GENERAL";
        private string LoginUrl => Settings.BaseUrl + "takelogin.php";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public RevoPeers(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new RevoPeersRequestGenerator { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new RevoPeersParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            UpdateCookies(null, null);

            var loginPage = await ExecuteAuth(new HttpRequest(Settings.BaseUrl + "login.php"));

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
                .SetHeader("Content-Type", "application/x-www-form-urlencoded")
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            if (response.Content != null && response.Content.Contains("/logout.php"))
            {
                UpdateCookies(response.GetCookies(), DateTime.Now.AddDays(30));

                _logger.Debug("RevoPeers authentication succeeded");
            }
            else
            {
                throw new IndexerAuthException("RevoPeers authentication failed");
            }
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            return httpResponse.HasHttpRedirect || !httpResponse.Content.Contains("/logout.php");
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
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping("23", NewznabStandardCategory.TVAnime);
            caps.Categories.AddCategoryMapping("2", NewznabStandardCategory.PCMac);
            caps.Categories.AddCategoryMapping("1", NewznabStandardCategory.PCISO);
            caps.Categories.AddCategoryMapping("22", NewznabStandardCategory.AudioAudiobook);
            caps.Categories.AddCategoryMapping("36", NewznabStandardCategory.BooksEBook);
            caps.Categories.AddCategoryMapping("40", NewznabStandardCategory.ConsoleWii);
            caps.Categories.AddCategoryMapping("16", NewznabStandardCategory.ConsolePS3);
            caps.Categories.AddCategoryMapping("4", NewznabStandardCategory.PCGames);
            caps.Categories.AddCategoryMapping("39", NewznabStandardCategory.ConsoleXBox360);
            caps.Categories.AddCategoryMapping("35", NewznabStandardCategory.ConsoleNDS);
            caps.Categories.AddCategoryMapping("34", NewznabStandardCategory.PCMobileOther);
            caps.Categories.AddCategoryMapping("51", NewznabStandardCategory.MoviesUHD);
            caps.Categories.AddCategoryMapping("10", NewznabStandardCategory.MoviesBluRay);
            caps.Categories.AddCategoryMapping("20", NewznabStandardCategory.MoviesDVD);
            caps.Categories.AddCategoryMapping("12", NewznabStandardCategory.MoviesHD);
            caps.Categories.AddCategoryMapping("44", NewznabStandardCategory.MoviesOther);
            caps.Categories.AddCategoryMapping("11", NewznabStandardCategory.MoviesHD);
            caps.Categories.AddCategoryMapping("19", NewznabStandardCategory.MoviesSD);
            caps.Categories.AddCategoryMapping("6", NewznabStandardCategory.Audio);
            caps.Categories.AddCategoryMapping("8", NewznabStandardCategory.AudioLossless);
            caps.Categories.AddCategoryMapping("46", NewznabStandardCategory.AudioOther);
            caps.Categories.AddCategoryMapping("29", NewznabStandardCategory.AudioVideo);
            caps.Categories.AddCategoryMapping("21", NewznabStandardCategory.TVSport);
            caps.Categories.AddCategoryMapping("70", NewznabStandardCategory.TVUHD);
            caps.Categories.AddCategoryMapping("43", NewznabStandardCategory.TVOther);
            caps.Categories.AddCategoryMapping("42", NewznabStandardCategory.TVHD);
            caps.Categories.AddCategoryMapping("45", NewznabStandardCategory.TVOther);
            caps.Categories.AddCategoryMapping("41", NewznabStandardCategory.TVSD);
            caps.Categories.AddCategoryMapping("52", NewznabStandardCategory.TVHD);
            caps.Categories.AddCategoryMapping("7", NewznabStandardCategory.TVSD);
            caps.Categories.AddCategoryMapping("9", NewznabStandardCategory.XXX);
            caps.Categories.AddCategoryMapping("49", NewznabStandardCategory.XXX);
            caps.Categories.AddCategoryMapping("47", NewznabStandardCategory.XXXDVD);
            caps.Categories.AddCategoryMapping("48", NewznabStandardCategory.XXX);

            return caps;
        }
    }

    public class RevoPeersRequestGenerator : IIndexerRequestGenerator
    {
        public UserPassTorrentBaseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var qc = new NameValueCollection
            {
                { "incldead", "1" }
            };

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                qc.Add("titleonly", "0");
                qc.Add("search", imdbId);
            }
            else
            {
                qc.Add("titleonly", "1");
                qc.Add("search", term);
            }

            var cats = Capabilities.Categories.MapTorznabCapsToTrackers(categories);

            if (cats.Count > 0)
            {
                foreach (var cat in cats)
                {
                    qc.Add($"c{cat}", "1");
                }
            }

            var searchUrl = Settings.BaseUrl + "browse.php?" + qc.GetQueryString();

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            request.HttpRequest.AllowAutoRedirect = false;

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

    public class RevoPeersParser : IParseIndexerResponse
    {
        private readonly UserPassTorrentBaseSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public RevoPeersParser(UserPassTorrentBaseSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();

            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(indexerResponse.Content);
            var rows = dom.QuerySelectorAll("#torrents-table > tbody > tr");

            foreach (var row in rows.Skip(1))
            {
                var qDetails = row.QuerySelector(".br_right > a");
                var details = _settings.BaseUrl + qDetails.GetAttribute("href");
                var title = qDetails.QuerySelector("b").TextContent;

                // Remove auto-generated [REQ] tag from fulfilled requests
                if (title.StartsWith("[REQ] "))
                {
                    title = title.Substring(6);
                }

                var qLink = row.QuerySelector("td:nth-child(4) > a");
                if (qLink == null)
                {
                    continue; // support/donation banner
                }

                var link = _settings.BaseUrl + qLink.GetAttribute("href");

                // dateString format "yyyy-MMM-dd hh:mm:ss" => eg "2015-04-25 23:38:12"
                var dateString = row.QuerySelector("td:nth-child(6) nobr").TextContent.Trim();
                var publishDate = DateTime.ParseExact(dateString, "yyyy-MM-ddHH:mm:ss", CultureInfo.InvariantCulture);

                var size = ParseUtil.GetBytes(row.QuerySelector("td:nth-child(7)").InnerHtml.Split('<').First().Trim());
                var files = ParseUtil.GetLongFromString(row.QuerySelector("td:nth-child(7) > a").TextContent);
                var grabs = ParseUtil.GetLongFromString(row.QuerySelector("td:nth-child(8)").TextContent);
                var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(9)").TextContent);
                var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(10)").TextContent);

                var category = row.QuerySelector(".br_type > a").GetAttribute("href").Replace("browse.php?cat=", string.Empty);

                var qImdb = row.QuerySelector("a[href*=\"www.imdb.com/\"]");
                var imdb = qImdb != null ? ParseUtil.GetImdbId(qImdb.GetAttribute("href").Split('/').Last()) : null;

                var release = new TorrentInfo
                {
                    InfoUrl = details,
                    Guid = details,
                    Title = title,
                    DownloadUrl = link,
                    PublishDate = publishDate,
                    Size = size,
                    Seeders = seeders,
                    Peers = seeders + leechers,
                    Grabs = (int)grabs,
                    Files = (int)files,
                    Categories = _categories.MapTrackerCatToNewznab(category),
                    ImdbId = imdb ?? 0,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800, // 48 hours
                    UploadVolumeFactor = 1,
                    DownloadVolumeFactor = 1
                };

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
