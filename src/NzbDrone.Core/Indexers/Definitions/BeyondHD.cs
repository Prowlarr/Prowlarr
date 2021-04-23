using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using FluentValidation;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
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
    public class BeyondHD : HttpIndexerBase<BeyondHDSettings>
    {
        public override string Name => "BeyondHD";

        public override string BaseUrl => "https://beyond-hd.me/";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public BeyondHD(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new BeyondHDRequestGenerator() { Settings = Settings, Capabilities = Capabilities, BaseUrl = BaseUrl };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new BeyondHDParser(Settings, Capabilities.Categories, BaseUrl);
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
                       }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Movies, "Movies");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TV, "TV");

            return caps;
        }
    }

    public class BeyondHDRequestGenerator : IIndexerRequestGenerator
    {
        public BeyondHDSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }
        public string BaseUrl { get; set; }

        public BeyondHDRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null, int tmdbId = 0)
        {
            var body = new Dictionary<string, object>
            {
                { "action", "search" },
                { "rsskey", Settings.RssKey }
            };

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                body.Add("imdb_id", imdbId);
            }

            if (tmdbId > 0)
            {
                body.Add("tmdb_id", tmdbId);
            }

            if (term.IsNotNullOrWhiteSpace())
            {
                body.Add("search", term);
            }

            var cats = Capabilities.Categories.MapTorznabCapsToTrackers(categories);

            if (cats.Count > 0)
            {
                body.Add("categories", string.Join(",", cats));
            }

            var searchUrl = BaseUrl + "api/torrents/" + Settings.ApiKey;

            var request = new HttpRequest(searchUrl, HttpAccept.Json);

            request.Headers.Add("Content-type", "application/json");
            request.Method = HttpMethod.POST;
            request.SetContent(body.ToJson());

            var indexerRequest = new IndexerRequest(request);

            yield return indexerRequest;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories, searchCriteria.ImdbId, searchCriteria.TmdbId.GetValueOrDefault()));

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

    public class BeyondHDParser : IParseIndexerResponse
    {
        private readonly BeyondHDSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        private readonly string _baseUrl;

        public BeyondHDParser(BeyondHDSettings settings, IndexerCapabilitiesCategories categories, string baseUrl)
        {
            _settings = settings;
            _categories = categories;
            _baseUrl = baseUrl;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from API request");
            }

            // TODO Have BHD fix their API response content type so we can proper check here
            // if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            // {
            //     throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from API request, expected {HttpAccept.Json.Value}");
            // }
            var jsonResponse = new HttpResponse<BeyondHDResponse>(indexerResponse.HttpResponse);

            foreach (var row in jsonResponse.Resource.Results)
            {
                var details = row.InfoUrl;
                var link = row.DownloadLink;

                var release = new TorrentInfo
                {
                    Title = row.Name,
                    DownloadUrl = link,
                    InfoHash = row.InfoHash,
                    InfoUrl = details,
                    Guid = details,
                    Category = _categories.MapTrackerCatDescToNewznab(row.Category),
                    PublishDate = DateTime.Parse(row.CreatedAt, CultureInfo.InvariantCulture),
                    Size = row.Size,
                    Grabs = row.Grabs,
                    Seeders = row.Seeders,
                    ImdbId = ParseUtil.GetImdbID(row.ImdbId).GetValueOrDefault(),
                    TmdbId = row.TmdbId.IsNullOrWhiteSpace() ? 0 : ParseUtil.CoerceInt(row.TmdbId.Split("/")[1]),
                    Peers = row.Leechers + row.Seeders,
                    DownloadVolumeFactor = row.Freeleech ? 0 : row.Promo75 ? 0.25 : row.Promo50 ? 0.5 : row.Promo25 ? 0.75 : 1,
                    UploadVolumeFactor = 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800, // 48 hours
                };

                torrentInfos.Add(release);
            }

            // order by date
            return torrentInfos.OrderByDescending(o => o.PublishDate).ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class BeyondHDSettingsValidator : AbstractValidator<BeyondHDSettings>
    {
        public BeyondHDSettingsValidator()
        {
            RuleFor(c => c.ApiKey).NotEmpty();
            RuleFor(c => c.RssKey).NotEmpty();
        }
    }

    public class BeyondHDSettings : IProviderConfig
    {
        private static readonly BeyondHDSettingsValidator Validator = new BeyondHDSettingsValidator();

        public BeyondHDSettings()
        {
        }

        [FieldDefinition(1, Label = "API Key", HelpText = "API Key from Site", Privacy = PrivacyLevel.ApiKey)]
        public string ApiKey { get; set; }

        [FieldDefinition(2, Label = "RSS Key", HelpText = "RSS Key from Site", Privacy = PrivacyLevel.ApiKey)]
        public string RssKey { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public class BeyondHDResponse
    {
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
    }
}
