using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        // Defaults until settings/UI are wired. TTL is short so stale indexer results do not linger.
        internal static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);
        internal const int DefaultMaxEntries = 100;

        private readonly ConcurrentDictionary<string, CacheEntry> _store = new();

        public bool TryGet(NewznabRequest request, List<int> indexerIds, bool interactiveSearch, out NewznabResults results)
        {
            results = null;

            if (!_store.TryGetValue(BuildKey(request, indexerIds, interactiveSearch), out var entry))
            {
                return false;
            }

            if (entry.IsExpired)
            {
                _store.TryRemove(entry.Key, out _);
                return false;
            }

            results = Clone(entry.Results);
            return true;
        }

        public void Set(NewznabRequest request, List<int> indexerIds, bool interactiveSearch, NewznabResults results)
        {
            if (results?.Releases == null)
            {
                return;
            }

            var key = BuildKey(request, indexerIds, interactiveSearch);

            _store[key] = new CacheEntry(key, Clone(results), DateTime.UtcNow + DefaultTtl);

            Trim();
        }

        internal static string BuildKey(NewznabRequest request, List<int> indexerIds, bool interactiveSearch)
        {
            var obj = JObject.FromObject(request);

            // Per-request metadata. Does not change indexer results; download URLs are rewritten after search.
            obj.Remove("source");
            obj.Remove("host");
            obj.Remove("server");

            if (obj.Value<string>("cat") is { Length: > 0 } cat)
            {
                obj["cat"] = string.Join(",", cat.Split(',').Select(c => c.Trim()).Where(c => c.Length > 0).OrderBy(c => c, StringComparer.Ordinal));
            }

            obj["_indexerIds"] = indexerIds == null || indexerIds.Count == 0
                ? "*"
                : string.Join(",", indexerIds.OrderBy(i => i));
            obj["_interactiveSearch"] = interactiveSearch;

            return obj.ToString(Formatting.None);
        }

        private void Trim()
        {
            if (_store.Count <= DefaultMaxEntries)
            {
                return;
            }

            foreach (var expired in _store.Where(kv => kv.Value.IsExpired).Select(kv => kv.Key).ToList())
            {
                _store.TryRemove(expired, out _);
            }

            var overflow = _store.Count - DefaultMaxEntries;

            if (overflow <= 0)
            {
                return;
            }

            foreach (var key in _store.OrderBy(kv => kv.Value.ExpiresAt).Take(overflow).Select(kv => kv.Key).ToList())
            {
                _store.TryRemove(key, out _);
            }
        }

        private static NewznabResults Clone(NewznabResults results)
        {
            return new NewznabResults
            {
                Releases = results.Releases.Select(r => (ReleaseInfo)r.Clone()).ToList()
            };
        }

        private sealed class CacheEntry
        {
            public CacheEntry(string key, NewznabResults results, DateTime expiresAt)
            {
                Key = key;
                Results = results;
                ExpiresAt = expiresAt;
            }

            public string Key { get; }
            public NewznabResults Results { get; }
            public DateTime ExpiresAt { get; }
            public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        }
    }
}
