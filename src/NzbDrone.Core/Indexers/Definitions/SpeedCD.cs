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
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class SpeedCD : TorrentIndexerBase<SpeedCDSettings>
    {
        public override string Name => "SpeedCD";
        public override string[] IndexerUrls => new string[]
        {
            "https://speed.cd/",
            "https://speed.click/",
            "https://speeders.me/"
        };

        public override string Description => "Your home now!";
        public override string Language => "en-US";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public SpeedCD(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
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

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.MoviesOther, "Movies/XviD");
            caps.Categories.AddCategoryMapping(42, NewznabStandardCategory.Movies, "Movies/Packs");
            caps.Categories.AddCategoryMapping(32, NewznabStandardCategory.Movies, "Movies/Kids");
            caps.Categories.AddCategoryMapping(43, NewznabStandardCategory.MoviesHD, "Movies/HD");
            caps.Categories.AddCategoryMapping(47, NewznabStandardCategory.Movies, "Movies/DiVERSiTY");
            caps.Categories.AddCategoryMapping(28, NewznabStandardCategory.MoviesBluRay, "Movies/B-Ray");
            caps.Categories.AddCategoryMapping(48, NewznabStandardCategory.Movies3D, "Movies/3D");
            caps.Categories.AddCategoryMapping(40, NewznabStandardCategory.MoviesDVD, "Movies/DVD-R");
            caps.Categories.AddCategoryMapping(56, NewznabStandardCategory.Movies, "Movies/Anime");
            caps.Categories.AddCategoryMapping(50, NewznabStandardCategory.TVSport, "TV/Sports");
            caps.Categories.AddCategoryMapping(52, NewznabStandardCategory.TVHD, "TV/B-Ray");
            caps.Categories.AddCategoryMapping(53, NewznabStandardCategory.TVSD, "TV/DVD-R");
            caps.Categories.AddCategoryMapping(41, NewznabStandardCategory.TV, "TV/Packs");
            caps.Categories.AddCategoryMapping(55, NewznabStandardCategory.TV, "TV/Kids");
            caps.Categories.AddCategoryMapping(57, NewznabStandardCategory.TV, "TV/DiVERSiTY");
            caps.Categories.AddCategoryMapping(49, NewznabStandardCategory.TVHD, "TV/HD");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVSD, "TV/Episodes");
            caps.Categories.AddCategoryMapping(30, NewznabStandardCategory.TVAnime, "TV/Anime");
            caps.Categories.AddCategoryMapping(25, NewznabStandardCategory.PCISO, "Games/PC ISO");
            caps.Categories.AddCategoryMapping(39, NewznabStandardCategory.ConsoleWii, "Games/Wii");
            caps.Categories.AddCategoryMapping(45, NewznabStandardCategory.ConsolePS3, "Games/PS3");
            caps.Categories.AddCategoryMapping(35, NewznabStandardCategory.Console, "Games/Nintendo");
            caps.Categories.AddCategoryMapping(33, NewznabStandardCategory.ConsoleXBox360, "Games/XboX360");
            caps.Categories.AddCategoryMapping(46, NewznabStandardCategory.PCMobileOther, "Mobile");
            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.PC0day, "Apps/0DAY");
            caps.Categories.AddCategoryMapping(51, NewznabStandardCategory.PCMac, "Mac");
            caps.Categories.AddCategoryMapping(54, NewznabStandardCategory.Books, "Educational");
            caps.Categories.AddCategoryMapping(27, NewznabStandardCategory.Books, "Books-Mags");
            caps.Categories.AddCategoryMapping(26, NewznabStandardCategory.Audio, "Music/Audio");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.Audio, "Music/Flac");
            caps.Categories.AddCategoryMapping(44, NewznabStandardCategory.Audio, "Music/Pack");
            caps.Categories.AddCategoryMapping(29, NewznabStandardCategory.AudioVideo, "Music/Video");

            return caps;
        }
    }

    public class SpeedCDRequestGenerator : IIndexerRequestGenerator
    {
        public SpeedCDSettings Settings { get; set; }
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
        private readonly SpeedCDSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public SpeedCDParser(SpeedCDSettings settings, IndexerCapabilitiesCategories categories)
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

    public class SpeedCDSettingsValidator : AbstractValidator<SpeedCDSettings>
    {
        public SpeedCDSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    public class SpeedCDSettings : IIndexerSettings
    {
        private static readonly SpeedCDSettingsValidator Validator = new SpeedCDSettingsValidator();

        public SpeedCDSettings()
        {
            Username = "";
            Password = "";
        }

        [FieldDefinition(1, Label = "Base Url", HelpText = "Select which baseurl Prowlarr will use for requests to the site", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls")]
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
