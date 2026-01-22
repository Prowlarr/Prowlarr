using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NzbDrone.Core.Applications.Listenarr
{
    public class ListenarrIndexer
    {
        public int Id { get; set; }
        public bool EnableRss { get; set; }
        public bool EnableAutomaticSearch { get; set; }
        public bool EnableInteractiveSearch { get; set; }
        public int Priority { get; set; }
        public string Name { get; set; }
        public string ImplementationName { get; set; }
        public string Implementation { get; set; }
        public List<string> Implementations { get; set; }
        public string ConfigContract { get; set; }
        public string InfoLink { get; set; }
        public int? DownloadClientId { get; set; }
        public HashSet<int> Tags { get; set; }
        public List<ListenarrField> Fields { get; set; }
        public bool Equals(ListenarrIndexer other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            // baseUrl comparison (case-insensitive)
            var baseUrlEqual = string.Equals(
                (string)Fields.FirstOrDefault(x => x.Name == "baseUrl")?.Value,
                (string)other.Fields.FirstOrDefault(x => x.Name == "baseUrl")?.Value,
                StringComparison.InvariantCultureIgnoreCase);

            // categories deep equality
            var catsEqual = JToken.DeepEquals(
                (JArray)Fields.FirstOrDefault(x => x.Name == "categories")?.Value,
                (JArray)other.Fields.FirstOrDefault(x => x.Name == "categories")?.Value);

            // apiKey: treat masked remote key as equal
            var apiKey = (string)Fields.FirstOrDefault(x => x.Name == "apiKey")?.Value;
            var otherApiKey = (string)other.Fields.FirstOrDefault(x => x.Name == "apiKey")?.Value;
            var apiKeyEqual = apiKey == otherApiKey || otherApiKey == "********";

            // apiPath compare (could be null)
            var apiPath = Fields.FirstOrDefault(x => x.Name == "apiPath")?.Value;
            var otherApiPath = other.Fields.FirstOrDefault(x => x.Name == "apiPath")?.Value;
            var apiPathEqual = Equals(apiPath, otherApiPath);

            return apiKeyEqual && apiPathEqual && baseUrlEqual && catsEqual;
        }
    }
}
