using System;
using System.Collections.Generic;
using System.Linq;
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
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class PreToMe : TorrentIndexerBase<PreToMeSettings>
    {
        public override string Name => "PreToMe";
        public override string BaseUrl => "https://pretome.info/";
        public override string Description => "BitTorrent site for High Quality, High Definition (HD) movies and TV Shows";
        private string LoginUrl => BaseUrl + "takelogin.php";
        public override string Language => "en-us";
        public override Encoding Encoding => Encoding.GetEncoding("iso-8859-1");
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public PreToMe(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new PreToMeRequestGenerator() { Settings = Settings, Capabilities = Capabilities, BaseUrl = BaseUrl };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new PreToMeParser(Settings, Capabilities.Categories, BaseUrl);
        }

        protected override async Task DoLogin()
        {
            UpdateCookies(null, null);

            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true,
                AllowAutoRedirect = true
            };

            var loginPage = await _httpClient.ExecuteAsync(new HttpRequest(BaseUrl + "login.php"));

            requestBuilder.Method = HttpMethod.POST;
            requestBuilder.PostProcess += r => r.RequestTimeout = TimeSpan.FromSeconds(15);
            requestBuilder.SetCookies(loginPage.GetCookies());

            var authLoginRequest = requestBuilder
                .AddFormParameter("returnto", "%2F")
                .AddFormParameter("login_pin", Settings.Pin)
                .AddFormParameter("login", "Login")
                .AddFormParameter("username", Settings.Username)
                .AddFormParameter("password", Settings.Password)
                .SetHeader("Content-Type", "multipart/form-data")
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            if (response.Content != null && response.Content.Contains("logout.php"))
            {
                UpdateCookies(response.GetCookies(), DateTime.Now + TimeSpan.FromDays(30));
                _logger.Debug("PreToMe authentication succeeded");
            }
            else
            {
                throw new IndexerAuthException("PreToMe authentication failed");
            }
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            if (httpResponse.HasHttpRedirect || !httpResponse.Content.Contains("logout.php"))
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

            caps.Categories.AddCategoryMapping("cat[]=22", NewznabStandardCategory.PC, "Applications");
            caps.Categories.AddCategoryMapping("cat[]=22&tags=Windows", NewznabStandardCategory.PC0day, "Applications/Windows");
            caps.Categories.AddCategoryMapping("cat[]=22&tags=MAC", NewznabStandardCategory.PCMac, "Applications/MAC");
            caps.Categories.AddCategoryMapping("cat[]=22&tags=Linux", NewznabStandardCategory.PC, "Applications/Linux");

            caps.Categories.AddCategoryMapping("cat[]=27", NewznabStandardCategory.BooksEBook, "Ebooks");

            caps.Categories.AddCategoryMapping("cat[]=4", NewznabStandardCategory.Console, "Games");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=PC", NewznabStandardCategory.PCGames, "Games/PC");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=RIP", NewznabStandardCategory.PCGames, "Games/RIP");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=ISO", NewznabStandardCategory.PCGames, "Games/ISO");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=XBOX360", NewznabStandardCategory.ConsoleXBox360, "Games/XBOX360");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=PS3", NewznabStandardCategory.ConsolePS3, "Games/PS3");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=Wii", NewznabStandardCategory.ConsoleWii, "Games/Wii");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=PSP", NewznabStandardCategory.ConsolePSP, "Games/PSP");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=GAMES-NSW", NewznabStandardCategory.ConsoleOther, "Games/NSW");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=NDS", NewznabStandardCategory.ConsoleNDS, "Games/NDS");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=Xbox", NewznabStandardCategory.ConsoleXBox, "Games/Xbox");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=NSW", NewznabStandardCategory.ConsoleOther, "Games/NSW");
            caps.Categories.AddCategoryMapping("cat[]=4&tags=PS2", NewznabStandardCategory.ConsoleOther, "Games/PS2");

            caps.Categories.AddCategoryMapping("cat[]=31", NewznabStandardCategory.Other, "Miscellaneous");
            caps.Categories.AddCategoryMapping("cat[]=31&tags=Ebook", NewznabStandardCategory.BooksEBook, "Miscellaneous/Ebook");
            caps.Categories.AddCategoryMapping("cat[]=31&tags=RARFiX", NewznabStandardCategory.Other, "Miscellaneous/RARFiX");

            caps.Categories.AddCategoryMapping("cat[]=19", NewznabStandardCategory.Movies, "Movies");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=x264", NewznabStandardCategory.Movies, "Movies/x264");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=720p", NewznabStandardCategory.MoviesHD, "Movies/720p");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=XviD", NewznabStandardCategory.MoviesSD, "Movies/XviD");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=BluRay", NewznabStandardCategory.MoviesHD, "Movies/BluRay");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=DVDRiP", NewznabStandardCategory.MoviesSD, "Movies/DVDRiP");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=1080p", NewznabStandardCategory.MoviesHD, "Movies/1080p");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=DVD", NewznabStandardCategory.MoviesSD, "Movies/DVD");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=DVDR", NewznabStandardCategory.MoviesSD, "Movies/DVDR");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=WMV", NewznabStandardCategory.Movies, "Movies/WMV");
            caps.Categories.AddCategoryMapping("cat[]=19&tags=CAM", NewznabStandardCategory.Movies, "Movies/CAM");

            caps.Categories.AddCategoryMapping("cat[]=6", NewznabStandardCategory.Audio, "Music");
            caps.Categories.AddCategoryMapping("cat[]=6&tags=MP3", NewznabStandardCategory.AudioMP3, "Music/MP3");
            caps.Categories.AddCategoryMapping("cat[]=6&tags=V2", NewznabStandardCategory.AudioMP3, "Music/V2");
            caps.Categories.AddCategoryMapping("cat[]=6&tags=FLAC", NewznabStandardCategory.AudioLossless, "Music/FLAC");
            caps.Categories.AddCategoryMapping("cat[]=6&tags=320kbps", NewznabStandardCategory.AudioMP3, "Music/320kbps");

            caps.Categories.AddCategoryMapping("cat[]=7", NewznabStandardCategory.TV, "TV");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=x264", NewznabStandardCategory.TVHD, "TV/x264");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=720p", NewznabStandardCategory.TVHD, "TV/720p");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=HDTV", NewznabStandardCategory.TVHD, "TV/HDTV");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=XviD", NewznabStandardCategory.TVSD, "TV/XviD");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=BluRay", NewznabStandardCategory.TVHD, "TV/BluRay");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=DVDRiP", NewznabStandardCategory.TVSD, "TV/DVDRiP");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=DVD", NewznabStandardCategory.TVSD, "TV/DVD");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=Documentary", NewznabStandardCategory.TVDocumentary, "TV/Documentary");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=PDTV", NewznabStandardCategory.TVSD, "TV/PDTV");
            caps.Categories.AddCategoryMapping("cat[]=7&tags=HD-DVD", NewznabStandardCategory.TVSD, "TV/HD-DVD");

            caps.Categories.AddCategoryMapping("cat[]=51", NewznabStandardCategory.XXX, "XXX");
            caps.Categories.AddCategoryMapping("cat[]=51&tags=XviD", NewznabStandardCategory.XXXXviD, "XXX/XviD");
            caps.Categories.AddCategoryMapping("cat[]=51&tags=DVDRiP", NewznabStandardCategory.XXXDVD, "XXX/DVDRiP");

            return caps;
        }
    }

    public class PreToMeRequestGenerator : IIndexerRequestGenerator
    {
        public PreToMeSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }
        public string BaseUrl { get; set; }

        public PreToMeRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var searchUrl = string.Format("{0}/browse.php", BaseUrl.TrimEnd('/'));

            var qc = new List<KeyValuePair<string, string>> // NameValueCollection don't support cat[]=19&cat[]=6
            {
                { "st", "1" } // search in title
            };

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                qc.Add("search", imdbId);
                qc.Add("sd", "1"); // search in description
            }
            else
            {
                qc.Add("search", term);
            }

            // parse categories and tags
            var catGroups = new HashSet<string>(); // HashSet instead of List to avoid duplicates
            var tagGroups = new HashSet<string>();
            var cats = Capabilities.Categories.MapTorznabCapsToTrackers(categories);
            foreach (var cat in cats)
            {
                // "cat[]=7&tags=x264"
                var cSplit = cat.Split('&');

                var gSplit = cSplit[0].Split('=');
                if (gSplit.Length > 1)
                {
                    catGroups.Add(gSplit[1]); // category = 7
                }

                if (cSplit.Length > 1)
                {
                    var tSplit = cSplit[1].Split('=');
                    if (tSplit.Length > 1)
                    {
                        tagGroups.Add(tSplit[1]); // tag = x264
                    }
                }
            }

            // add categories
            foreach (var cat in catGroups)
            {
                qc.Add("cat[]", cat);
            }

            // do not include too many tags as it'll mess with their servers
            if (tagGroups.Count < 7)
            {
                qc.Add("tags", string.Join(",", tagGroups));

                // if tags are specified match any
                // if no tags are specified match all, with any we get random results
                qc.Add("tf", tagGroups.Any() ? "any" : "all");
            }

            searchUrl = searchUrl + "?" + qc.GetQueryString();

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories, searchCriteria.ImdbId));

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

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories, searchCriteria.ImdbId));

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

    public class PreToMeParser : IParseIndexerResponse
    {
        private readonly PreToMeSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        private readonly string _baseUrl;

        public PreToMeParser(PreToMeSettings settings, IndexerCapabilitiesCategories categories, string baseUrl)
        {
            _settings = settings;
            _categories = categories;
            _baseUrl = baseUrl;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var dom = parser.ParseDocument(indexerResponse.Content);
            var rows = dom.QuerySelectorAll("table > tbody > tr.browse");
            foreach (var row in rows)
            {
                var qLink = row.Children[1].QuerySelector("a");
                var title = qLink.GetAttribute("title");
                if (qLink.QuerySelectorAll("span").Length == 1 && title.StartsWith("NEW! |"))
                {
                    title = title.Substring(6).Trim();
                }

                // TODO: Asses if we should be throwing this out
                //if (!query.MatchQueryStringAND(title))
                //{
                //    continue; // we have to skip bad titles due to tags + any word search
                //}
                var details = _baseUrl + qLink.GetAttribute("href");
                var link = _baseUrl + row.Children[2].QuerySelector("a").GetAttribute("href");
                var dateStr = Regex.Replace(row.Children[5].InnerHtml, @"\<br[\s]{0,1}[\/]{0,1}\>", " ");
                var publishDate = DateTimeUtil.FromTimeAgo(dateStr);
                var files = ParseUtil.CoerceInt(row.Children[3].TextContent);
                var size = ReleaseInfo.GetBytes(row.Children[7].TextContent);
                var grabs = ParseUtil.CoerceInt(row.Children[8].TextContent);
                var seeders = ParseUtil.CoerceInt(row.Children[9].TextContent);
                var leechers = ParseUtil.CoerceInt(row.Children[10].TextContent);
                var cat = row.FirstElementChild.FirstElementChild.GetAttribute("href").Replace("browse.php?", string.Empty);

                var release = new TorrentInfo
                {
                    Title = title,
                    InfoUrl = details,
                    Guid = details,
                    DownloadUrl = link,
                    PublishDate = publishDate,
                    Size = size,
                    Category = _categories.MapTrackerCatToNewznab(cat),
                    Files = files,
                    Grabs = grabs,
                    Seeders = seeders,
                    Peers = leechers + seeders,
                    MinimumRatio = 0.75,
                    MinimumSeedTime = 216000, // 60 hours
                    DownloadVolumeFactor = 0, // ratioless
                    UploadVolumeFactor = 1
                };

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class PreToMeSettingsValidator : AbstractValidator<PreToMeSettings>
    {
        public PreToMeSettingsValidator()
        {
            RuleFor(c => c.Pin).NotEmpty();
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    public class PreToMeSettings : IProviderConfig
    {
        private static readonly PreToMeSettingsValidator Validator = new PreToMeSettingsValidator();

        public PreToMeSettings()
        {
            Pin = "";
            Username = "";
            Password = "";
        }

        [FieldDefinition(1, Label = "Pin", Advanced = true, HelpText = "Site Pin")]
        public string Pin { get; set; }

        [FieldDefinition(2, Label = "Username", Advanced = true, HelpText = "Site username")]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Password", Advanced = true, HelpText = "Site Password")]
        public string Password { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
