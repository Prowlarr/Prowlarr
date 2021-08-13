using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentValidation;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class TorrentLeech : TorrentIndexerBase<TorrentLeechSettings>
    {
        public override string Name => "TorrentLeech";

        public override string[] IndexerUrls => new string[] { "https://www.torrentleech.org/" };
        public override string Description => "This is what happens when you seed";
        private string LoginUrl => Settings.BaseUrl + "user/account/login/";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public TorrentLeech(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new TorrentLeechRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new TorrentLeechParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true
            };

            requestBuilder.Method = HttpMethod.POST;
            requestBuilder.PostProcess += r => r.RequestTimeout = TimeSpan.FromSeconds(15);

            var cookies = Cookies;

            Cookies = null;
            var authLoginRequest = requestBuilder
                .AddFormParameter("username", Settings.Username)
                .AddFormParameter("password", Settings.Password)
                .SetHeader("Content-Type", "multipart/form-data")
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            cookies = response.GetCookies();
            UpdateCookies(cookies, DateTime.Now + TimeSpan.FromDays(30));

            _logger.Debug("TorrentLeech authentication succeeded.");
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            if (httpResponse.Content.Contains("/user/account/login"))
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
                                       TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
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

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Movies, "Movies");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.MoviesSD, "Movies Cam");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.MoviesSD, "Movies TS/TC");
            caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.MoviesSD, "Movies DVDRip/DVDScreener");
            caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.MoviesDVD, "Movies DVD-R");
            caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.MoviesBluRay, "Movies Bluray");
            caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.MoviesHD, "Movies BlurayRip");
            caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.Movies, "Movies Boxsets");
            caps.Categories.AddCategoryMapping(29, NewznabStandardCategory.TVDocumentary, "Documentaries");
            caps.Categories.AddCategoryMapping(47, NewznabStandardCategory.MoviesUHD, "Movies 4K");
            caps.Categories.AddCategoryMapping(36, NewznabStandardCategory.MoviesForeign, "Movies Foreign");
            caps.Categories.AddCategoryMapping(37, NewznabStandardCategory.MoviesWEBDL, "Movies WEBRip");
            caps.Categories.AddCategoryMapping(43, NewznabStandardCategory.MoviesHD, "Movies HDRip");

            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TV, "TV");
            caps.Categories.AddCategoryMapping(26, NewznabStandardCategory.TVSD, "TV Episodes");
            caps.Categories.AddCategoryMapping(27, NewznabStandardCategory.TV, "TV Boxsets");
            caps.Categories.AddCategoryMapping(32, NewznabStandardCategory.TVHD, "TV Episodes HD");
            caps.Categories.AddCategoryMapping(44, NewznabStandardCategory.TVForeign, "TV Foreign");

            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.PCGames, "Games");
            caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.PCGames, "Games PC");
            caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.ConsoleXBox, "Games XBOX");
            caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.ConsoleXBox360, "Games XBOX360");
            caps.Categories.AddCategoryMapping(40, NewznabStandardCategory.ConsoleXBoxOne, "Games XBOXONE");
            caps.Categories.AddCategoryMapping(20, NewznabStandardCategory.ConsolePS3, "Games PS2");
            caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.ConsolePS3, "Games Mac");
            caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.ConsolePSP, "Games PSP");
            caps.Categories.AddCategoryMapping(28, NewznabStandardCategory.ConsoleWii, "Games Wii");
            caps.Categories.AddCategoryMapping(30, NewznabStandardCategory.ConsoleNDS, "Games Nintendo DS");
            caps.Categories.AddCategoryMapping(39, NewznabStandardCategory.ConsolePS4, "Games PS4");
            caps.Categories.AddCategoryMapping(42, NewznabStandardCategory.PCMac, "Games Mac");
            caps.Categories.AddCategoryMapping(48, NewznabStandardCategory.ConsoleOther, "Games Nintendo Switch");

            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.Audio, "Music");
            caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.AudioVideo, "Music videos");
            caps.Categories.AddCategoryMapping(31, NewznabStandardCategory.Audio, "Audio");

            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.TV, "Animation");
            caps.Categories.AddCategoryMapping(34, NewznabStandardCategory.TVAnime, "TV Anime");
            caps.Categories.AddCategoryMapping(35, NewznabStandardCategory.TV, "TV Cartoons");

            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.Books, "Books");
            caps.Categories.AddCategoryMapping(45, NewznabStandardCategory.BooksEBook, "Books EBooks");
            caps.Categories.AddCategoryMapping(46, NewznabStandardCategory.BooksComics, "Books Comics");

            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.PC, "Apps");
            caps.Categories.AddCategoryMapping(23, NewznabStandardCategory.PCISO, "PC ISO");
            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.PCMac, "PC Mac");
            caps.Categories.AddCategoryMapping(25, NewznabStandardCategory.PCMobileOther, "PC Mobile");
            caps.Categories.AddCategoryMapping(33, NewznabStandardCategory.PC0day, "PC 0-day");
            caps.Categories.AddCategoryMapping(38, NewznabStandardCategory.Other, "Education");

            return caps;
        }
    }

    public class TorrentLeechRequestGenerator : IIndexerRequestGenerator
    {
        public TorrentLeechSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public TorrentLeechRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var searchString = Regex.Replace(term, @"(^|\s)-", " ");

            var searchUrl = Settings.BaseUrl + "torrents/browse/list/";

            if (Settings.FreeLeechOnly)
            {
                searchUrl += "facets/tags%3AFREELEECH/";
            }

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                searchUrl += "imdbID/" + imdbId + "/";
            }
            else if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchUrl += "exact/1/query/" + WebUtility.UrlEncode(searchString) + "/";
            }

            var cats = Capabilities.Categories.MapTorznabCapsToTrackers(categories);
            if (cats.Count > 0)
            {
                searchUrl += "categories/" + string.Join(",", cats);
            }
            else
            {
                searchUrl += "newfilter/2"; // include 0day and music
            }

            var request = new IndexerRequest(searchUrl, HttpAccept.Rss);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SearchTerm), searchCriteria.Categories, searchCriteria.FullImdbId));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SearchTerm), searchCriteria.Categories));

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

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class TorrentLeechParser : IParseIndexerResponse
    {
        private readonly TorrentLeechSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public TorrentLeechParser(TorrentLeechSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();

            var rows = JsonConvert.DeserializeObject<dynamic>(indexerResponse.Content).torrentList;
            foreach (var row in rows ?? Enumerable.Empty<dynamic>())
            {
                var title = row.name.ToString();

                var torrentId = row.fid.ToString();
                var details = new Uri(_settings.BaseUrl + "torrent/" + torrentId);
                var link = new Uri(_settings.BaseUrl + "download/" + torrentId + "/" + row.filename);
                var publishDate = DateTime.ParseExact(row.addedTimestamp.ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                var seeders = (int)row.seeders;
                var leechers = (int)row.leechers;
                var grabs = (int)row.completed;
                var size = (long)row.size;
                var cats = _categories.MapTrackerCatToNewznab(((int)row.categoryID).ToString());
                var imdb = (string)row.imdbID;
                var imdbId = 0;
                if (imdb.Length > 2)
                {
                    imdbId = int.Parse(imdb.Substring(2));
                }

                // freeleech #6579 #6624 #7367
                string dlMultiplier = row.download_multiplier.ToString();
                var dlVolumeFactor = dlMultiplier.IsNullOrWhiteSpace() ? 1 : ParseUtil.CoerceInt(dlMultiplier);

                var release = new TorrentInfo
                {
                    Title = title,
                    InfoUrl = details.AbsoluteUri,
                    Guid = details.AbsoluteUri,
                    DownloadUrl = link.AbsoluteUri,
                    PublishDate = publishDate,
                    Categories = cats,
                    Size = size,
                    Grabs = grabs,
                    Seeders = seeders,
                    Peers = seeders + leechers,
                    ImdbId = imdbId,
                    UploadVolumeFactor = 1,
                    DownloadVolumeFactor = dlVolumeFactor,
                    MinimumRatio = 1,
                    MinimumSeedTime = 864000 // 10 days for registered users, less for upgraded users
                };

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class TorrentLeechSettingsValidator : AbstractValidator<TorrentLeechSettings>
    {
        public TorrentLeechSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();

            RuleFor(c => c.VipExpiration).Must(c => c.IsValidDate())
                                         .When(c => c.VipExpiration.IsNotNullOrWhiteSpace())
                                         .WithMessage("Correctly formatted date is required");

            RuleFor(c => c.VipExpiration).Must(c => c.IsFutureDate())
                                         .When(c => c.VipExpiration.IsNotNullOrWhiteSpace())
                                         .WithMessage("Must be a future date");
        }
    }

    public class TorrentLeechSettings : IIndexerSettings
    {
        private static readonly TorrentLeechSettingsValidator Validator = new TorrentLeechSettingsValidator();

        public TorrentLeechSettings()
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

        [FieldDefinition(4, Label = "FreeLeech Only", Type = FieldType.Checkbox, Advanced = true, HelpText = "Search Freeleech torrents only")]
        public bool FreeLeechOnly { get; set; }

        [FieldDefinition(5, Label = "VIP Expiration", HelpText = "Enter date (yyyy-mm-dd) for VIP Expiration or blank, Prowlarr will notify 1 week from expiration of VIP")]
        public string VipExpiration { get; set; }

        [FieldDefinition(6)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
