using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using FluentValidation;
using Newtonsoft.Json;
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
    public class BeyondHD : TorrentIndexerBase<BeyondHDSettings>
    {
        public override string Name => "BeyondHD";
        public override string[] IndexerUrls => new[] { "https://beyond-hd.me/" };
        public override string Description => "BeyondHD (BHD) is a Private Torrent Tracker for HD MOVIES / TV";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
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
            return new BeyondHDParser(Capabilities.Categories);
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
                    MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId
                },
                Flags = new List<IndexerFlag>
                {
                    IndexerFlag.Internal
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

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null, int tmdbId = 0)
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

            var cats = _capabilities.Categories.MapTorznabCapsToTrackers(categories);

            if (cats.Count > 0)
            {
                body.Add("categories", string.Join(",", cats));
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
            request.ContentSummary = body.ToJson(Formatting.None);

            yield return new IndexerRequest(request);
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories, searchCriteria.FullImdbId, searchCriteria.TmdbId.GetValueOrDefault()));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(searchCriteria.SanitizedTvSearchString, searchCriteria.Categories, searchCriteria.FullImdbId));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class BeyondHDParser : IParseIndexerResponse
    {
        private readonly IndexerCapabilitiesCategories _categories;

        public BeyondHDParser(IndexerCapabilitiesCategories categories)
        {
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

            var jsonResponse = new HttpResponse<BeyondHDResponse>(indexerHttpResponse);

            if (jsonResponse.Resource.StatusCode == 0)
            {
                throw new IndexerException(indexerResponse, $"Indexer Error: {jsonResponse.Resource.StatusMessage}");
            }

            var releaseInfos = new List<ReleaseInfo>();

            foreach (var row in jsonResponse.Resource.Results)
            {
                var details = row.InfoUrl;
                var link = row.DownloadLink;

                // BHD can return crazy values for tmdb
                var tmdbId = row.TmdbId.IsNullOrWhiteSpace() ? 0 : ParseUtil.TryCoerceInt(row.TmdbId.Split("/")[1], out var tmdbResult) ? tmdbResult : 0;
                var imdbId = ParseUtil.GetImdbId(row.ImdbId).GetValueOrDefault();

                var flags = new HashSet<IndexerFlag>();

                if (row.Internal)
                {
                    flags.Add(IndexerFlag.Internal);
                }

                var release = new TorrentInfo
                {
                    Title = row.Name,
                    DownloadUrl = link,
                    InfoHash = row.InfoHash,
                    InfoUrl = details,
                    Guid = details,
                    Categories = _categories.MapTrackerCatDescToNewznab(row.Category),
                    PublishDate = DateTime.Parse(row.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                    IndexerFlags = flags,
                    Size = row.Size,
                    Grabs = row.Grabs,
                    Seeders = row.Seeders,
                    ImdbId = imdbId,
                    TmdbId = tmdbId,
                    Peers = row.Leechers + row.Seeders,
                    DownloadVolumeFactor = row.Freeleech || row.Limited ? 0 : row.Promo75 ? 0.25 : row.Promo50 ? 0.5 : row.Promo25 ? 0.75 : 1,
                    UploadVolumeFactor = 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800, // 120 hours
                };

                releaseInfos.Add(release);
            }

            // order by date
            return releaseInfos
                .OrderByDescending(o => o.PublishDate)
                .ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class BeyondHDSettingsValidator : NoAuthSettingsValidator<BeyondHDSettings>
    {
        public BeyondHDSettingsValidator()
        {
            RuleFor(c => c.ApiKey).NotEmpty();
            RuleFor(c => c.RssKey).NotEmpty();
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
        }

        [FieldDefinition(2, Label = "API Key", HelpText = "API Key from the Site (Found in My Security => API Key)", Privacy = PrivacyLevel.ApiKey)]
        public string ApiKey { get; set; }

        [FieldDefinition(3, Label = "RSS Key", HelpText = "RSS Key from the Site (Found in My Security => RSS Key)", Privacy = PrivacyLevel.ApiKey)]
        public string RssKey { get; set; }

        [FieldDefinition(4, Label = "Freeleech Only", Type = FieldType.Checkbox, HelpText = "Search freeleech only")]
        public bool FreeleechOnly { get; set; }

        [FieldDefinition(5, Label = "Limited Only", Type = FieldType.Checkbox, HelpText = "Search freeleech only (Limited UL)")]
        public bool LimitedOnly { get; set; }

        [FieldDefinition(6, Label = "Refund Only", Type = FieldType.Checkbox, HelpText = "Search refund only")]
        public bool RefundOnly { get; set; }

        [FieldDefinition(7, Label = "Rewind Only", Type = FieldType.Checkbox, HelpText = "Search rewind only")]
        public bool RewindOnly { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public class BeyondHDResponse
    {
        [JsonProperty(PropertyName = "status_code")]
        public int StatusCode { get; set; }

        [JsonProperty(PropertyName = "status_message")]
        public string StatusMessage { get; set; }
        public List<BeyondHDTorrent> Results { get; set; }
    }

    public class BeyondHDTorrent
    {
        public string Name { get; set; }

        [JsonProperty(PropertyName = "info_hash")]
        public string InfoHash { get; set; }
        public string Category { get; set; }
        public string Type { get; set; }
        public long Size { get; set; }

        [JsonProperty(PropertyName = "times_completed")]
        public int Grabs { get; set; }
        public int Seeders { get; set; }
        public int Leechers { get; set; }

        [JsonProperty(PropertyName = "created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty(PropertyName = "download_url")]
        public string DownloadLink { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string InfoUrl { get; set; }

        [JsonProperty(PropertyName = "imdb_id")]
        public string ImdbId { get; set; }

        [JsonProperty(PropertyName = "tmdb_id")]
        public string TmdbId { get; set; }
        public bool Freeleech { get; set; }
        public bool Promo25 { get; set; }
        public bool Promo50 { get; set; }
        public bool Promo75 { get; set; }
        public bool Limited { get; set; }
        public bool Internal { get; set; }
    }
}
