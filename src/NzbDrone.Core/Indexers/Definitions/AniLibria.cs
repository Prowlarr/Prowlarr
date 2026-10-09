using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class AniLibria : TorrentIndexerBase<NoAuthTorrentBaseSettings>
    {
        public override string Name => "AniLibria";
        public override string[] IndexerUrls => new[] { "https://aniliberty.top/" };
        public override string Description => "AniLibria is a public Russian anime torrent indexer";
        public override string Language => "ru-RU";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        private string ApiUrl => $"{Settings.BaseUrl}api/v1/";

        public AniLibria(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AniLibriaRequestGenerator(ApiUrl);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new AniLibriaParser(ApiUrl);
        }

        protected override async Task<IndexerQueryResult> FetchPage(IndexerRequest request, IParseIndexerResponse parser)
        {
            if (!request.Url.FullUri.Contains("/app/search/releases?", StringComparison.Ordinal))
            {
                return await base.FetchPage(request, parser);
            }

            var response = await FetchIndexerResponse(request);
            var releaseIds = AniLibriaParser.ParseSearchReleaseIds(response);

            if (releaseIds.Count == 0)
            {
                return new IndexerQueryResult
                {
                    Releases = new List<ReleaseInfo>(),
                    Response = response.HttpResponse
                };
            }

            var detailsRequest = new IndexerRequest($"{ApiUrl}anime/releases/list?ids={string.Join(",", releaseIds)}", HttpAccept.Json);

            return await base.FetchPage(detailsRequest, parser);
        }

        private static IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.TVAnime);
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.MoviesOther);
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.TV);
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.OtherMisc);

            return caps;
        }
    }

    public class AniLibriaRequestGenerator : IIndexerRequestGenerator
    {
        private readonly string _apiUrl;

        public AniLibriaRequestGenerator(string apiUrl)
        {
            _apiUrl = apiUrl;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            return GetSearchRequests(searchCriteria, searchCriteria.SanitizedSearchTerm);
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            return GetSearchRequests(searchCriteria, searchCriteria.SanitizedTvSearchString);
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            return GetSearchRequests(searchCriteria, searchCriteria.SanitizedSearchTerm);
        }

        private IndexerPageableRequestChain GetSearchRequests(SearchCriteriaBase searchCriteria, string searchTerm)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            if (searchCriteria.IsRssSearch)
            {
                pageableRequests.Add(GetRecentRequest());
            }
            else if (searchTerm.IsNotNullOrWhiteSpace())
            {
                pageableRequests.Add(GetSearchRequest(searchTerm));
            }

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetSearchRequest(string searchTerm)
        {
            yield return new IndexerRequest($"{_apiUrl}app/search/releases?query={Uri.EscapeDataString(searchTerm.Trim())}", HttpAccept.Json);
        }

        private IEnumerable<IndexerRequest> GetRecentRequest()
        {
            yield return new IndexerRequest($"{_apiUrl}anime/torrents?limit=50", HttpAccept.Json);
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class AniLibriaParser : IParseIndexerResponse
    {
        private readonly string _apiUrl;

        public AniLibriaParser(string apiUrl)
        {
            _apiUrl = apiUrl;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            if (indexerResponse.HttpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return new List<ReleaseInfo>();
            }

            ValidateJsonResponse(indexerResponse);

            try
            {
                var jsonResponse = STJson.Deserialize<AniLibriaApiResponse>(indexerResponse.Content);
                if (jsonResponse?.Data == null)
                {
                    throw new IndexerException(indexerResponse, "Unexpected AniLibria API response; expected an object with a data JSON array");
                }

                var releaseInfos = new List<ReleaseInfo>();

                foreach (var entry in jsonResponse.Data)
                {
                    if (entry.Torrents != null)
                    {
                        foreach (var torrent in entry.Torrents)
                        {
                            AddTorrent(releaseInfos, torrent, entry);
                        }
                    }
                    else if (entry.Release != null)
                    {
                        AddTorrent(releaseInfos, entry, entry.Release);
                    }
                }

                return releaseInfos;
            }
            catch (JsonException ex)
            {
                throw new IndexerException(indexerResponse, "Unable to parse AniLibria API response", ex);
            }
        }

        internal static List<long> ParseSearchReleaseIds(IndexerResponse indexerResponse)
        {
            if (indexerResponse.HttpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return new List<long>();
            }

            ValidateJsonResponse(indexerResponse);

            try
            {
                var releases = STJson.Deserialize<List<AniLibriaRelease>>(indexerResponse.Content);
                if (releases == null)
                {
                    throw new IndexerException(indexerResponse, "Unexpected AniLibria search response; expected a JSON array");
                }

                return releases.Where(release => release.Id > 0).Select(release => release.Id).Distinct().ToList();
            }
            catch (JsonException ex)
            {
                throw new IndexerException(indexerResponse, "Unable to parse AniLibria search response", ex);
            }
        }

        private void AddTorrent(ICollection<ReleaseInfo> releaseInfos, AniLibriaApiEntry torrent, AniLibriaApiEntry release)
        {
            if (torrent?.Id <= 0 || !IsValidInfoHash(torrent.Hash))
            {
                return;
            }

            var title = torrent.Label ?? GetFallbackTitle(release);
            if (title.IsNullOrWhiteSpace())
            {
                return;
            }

            var publishDate = DateTime.UtcNow;
            if (DateTimeOffset.TryParse(torrent.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var parsedPublishDate))
            {
                publishDate = parsedPublishDate.UtcDateTime;
            }

            var alias = release.Alias;
            string infoUrl = null;
            if (alias.IsNotNullOrWhiteSpace())
            {
                infoUrl = $"{_apiUrl}anime/releases/release/{alias}";
            }
            else if (release.Id > 0)
            {
                infoUrl = $"{_apiUrl}anime/releases/{release.Id}";
            }

            var releaseInfo = new TorrentInfo
            {
                Guid = $"{_apiUrl}anime/torrents/{torrent.Id}",
                Title = title,
                InfoUrl = infoUrl,
                DownloadUrl = $"{_apiUrl}anime/torrents/{torrent.Id}/file",
                MagnetUrl = torrent.Magnet,
                InfoHash = torrent.Hash.Trim().ToUpperInvariant(),
                Size = torrent.Size,
                Seeders = torrent.Seeders,
                Peers = torrent.Seeders + torrent.Leechers,
                Grabs = torrent.CompletedTimes,
                PublishDate = publishDate,
                Categories = new List<IndexerCategory> { GetCategory(release.Type?.Value) },
                Resolution = NormalizeResolution(torrent.Quality?.Value),
                Source = torrent.Type?.Value,
                Codec = torrent.Codec?.Value,
                DownloadVolumeFactor = 0,
                UploadVolumeFactor = 1
            };

            releaseInfos.Add(releaseInfo);
        }

        private static void ValidateJsonResponse(IndexerResponse indexerResponse)
        {
            var httpResponse = indexerResponse.HttpResponse;
            if (httpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {httpResponse.StatusCode} code from AniLibria");
            }

            if (httpResponse.Headers.ContentType.IsNullOrWhiteSpace() || !httpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value, StringComparison.OrdinalIgnoreCase))
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {httpResponse.Headers.ContentType} from AniLibria, expected {HttpAccept.Json.Value}");
            }
        }

        private static string GetFallbackTitle(AniLibriaApiEntry release)
        {
            return release.Name?.Main ?? release.Name?.English;
        }

        private static bool IsValidInfoHash(string hash)
        {
            return hash?.Trim().Length == 40 && hash.Trim().All(Uri.IsHexDigit);
        }

        private static IndexerCategory GetCategory(string releaseType)
        {
            return releaseType?.ToUpperInvariant() switch
            {
                "MOVIE" => NewznabStandardCategory.MoviesOther,
                "TV" or "OVA" or "ONA" or "OAD" or "SPECIAL" or "WEB" => NewznabStandardCategory.TVAnime,
                "DORAMA" => NewznabStandardCategory.TV,
                _ => NewznabStandardCategory.OtherMisc
            };
        }

        private static string NormalizeResolution(string resolution)
        {
            return resolution?.ToLowerInvariant() switch
            {
                "2k" => "1440p",
                "4k" => "2160p",
                "8k" => "4320p",
                _ => resolution
            };
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    internal sealed class AniLibriaApiResponse
    {
        public IReadOnlyCollection<AniLibriaApiEntry> Data { get; init; }
    }

    internal sealed class AniLibriaRelease
    {
        public long Id { get; init; }
    }

    internal sealed class AniLibriaApiEntry
    {
        public long Id { get; init; }
        public string Alias { get; init; }
        public AniLibriaValue Type { get; init; }
        public AniLibriaName Name { get; init; }
        public IReadOnlyCollection<AniLibriaApiEntry> Torrents { get; init; }
        public AniLibriaApiEntry Release { get; init; }
        public string Hash { get; init; }
        public string Label { get; init; }
        public string Magnet { get; init; }
        public long Size { get; init; }
        public int Seeders { get; init; }
        public int Leechers { get; init; }

        [JsonPropertyName("completed_times")]
        public int CompletedTimes { get; init; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; init; }

        public AniLibriaValue Quality { get; init; }
        public AniLibriaValue Codec { get; init; }
    }

    internal sealed class AniLibriaValue
    {
        public string Value { get; init; }
    }

    internal sealed class AniLibriaName
    {
        public string Main { get; init; }
        public string English { get; init; }
    }
}
