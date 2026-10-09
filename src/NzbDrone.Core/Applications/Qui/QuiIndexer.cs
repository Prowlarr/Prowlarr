using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace NzbDrone.Core.Applications.Qui
{
    public class QuiCategory
    {
        [JsonProperty("indexer_id")]
        public int IndexerId { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("category_name")]
        public string CategoryName { get; set; }

        [JsonProperty("parent_category_id")]
        public int? ParentCategoryId { get; set; }
    }

    public class QuiIndexer : IEquatable<QuiIndexer>
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("base_url")]
        public string BaseUrl { get; set; }

        [JsonProperty("api_key")]
        public string ApiKey { get; set; }

        [JsonProperty("backend")]
        public string Backend { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("priority")]
        public int Priority { get; set; }

        [JsonProperty("timeout_seconds")]
        public int TimeoutSeconds { get; set; }

        [JsonProperty("limit_default")]
        public int LimitDefault { get; set; }

        [JsonProperty("limit_max")]
        public int LimitMax { get; set; }

        [JsonProperty("indexer_id")]
        public string IndexerId { get; set; }

        [JsonProperty("capabilities")]
        public List<string> Capabilities { get; set; }

        [JsonProperty("categories")]
        public List<QuiCategory> Categories { get; set; }

        public bool Equals(QuiIndexer other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            var thisCategories = (Categories ?? Enumerable.Empty<QuiCategory>()).Select(c => c.CategoryId).OrderBy(c => c);
            var otherCategories = (other.Categories ?? Enumerable.Empty<QuiCategory>()).Select(c => c.CategoryId).OrderBy(c => c);

            var thisCapabilities = (Capabilities ?? Enumerable.Empty<string>()).OrderBy(c => c);
            var otherCapabilities = (other.Capabilities ?? Enumerable.Empty<string>()).OrderBy(c => c);

            return other.BaseUrl == BaseUrl &&
                other.ApiKey == ApiKey &&
                other.Name == Name &&
                other.Backend == Backend &&
                other.Enabled == Enabled &&
                other.Priority == Priority &&
                other.TimeoutSeconds == TimeoutSeconds &&
                other.LimitDefault == LimitDefault &&
                other.LimitMax == LimitMax &&
                other.IndexerId == IndexerId &&
                otherCapabilities.SequenceEqual(thisCapabilities) &&
                otherCategories.SequenceEqual(thisCategories);
        }

        public override bool Equals(object obj) => Equals(obj as QuiIndexer);

        public override int GetHashCode()
        {
            return HashCode.Combine(BaseUrl, ApiKey, Name, Backend, Enabled, Priority, IndexerId);
        }
    }
}
