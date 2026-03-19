using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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

            var queryParams = new NameValueCollection
            {
                { "action", "search" },
                { "age", ">0" },
            };

            if (searchCriteria.TvMazeId is > 0)
            {
                queryParams.Set("tvmaze", searchCriteria.TvMazeId.ToString());
            }
            else if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace())
            {
                queryParams.Set("imdb", searchCriteria.FullImdbId);
            }

            var searchQuery = searchCriteria.SanitizedSearchTerm.Trim();

            if (searchQuery.IsNotNullOrWhiteSpace())
            {
                queryParams.Set("release", searchQuery);
            }

            if (searchCriteria.Season.HasValue &&
                searchCriteria.Episode.IsNotNullOrWhiteSpace() &&
                DateTime.TryParseExact($"{searchCriteria.Season} {searchCriteria.Episode}", "yyyy MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var showDate))
            {
                if (searchQuery.IsNotNullOrWhiteSpace())
                {
                    queryParams.Set("name", searchQuery);
                }

                queryParams.Set("release", showDate.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture));
            }
            else
            {
                if (searchCriteria.Season.HasValue)
                {
                    queryParams.Set("season", searchCriteria.Season.Value.ToString());
                }

                if (searchCriteria.Episode.IsNotNullOrWhiteSpace() && int.TryParse(searchCriteria.Episode, out var episodeNumber))
                {
                    queryParams.Set("episode", episodeNumber.ToString());
                }
            }

            if ((queryParams.Get("season").IsNotNullOrWhiteSpace() || queryParams.Get("episode").IsNotNullOrWhiteSpace()) &&
                queryParams.Get("name").IsNullOrWhiteSpace() &&
                queryParams.Get("release").IsNullOrWhiteSpace() &&
                queryParams.Get("tvmaze").IsNullOrWhiteSpace() &&
                queryParams.Get("imdb").IsNullOrWhiteSpace())
            {
                _logger.Warn("NBL API does not support season calls without name, series, id, imdb, tvmaze, or time keys.");

                return new IndexerPageableRequestChain();
            }

            if (queryParams.Get("name") is { Length: > 0 and < 3 } || queryParams.Get("release") is { Length: > 0 and < 3 })
            {
                _logger.Warn("NBL API does not support release calls that are 2 characters or fewer.");

                return new IndexerPageableRequestChain();
            }

            pageableRequests.Add(GetPagedRequests(queryParams, searchCriteria));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var queryParams = new NameValueCollection
            {
                { "action", "search" },
                { "age", ">0" },
            };

            var searchQuery = searchCriteria.SanitizedSearchTerm.Trim();

            if (searchQuery.IsNotNullOrWhiteSpace())
            {
                queryParams.Set("release", searchQuery);
            }

            if (queryParams.Get("release") is { Length: > 0 and < 3 })
            {
                _logger.Debug("NBL API does not support release calls that are 2 characters or fewer.");

                return new IndexerPageableRequestChain();
            }

            pageableRequests.Add(GetPagedRequests(queryParams, searchCriteria));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(NameValueCollection parameters, SearchCriteriaBase searchCriteria)
        {
            parameters.Set("api_key", _settings.ApiKey);
            parameters.Set("per_page", searchCriteria.Limit.GetValueOrDefault(100).ToString());

            if (searchCriteria.Limit > 0 && searchCriteria.Offset > 0)
            {
                var page = searchCriteria.Offset / searchCriteria.Limit;
                parameters.Set("page", page.ToString());
            }

            var apiUrl = $"{_settings.BaseUrl}api.php?{parameters.GetQueryString()}";

            yield return new IndexerRequest(apiUrl, HttpAccept.Json);
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

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, "Unexpected response status '{0}' code from indexer request. Check the logs for more information.", indexerResponse.HttpResponse.StatusCode);
            }

            NebulanceResponse jsonResponse;

            try
            {
                jsonResponse = STJson.Deserialize<NebulanceResponse>(indexerResponse.HttpResponse.Content);
            }
            catch (Exception ex)
            {
                STJson.TryDeserialize<JsonRpcResponse<string>>(indexerResponse.HttpResponse.Content, out var response);

                throw new IndexerException(indexerResponse, "Unexpected response from indexer request: {0}", ex, response?.Result ?? ex.Message);
            }

            if (jsonResponse.Error != null)
            {
                throw new IndexerException(indexerResponse, "Indexer API call returned an error [{0}]", jsonResponse.Error?.Message);
            }

            if (jsonResponse.TotalResults == 0 || jsonResponse.Items == null || jsonResponse.Items.Count == 0)
            {
                return torrentInfos;
            }

            foreach (var row in jsonResponse.Items)
            {
                var details = _settings.BaseUrl + "torrents.php?id=" + row.TorrentId;

                var title = row.ReleaseTitle.IsNotNullOrWhiteSpace() ? row.ReleaseTitle : row.GroupName;

                var release = new TorrentInfo
                {
                    Guid = details,
                    InfoUrl = details,
                    DownloadUrl = row.DownloadLink,
                    Title = title.Trim(),
                    Categories = new List<IndexerCategory> { TvCategoryFromQualityParser.ParseTvShowQuality(row.ReleaseTitle) },
                    Size = row.Size,
                    Files = row.FileList.Count,
                    PublishDate = DateTime.Parse(row.PublishDateUtc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                    Grabs = row.Snatch,
                    Seeders = row.Seed,
                    Peers = row.Seed + row.Leech,
                    Scene = row.Tags?.ContainsIgnoreCase("scene"),
                    MinimumRatio = 0, // ratioless
                    MinimumSeedTime = row.Category.ToUpperInvariant() == "SEASON" ? 432000 : 86400, // 120 hours for seasons and 24 hours for episodes
                    DownloadVolumeFactor = 0, // ratioless tracker
                    UploadVolumeFactor = 1,
                };

                if (row.ImdbId.IsNotNullOrWhiteSpace())
                {
                    release.ImdbId = ParseUtil.GetImdbId(row.ImdbId).GetValueOrDefault();
                }

                if (row.TvMazeId is > 0)
                {
                    release.TvMazeId = row.TvMazeId.Value;
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

        [FieldDefinition(2, Label = "ApiKey", HelpText = "IndexerNebulanceSettingsApiKeyHelpText", Privacy = PrivacyLevel.ApiKey)]
        public string ApiKey { get; set; }
    }

    public class NebulanceResponse
    {
        [JsonPropertyName("total_results")]
        public int TotalResults { get; init; }

        public IReadOnlyCollection<NebulanceTorrent> Items { get; init; }

        public NebulanceErrorMessage Error { get; init; }
    }

    public class NebulanceTorrent
    {
        [JsonPropertyName("rls_name")]
        public string ReleaseTitle { get; init; }

        [JsonPropertyName("cat")]
        public string Category { get; init; }

        public long Size { get; init; }
        public int Seed { get; init; }
        public int Leech { get; init; }
        public int Snatch { get; init; }

        [JsonPropertyName("download")]
        public string DownloadLink { get; init; }

        [JsonPropertyName("file_list")]
        public IReadOnlyCollection<string> FileList { get; init; } = [];

        [JsonPropertyName("group_name")]
        public string GroupName { get; init; }

        [JsonPropertyName("group_id")]
        public int TorrentId { get; init; }

        [JsonPropertyName("imdb_id")]
        public string ImdbId { get; init; }

        [JsonPropertyName("tvmaze_id")]
        public int? TvMazeId { get; init; }

        [JsonPropertyName("rls_utc")]
        public string PublishDateUtc { get; init; }

        public IReadOnlyCollection<string> Tags { get; init; } = [];
    }

    public class NebulanceErrorMessage
    {
        public string Message { get; init; }
    }
}
