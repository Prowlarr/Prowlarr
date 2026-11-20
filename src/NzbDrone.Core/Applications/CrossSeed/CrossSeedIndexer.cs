using Newtonsoft.Json;

namespace NzbDrone.Core.Applications.CrossSeed
{
    public class CrossSeedIndexer
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("apikey")]
        public string ApiKey { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        public bool Equals(CrossSeedIndexer other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            return other.Name == Name &&
                other.Url == Url &&
                other.ApiKey == ApiKey &&
                other.Enabled == Enabled;
        }
    }
}
