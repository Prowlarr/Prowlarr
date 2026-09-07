using System;
using System.Collections.Generic;
using System.Linq;
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
        bool TryGet(NewznabRequest request, List<int> indexerIds, bool interactiveSearch, out NewznabResults results);
        void Set(NewznabRequest request, List<int> indexerIds, bool interactiveSearch, NewznabResults results);
    }

    public class ReleaseSearchCache : IReleaseSearchCache
    {
        private const int DefaultTtlMinutes = 5;

        // Per-request metadata that does not change which releases an indexer returns.
        private static readonly string[] MetadataFields = { "source", "host", "server" };

        private readonly IConfigService _configService;
        private readonly ICached<NewznabResults> _cache;

        public ReleaseSearchCache(IConfigService configService, ICacheManager cacheManager)
        {
            _configService = configService;
            _cache = cacheManager.GetCache<NewznabResults>(GetType(), "searchResults");
        }

        public bool TryGet(NewznabRequest request, List<int> indexerIds, bool interactiveSearch, out NewznabResults results)
        {
            results = null;

            if (!_configService.SearchCacheEnabled)
            {
                return false;
            }

            var key = BuildKey(request, indexerIds, interactiveSearch);
            var cached = _cache.Find(key);

            // Callers rewrite DownloadUrl on the releases they receive, so never hand out the cached instances.
            if (cached == null)
            {
                return false;
            }

            _cache.Set(key, cached, Ttl());
            results = Clone(cached);
            return true;
        }

        public void Set(NewznabRequest request, List<int> indexerIds, bool interactiveSearch, NewznabResults results)
        {
            if (!_configService.SearchCacheEnabled || results?.Releases == null)
            {
                return;
            }

            _cache.ClearExpired();
            _cache.Set(BuildKey(request, indexerIds, interactiveSearch), Clone(results), Ttl());
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
