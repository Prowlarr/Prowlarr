using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentValidation;
using Newtonsoft.Json;
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
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public abstract class SpeedAppBase : TorrentIndexerBase<SpeedAppSettings>
    {
        private string LoginUrl => Settings.BaseUrl + "api/login";
        public override Encoding Encoding => Encoding.UTF8;
        public override bool SupportsPagination => true;
        public override int PageSize => 100;
        public override IndexerCapabilities Capabilities => SetCapabilities();
        protected virtual int MinimumSeedTime => 172800; // 48 hours
        private IIndexerRepository _indexerRepository;

        public SpeedAppBase(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger, IIndexerRepository indexerRepository)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
            _indexerRepository = indexerRepository;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new SpeedAppRequestGenerator(Capabilities, Settings, PageSize);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new SpeedAppParser(Settings, Capabilities.Categories, MinimumSeedTime);
        }

        protected override IList<ReleaseInfo> CleanupReleases(IEnumerable<ReleaseInfo> releases, SearchCriteriaBase searchCriteria)
        {
            var cleanReleases = base.CleanupReleases(releases, searchCriteria);

            return FilterReleasesByQuery(cleanReleases, searchCriteria).ToList();
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            return Settings.ApiKey.IsNullOrWhiteSpace() || httpResponse.StatusCode == HttpStatusCode.Unauthorized;
        }

        protected override async Task DoLogin()
        {
            var request = new HttpRequestBuilder(LoginUrl)
                {
                    LogResponseContent = true,
                    AllowAutoRedirect = true
                }
                .Post()
                .Build();

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

        protected override Task<HttpRequest> GetDownloadRequest(Uri link)
        {
            var requestBuilder = new HttpRequestBuilder(link.AbsoluteUri)
            {
                AllowAutoRedirect = FollowRedirect
            };

            var request = requestBuilder
                .SetHeader("Authorization", $"Bearer {Settings.ApiKey}")
                .Build();

            return Task.FromResult(request);
        }

        protected virtual IndexerCapabilities SetCapabilities()
        {
            return new IndexerCapabilities();
        }
    }

    public class SpeedAppRequestGenerator : IIndexerRequestGenerator
    {
        private readonly IndexerCapabilities _capabilities;
        private readonly SpeedAppSettings _settings;
        private readonly int _pageSize;

        public Func<IDictionary<string, string>> GetCookies { get; set; }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public SpeedAppRequestGenerator(IndexerCapabilities capabilities, SpeedAppSettings settings, int pageSize)
        {
            _capabilities = capabilities;
            _settings = settings;
            _pageSize = pageSize;
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

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria, imdbId, season, episode));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, SearchCriteriaBase searchCriteria, string imdbId = null, int? season = null, string episode = null)
        {
            var parameters = new NameValueCollection
            {
                { "itemsPerPage", Math.Min(_pageSize, searchCriteria.Limit).ToString() },
                { "sort", "torrent.createdAt" },
                { "direction", "desc" }
            };

            if (searchCriteria.Limit > 0 && searchCriteria.Offset > 0)
            {
                var page = (int)(searchCriteria.Offset / searchCriteria.Limit) + 1;
                parameters.Set("page", page.ToString());
            }

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                parameters.Set("imdbId", imdbId);
            }
            else
            {
                parameters.Set("search", term);
            }

            if (season != null)
            {
                parameters.Set("season", season.Value.ToString());
            }

            if (episode != null)
            {
                parameters.Set("episode", episode);
            }

            var cats = _capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories);
            if (cats.Count > 0)
            {
                foreach (var cat in cats)
                {
                    parameters.Add("categories[]", cat);
                }
            }

            var searchUrl = _settings.BaseUrl + "api/torrent?" + parameters.GetQueryString(duplicateKeysIfMulti: true);

            var request = new IndexerRequest(searchUrl, HttpAccept.Json);
            request.HttpRequest.Headers.Set("Authorization", $"Bearer {_settings.ApiKey}");

            yield return request;
        }
    }

    public class SpeedAppParser : IParseIndexerResponse
    {
        private readonly SpeedAppSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        private readonly int _minimumSeedTime;

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public SpeedAppParser(SpeedAppSettings settings, IndexerCapabilitiesCategories categories, int minimumSeedTime)
        {
            _settings = settings;
            _categories = categories;
            _minimumSeedTime = minimumSeedTime;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from indexer request");
            }

            if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from indexer request, expected {HttpAccept.Json.Value}");
            }

            var jsonResponse = new HttpResponse<List<SpeedAppTorrent>>(indexerResponse.HttpResponse);

            return jsonResponse.Resource.Select(torrent => new TorrentInfo
            {
                Guid = torrent.Id.ToString(),
                Title = CleanTitle(torrent.Name),
                Description = torrent.ShortDescription,
                Size = torrent.Size,
                ImdbId = ParseUtil.GetImdbId(torrent.ImdbId).GetValueOrDefault(),
                DownloadUrl = $"{_settings.BaseUrl}api/torrent/{torrent.Id}/download",
                PosterUrl = torrent.Poster,
                InfoUrl = torrent.Url,
                Grabs = torrent.TimesCompleted,
                PublishDate = torrent.CreatedAt,
                Categories = _categories.MapTrackerCatToNewznab(torrent.Category.Id.ToString()),
                InfoHash = null,
                Seeders = torrent.Seeders,
                Peers = torrent.Leechers + torrent.Seeders,
                MinimumRatio = 1,
                MinimumSeedTime = _minimumSeedTime,
                DownloadVolumeFactor = torrent.DownloadVolumeFactor,
                UploadVolumeFactor = torrent.UploadVolumeFactor,
            }).ToArray();
        }

        private static string CleanTitle(string title)
        {
            title = Regex.Replace(title, @"\[REQUEST(ED)?\]", string.Empty, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            return title.Trim(' ', '.');
        }
    }

    public class SpeedAppSettingsValidator : NoAuthSettingsValidator<SpeedAppSettings>
    {
        public SpeedAppSettingsValidator()
        {
            RuleFor(c => c.Email).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    public class SpeedAppSettings : NoAuthTorrentBaseSettings
    {
        private static readonly SpeedAppSettingsValidator Validator = new ();

        public SpeedAppSettings()
        {
            Email = "";
            Password = "";
        }

        [FieldDefinition(2, Label = "Email", HelpText = "Site Email", Privacy = PrivacyLevel.UserName)]
        public string Email { get; set; }

        [FieldDefinition(3, Label = "Password", HelpText = "Site Password", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Password { get; set; }

        [FieldDefinition(4, Label = "ApiKey", Hidden = HiddenType.Hidden)]
        public string ApiKey { get; set; }

        public override NzbDroneValidationResult Validate()
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
