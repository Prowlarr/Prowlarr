using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
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
    public class BB : TorrentIndexerBase<UserPassTorrentBaseSettings>
    {
        public override string Name => "BB";
        public override string[] IndexerUrls => new[] { StringUtil.FromBase64("aHR0cHM6Ly9iYWNvbmJpdHMub3JnLw==") };
        private string LoginUrl => Settings.BaseUrl + "login.php";
        public override string Description => "BB is a Private Torrent Tracker for 0DAY / GENERAL";
        public override string Language => "en-US";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public BB(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new BBRequestGenerator { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new BBParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true,
                AllowAutoRedirect = true,
                Method = HttpMethod.Post
            };

            var cookies = Cookies;
            Cookies = null;

            var authLoginRequest = requestBuilder
                .AddFormParameter("username", Settings.Username)
                .AddFormParameter("password", Settings.Password)
                .AddFormParameter("keeplogged", "1")
                .AddFormParameter("login", "Log+In!")
                .SetHeader("Content-Type", "application/x-www-form-urlencoded")
                .SetHeader("Referer", LoginUrl)
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            if (CheckIfLoginNeeded(response))
            {
                var parser = new HtmlParser();
                var dom = parser.ParseDocument(response.Content);
                var messageEl = dom.QuerySelectorAll("#loginform");
                var messages = new List<string>();
                for (var i = 0; i < 13; i++)
                {
                    var child = messageEl[0].ChildNodes[i];
                    messages.Add(child.Text().Trim());
                }

                var message = string.Join(" ", messages);

                throw new IndexerAuthException(message);
            }

            cookies = response.GetCookies();
            UpdateCookies(cookies, DateTime.Now.AddDays(30));

            _logger.Debug("BB authentication succeeded.");
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            return !httpResponse.Content.Contains("logout.php");
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

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Audio);
            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.AudioMP3);
            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.AudioLossless);
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.PC);
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.BooksEBook);
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.AudioAudiobook);
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.Other);
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.BooksMags);
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.BooksComics);
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.TVAnime);
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.Movies);
            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.TVHD);
            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.TVSD);
            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.TV);
            caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.PCGames);
            caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.Console);
            caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.Other);
            caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.Other);

            return caps;
        }
    }

    public class BBRequestGenerator : IIndexerRequestGenerator
    {
        public UserPassTorrentBaseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
        {
            var searchUrl = string.Format("{0}/torrents.php", Settings.BaseUrl.TrimEnd('/'));

            // TODO: IMDB search is available but it requires to parse the details page
            var qc = new NameValueCollection
            {
                { "order_by", "s3" },
                { "order_way", "desc" },
                { "disablegrouping", "1" },
                { "searchtags", "" },
                { "tags_type", "0" },
                { "action", "basic" },
                { "searchstr", term.Replace(".", " ") }
            };

            var catList = Capabilities.Categories.MapTorznabCapsToTrackers(categories);

            foreach (var cat in catList)
            {
                qc.Add($"filter_cat[{cat}]", "1");
            }

            searchUrl = searchUrl + "?" + qc.GetQueryString();

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

    public class BBParser : IParseIndexerResponse
    {
        private readonly UserPassTorrentBaseSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public BBParser(UserPassTorrentBaseSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);
            var rows = dom.QuerySelectorAll("#torrent_table > tbody > tr.torrent");

            foreach (var row in rows)
            {
                var release = new TorrentInfo();

                release.MinimumRatio = 1;
                release.MinimumSeedTime = 172800; // 48 hours

                var catStr = row.Children[0].FirstElementChild.GetAttribute("href").Split(new[] { '[', ']' })[1];
                release.Categories = _categories.MapTrackerCatToNewznab(catStr);

                var qDetails = row.Children[1].QuerySelector("a[title='View Torrent']");
                release.InfoUrl = _settings.BaseUrl + qDetails.GetAttribute("href");
                release.Guid = release.InfoUrl;

                var qDownload = row.Children[1].QuerySelector("a[title='Download']");
                release.DownloadUrl = _settings.BaseUrl + qDownload.GetAttribute("href");

                var dateStr = row.Children[3].TextContent.Trim().Replace(" and", "");
                release.PublishDate = DateTimeUtil.FromTimeAgo(dateStr);

                var sizeStr = row.Children[4].TextContent;
                release.Size = ParseUtil.GetBytes(sizeStr);

                release.Files = ParseUtil.CoerceInt(row.Children[2].TextContent.Trim());
                release.Seeders = ParseUtil.CoerceInt(row.Children[7].TextContent.Trim());
                release.Peers = ParseUtil.CoerceInt(row.Children[8].TextContent.Trim()) + release.Seeders;

                var grabs = row.QuerySelector("td:nth-child(6)").TextContent;
                release.Grabs = ParseUtil.CoerceInt(grabs);

                if (row.QuerySelector("strong:contains(\"Freeleech!\")") != null)
                {
                    release.DownloadVolumeFactor = 0;
                }
                else
                {
                    release.DownloadVolumeFactor = 1;
                }

                release.UploadVolumeFactor = 1;

                var title = row.QuerySelector("td:nth-child(2)");
                foreach (var element in title.QuerySelectorAll("span, strong, div, br"))
                {
                    element.Remove();
                }

                release.Title = ParseUtil.NormalizeMultiSpaces(title.TextContent.Replace(" - ]", "]"));

                //change "Season #" to "S##" for TV shows
                if (catStr == "10")
                {
                    release.Title = Regex.Replace(release.Title,
                        @"Season (\d+)",
                        m => string.Format("S{0:00}",
                        int.Parse(m.Groups[1].Value)));
                }

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
