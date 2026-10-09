using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NzbDrone.Common;
using NzbDrone.Common.Cache;
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
        private readonly ICached<NewznabResults> _cache;
        private readonly ConcurrentDictionary<string, Lazy<Task<NewznabResults>>> _inFlight = new();

        public ReleaseSearchCache(IConfigService configService, ICacheManager cacheManager)
        {
            _configService = configService;
            _cache = cacheManager.GetCache<NewznabResults>(GetType(), "searchResults");
        }

        public async Task<NewznabResults> GetOrSearch(NewznabRequest request, List<int> indexerIds, bool interactiveSearch, Func<Task<NewznabResults>> search)
        {
            if (!_configService.SearchCacheEnabled)
            {
                return await search();
            }

            var key = BuildKey(request, indexerIds, interactiveSearch);
            var cached = _cache.Find(key);

            // Callers rewrite DownloadUrl on the releases they receive, so never hand out the cached instances.
            if (cached != null)
            {
                return Clone(cached);
            }

            var mine = new Lazy<Task<NewznabResults>>(search, LazyThreadSafetyMode.ExecutionAndPublication);

            // Identical searches that arrive while one is running can share its result.
            var inFlight = _inFlight.GetOrAdd(key, mine);
            var isOwner = ReferenceEquals(inFlight, mine);

            try
            {
                var results = await inFlight.Value;

                if (results?.Releases == null)
                {
                    return results;
                }

                if (isOwner)
                {
                    _cache.ClearExpired();
                    _cache.Set(key, Clone(results), Ttl());
                }

                return Clone(results);
            }
            finally
            {
                if (isOwner)
                {
                    _inFlight.TryRemove(key, out _);
                }
            }
        }

        private TimeSpan Ttl()
        {
            var ttlMinutes = _configService.SearchCacheTtl;
            if (ttlMinutes <= 0)
            {
                ttlMinutes = DefaultTtlMinutes;
            }

            return TimeSpan.FromMinutes(ttlMinutes);
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
    }
}
