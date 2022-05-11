using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NzbDrone.Core.Applications.Radarr
{
    public class RadarrIndexer
    {
        public int Id { get; set; }
        public bool EnableRss { get; set; }
        public bool EnableAutomaticSearch { get; set; }
        public bool EnableInteractiveSearch { get; set; }
        public int Priority { get; set; }
        public string Name { get; set; }
        public string ImplementationName { get; set; }
        public string Implementation { get; set; }
        public string ConfigContract { get; set; }
        public string InfoLink { get; set; }
        public HashSet<int> Tags { get; set; }
        public List<RadarrField> Fields { get; set; }

        public bool Equals(RadarrIndexer other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            var baseUrl = (string)Fields.FirstOrDefault(x => x.Name == "baseUrl").Value == (string)other.Fields.FirstOrDefault(x => x.Name == "baseUrl").Value;
            var apiPath = (string)Fields.FirstOrDefault(x => x.Name == "apiPath").Value == (string)other.Fields.FirstOrDefault(x => x.Name == "apiPath").Value;
            var apiKey = (string)Fields.FirstOrDefault(x => x.Name == "apiKey").Value == (string)other.Fields.FirstOrDefault(x => x.Name == "apiKey").Value;
            var cats = JToken.DeepEquals((JArray)Fields.FirstOrDefault(x => x.Name == "categories").Value, (JArray)other.Fields.FirstOrDefault(x => x.Name == "categories").Value);

            var minimumSeeders = Fields.FirstOrDefault(x => x.Name == "minimumSeeders")?.Value == null ? null : (int?)Convert.ToInt32(Fields.FirstOrDefault(x => x.Name == "minimumSeeders").Value);
            var otherMinimumSeeders = other.Fields.FirstOrDefault(x => x.Name == "minimumSeeders")?.Value == null ? null : (int?)Convert.ToInt32(other.Fields.FirstOrDefault(x => x.Name == "minimumSeeders").Value);
            var minimumSeedersCompare = minimumSeeders == otherMinimumSeeders;

            var seedTime = Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedTime")?.Value == null ? null : (int?)Convert.ToInt32(Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedTime").Value);
            var otherSeedTime = other.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedTime")?.Value == null ? null : (int?)Convert.ToInt32(other.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedTime").Value);
            var seedTimeCompare = seedTime == otherSeedTime;

            var seedRatio = Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedRatio")?.Value == null ? null : (double?)Convert.ToDouble(Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedRatio").Value);
            var otherSeedRatio = other.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedRatio")?.Value == null ? null : (double?)Convert.ToDouble(other.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedRatio").Value);
            var seedRatioCompare = seedRatio == otherSeedRatio;

            return other.EnableRss == EnableRss &&
                other.EnableAutomaticSearch == EnableAutomaticSearch &&
                other.EnableInteractiveSearch == EnableInteractiveSearch &&
                other.Name == Name &&
                other.Implementation == Implementation &&
                other.Priority == Priority &&
                other.Id == Id &&
                apiKey && apiPath && baseUrl && cats && minimumSeedersCompare && seedRatioCompare && seedTimeCompare;
        }
    }
}
