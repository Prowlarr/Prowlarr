using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using FluentValidation;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class TorrentSeeds : TorrentIndexerBase<TorrentSeedsSettings>
    {
        public override string Name => "TorrentSeeds";

        public override string[] IndexerUrls => new string[] { "https://torrentseeds.org/" };
        public override string Description => "TorrentSeeds is a Private site for MOVIES / TV / GENERAL";
        private string LoginUrl => Settings.BaseUrl + "takelogin.php";
        private string CaptchaUrl => Settings.BaseUrl + "simpleCaptcha.php?numImages=1";
        private string TokenUrl => Settings.BaseUrl + "login.php";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public TorrentSeeds(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new TorrentSeedsRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new TorrentSeedsParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true
            };

            Cookies = null;

            var loginPage = await ExecuteAuth(new HttpRequest(CaptchaUrl));
            var json1 = JObject.Parse(loginPage.Content);
            var captchaSelection = json1["images"][0]["hash"];

            requestBuilder.Method = HttpMethod.POST;
            requestBuilder.PostProcess += r => r.RequestTimeout = TimeSpan.FromSeconds(15);
            requestBuilder.SetCookies(loginPage.GetCookies());

            var authLoginRequest = requestBuilder
                .AddFormParameter("username", Settings.Username)
                .AddFormParameter("password", Settings.Password)
                .AddFormParameter("submitme", "X")
                .AddFormParameter("captchaSelection", (string)captchaSelection)
                .SetHeader("Content-Type", "multipart/form-data")
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            if (CheckIfLoginNeeded(response))
            {
                throw new IndexerAuthException("TorrentSeeds Login Failed");
            }

            var cookies = response.GetCookies();
            UpdateCookies(cookies, DateTime.Now + TimeSpan.FromDays(30));

            _logger.Debug("TorrentSeeds authentication succeeded.");
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            if ((httpResponse.HasHttpRedirect && httpResponse.Headers.GetSingleValue("Location").Contains("/login.php?")) ||
                (!httpResponse.HasHttpRedirect && !httpResponse.Content.Contains("/logout.php?")))
            {
                return true;
            }

            return false;
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

            caps.Categories.AddCategoryMapping(37, NewznabStandardCategory.TVAnime, "Anime/HD");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.TVAnime, "Anime/SD");
            caps.Categories.AddCategoryMapping(72, NewznabStandardCategory.TVAnime, "Anime/UHD");
            caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.PC0day, "Apps/0DAY");
            caps.Categories.AddCategoryMapping(27, NewznabStandardCategory.Books, "Apps/Bookware");
            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.PCISO, "Apps/ISO");
            caps.Categories.AddCategoryMapping(73, NewznabStandardCategory.AudioAudiobook, "Music/Audiobooks");
            caps.Categories.AddCategoryMapping(47, NewznabStandardCategory.ConsoleOther, "Console/NSW");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.ConsolePS3, "Console/PS3");
            caps.Categories.AddCategoryMapping(30, NewznabStandardCategory.ConsolePS4, "Console/PS4");
            caps.Categories.AddCategoryMapping(71, NewznabStandardCategory.ConsolePS4, "Console/PS5");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.ConsolePSP, "Console/PSP");
            caps.Categories.AddCategoryMapping(70, NewznabStandardCategory.ConsolePSVita, "Console/PSV");
            caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.ConsoleWii, "Console/WII");
            caps.Categories.AddCategoryMapping(29, NewznabStandardCategory.ConsoleWiiU, "Console/WIIU");
            caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.ConsoleXBox360, "Console/XBOX360");
            caps.Categories.AddCategoryMapping(32, NewznabStandardCategory.BooksEBook, "E-books");
            caps.Categories.AddCategoryMapping(63, NewznabStandardCategory.ConsoleOther, "Games/DOX");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.PCGames, "Games/ISO");
            caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.PCGames, "Games/PC Rips");
            caps.Categories.AddCategoryMapping(31, NewznabStandardCategory.MoviesBluRay, "Movies/Bluray");
            caps.Categories.AddCategoryMapping(50, NewznabStandardCategory.MoviesBluRay, "Movies/Bluray-UHD");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.MoviesDVD, "Movies/DVDR");
            caps.Categories.AddCategoryMapping(69, NewznabStandardCategory.MoviesForeign, "Movies/DVDR-Foreign");
            caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.MoviesHD, "Movies/HD");
            caps.Categories.AddCategoryMapping(39, NewznabStandardCategory.MoviesForeign, "Movies/HD-Foreign");
            caps.Categories.AddCategoryMapping(74, NewznabStandardCategory.MoviesHD, "Movies/Remuxes");
            caps.Categories.AddCategoryMapping(25, NewznabStandardCategory.MoviesSD, "Movies/SD");
            caps.Categories.AddCategoryMapping(62, NewznabStandardCategory.MoviesForeign, "Movies/SD-Foreign");
            caps.Categories.AddCategoryMapping(49, NewznabStandardCategory.MoviesUHD, "Movies/UHD");
            caps.Categories.AddCategoryMapping(76, NewznabStandardCategory.MoviesForeign, "Movies/UHD-Foreign");
            caps.Categories.AddCategoryMapping(33, NewznabStandardCategory.AudioLossless, "Music/FLAC");
            caps.Categories.AddCategoryMapping(28, NewznabStandardCategory.AudioOther, "Music/MBluRay-Rips");
            caps.Categories.AddCategoryMapping(34, NewznabStandardCategory.AudioOther, "Music/MDVDR");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.AudioMP3, "Music/MP3");
            caps.Categories.AddCategoryMapping(20, NewznabStandardCategory.AudioVideo, "Music/MVID");
            caps.Categories.AddCategoryMapping(77, NewznabStandardCategory.TVAnime, "Anime/Packs");
            caps.Categories.AddCategoryMapping(78, NewznabStandardCategory.BooksEBook, "Books/Packs");
            caps.Categories.AddCategoryMapping(80, NewznabStandardCategory.MoviesHD, "Movies/HD-Packs");
            caps.Categories.AddCategoryMapping(81, NewznabStandardCategory.MoviesHD, "Movies/Remux-Packs");
            caps.Categories.AddCategoryMapping(79, NewznabStandardCategory.MoviesSD, "Movies/SD-Packs");
            caps.Categories.AddCategoryMapping(68, NewznabStandardCategory.Audio, "Music/Packs");
            caps.Categories.AddCategoryMapping(67, NewznabStandardCategory.TVHD, "TV/HD-Packs");
            caps.Categories.AddCategoryMapping(82, NewznabStandardCategory.TVHD, "TV/Remux-Packs");
            caps.Categories.AddCategoryMapping(65, NewznabStandardCategory.TVSD, "TV/SD-Packs");
            caps.Categories.AddCategoryMapping(84, NewznabStandardCategory.TVUHD, "TV/UHD-Packs");
            caps.Categories.AddCategoryMapping(85, NewznabStandardCategory.XXX, "XXX/Packs");
            caps.Categories.AddCategoryMapping(23, NewznabStandardCategory.TVSD, "TV/DVDR");
            caps.Categories.AddCategoryMapping(26, NewznabStandardCategory.TVHD, "TV/HD");
            caps.Categories.AddCategoryMapping(64, NewznabStandardCategory.TVForeign, "TV/HD-Foreign");
            caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.TVHD, "TV/HD-Retail");
            caps.Categories.AddCategoryMapping(36, NewznabStandardCategory.TVSport, "TV/HD-Sport");
            caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.TVSD, "TV/SD");
            caps.Categories.AddCategoryMapping(86, NewznabStandardCategory.TVForeign, "TV/SD-Foreign");
            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.TVSD, "TV/SD-Retail");
            caps.Categories.AddCategoryMapping(35, NewznabStandardCategory.TVSport, "TV/SD-Sport");
            caps.Categories.AddCategoryMapping(61, NewznabStandardCategory.TVUHD, "TV/UHD");
            caps.Categories.AddCategoryMapping(87, NewznabStandardCategory.TVForeign, "TV/UHD-Foreign");
            caps.Categories.AddCategoryMapping(53, NewznabStandardCategory.XXX, "XXX/HD");
            caps.Categories.AddCategoryMapping(88, NewznabStandardCategory.XXXImageSet, "XXX/Image-Sets");
            caps.Categories.AddCategoryMapping(57, NewznabStandardCategory.XXX, "XXX/Paysite");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.XXX, "XXX/SD");

            return caps;
        }
    }

    public class TorrentSeedsRequestGenerator : IIndexerRequestGenerator
    {
        public TorrentSeedsSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public TorrentSeedsRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
        {
            // remove operator characters
            var cleanSearchString = Regex.Replace(term.Trim(), "[ _.+-]+", " ", RegexOptions.Compiled);

            var searchUrl = Settings.BaseUrl + "browse_elastic.php";
            var queryCollection = new NameValueCollection
            {
                { "search_in", "name" },
                { "search_mode", "all" },
                { "order_by", "added" },
                { "order_way", "desc" }
            };

            if (!string.IsNullOrWhiteSpace(cleanSearchString))
            {
                queryCollection.Add("query", cleanSearchString);
            }

            foreach (var cat in Capabilities.Categories.MapTorznabCapsToTrackers(categories))
            {
                queryCollection.Add($"cat[{cat}]", "1");
            }

            searchUrl += "?" + queryCollection.GetQueryString();

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

    public class TorrentSeedsParser : IParseIndexerResponse
    {
        private readonly TorrentSeedsSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public TorrentSeedsParser(TorrentSeedsSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);
            var rows = dom.QuerySelectorAll("table.table-bordered > tbody > tr[class*=\"torrent_row_\"]");
            foreach (var row in rows)
            {
                var release = new TorrentInfo();
                release.MinimumRatio = 1;
                release.MinimumSeedTime = 72 * 60 * 60;
                var qCatLink = row.QuerySelector("a[href^=\"/browse_elastic.php?cat=\"]");
                var catStr = qCatLink.GetAttribute("href").Split('=')[1];
                release.Categories = _categories.MapTrackerCatToNewznab(catStr);
                var qDetailsLink = row.QuerySelector("a[href^=\"/details.php?id=\"]");
                var qDetailsTitle = row.QuerySelector("td:has(a[href^=\"/details.php?id=\"]) b");
                release.Title = qDetailsTitle.TextContent.Trim();
                var qDlLink = row.QuerySelector("a[href^=\"/download.php?torrent=\"]");

                release.DownloadUrl = _settings.BaseUrl + qDlLink.GetAttribute("href").TrimStart('/');
                release.InfoUrl = _settings.BaseUrl + qDetailsLink.GetAttribute("href").TrimStart('/');
                release.Guid = release.InfoUrl;

                var qColumns = row.QuerySelectorAll("td");
                release.Files = ParseUtil.CoerceInt(qColumns[3].TextContent);
                release.PublishDate = DateTimeUtil.FromUnknown(qColumns[5].TextContent);
                release.Size = ParseUtil.GetBytes(qColumns[6].TextContent);
                release.Grabs = ParseUtil.CoerceInt(qColumns[7].TextContent.Replace("Times", ""));
                release.Seeders = ParseUtil.CoerceInt(qColumns[8].TextContent);
                release.Peers = ParseUtil.CoerceInt(qColumns[9].TextContent) + release.Seeders;

                var qImdb = row.QuerySelector("a[href*=\"www.imdb.com\"]");
                if (qImdb != null)
                {
                    var deRefUrl = qImdb.GetAttribute("href");
                    release.ImdbId = ParseUtil.GetImdbID(WebUtility.UrlDecode(deRefUrl).Split('/').Last()) ?? 0;
                }

                release.DownloadVolumeFactor = row.QuerySelector("span.freeleech") != null ? 0 : 1;
                release.UploadVolumeFactor = 1;
                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class TorrentSeedsSettingsValidator : AbstractValidator<TorrentSeedsSettings>
    {
        public TorrentSeedsSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    public class TorrentSeedsSettings : IIndexerSettings
    {
        private static readonly TorrentSeedsSettingsValidator Validator = new TorrentSeedsSettingsValidator();

        public TorrentSeedsSettings()
        {
            Username = "";
            Password = "";
        }

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Username", HelpText = "Site Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Password", HelpText = "Site Password", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Password { get; set; }

        [FieldDefinition(4)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
