using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NzbDrone.Common;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.IndexerSearch
{
    public interface IReleaseSearchCache
    {
        Task<NewznabResults> GetOrSearch(NewznabRequest request, List<int> indexerIds, bool interactiveSearch, Func<Task<NewznabResults>> search);
    }

    public class ReleaseSearchCache : IReleaseSearchCache
    {
        private const int DefaultTtlMinutes = 5;

        // Per-request metadata that does not change which releases an indexer returns.
        private static readonly string[] MetadataFields = { "source", "host", "server" };

        private readonly IConfigService _configService;
        private readonly HybridCache _cache;

        public ReleaseSearchCache(IConfigService configService, HybridCache cache)
        {
            _configService = configService;
            _cache = cache;
        }

        public async Task<NewznabResults> GetOrSearch(NewznabRequest request, List<int> indexerIds, bool interactiveSearch, Func<Task<NewznabResults>> search)
        {
            if (!_configService.SearchCacheEnabled)
            {
                return await search();
            }

            var ttlMinutes = _configService.SearchCacheTtl;
            if (ttlMinutes <= 0)
            {
                ttlMinutes = DefaultTtlMinutes;
            }

            
            var hit = await _cache.GetOrCreateAsync(
                BuildKey(request, indexerIds, interactiveSearch),
                async _ => new CachedSearchHit(await search()),
                new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(ttlMinutes),
                    Flags = HybridCacheEntryFlags.DisableDistributedCache
                });

            if (hit.Results?.Releases == null)
            {
                return hit.Results;
            }

            // Callers rewrite DownloadUrl on the releases they receive, so never hand out the cached instances.
            return Clone(hit.Results);
        }

        internal static string BuildKey(NewznabRequest request, List<int> indexerIds, bool interactiveSearch)
        {
            var obj = JObject.Parse(request.ToJson());

            foreach (var field in MetadataFields)
            {
                obj.Remove(field);
            }

            obj["indexerIds"] = indexerIds == null || indexerIds.Count == 0
                ? "*"
                : string.Join(",", indexerIds.OrderBy(i => i));
            obj["interactiveSearch"] = interactiveSearch;

            return HashUtil.ComputeSha256Hash(obj.ToString(Formatting.None));
        }

        private static NewznabResults Clone(NewznabResults results)
        {
            return new NewznabResults
            {
                Releases = results.Releases.Select(r => (ReleaseInfo)r.Clone()).ToList()
            };
        }

        // Since results hold usenet or torrent releases and HybridCache serialises to/from JSON, we need this wrapper to hold the original objects (Immutable & seal allow the original object to be returned, but skipping json also prevents distributed cache option)
        [ImmutableObject(true)]
        private sealed class CachedSearchHit
        {
            public CachedSearchHit(NewznabResults results)
            {
                Results = results;
            }

            [System.Text.Json.Serialization.JsonIgnore]
            public NewznabResults Results { get; }
        }
    }
}
