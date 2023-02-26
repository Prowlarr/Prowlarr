using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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
    public class HDSpace : TorrentIndexerBase<UserPassTorrentBaseSettings>
    {
        public override string Name => "HD-Space";
        public override string[] IndexerUrls => new[] { "https://hd-space.org/" };
        public override string Description => "HD-Space (HDS) is a Private Torrent Tracker for HD MOVIES / TV";
        public override string Language => "en-US";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();
        private string LoginUrl => Settings.BaseUrl + "index.php?page=login";

        public HDSpace(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new HDSpaceRequestGenerator { Settings = Settings, Capabilities = Capabilities };
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
                AllowAutoRedirect = true,
                Method = HttpMethod.Post
            };

            var cookies = Cookies;
            Cookies = null;

            var authLoginRequest = requestBuilder
                .AddFormParameter("uid", Settings.Username)
                .AddFormParameter("pwd", Settings.Password)
                .SetCookies(loginPage.GetCookies())
                .SetHeader("Content-Type", "application/x-www-form-urlencoded")
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
            UpdateCookies(cookies, DateTime.Now.AddDays(30));

            _logger.Debug("HDSpace authentication succeeded.");
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
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.MoviesBluRay, "Movie / Blu-ray");
            caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.MoviesHD, "Movie / 1080p");
            caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.MoviesHD, "Movie / 720p");
            caps.Categories.AddCategoryMapping(46, NewznabStandardCategory.MoviesUHD, "Movie / 2160p");
            caps.Categories.AddCategoryMapping(40, NewznabStandardCategory.MoviesHD, "Movie / Remux");
            caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.MoviesHD, "Movie / HD-DVD");
            caps.Categories.AddCategoryMapping(41, NewznabStandardCategory.MoviesUHD, "Movie / 4K UHD");
            caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.TVHD, "TV Show / 720p HDTV");
            caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.TVHD, "TV Show / 1080p HDTV");
            caps.Categories.AddCategoryMapping(45, NewznabStandardCategory.TVUHD, "TV Show / 2160p HDTV");
            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.TVDocumentary, "Documentary / 720p");
            caps.Categories.AddCategoryMapping(25, NewznabStandardCategory.TVDocumentary, "Documentary / 1080p");
            caps.Categories.AddCategoryMapping(47, NewznabStandardCategory.TVDocumentary, "Documentary / 2160p");
            caps.Categories.AddCategoryMapping(27, NewznabStandardCategory.TVAnime, "Animation / 720p");
            caps.Categories.AddCategoryMapping(28, NewznabStandardCategory.TVAnime, "Animation / 1080p");
            caps.Categories.AddCategoryMapping(48, NewznabStandardCategory.TVAnime, "Animation / 2160p");
            caps.Categories.AddCategoryMapping(30, NewznabStandardCategory.AudioLossless, "Music / HQ Audio");
            caps.Categories.AddCategoryMapping(31, NewznabStandardCategory.AudioVideo, "Music / Videos");
            caps.Categories.AddCategoryMapping(33, NewznabStandardCategory.XXX, "XXX / 720p");
            caps.Categories.AddCategoryMapping(34, NewznabStandardCategory.XXX, "XXX / 1080p");
            caps.Categories.AddCategoryMapping(49, NewznabStandardCategory.XXX, "XXX / 2160p");
            caps.Categories.AddCategoryMapping(36, NewznabStandardCategory.MoviesOther, "Trailers");
            caps.Categories.AddCategoryMapping(37, NewznabStandardCategory.PC, "Software");
            caps.Categories.AddCategoryMapping(38, NewznabStandardCategory.Other, "Others");

            return caps;
        }
    }

    public class HDSpaceRequestGenerator : IIndexerRequestGenerator
    {
        public UserPassTorrentBaseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdb = null)
        {
            var queryCollection = new NameValueCollection
            {
                { "page", "torrents" },
                { "active", "0" },
            };

            var catList = Capabilities.Categories.MapTorznabCapsToTrackers(categories);
            if (catList.Any())
            {
                queryCollection.Set("category", string.Join(";", catList));
            }

            if (imdb.IsNotNullOrWhiteSpace())
            {
                queryCollection.Set("options", "2");
                queryCollection.Set("search", imdb);
            }
            else
            {
                queryCollection.Set("options", "0");
                queryCollection.Set("search", term.Replace(".", " "));
            }

            var searchUrl = $"{Settings.BaseUrl.TrimEnd('/')}/index.php?{queryCollection.GetQueryString()}";

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            return GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories, searchCriteria.ImdbId);
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories);
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            return GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories, searchCriteria.ImdbId);
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new List<IndexerRequest>();
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            return GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories);
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
            var rows = searchResultDocument.QuerySelectorAll("div#bodyarea table.lista:not(:contains(\"Our Team Recommend\")) > tbody > tr:has(a[href^=\"index.php?page=torrent-details&id=\"])");

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

                var qLink = row.QuerySelector("td:nth-child(2) a[href^=\"index.php?page=torrent-details&id=\"]");
                release.Title = qLink?.TextContent.Trim();
                release.InfoUrl = _settings.BaseUrl + qLink?.GetAttribute("href");
                release.Guid = release.InfoUrl;

                var downloadUrl = row.QuerySelector("td:nth-child(4) a[href^=\"download.php?id=\"]")?.GetAttribute("href");
                release.DownloadUrl = _settings.BaseUrl + downloadUrl;

                // Use the torrent filename as release title
                var torrentTitle = ParseUtil.GetArgumentFromQueryString(downloadUrl, "f")?.Replace(".torrent", "").Trim();
                if (torrentTitle.IsNotNullOrWhiteSpace())
                {
                    release.Title = WebUtility.HtmlDecode(torrentTitle);
                }

                var qGenres = row.QuerySelector("td:nth-child(2) span[style=\"color: #000000 \"]");
                if (qGenres != null)
                {
                    var description = qGenres.TextContent.Split('\xA0').Last().Replace(" ", "");
                    release.Description = description;
                    release.Genres = description.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
                }

                var imdbLink = row.QuerySelector("td:nth-child(2) a[href*=imdb]");
                if (imdbLink != null)
                {
                    release.ImdbId = ParseUtil.GetImdbID(imdbLink.GetAttribute("href").Split('/').Last()).GetValueOrDefault();
                }

                //"July 11, 2015, 13:34:09", "Today|Yesterday at 20:04:23"
                release.PublishDate = DateTimeUtil.FromUnknown(row.QuerySelector("td:nth-child(5)")?.TextContent.Trim());
                release.Size = ParseUtil.GetBytes(row.QuerySelector("td:nth-child(6)")?.TextContent.Trim());
                release.Seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(8)")?.TextContent);
                release.Peers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(9)")?.TextContent) + release.Seeders;
                var grabs = row.QuerySelector("td:nth-child(10)")?.TextContent.Trim().Replace("---", "0");
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

                var categoryLink = row.QuerySelector("a[href^=\"index.php?page=torrents&category=\"]").GetAttribute("href");
                var cat = ParseUtil.GetArgumentFromQueryString(categoryLink, "category");
                release.Categories = _categories.MapTrackerCatToNewznab(cat);

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
