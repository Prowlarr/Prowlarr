using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
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
        internal const string ApiUrl = "https://aniliberty.top/api/v1/";

        public override string Name => "AniLibria";
        public override string[] IndexerUrls => new[] { "https://anilibria.top/" };
        public override string Language => "ru-RU";
        public override string Description => "AniLibria is a public Russian anime torrent indexer";
        public override Encoding Encoding => Encoding.UTF8;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public AniLibria(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AniLibriaRequestGenerator();
        }

        public override IParseIndexerResponse GetParser()
        {
            return new AniLibriaParser();
        }

        // AniLibria search returns release IDs, while the batch endpoint returns torrents nested in releases.
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

        private static IndexerPageableRequestChain GetSearchRequests(SearchCriteriaBase searchCriteria, string searchTerm)
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

        private static IEnumerable<IndexerRequest> GetSearchRequest(string searchTerm)
        {
            yield return new IndexerRequest($"{AniLibria.ApiUrl}app/search/releases?query={Uri.EscapeDataString(searchTerm.Trim())}", HttpAccept.Json);
        }

        private static IEnumerable<IndexerRequest> GetRecentRequest()
        {
            yield return new IndexerRequest($"{AniLibria.ApiUrl}anime/torrents?limit=50", HttpAccept.Json);
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class AniLibriaParser : IParseIndexerResponse
    {
        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            if (indexerResponse.HttpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                return new List<ReleaseInfo>();
            }

            ValidateJsonResponse(indexerResponse);

            try
            {
                using var document = JsonDocument.Parse(indexerResponse.Content);
                var entries = GetDataArray(document.RootElement, indexerResponse);
                var releaseInfos = new List<ReleaseInfo>();

                foreach (var entry in entries.EnumerateArray())
                {
                    if (entry.ValueKind != JsonValueKind.Object)
                    {
                        throw new IndexerException(indexerResponse, "Unexpected AniLibria API response; expected each release entry to be a JSON object");
                    }

                    if (entry.TryGetProperty("torrents", out var torrents) && torrents.ValueKind == JsonValueKind.Array)
                    {
                        AddReleaseTorrents(releaseInfos, entry, torrents);
                    }
                    else if (entry.TryGetProperty("release", out var release) && release.ValueKind == JsonValueKind.Object)
                    {
                        AddTorrent(releaseInfos, entry, release);
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
                using var document = JsonDocument.Parse(indexerResponse.Content);
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    throw new IndexerException(indexerResponse, "Unexpected AniLibria search response; expected a JSON array");
                }

                var ids = new List<long>();
                foreach (var release in document.RootElement.EnumerateArray())
                {
                    if (release.ValueKind != JsonValueKind.Object)
                    {
                        throw new IndexerException(indexerResponse, "Unexpected AniLibria search response; expected each release entry to be a JSON object");
                    }

                    if (TryGetInt64(release, "id", out var id) && id > 0)
                    {
                        ids.Add(id);
                    }
                }

                return ids.Distinct().ToList();
            }
            catch (JsonException ex)
            {
                throw new IndexerException(indexerResponse, "Unable to parse AniLibria search response", ex);
            }
        }

        private static void AddReleaseTorrents(ICollection<ReleaseInfo> releaseInfos, JsonElement release, JsonElement torrents)
        {
            foreach (var torrent in torrents.EnumerateArray())
            {
                AddTorrent(releaseInfos, torrent, release);
            }
        }

        private static void AddTorrent(ICollection<ReleaseInfo> releaseInfos, JsonElement torrent, JsonElement release)
        {
            if (torrent.ValueKind != JsonValueKind.Object || release.ValueKind != JsonValueKind.Object ||
                !TryGetInt64(torrent, "id", out var torrentId) || torrentId <= 0 ||
                !TryGetString(torrent, "hash", out var hash) || !IsValidInfoHash(hash))
            {
                return;
            }

            var title = GetString(torrent, "label");
            if (title.IsNullOrWhiteSpace())
            {
                title = GetFallbackTitle(release);
            }

            if (title.IsNullOrWhiteSpace())
            {
                return;
            }

            var createdAt = GetString(torrent, "created_at");
            if (!DateTimeOffset.TryParse(createdAt, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var publishDate))
            {
                return;
            }

            var releaseId = TryGetInt64(release, "id", out var id) && id > 0 ? id : (long?)null;
            var alias = GetString(release, "alias");
            if (alias.IsNullOrWhiteSpace() && !releaseId.HasValue)
            {
                return;
            }

            var infoUrl = alias.IsNotNullOrWhiteSpace()
                ? $"https://anilibria.top/anime/releases/release/{alias}"
                : releaseId.HasValue ? $"{AniLibria.ApiUrl}anime/releases/{releaseId.Value}" : null;

            var seeders = GetInt32(torrent, "seeders");
            var leechers = GetInt32(torrent, "leechers");
            var releaseInfo = new TorrentInfo
            {
                Guid = $"{AniLibria.ApiUrl}anime/torrents/{torrentId}",
                Title = title,
                InfoUrl = infoUrl,
                DownloadUrl = $"{AniLibria.ApiUrl}anime/torrents/{torrentId}/file",
                MagnetUrl = GetString(torrent, "magnet"),
                InfoHash = hash.Trim().ToUpperInvariant(),
                Size = GetInt64(torrent, "size"),
                Seeders = seeders,
                Peers = seeders + leechers,
                Grabs = GetInt32(torrent, "completed_times"),
                PublishDate = publishDate.UtcDateTime,
                Categories = new List<IndexerCategory> { GetCategory(GetNestedString(release, "type", "value")) },
                Resolution = NormalizeResolution(GetNestedString(torrent, "quality", "value")),
                Source = GetNestedString(torrent, "type", "value"),
                Codec = GetNestedString(torrent, "codec", "value"),
                DownloadVolumeFactor = 0,
                UploadVolumeFactor = 1
            };

            releaseInfos.Add(releaseInfo);
        }

        private static JsonElement GetDataArray(JsonElement root, IndexerResponse indexerResponse)
        {
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
            {
                return data;
            }

            throw new IndexerException(indexerResponse, "Unexpected AniLibria API response; expected an object with a data JSON array");
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

        private static string GetFallbackTitle(JsonElement release)
        {
            return GetNestedString(release, "name", "main") ?? GetNestedString(release, "name", "english");
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

        private static string GetNestedString(JsonElement element, string parentName, string propertyName)
        {
            return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(parentName, out var parent) && parent.ValueKind == JsonValueKind.Object
                ? GetString(parent, propertyName)
                : null;
        }

        private static string GetString(JsonElement element, string propertyName)
        {
            return TryGetString(element, propertyName, out var value) ? value : null;
        }

        private static bool TryGetString(JsonElement element, string propertyName, out string value)
        {
            value = null;
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            value = property.GetString();
            return true;
        }

        private static int GetInt32(JsonElement element, string propertyName)
        {
            return TryGetInt64(element, propertyName, out var value) && value <= int.MaxValue && value >= int.MinValue ? (int)value : 0;
        }

        private static long GetInt64(JsonElement element, string propertyName)
        {
            return TryGetInt64(element, propertyName, out var value) ? value : 0;
        }

        private static bool TryGetInt64(JsonElement element, string propertyName, out long value)
        {
            value = 0;
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(propertyName, out var property))
            {
                return false;
            }

            if (property.ValueKind == JsonValueKind.Number)
            {
                return property.TryGetInt64(out value);
            }

            return property.ValueKind == JsonValueKind.String && long.TryParse(property.GetString(), out value);
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
