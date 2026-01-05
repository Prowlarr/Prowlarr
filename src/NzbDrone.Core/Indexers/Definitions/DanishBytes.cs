using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
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
    public class DanishBytes : TorrentIndexerBase<DanishBytesSettings>
    {
        public override string Name => "DanishBytes";

        public override string[] IndexerUrls => new[]
        {
            "https://danishbytes.club/",
            "https://danishbytes2.org/",
            "https://dbytes.org/"
        };
        public override string[] LegacyUrls => new[] { "https://danishbytes.org", "https://danishbytes.art" };

        public override string Description => "DanishBytes is a Private Danish Torrent Tracker for MOVIES / TV / GENERAL";
        public override string Language => "da-DK";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override bool SupportsPagination => true;
        public override int PageSize => 100;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public DanishBytes(IIndexerHttpClient httpClient,
                           IEventAggregator eventAggregator,
                           IIndexerStatusService indexerStatusService,
                           IConfigService configService,
                           Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new DanishBytesRequestGenerator(Settings, Capabilities, PageSize);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new DanishBytesParser(Settings, Capabilities.Categories);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                LimitsDefault = PageSize,
                LimitsMax = PageSize,
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId, TvSearchParam.TvdbId, TvSearchParam.TmdbId
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId
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
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TV, "TV");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.PC0day, "Apps");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.PCGames, "Games");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.Audio, "Music");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.Books, "Books");
            caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.AudioAudiobook, "AudioBooks");
            caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.AudioOther, "Podcasts");

            return caps;
        }
    }

    public class DanishBytesRequestGenerator : IIndexerRequestGenerator
    {
        private readonly DanishBytesSettings _settings;
        private readonly IndexerCapabilities _capabilities;
        private readonly int _pageSize;

        public DanishBytesRequestGenerator(DanishBytesSettings settings, IndexerCapabilities capabilities, int pageSize)
        {
            _settings = settings;
            _capabilities = capabilities;
            _pageSize = pageSize;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var imdbId = ParseUtil.GetImdbId(searchCriteria.FullImdbId)?.ToString();
            var tmdbId = searchCriteria.TmdbId.GetValueOrDefault();

            return BuildRequestChain(searchCriteria, searchCriteria.SanitizedSearchTerm, imdbId, tmdbId: tmdbId);
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return BuildRequestChain(searchCriteria, searchCriteria.SanitizedSearchTerm);
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var imdbId = ParseUtil.GetImdbId(searchCriteria.FullImdbId)?.ToString();
            var tmdbId = searchCriteria.TmdbId.GetValueOrDefault();
            var tvdbId = searchCriteria.TvdbId.GetValueOrDefault();

            return BuildRequestChain(searchCriteria, searchCriteria.SanitizedTvSearchString, imdbId, tmdbId, tvdbId);
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return BuildRequestChain(searchCriteria, searchCriteria.SanitizedSearchTerm);
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            return BuildRequestChain(searchCriteria, searchCriteria.SanitizedSearchTerm);
        }

        private IndexerPageableRequestChain BuildRequestChain(SearchCriteriaBase searchCriteria, string searchTerm, string imdbId = null, int tmdbId = 0, int tvdbId = 0)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(searchCriteria, searchTerm, imdbId, tmdbId, tvdbId));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(SearchCriteriaBase searchCriteria, string searchTerm, string imdbId, int tmdbId, int tvdbId)
        {
            var categories = _capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories);

            var page = 1;
            var pageSize = Math.Min(_pageSize, searchCriteria.Limit.GetValueOrDefault(_pageSize));

            if (searchCriteria.Limit is > 0 && searchCriteria.Offset is > 0)
            {
                page = (int)(searchCriteria.Offset / searchCriteria.Limit) + 1;
            }

            var parameters = new NameValueCollection
            {
                { "qty", pageSize.ToString() },
                { "page", page.ToString() },
                { "sorting", MapSort(_settings.Sort) },
                { "direction", MapDirection(_settings.SortDirection) }
            };

            if (searchTerm.IsNotNullOrWhiteSpace())
            {
                parameters.Set("search", searchTerm.Trim());
            }

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                parameters.Set("imdb", imdbId);
            }

            if (tmdbId > 0)
            {
                parameters.Set("tmdb", tmdbId.ToString());
            }

            if (tvdbId > 0)
            {
                parameters.Set("tvdb", tvdbId.ToString());
            }

            if (_settings.FreeleechOnly)
            {
                parameters.Set("freeleech", "1");
            }

            foreach (var cat in categories)
            {
                parameters.Add("categories[]", cat);
            }

            var searchUrl = $"{_settings.BaseUrl.TrimEnd('/')}/api/torrents/v2/filter?{parameters.GetQueryString()}";

            var request = new IndexerRequest(searchUrl, HttpAccept.Json);
            request.HttpRequest.Headers.Add("Authorization", $"Bearer {_settings.ApiKey}");
            request.HttpRequest.SuppressHttpErrorStatusCodes = new[] { HttpStatusCode.NotFound };

            yield return request;
        }

        private static string MapSort(DanishBytesSort sort)
        {
            return sort switch
            {
                DanishBytesSort.Seeders => "seeders",
                DanishBytesSort.Size => "size",
                DanishBytesSort.Title => "name",
                _ => "created_at"
            };
        }

        private static string MapDirection(DanishBytesSortDirection direction)
        {
            return direction == DanishBytesSortDirection.Asc ? "asc" : "desc";
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class DanishBytesParser : IParseIndexerResponse
    {
        // Tracker language codes are not always standard ISO, so we map the known ones explicitly.
        private static readonly Dictionary<string, string> LanguageMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "dk", "Danish" },
            { "no", "Norwegian" },
            { "se", "Swedish" },
            { "fi", "Finnish" },
            { "is", "Icelandic" },
            { "fo", "Faroese" },
            { "gb", "English" },
            { "jp", "Japanese" },
            { "fr", "French" },
            { "es", "Spanish" },
            { "de", "German" },
            { "it", "Italian" },
            { "kr", "Korean" },
            { "po", "Polish" },
            { "ru", "Russian" },
            { "cn", "Chinese" },
            { "pt", "Portuguese" },
            { "nl", "Dutch" },
            { "tu", "Turkish" },
            { "ae", "Arabic" },
            { "in", "Hindi" },
            { "th", "Thai" },
            { "hu", "Hungarian" },
            { "gr", "Greek" },
            { "ua", "Ukrainian" },
            { "id", "Indonesian" },
            { "ro", "Romanian" },
            { "vi", "Vietnamese" }
        };

        private readonly DanishBytesSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public DanishBytesParser(DanishBytesSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            if (indexerResponse.HttpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return Array.Empty<ReleaseInfo>();
            }

            if (indexerResponse.HttpResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new IndexerAuthException("The API key was not accepted by the tracker.");
            }

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from indexer request");
            }

            if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from indexer request, expected {HttpAccept.Json.Value}");
            }

            var jsonResponse = STJson.Deserialize<DanishBytesResponse>(indexerResponse.HttpResponse.Content);

            if (jsonResponse?.Torrents == null)
            {
                return Array.Empty<ReleaseInfo>();
            }

            var baseUrl = _settings.BaseUrl.TrimEnd('/');

            var releases = new List<ReleaseInfo>();

            foreach (var row in jsonResponse.Torrents)
            {
                var details = $"{baseUrl}/torrents/{row.Id}";
                var download = $"{baseUrl}/torrent/download/{row.Id}.{_settings.RssKey}";

                var release = new TorrentInfo
                {
                    Guid = details,
                    InfoUrl = details,
                    DownloadUrl = download,
                    Title = row.Name,
                    Size = row.Size,
                    InfoHash = row.InfoHash,
                    Categories = _categories.MapTrackerCatToNewznab(row.CategoryId?.ToString()) ?? new List<IndexerCategory>(),
                    PublishDate = ParseDate(row.CreatedAt),
                    Seeders = row.Seeders,
                    Peers = (row.Seeders ?? 0) + (row.Leechers ?? 0),
                    Grabs = row.TimesCompleted,
                    DownloadVolumeFactor = row.Free == 1 ? 0 : 1,
                    UploadVolumeFactor = row.DoubleUp == 1 ? 2 : 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800,
                    PosterUrl = row.PosterImage,
                    Languages = MapLanguages(GetLanguageCodes(row.LanguageCodes)),
                    Subs = MapLanguages(GetLanguageCodes(row.SubtitleCodes))
                };

                release.ImdbId = ParseUtil.GetImdbId(row.Imdb).GetValueOrDefault();

                if (ParseUtil.TryCoerceInt(row.Tmdb, out var tmdbId))
                {
                    release.TmdbId = tmdbId;
                }

                if (ParseUtil.TryCoerceInt(row.Tvdb, out var tvdbId))
                {
                    release.TvdbId = tvdbId;
                }

                releases.Add(release);
            }

            return releases
                .OrderByDescending(o => o.PublishDate)
                .ToArray();
        }

        private static DateTime ParseDate(string date)
        {
            if (DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsedDate))
            {
                return parsedDate;
            }

            return DateTime.UtcNow;
        }

        private static IEnumerable<string> GetLanguageCodes(JsonElement element)
        {
            if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                yield break;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.Array:
                    // API normally sends a simple array of codes
                    foreach (var codeElement in element.EnumerateArray())
                    {
                        var code = GetElementString(codeElement);

                        if (code.IsNotNullOrWhiteSpace())
                        {
                            yield return code;
                        }
                    }

                    break;
                case JsonValueKind.Object:
                    // API sometimes returns an object keyed by index instead of an array
                    foreach (var property in element.EnumerateObject())
                    {
                        var code = GetElementString(property.Value) ?? property.Name;

                        if (code.IsNotNullOrWhiteSpace())
                        {
                            yield return code;
                        }
                    }

                    break;
                case JsonValueKind.String:
                    var value = element.GetString();
                    if (value.IsNotNullOrWhiteSpace())
                    {
                        yield return value;
                    }

                    break;
            }
        }

        private static string GetElementString(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };
        }

        private static List<string> MapLanguages(IEnumerable<string> codes)
        {
            return codes?
                .Where(code => code.IsNotNullOrWhiteSpace() && !code.Equals("xx", StringComparison.OrdinalIgnoreCase)) // "xx" means no subtitles
                .Select(code => LanguageMap.TryGetValue(code.ToLowerInvariant(), out var language) ? language : code.ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class DanishBytesSettingsValidator : NoAuthSettingsValidator<DanishBytesSettings>
    {
        public DanishBytesSettingsValidator()
        {
            RuleFor(c => c.ApiKey).NotEmpty();
            RuleFor(c => c.RssKey).NotEmpty();
        }
    }

    public class DanishBytesSettings : NoAuthTorrentBaseSettings
    {
        private static readonly DanishBytesSettingsValidator Validator = new();

        public DanishBytesSettings()
        {
            BaseUrl = "https://danishbytes.club/";
            FreeleechOnly = false;
            Sort = DanishBytesSort.CreatedAt;
            SortDirection = DanishBytesSortDirection.Desc;
        }

        [FieldDefinition(2, Label = "APIKey", Privacy = PrivacyLevel.ApiKey, HelpText = "Find or Generate a new API Token by accessing your DanishBytes account 'My Security' page and clicking on the 'API Token' tab.", HelpTextWarning = "If you have not logged in for 90 days, the user is automatically deleted.")]
        public string ApiKey { get; set; }

        [FieldDefinition(3, Label = "RSSKey", Privacy = PrivacyLevel.ApiKey, HelpText = "Find or Generate a new RSS key by accessing your DanishBytes account 'My Security' page and clicking on the 'RSS Key (RID)' tab.")]
        public string RssKey { get; set; }

        [FieldDefinition(4, Label = "Search freeleech only", Type = FieldType.Checkbox)]
        public bool FreeleechOnly { get; set; }

        [FieldDefinition(5, Label = "Sort requested from site", Type = FieldType.Select, SelectOptions = typeof(DanishBytesSort))]
        public DanishBytesSort Sort { get; set; }

        [FieldDefinition(6, Label = "Order requested from site", Type = FieldType.Select, SelectOptions = typeof(DanishBytesSortDirection))]
        public DanishBytesSortDirection SortDirection { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public enum DanishBytesSort
    {
        [FieldOption(Label = "Created")]
        CreatedAt,

        [FieldOption(Label = "Seeders")]
        Seeders,

        [FieldOption(Label = "Size")]
        Size,

        [FieldOption(Label = "Title")]
        Title
    }

    public enum DanishBytesSortDirection
    {
        [FieldOption(Label = "Descending")]
        Desc,

        [FieldOption(Label = "Ascending")]
        Asc
    }

    public class DanishBytesResponse
    {
        [JsonPropertyName("torrents")]
        public List<DanishBytesTorrent> Torrents { get; set; }

        [JsonPropertyName("resultsCount")]
        public int ResultsCount { get; set; }
    }

    public class DanishBytesTorrent
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("size")]
        public long? Size { get; set; }

        [JsonPropertyName("seeders")]
        public int? Seeders { get; set; }

        [JsonPropertyName("leechers")]
        public int? Leechers { get; set; }

        [JsonPropertyName("times_completed")]
        public int? TimesCompleted { get; set; }

        [JsonPropertyName("info_hash")]
        public string InfoHash { get; set; }

        [JsonPropertyName("category_id")]
        public int? CategoryId { get; set; }

        [JsonPropertyName("imdb")]
        public string Imdb { get; set; }

        [JsonPropertyName("tmdb")]
        public string Tmdb { get; set; }

        [JsonPropertyName("tvdb")]
        public string Tvdb { get; set; }

        [JsonPropertyName("free")]
        public int? Free { get; set; }

        [JsonPropertyName("doubleup")]
        public int? DoubleUp { get; set; }

        [JsonPropertyName("poster_image")]
        public string PosterImage { get; set; }

        [JsonPropertyName("language_codes")]
        public JsonElement LanguageCodes { get; set; }

        [JsonPropertyName("language_codes_subs")]
        public JsonElement SubtitleCodes { get; set; }
    }
}
