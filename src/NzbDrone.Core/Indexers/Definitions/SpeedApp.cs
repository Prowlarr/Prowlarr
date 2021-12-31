using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class SpeedApp : TorrentIndexerBase<SpeedAppSettings>
    {
        public override string Name => "SpeedApp.io";

        public override string[] IndexerUrls => new string[] { "https://speedapp.io" };

        private string ApiUrl => $"{Settings.BaseUrl}/api";

        private string LoginUrl => $"{ApiUrl}/login";

        public override string Description => "SpeedApp is a ROMANIAN Private Torrent Tracker for MOVIES / TV / GENERAL";

        public override string Language => "ro-RO";

        public override Encoding Encoding => Encoding.UTF8;

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public override IndexerCapabilities Capabilities => SetCapabilities();

        private IIndexerRepository _indexerRepository;

        public SpeedApp(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger, IIndexerRepository indexerRepository)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
            _indexerRepository = indexerRepository;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new SpeedAppRequestGenerator(Capabilities, Settings);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new SpeedAppParser(Settings, Capabilities.Categories);
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            return Settings.ApiKey.IsNullOrWhiteSpace() || httpResponse.StatusCode == HttpStatusCode.Unauthorized;
        }

        protected override async Task DoLogin()
        {
            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true,
                AllowAutoRedirect = true,
                Method = HttpMethod.POST,
            };

            var request = requestBuilder.Build();

            var data = new SpeedAppAuthenticationRequest
            {
                Email = Settings.Email,
                Password = Settings.Password
            };

            request.SetContent(JsonConvert.SerializeObject(data));

            request.Headers.ContentType = MediaTypeNames.Application.Json;

            var response = await ExecuteAuth(request);

            var statusCode = (int)response.StatusCode;

            if (statusCode is < 200 or > 299)
            {
                throw new HttpException(response);
            }

            var parsedResponse = JsonConvert.DeserializeObject<SpeedAppAuthenticationResponse>(response.Content);

            Settings.ApiKey = parsedResponse.Token;

            if (Definition.Id > 0)
            {
                _indexerRepository.UpdateSettings((IndexerDefinition)Definition);
            }

            _logger.Debug("SpeedApp authentication succeeded.");
        }

        protected override void ModifyRequest(IndexerRequest request)
        {
            request.HttpRequest.Headers.Set("Authorization", $"Bearer {Settings.ApiKey}");
        }

        public override async Task<byte[]> Download(Uri link)
        {
            Cookies = GetCookies();

            if (link.Scheme == "magnet")
            {
                ValidateMagnet(link.OriginalString);
                return Encoding.UTF8.GetBytes(link.OriginalString);
            }

            var requestBuilder = new HttpRequestBuilder(link.AbsoluteUri);

            if (Cookies != null)
            {
                requestBuilder.SetCookies(Cookies);
            }

            var request = requestBuilder.Build();
            request.AllowAutoRedirect = FollowRedirect;
            request.Headers.Set("Authorization", $"Bearer {Settings.ApiKey}");

            byte[] torrentData;

            try
            {
                var response = await _httpClient.ExecuteProxiedAsync(request, Definition);
                torrentData = response.ResponseData;
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.Error(ex, "Downloading torrent file for release failed since it no longer exists ({0})", link.AbsoluteUri);
                    throw new ReleaseUnavailableException("Downloading torrent failed", ex);
                }

                if ((int)ex.Response.StatusCode == 429)
                {
                    _logger.Error("API Grab Limit reached for {0}", link.AbsoluteUri);
                }
                else
                {
                    _logger.Error(ex, "Downloading torrent file for release failed ({0})", link.AbsoluteUri);
                }

                throw new ReleaseDownloadException("Downloading torrent failed", ex);
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Downloading torrent file for release failed ({0})", link.AbsoluteUri);

                throw new ReleaseDownloadException("Downloading torrent failed", ex);
            }
            catch (Exception)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.Error("Downloading torrent failed");
                throw;
            }

            return torrentData;
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q,
                    TvSearchParam.Season,
                    TvSearchParam.Ep,
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q,
                    MovieSearchParam.ImdbId,
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q,
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q,
                },
            };

            caps.Categories.AddCategoryMapping(38, NewznabStandardCategory.Movies, "Movie Packs");
            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.MoviesSD, "Movies: SD");
            caps.Categories.AddCategoryMapping(35, NewznabStandardCategory.MoviesSD, "Movies: SD Ro");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.MoviesHD, "Movies: HD");
            caps.Categories.AddCategoryMapping(29, NewznabStandardCategory.MoviesHD, "Movies: HD Ro");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.MoviesDVD, "Movies: DVD");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.MoviesDVD, "Movies: DVD Ro");
            caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.MoviesBluRay, "Movies: BluRay");
            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.MoviesBluRay, "Movies: BluRay Ro");
            caps.Categories.AddCategoryMapping(59, NewznabStandardCategory.Movies, "Movies: Ro");
            caps.Categories.AddCategoryMapping(57, NewznabStandardCategory.MoviesUHD, "Movies: 4K (2160p) Ro");
            caps.Categories.AddCategoryMapping(61, NewznabStandardCategory.MoviesUHD, "Movies: 4K (2160p)");
            caps.Categories.AddCategoryMapping(41, NewznabStandardCategory.TV, "TV Packs");
            caps.Categories.AddCategoryMapping(66, NewznabStandardCategory.TV, "TV Packs Ro");
            caps.Categories.AddCategoryMapping(45, NewznabStandardCategory.TVSD, "TV Episodes");
            caps.Categories.AddCategoryMapping(46, NewznabStandardCategory.TVSD, "TV Episodes Ro");
            caps.Categories.AddCategoryMapping(43, NewznabStandardCategory.TVHD, "TV Episodes HD");
            caps.Categories.AddCategoryMapping(44, NewznabStandardCategory.TVHD, "TV Episodes HD Ro");
            caps.Categories.AddCategoryMapping(60, NewznabStandardCategory.TV, "TV Ro");
            caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.PCGames, "Games: PC-ISO");
            caps.Categories.AddCategoryMapping(52, NewznabStandardCategory.Console, "Games: Console");
            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.PC0day, "Applications");
            caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.PC, "Applications: Linux");
            caps.Categories.AddCategoryMapping(37, NewznabStandardCategory.PCMac, "Applications: Mac");
            caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.PCMobileOther, "Applications: Mobile");
            caps.Categories.AddCategoryMapping(62, NewznabStandardCategory.TV, "TV Cartoons");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.TVAnime, "TV Anime / Hentai");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.BooksEBook, "E-books");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.Audio, "Music");
            caps.Categories.AddCategoryMapping(64, NewznabStandardCategory.AudioVideo, "Music Video");
            caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.Other, "Images");
            caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.TVSport, "TV Sports");
            caps.Categories.AddCategoryMapping(58, NewznabStandardCategory.TVSport, "TV Sports Ro");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.TVDocumentary, "TV Documentary");
            caps.Categories.AddCategoryMapping(63, NewznabStandardCategory.TVDocumentary, "TV Documentary Ro");
            caps.Categories.AddCategoryMapping(65, NewznabStandardCategory.Other, "Tutorial");
            caps.Categories.AddCategoryMapping(67, NewznabStandardCategory.OtherMisc, "Miscellaneous");
            caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.XXX, "XXX Movies");
            caps.Categories.AddCategoryMapping(47, NewznabStandardCategory.XXX, "XXX DVD");
            caps.Categories.AddCategoryMapping(48, NewznabStandardCategory.XXX, "XXX HD");
            caps.Categories.AddCategoryMapping(49, NewznabStandardCategory.XXXImageSet, "XXX Images");
            caps.Categories.AddCategoryMapping(50, NewznabStandardCategory.XXX, "XXX Packs");
            caps.Categories.AddCategoryMapping(51, NewznabStandardCategory.XXX, "XXX SD");

            return caps;
        }
    }

    public class SpeedAppRequestGenerator : IIndexerRequestGenerator
    {
        public Func<IDictionary<string, string>> GetCookies { get; set; }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        private IndexerCapabilities Capabilities { get; }

        private SpeedAppSettings Settings { get; }

        public SpeedAppRequestGenerator(IndexerCapabilities capabilities, SpeedAppSettings settings)
        {
            Capabilities = capabilities;
            Settings = settings;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            return GetSearch(searchCriteria, searchCriteria.FullImdbId);
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return GetSearch(searchCriteria);
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            return GetSearch(searchCriteria, searchCriteria.FullImdbId, searchCriteria.Season, searchCriteria.Episode);
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return GetSearch(searchCriteria);
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            return GetSearch(searchCriteria);
        }

        private IndexerPageableRequestChain GetSearch(SearchCriteriaBase searchCriteria, string imdbId = null, int? season = null, string episode = null)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories, imdbId, season, episode));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null, int? season = null, string episode = null)
        {
            var qc = new NameValueCollection();

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                qc.Add("imdbId", imdbId);
            }
            else
            {
                qc.Add("search", term);
            }

            if (season != null)
            {
                qc.Add("season", season.Value.ToString());
            }

            if (episode != null)
            {
                qc.Add("episode", episode);
            }

            var cats = Capabilities.Categories.MapTorznabCapsToTrackers(categories);

            if (cats.Count > 0)
            {
                foreach (var cat in cats)
                {
                    qc.Add("categories[]", cat);
                }
            }

            var searchUrl = Settings.BaseUrl + "/api/torrent?" + qc.GetQueryString(duplicateKeysIfMulti: true);

            var request = new IndexerRequest(searchUrl, HttpAccept.Json);

            request.HttpRequest.Headers.Set("Authorization", $"Bearer {Settings.ApiKey}");

            yield return request;
        }
    }

    public class SpeedAppParser : IParseIndexerResponse
    {
        private readonly SpeedAppSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public SpeedAppParser(SpeedAppSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from API request");
            }

            if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from API request, expected {HttpAccept.Json.Value}");
            }

            var jsonResponse = new HttpResponse<List<SpeedAppTorrent>>(indexerResponse.HttpResponse);

            return jsonResponse.Resource.Select(torrent => new TorrentInfo
            {
                Guid = torrent.Id.ToString(),
                Title = torrent.Name,
                Description = torrent.ShortDescription,
                Size = torrent.Size,
                ImdbId = ParseUtil.GetImdbID(torrent.ImdbId).GetValueOrDefault(),
                DownloadUrl = $"{_settings.BaseUrl}/api/torrent/{torrent.Id}/download",
                PosterUrl = torrent.Poster,
                InfoUrl = torrent.Url,
                Grabs = torrent.TimesCompleted,
                PublishDate = torrent.CreatedAt,
                Categories = _categories.MapTrackerCatToNewznab(torrent.Category.Id.ToString()),
                InfoHash = null,
                Seeders = torrent.Seeders,
                Peers = torrent.Leechers + torrent.Seeders,
                MinimumRatio = 1,
                MinimumSeedTime = 172800,
                DownloadVolumeFactor = torrent.DownloadVolumeFactor,
                UploadVolumeFactor = torrent.UploadVolumeFactor,
            }).ToArray();
        }
    }

    public class SpeedAppSettingsValidator : AbstractValidator<SpeedAppSettings>
    {
        public SpeedAppSettingsValidator()
        {
            RuleFor(c => c.Email).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    public class SpeedAppSettings : IIndexerSettings
    {
        private static readonly SpeedAppSettingsValidator Validator = new ();

        public SpeedAppSettings()
        {
            Email = "";
            Password = "";
        }

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Email", HelpText = "Site Email", Privacy = PrivacyLevel.UserName)]
        public string Email { get; set; }

        [FieldDefinition(3, Label = "Password", HelpText = "Site Password", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Password { get; set; }

        [FieldDefinition(4, Label = "API Key", Hidden = HiddenType.Hidden)]
        public string ApiKey { get; set; }

        [FieldDefinition(5)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public class SpeedAppCategory
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class SpeedAppCountry
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("flag_image")]
        public string FlagImage { get; set; }
    }

    public class SpeedAppUploadedBy
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("class")]
        public int Class { get; set; }

        [JsonProperty("avatar")]
        public string Avatar { get; set; }

        [JsonProperty("uploaded")]
        public int Uploaded { get; set; }

        [JsonProperty("downloaded")]
        public int Downloaded { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("country")]
        public SpeedAppCountry Country { get; set; }

        [JsonProperty("passkey")]
        public string Passkey { get; set; }

        [JsonProperty("invites")]
        public int Invites { get; set; }

        [JsonProperty("timezone")]
        public string Timezone { get; set; }

        [JsonProperty("hit_and_run_count")]
        public int HitAndRunCount { get; set; }

        [JsonProperty("snatch_count")]
        public int SnatchCount { get; set; }

        [JsonProperty("need_seed")]
        public int NeedSeed { get; set; }

        [JsonProperty("average_seed_time")]
        public int AverageSeedTime { get; set; }

        [JsonProperty("free_leech_tokens")]
        public int FreeLeechTokens { get; set; }

        [JsonProperty("double_upload_tokens")]
        public int DoubleUploadTokens { get; set; }
    }

    public class SpeedAppTag
    {
        [JsonProperty("translated_name")]
        public string TranslatedName { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("match_list")]
        public List<string> MatchList { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    public class SpeedAppTorrent
    {
        [JsonProperty("download_volume_factor")]
        public float DownloadVolumeFactor { get; set; }

        [JsonProperty("upload_volume_factor")]
        public float UploadVolumeFactor { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("category")]
        public SpeedAppCategory Category { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("times_completed")]
        public int TimesCompleted { get; set; }

        [JsonProperty("leechers")]
        public int Leechers { get; set; }

        [JsonProperty("seeders")]
        public int Seeders { get; set; }

        [JsonProperty("uploaded_by")]
        public SpeedAppUploadedBy UploadedBy { get; set; }

        [JsonProperty("short_description")]
        public string ShortDescription { get; set; }

        [JsonProperty("poster")]
        public string Poster { get; set; }

        [JsonProperty("season")]
        public int Season { get; set; }

        [JsonProperty("episode")]
        public int Episode { get; set; }

        [JsonProperty("tags")]
        public List<SpeedAppTag> Tags { get; set; }

        [JsonProperty("imdb_id")]
        public string ImdbId { get; set; }
    }

    public class SpeedAppAuthenticationRequest
    {
        [JsonProperty("username")]
        public string Email { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }

    public class SpeedAppAuthenticationResponse
    {
        [JsonProperty("token")]
        public string Token { get; set; }
    }
}
