using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class BakaBT : TorrentIndexerBase<BakaBTSettings>
    {
        public override string Name => "BakaBT";

        public override string[] IndexerUrls => new[] { "https://bakabt.me/" };
        public override string Description => "Anime Community";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public BakaBT(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new BakaBTRequestGenerator { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new BakaBTParser(Settings, Capabilities.Categories);
        }

        public override async Task<IndexerDownloadResponse> Download(Uri link)
        {
            var request = new HttpRequestBuilder(link.ToString())
                .SetCookies(GetCookies() ?? new Dictionary<string, string>())
                .Build();

            var response = await _httpClient.ExecuteProxiedAsync(request, Definition);

            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(response.Content);
            var downloadLink = dom.QuerySelector(".download_link")?.GetAttribute("href");

            if (downloadLink.IsNullOrWhiteSpace())
            {
                throw new Exception("Unable to find download link.");
            }

            return await base.Download(new Uri(Settings.BaseUrl + downloadLink));
        }

        protected override async Task DoLogin()
        {
            UpdateCookies(null, null);

            var loginUrl = Settings.BaseUrl + "login.php";

            var requestBuilder = new HttpRequestBuilder(loginUrl)
            {
                LogResponseContent = true,
                AllowAutoRedirect = true,
                Method = HttpMethod.Post
            };

            var loginPage = await ExecuteAuth(new HttpRequest(loginUrl));

            var parser = new HtmlParser();
            using var dom = await parser.ParseDocumentAsync(loginPage.Content);
            var loginKey = dom.QuerySelector("input[name=\"loginKey\"]");
            if (loginKey != null)
            {
                requestBuilder.AddFormParameter("loginKey", loginKey.GetAttribute("value"));
            }

            var authLoginRequest = requestBuilder
                .SetCookies(loginPage.GetCookies())
                .AddFormParameter("username", Settings.Username)
                .AddFormParameter("password", Settings.Password)
                .AddFormParameter("returnto", "/index.php")
                .SetHeader("Content-Type", "application/x-www-form-urlencoded")
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            if (CheckIfLoginNeeded(response))
            {
                var htmlParser = new HtmlParser();
                using var document = await htmlParser.ParseDocumentAsync(response.Content);
                var errorMessage = document.QuerySelector("#loginError, .error")?.TextContent.Trim();

                throw new IndexerAuthException(errorMessage ?? "Unknown error message, please report.");
            }

            UpdateCookies(response.GetCookies(), DateTime.Now.AddDays(30));

            _logger.Debug("BakaBT authentication succeeded");
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            return httpResponse.Content.Contains("loginForm");
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

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.TVAnime, "Anime Series");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVAnime, "OVA");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.AudioOther, "Soundtrack");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.BooksComics, "Manga");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.Movies, "Anime Movie");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.TVOther, "Live Action");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.BooksOther, "Artbook");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.AudioVideo, "Music Video");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.BooksEBook, "Light Novel");

            return caps;
        }
    }

    public class BakaBTRequestGenerator : IIndexerRequestGenerator
    {
        public BakaBTSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term)
        {
            var searchString = term;
            var searchUrl = Settings.BaseUrl + "browse.php?only=0&incomplete=1&lossless=1&hd=1&multiaudio=1&bonus=1&reorder=1&q=";
            if (Settings.AdultContent)
            {
                searchUrl = Settings.BaseUrl + "browse.php?only=0&hentai=1&incomplete=1&lossless=1&hd=1&multiaudio=1&bonus=1&reorder=1&q=";
            }

            var match = Regex.Match(term, @".*(?=\s(?:[Ee]\d+|\d+)$)");
            if (match.Success)
            {
                searchString = match.Value;
            }

            var episodeSearchUrl = searchUrl + WebUtility.UrlEncode(searchString);

            var request = new IndexerRequest(episodeSearchUrl, null);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm)));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm)));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm)));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm)));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm)));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class BakaBTParser : IParseIndexerResponse
    {
        private readonly BakaBTSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        private readonly List<IndexerCategory> _defaultCategories = new () { NewznabStandardCategory.TVAnime };

        public BakaBTParser(BakaBTSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releaseInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(indexerResponse.Content);
            var rows = dom.QuerySelectorAll(".torrents tr.torrent, .torrents tr.torrent_alt");
            var currentCategories = new List<IndexerCategory> { NewznabStandardCategory.TVAnime };

            foreach (var row in rows)
            {
                var downloadVolumeFactor = row.QuerySelector("span.freeleech") != null ? 0 : 1;

                // Skip non-freeleech results when freeleech only is set
                if (_settings.FreeleechOnly && downloadVolumeFactor != 0)
                {
                    continue;
                }

                var qTitleLink = row.QuerySelector("a.title, a.alt_title");
                if (qTitleLink == null)
                {
                    continue;
                }

                var title = qTitleLink.TextContent.Trim();

                // Insert before the release info
                var taidx = title.IndexOf('(');
                var tbidx = title.IndexOf('[');

                if (taidx == -1)
                {
                    taidx = title.Length;
                }

                if (tbidx == -1)
                {
                    tbidx = title.Length;
                }

                var titleSplit = Math.Min(taidx, tbidx);
                var titleSeries = title.Substring(0, titleSplit);
                var releaseInfo = title.Substring(titleSplit);

                currentCategories = GetNextCategory(row, currentCategories).ToList();

                var stringSeparator = new[] { " | " };
                var titles = titleSeries.Split(stringSeparator, StringSplitOptions.RemoveEmptyEntries);

                if (titles.Length > 1 && !_settings.AddRomajiTitle)
                {
                    titles = titles.Skip(1).ToArray();
                }

                foreach (var name in titles)
                {
                    var release = new TorrentInfo();

                    release.Title = (name + releaseInfo).Trim();

                    // Ensure the season is defined as this tracker only deals with full seasons
                    if (!release.Title.Contains("Season", StringComparison.CurrentCulture) && _settings.AppendSeason)
                    {
                        // Insert before the release info
                        var aidx = release.Title.IndexOf('(');
                        var bidx = release.Title.IndexOf('[');

                        if (aidx == -1)
                        {
                            aidx = release.Title.Length;
                        }

                        if (bidx == -1)
                        {
                            bidx = release.Title.Length;
                        }

                        var insertPoint = Math.Min(aidx, bidx);
                        release.Title = release.Title.Substring(0, insertPoint) + " Season 1 " + release.Title.Substring(insertPoint);
                    }

                    release.Categories = currentCategories;

                    release.Description = row.QuerySelector("span.tags")?.TextContent;
                    release.Guid = _settings.BaseUrl + qTitleLink.GetAttribute("href");
                    release.InfoUrl = release.Guid;

                    release.DownloadUrl = _settings.BaseUrl + row.QuerySelector(".peers a").GetAttribute("href");

                    var grabs = row.QuerySelectorAll(".peers")[0].FirstChild.NodeValue.TrimEnd().TrimEnd('/').TrimEnd();
                    grabs = grabs.Replace("k", "000");
                    release.Grabs = int.Parse(grabs);
                    release.Seeders = int.Parse(row.QuerySelectorAll(".peers a")[0].TextContent);
                    release.Peers = release.Seeders + int.Parse(row.QuerySelectorAll(".peers a")[1].TextContent);

                    release.MinimumRatio = 1;
                    release.MinimumSeedTime = 172800; // 48 hours

                    var size = row.QuerySelector(".size").TextContent;
                    release.Size = ParseUtil.GetBytes(size);

                    //22 Jul 15
                    var dateStr = row.QuerySelector(".added").TextContent.Replace("'", string.Empty);
                    if (dateStr.Split(' ')[0].Length == 1)
                    {
                        dateStr = "0" + dateStr;
                    }

                    if (string.Equals(dateStr, "yesterday", StringComparison.InvariantCultureIgnoreCase))
                    {
                        release.PublishDate = DateTime.Now.AddDays(-1);
                    }
                    else if (string.Equals(dateStr, "today", StringComparison.InvariantCultureIgnoreCase))
                    {
                        release.PublishDate = DateTime.Now;
                    }
                    else
                    {
                        release.PublishDate = DateTime.ParseExact(dateStr, "dd MMM yy", CultureInfo.InvariantCulture);
                    }

                    release.DownloadVolumeFactor = downloadVolumeFactor;
                    release.UploadVolumeFactor = 1;

                    releaseInfos.Add(release);
                }
            }

            return releaseInfos.ToArray();
        }

        private ICollection<IndexerCategory> GetNextCategory(IElement row, ICollection<IndexerCategory> currentCategories)
        {
            var nextCategoryName = GetCategoryName(row);
            if (nextCategoryName != null)
            {
                currentCategories = _categories.MapTrackerCatDescToNewznab(nextCategoryName);
                if (currentCategories.Count == 0)
                {
                    return _defaultCategories;
                }
            }

            return currentCategories;
        }

        private string GetCategoryName(IElement row)
        {
            var categoryElement = row.QuerySelector("td.category span");
            if (categoryElement == null)
            {
                return null;
            }

            var categoryName = categoryElement.GetAttribute("title");

            return categoryName.IsNotNullOrWhiteSpace() ? categoryName : null;
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class BakaBTSettings : UserPassTorrentBaseSettings
    {
        public BakaBTSettings()
        {
            FreeleechOnly = false;
        }

        [FieldDefinition(4, Label = "Freeleech Only", Type = FieldType.Checkbox, HelpText = "Show freeleech torrents only")]
        public bool FreeleechOnly { get; set; }

        [FieldDefinition(5, Label = "Add Romaji Title", Type = FieldType.Checkbox, HelpText = "Add releases for Romaji Title")]
        public bool AddRomajiTitle { get; set; }

        [FieldDefinition(6, Label = "Append Season", Type = FieldType.Checkbox, HelpText = "Append Season for Sonarr Compatibility")]
        public bool AppendSeason { get; set; }

        [FieldDefinition(7, Label = "Adult Content", Type = FieldType.Checkbox, HelpText = "Allow Adult Content (Must be enabled in BakaBT settings)")]
        public bool AdultContent { get; set; }
    }
}
