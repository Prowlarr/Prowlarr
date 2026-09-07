using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NzbDrone.Common;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Serializer;
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
        // Default until settings/UI are wired.
        internal static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

        // Per-request metadata that does not change which releases an indexer returns.
        private static readonly string[] MetadataFields = { "source", "host", "server" };

        private readonly ICached<NewznabResults> _cache;

        public ReleaseSearchCache(ICacheManager cacheManager)
        {
            _cache = cacheManager.GetCache<NewznabResults>(GetType(), "searchResults");
        }

        public bool TryGet(NewznabRequest request, List<int> indexerIds, bool interactiveSearch, out NewznabResults results)
        {
            var cached = _cache.Find(BuildKey(request, indexerIds, interactiveSearch));

            // Callers rewrite DownloadUrl on the releases they receive, so never hand out the cached instances.
            results = cached == null ? null : Clone(cached);

            return results != null;
        }

        public void Set(NewznabRequest request, List<int> indexerIds, bool interactiveSearch, NewznabResults results)
        {
            if (results?.Releases == null)
            {
                return;
            }

            _cache.ClearExpired();

            _cache.Set(BuildKey(request, indexerIds, interactiveSearch), Clone(results), DefaultTtl);
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
