using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
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
using static Newtonsoft.Json.Formatting;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class BeyondHD : TorrentIndexerBase<BeyondHDSettings>
    {
        public override string Name => "BeyondHD";
        public override string[] IndexerUrls => new[] { "https://beyond-hd.me/" };
        public override string Description => "BeyondHD (BHD) is a Private Torrent Tracker for HD MOVIES / TV";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override bool SupportsPagination => true;
        public override int PageSize => 100;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public BeyondHD(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new BeyondHDRequestGenerator(Settings, Capabilities);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new BeyondHDParser(Settings, Capabilities.Categories);
        }

        protected override IList<ReleaseInfo> CleanupReleases(IEnumerable<ReleaseInfo> releases, SearchCriteriaBase searchCriteria)
        {
            var cleanReleases = base.CleanupReleases(releases, searchCriteria);

            return FilterReleasesByQuery(cleanReleases, searchCriteria).ToList();
        }

        private static IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId
                },
                Flags = new List<IndexerFlag>
                {
                    IndexerFlag.Internal,
                    IndexerFlag.Exclusive,
                }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Movies, "Movies");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TV, "TV");

            return caps;
        }
    }

    public class BeyondHDRequestGenerator : IIndexerRequestGenerator
    {
        private readonly BeyondHDSettings _settings;
        private readonly IndexerCapabilities _capabilities;

        public BeyondHDRequestGenerator(BeyondHDSettings settings, IndexerCapabilities capabilities)
        {
            _settings = settings;
            _capabilities = capabilities;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(SearchCriteriaBase searchCriteria, string term, string imdbId = null, int tmdbId = 0)
        {
            var body = new Dictionary<string, object>
            {
                { "action", "search" },
                { "rsskey", _settings.RssKey }
            };

            if (_settings.FreeleechOnly)
            {
                body.Add("freeleech", 1);
            }

            if (_settings.LimitedOnly)
            {
                body.Add("limited", 1);
            }

            if (_settings.RefundOnly)
            {
                body.Add("refund", 1);
            }

            if (_settings.RewindOnly)
            {
                body.Add("rewind", 1);
            }

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                body.Add("imdb_id", imdbId);
            }
            else if (tmdbId > 0)
            {
                body.Add("tmdb_id", tmdbId);
            }

            if (term.IsNotNullOrWhiteSpace())
            {
                body.Add("search", term);
            }

            var cats = _capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories);

            if (cats.Count > 0)
            {
                body.Add("categories", cats.Select(int.Parse).ToArray());
            }

            if (_settings.SearchTypes.Any())
            {
                var searchTypes = _settings.SearchTypes
                    .Cast<BeyondHDSearchType>()
                    .Select(x => x.GetAttribute<FieldOptionAttribute>()?.Label)
                    .Where(x => x != null)
                    .ToHashSet();

                if (searchTypes.Any())
                {
                    body.Add("types", searchTypes.ToArray());
                }
            }

            if (searchCriteria.Limit is > 0 && searchCriteria.Offset is > 0)
            {
                var page = (int)(searchCriteria.Offset / searchCriteria.Limit) + 1;
                body.Add("page", page);
            }

            var searchUrl = $"{_settings.BaseUrl}api/torrents/{_settings.ApiKey}";

            var request = new HttpRequest(searchUrl, HttpAccept.Json)
            {
                Headers =
                {
                    ContentType = "application/json"
                },
                Method = HttpMethod.Post
            };
            request.SetContent(body.ToJson());
            request.ContentSummary = body.ToJson(None);

            yield return new IndexerRequest(request);
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(searchCriteria, searchCriteria.SanitizedSearchTerm, searchCriteria.FullImdbId, searchCriteria.TmdbId.GetValueOrDefault()));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(searchCriteria, searchCriteria.SanitizedSearchTerm));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(searchCriteria, searchCriteria.SanitizedTvSearchString, searchCriteria.FullImdbId));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(searchCriteria, searchCriteria.SanitizedSearchTerm));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(searchCriteria, searchCriteria.SanitizedSearchTerm));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class BeyondHDParser : IParseIndexerResponse
    {
        private readonly BeyondHDSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public BeyondHDParser(BeyondHDSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var indexerHttpResponse = indexerResponse.HttpResponse;

            if (indexerHttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerHttpResponse.StatusCode} code from indexer request");
            }

            if (!indexerHttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerHttpResponse.Headers.ContentType} from indexer request, expected {HttpAccept.Json.Value}");
            }

            if (indexerResponse.Content.ContainsIgnoreCase("Invalid API Key"))
            {
                throw new IndexerAuthException("API Key invalid or not authorized");
            }

            var jsonResponse = STJson.Deserialize<BeyondHDResponse>(indexerResponse.Content);

            if (jsonResponse.StatusCode == 0)
            {
                throw new IndexerException(indexerResponse, $"Indexer Error: {jsonResponse.StatusMessage}");
            }

            var releaseInfos = new List<ReleaseInfo>();

            foreach (var row in jsonResponse.Results)
            {
                // Skip invalid results when freeleech or limited filtering is set
                if ((_settings.FreeleechOnly && !row.Freeleech) || (_settings.LimitedOnly && !row.Limited))
                {
                    continue;
                }

                var details = row.InfoUrl;
                var link = row.DownloadLink;

                var release = new TorrentInfo
                {
                    Title = row.Name,
                    DownloadUrl = link,
                    InfoHash = row.InfoHash,
                    InfoUrl = details,
                    Guid = details,
                    Categories = _categories.MapTrackerCatDescToNewznab(row.Category),
                    PublishDate = DateTime.Parse(row.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                    IndexerFlags = GetIndexerFlags(row),
                    Size = row.Size,
                    Grabs = row.Grabs,
                    Seeders = row.Seeders,
                    ImdbId = ParseUtil.GetImdbId(row.ImdbId).GetValueOrDefault(),
                    Peers = row.Leechers + row.Seeders,
                    DownloadVolumeFactor = row.Freeleech || row.Limited ? 0 : row.Promo75 ? 0.25 : row.Promo50 ? 0.5 : row.Promo25 ? 0.75 : 1,
                    UploadVolumeFactor = 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800, // 120 hours
                };

                // BHD can return crazy values for tmdb
                if (row.TmdbId.IsNotNullOrWhiteSpace())
                {
                    var tmdbId = row.TmdbId.Split("/").ElementAtOrDefault(1);
                    release.TmdbId = tmdbId != null && ParseUtil.TryCoerceInt(tmdbId, out var tmdbResult) ? tmdbResult : 0;
                }

                releaseInfos.Add(release);
            }

            // order by date
            return releaseInfos
                .OrderByDescending(o => o.PublishDate)
                .ToArray();
        }

        private static HashSet<IndexerFlag> GetIndexerFlags(BeyondHDTorrent item)
        {
            var flags = new HashSet<IndexerFlag>();

            if (item.Internal)
            {
                flags.Add(IndexerFlag.Internal);
            }

            if (item.Exclusive)
            {
                flags.Add(IndexerFlag.Exclusive);
            }

            return flags;
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class BeyondHDSettingsValidator : NoAuthSettingsValidator<BeyondHDSettings>
    {
        public BeyondHDSettingsValidator()
        {
            RuleFor(c => c.ApiKey).NotEmpty().Length(32);
            RuleFor(c => c.RssKey).NotEmpty().Length(32);
        }
    }

    public class BeyondHDSettings : NoAuthTorrentBaseSettings
    {
        private static readonly BeyondHDSettingsValidator Validator = new ();

        public BeyondHDSettings()
        {
            FreeleechOnly = false;
            LimitedOnly = false;
            RefundOnly = false;
            RewindOnly = false;
            SearchTypes = Array.Empty<int>();
        }

        [FieldDefinition(2, Label = "ApiKey", HelpText = "IndexerBeyondHDSettingsApiKeyHelpText", Privacy = PrivacyLevel.ApiKey)]
        public string ApiKey { get; set; }

        [FieldDefinition(3, Label = "IndexerSettingsRssKey", HelpText = "IndexerBeyondHDSettingsRssKeyHelpText", Privacy = PrivacyLevel.ApiKey)]
        public string RssKey { get; set; }

        [FieldDefinition(4, Label = "IndexerSettingsFreeleechOnly", Type = FieldType.Checkbox, HelpText = "IndexerBeyondHDSettingsFreeleechOnlyHelpText")]
        public bool FreeleechOnly { get; set; }

        [FieldDefinition(5, Label = "IndexerBeyondHDSettingsLimitedOnly", Type = FieldType.Checkbox, HelpText = "IndexerBeyondHDSettingsLimitedOnlyHelpText")]
        public bool LimitedOnly { get; set; }

        [FieldDefinition(6, Label = "IndexerBeyondHDSettingsRefundOnly", Type = FieldType.Checkbox, HelpText = "IndexerBeyondHDSettingsRefundOnlyHelpText")]
        public bool RefundOnly { get; set; }

        [FieldDefinition(7, Label = "IndexerBeyondHDSettingsRewindOnly", Type = FieldType.Checkbox, HelpText = "IndexerBeyondHDSettingsRewindOnlyHelpText")]
        public bool RewindOnly { get; set; }

        [FieldDefinition(8, Label = "IndexerBeyondHDSettingsSearchTypes", Type = FieldType.Select, SelectOptions = typeof(BeyondHDSearchType), HelpText = "IndexerBeyondHDSettingsSearchTypesHelpText", Advanced = true)]
        public IEnumerable<int> SearchTypes { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public enum BeyondHDSearchType
    {
        [FieldOption(Label = "UHD 100")]
        TypeUhd100,

        [FieldOption(Label = "UHD 66")]
        TypeUhd66,

        [FieldOption(Label = "UHD 50")]
        TypeUhd50,

        [FieldOption(Label = "UHD Remux")]
        TypeUhdRemux,

        [FieldOption(Label = "BD 50")]
        TypeBd50,

        [FieldOption(Label = "BD 25")]
        TypeBd25,

        [FieldOption(Label = "BD Remux")]
        TypeBdRemux,

        [FieldOption(Label = "2160p")]
        Type2160P,

        [FieldOption(Label = "1080p")]
        Type1080P,

        [FieldOption(Label = "1080i")]
        Type1080I,

        [FieldOption(Label = "720p")]
        Type720P,

        [FieldOption(Label = "576p")]
        Type576P,

        [FieldOption(Label = "540p")]
        Type540P,

        [FieldOption(Label = "DVD 9")]
        TypeDvd9,

        [FieldOption(Label = "DVD 5")]
        TypeDvd5,

        [FieldOption(Label = "DVD Remux")]
        TypeDvdRemux,

        [FieldOption(Label = "480p")]
        Type480P,

        [FieldOption(Label = "Other")]
        TypeOther,
    }

    public class BeyondHDResponse
    {
        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("status_message")]
        public string StatusMessage { get; set; }
        public List<BeyondHDTorrent> Results { get; set; }
    }

    public class BeyondHDTorrent
    {
        public string Name { get; set; }

        [JsonPropertyName("info_hash")]
        public string InfoHash { get; set; }
        public string Category { get; set; }
        public string Type { get; set; }
        public long Size { get; set; }

        [JsonPropertyName("times_completed")]
        public int Grabs { get; set; }
        public int Seeders { get; set; }
        public int Leechers { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }

        [JsonPropertyName("download_url")]
        public string DownloadLink { get; set; }

        [JsonPropertyName("url")]
        public string InfoUrl { get; set; }

        [JsonPropertyName("imdb_id")]
        public string ImdbId { get; set; }

        [JsonPropertyName("tmdb_id")]
        public string TmdbId { get; set; }

        public bool Freeleech { get; set; }

        public bool Promo25 { get; set; }

        public bool Promo50 { get; set; }

        public bool Promo75 { get; set; }

        public bool Limited { get; set; }

        public bool Exclusive { get; set; }

        public bool Internal { get; set; }
    }
}
