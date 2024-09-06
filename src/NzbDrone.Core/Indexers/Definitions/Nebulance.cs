using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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

namespace NzbDrone.Core.Indexers.Definitions
{
    public class Nebulance : TorrentIndexerBase<NebulanceSettings>
    {
        public override string Name => "Nebulance";
        public override string[] IndexerUrls => new[] { "https://nebulance.io/" };
        public override string Description => "Nebulance (NBL) is a ratioless Private Torrent Tracker for TV";
        public override string Language => "en-US";
        public override Encoding Encoding => Encoding.UTF8;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override bool SupportsRedirect => true;
        public override bool SupportsPagination => true;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public Nebulance(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new NebulanceRequestGenerator(Settings, _logger);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new NebulanceParser(Settings);
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            return false;
        }

        protected override Task<HttpRequest> GetDownloadRequest(Uri link)
        {
            // Avoid using cookies to prevent redirects to login page
            var requestBuilder = new HttpRequestBuilder(link.AbsoluteUri)
            {
                AllowAutoRedirect = FollowRedirect
            };

            var request = requestBuilder.Build();

            return Task.FromResult(request);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId, TvSearchParam.TvMazeId
                },
                SupportsRawSearch = true
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.TV);
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVSD);
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.TVHD);
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.TVUHD);

            return caps;
        }
    }

    public class NebulanceRequestGenerator : IIndexerRequestGenerator
    {
        private readonly NebulanceSettings _settings;
        private readonly Logger _logger;

        public NebulanceRequestGenerator(NebulanceSettings settings, Logger logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var queryParams = new NebulanceQuery
            {
                Age = ">0"
            };

            if (searchCriteria.TvMazeId is > 0)
            {
                queryParams.TvMaze = searchCriteria.TvMazeId.Value;
            }
            else if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace())
            {
                queryParams.Imdb = searchCriteria.FullImdbId;
            }

            var searchQuery = searchCriteria.SanitizedSearchTerm.Trim();

            if (searchQuery.IsNotNullOrWhiteSpace())
            {
                queryParams.Release = searchQuery;
            }

            if (searchCriteria.Season.HasValue &&
                searchCriteria.Episode.IsNotNullOrWhiteSpace() &&
                DateTime.TryParseExact($"{searchCriteria.Season} {searchCriteria.Episode}", "yyyy MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var showDate))
            {
                if (searchQuery.IsNotNullOrWhiteSpace())
                {
                    queryParams.Name = searchQuery;
                }

                queryParams.Release = showDate.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture);
            }
            else
            {
                if (searchCriteria.Season.HasValue)
                {
                    queryParams.Season = searchCriteria.Season.Value;
                }

                if (searchCriteria.Episode.IsNotNullOrWhiteSpace() && int.TryParse(searchCriteria.Episode, out var episodeNumber))
                {
                    queryParams.Episode = episodeNumber;
                }
            }

            if ((queryParams.Season.HasValue || queryParams.Episode.HasValue) &&
                queryParams.Name.IsNullOrWhiteSpace() &&
                queryParams.Release.IsNullOrWhiteSpace() &&
                !queryParams.TvMaze.HasValue &&
                queryParams.Imdb.IsNullOrWhiteSpace())
            {
                _logger.Debug("NBL API does not support season calls without name, series, id, imdb, tvmaze, or time keys.");

                return new IndexerPageableRequestChain();
            }

            if (queryParams.Name is { Length: > 0 and < 3 } || queryParams.Release is { Length: > 0 and < 3 })
            {
                _logger.Debug("NBL API does not support release calls that are 2 characters or fewer.");

                return new IndexerPageableRequestChain();
            }

            pageableRequests.Add(GetPagedRequests(queryParams, searchCriteria.Limit, searchCriteria.Offset));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var queryParams = new NebulanceQuery
            {
                Age = ">0"
            };

            var searchQuery = searchCriteria.SanitizedSearchTerm.Trim();

            if (searchQuery.IsNotNullOrWhiteSpace())
            {
                queryParams.Release = searchQuery;
            }

            if (queryParams.Release is { Length: > 0 and < 3 })
            {
                _logger.Debug("NBL API does not support release calls that are 2 characters or fewer.");

                return new IndexerPageableRequestChain();
            }

            pageableRequests.Add(GetPagedRequests(queryParams, searchCriteria.Limit, searchCriteria.Offset));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(NebulanceQuery parameters, int? results, int? offset)
        {
            var apiUrl = _settings.BaseUrl + "api.php";

            var builder = new JsonRpcRequestBuilder(apiUrl)
                .Call("getTorrents", _settings.ApiKey, parameters, results ?? 100, offset ?? 0);

            builder.SuppressHttpError = true;

            yield return new IndexerRequest(builder.Build());
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class NebulanceParser : IParseIndexerResponse
    {
        private readonly NebulanceSettings _settings;

        public NebulanceParser(NebulanceSettings settings)
        {
            _settings = settings;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var responseCode = indexerResponse.HttpResponse.StatusCode;

            switch (responseCode)
            {
                case HttpStatusCode.OK:
                    break;
                case HttpStatusCode.Forbidden:
                    throw new IndexerAuthException("You do not meet the login requirements: VPN with 2FA or ISP in home country.");
                default:
                    throw new IndexerException(indexerResponse, "Unexpected response status '{0}' code from indexer request", responseCode);
            }

            JsonRpcResponse<NebulanceResponse> jsonResponse;

            try
            {
                jsonResponse = STJson.Deserialize<JsonRpcResponse<NebulanceResponse>>(indexerResponse.HttpResponse.Content);
            }
            catch (Exception ex)
            {
                STJson.TryDeserialize<JsonRpcResponse<string>>(indexerResponse.HttpResponse.Content, out var response);

                throw new IndexerException(indexerResponse, "Unexpected JSON response from indexer request: {0}", ex, response?.Result ?? ex.Message);
            }

            if (jsonResponse.Error != null || jsonResponse.Result == null)
            {
                throw new IndexerException(indexerResponse, "Indexer API call returned an error [{0}]", jsonResponse.Error);
            }

            if (jsonResponse.Result?.Items == null || jsonResponse.Result.Items.Count == 0)
            {
                return torrentInfos;
            }

            var rows = jsonResponse.Result.Items;

            foreach (var row in rows)
            {
                var details = _settings.BaseUrl + "torrents.php?id=" + row.TorrentId;

                var title = row.ReleaseTitle.IsNotNullOrWhiteSpace() ? row.ReleaseTitle : row.GroupName;

                var release = new TorrentInfo
                {
                    Guid = details,
                    InfoUrl = details,
                    DownloadUrl = row.Download,
                    Title = title.Trim(),
                    Categories = new List<IndexerCategory> { TvCategoryFromQualityParser.ParseTvShowQuality(row.ReleaseTitle) },
                    Size = ParseUtil.CoerceLong(row.Size),
                    Files = row.FileList.Count(),
                    PublishDate = DateTime.Parse(row.PublishDateUtc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                    Grabs = ParseUtil.CoerceInt(row.Snatch),
                    Seeders = ParseUtil.CoerceInt(row.Seed),
                    Peers = ParseUtil.CoerceInt(row.Seed) + ParseUtil.CoerceInt(row.Leech),
                    Scene = row.Tags?.ContainsIgnoreCase("scene"),
                    MinimumRatio = 0, // ratioless
                    MinimumSeedTime = row.Category.ToLower() == "season" ? 432000 : 86400, // 120 hours for seasons and 24 hours for episodes
                    DownloadVolumeFactor = 0, // ratioless tracker
                    UploadVolumeFactor = 1,
                    PosterUrl = row.Banner
                };

                if (row.TvMazeId.IsNotNullOrWhiteSpace())
                {
                    release.TvMazeId = ParseUtil.CoerceInt(row.TvMazeId);
                }

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class NebulanceSettings : NoAuthTorrentBaseSettings
    {
        public NebulanceSettings()
        {
            ApiKey = "";
        }

        [FieldDefinition(4, Label = "ApiKey", HelpText = "IndexerNebulanceSettingsApiKeyHelpText")]
        public string ApiKey { get; set; }
    }

    public class NebulanceQuery
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Time { get; set; }

        [JsonProperty(PropertyName = "age", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Age { get; set; }

        [JsonProperty(PropertyName = "tvmaze", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? TvMaze { get; set; }

        [JsonProperty(PropertyName = "imdb", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Imdb { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Hash { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string[] Tags { get; set; }

        [JsonProperty(PropertyName = "name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "release", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Release { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Category { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Series { get; set; }

        [JsonProperty(PropertyName = "season", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? Season { get; set; }

        [JsonProperty(PropertyName = "episode", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? Episode { get; set; }

        public NebulanceQuery Clone()
        {
            return MemberwiseClone() as NebulanceQuery;
        }
    }

    public class NebulanceResponse
    {
        public List<NebulanceTorrent> Items { get; set; }
    }

    public class NebulanceTorrent
    {
        [JsonPropertyName("rls_name")]
        public string ReleaseTitle { get; set; }

        [JsonPropertyName("cat")]
        public string Category { get; set; }

        public string Size { get; set; }
        public string Seed { get; set; }
        public string Leech { get; set; }
        public string Snatch { get; set; }
        public string Download { get; set; }

        [JsonPropertyName("file_list")]
        public IEnumerable<string> FileList { get; set; } = Array.Empty<string>();

        [JsonPropertyName("group_name")]
        public string GroupName { get; set; }

        [JsonPropertyName("series_banner")]
        public string Banner { get; set; }

        [JsonPropertyName("group_id")]
        public string TorrentId { get; set; }

        [JsonPropertyName("series_id")]
        public string TvMazeId { get; set; }

        [JsonPropertyName("rls_utc")]
        public string PublishDateUtc { get; set; }

        public IEnumerable<string> Tags { get; set; } = Array.Empty<string>();
    }
}
