using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NzbDrone.Core.Applications.Sonarr
{
    public class SonarrIndexer
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
        public int? DownloadClientId { get; set; }
        public int? SeasonSearchMaximumSingleEpisodeAge { get; set; }
        public HashSet<int> Tags { get; set; }
        public List<SonarrField> Fields { get; set; }

        public bool Equals(SonarrIndexer other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            var baseUrl = (string)Fields.FirstOrDefault(x => x.Name == "baseUrl").Value == (string)other.Fields.FirstOrDefault(x => x.Name == "baseUrl").Value;
            var cats = JToken.DeepEquals((JArray)Fields.FirstOrDefault(x => x.Name == "categories").Value, (JArray)other.Fields.FirstOrDefault(x => x.Name == "categories").Value);
            var animeCats = JToken.DeepEquals((JArray)Fields.FirstOrDefault(x => x.Name == "animeCategories").Value, (JArray)other.Fields.FirstOrDefault(x => x.Name == "animeCategories").Value);

            var apiKey = (string)Fields.FirstOrDefault(x => x.Name == "apiKey")?.Value;
            var otherApiKey = (string)other.Fields.FirstOrDefault(x => x.Name == "apiKey")?.Value;
            var apiKeyCompare = apiKey == otherApiKey || otherApiKey == "********";

            var apiPath = Fields.FirstOrDefault(x => x.Name == "apiPath")?.Value == null ? null : Fields.FirstOrDefault(x => x.Name == "apiPath").Value;
            var otherApiPath = other.Fields.FirstOrDefault(x => x.Name == "apiPath")?.Value == null ? null : other.Fields.FirstOrDefault(x => x.Name == "apiPath").Value;
            var apiPathCompare = apiPath.Equals(otherApiPath);

            var animeStandardFormatSearch = Fields.FirstOrDefault(x => x.Name == "animeStandardFormatSearch")?.Value == null ? null : (bool?)Convert.ToBoolean(Fields.FirstOrDefault(x => x.Name == "animeStandardFormatSearch").Value);
            var otherAnimeStandardFormatSearch = other.Fields.FirstOrDefault(x => x.Name == "animeStandardFormatSearch")?.Value == null ? null : (bool?)Convert.ToBoolean(other.Fields.FirstOrDefault(x => x.Name == "animeStandardFormatSearch").Value);
            var animeStandardFormatSearchCompare = animeStandardFormatSearch == otherAnimeStandardFormatSearch;

            var minimumSeeders = Fields.FirstOrDefault(x => x.Name == "minimumSeeders")?.Value == null ? null : (int?)Convert.ToInt32(Fields.FirstOrDefault(x => x.Name == "minimumSeeders").Value);
            var otherMinimumSeeders = other.Fields.FirstOrDefault(x => x.Name == "minimumSeeders")?.Value == null ? null : (int?)Convert.ToInt32(other.Fields.FirstOrDefault(x => x.Name == "minimumSeeders").Value);
            var minimumSeedersCompare = minimumSeeders == otherMinimumSeeders;

            var seedTime = Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedTime")?.Value == null ? null : (int?)Convert.ToInt32(Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedTime").Value);
            var otherSeedTime = other.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedTime")?.Value == null ? null : (int?)Convert.ToInt32(other.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedTime").Value);
            var seedTimeCompare = seedTime == otherSeedTime;

            var seasonSeedTime = Fields.FirstOrDefault(x => x.Name == "seedCriteria.seasonPackSeedTime")?.Value == null ? null : (int?)Convert.ToInt32(Fields.FirstOrDefault(x => x.Name == "seedCriteria.seasonPackSeedTime").Value);
            var otherSeasonSeedTime = other.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seasonPackSeedTime")?.Value == null ? null : (int?)Convert.ToInt32(other.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seasonPackSeedTime").Value);
            var seasonSeedTimeCompare = seasonSeedTime == otherSeasonSeedTime;

            var seedRatio = Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedRatio")?.Value == null ? null : (double?)Convert.ToDouble(Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedRatio").Value);
            var otherSeedRatio = other.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedRatio")?.Value == null ? null : (double?)Convert.ToDouble(other.Fields.FirstOrDefault(x => x.Name == "seedCriteria.seedRatio").Value);
            var seedRatioCompare = seedRatio == otherSeedRatio;

            var rejectBlocklistedTorrentHashesWhileGrabbing = Fields.FirstOrDefault(x => x.Name == "rejectBlocklistedTorrentHashesWhileGrabbing")?.Value == null ? null : (bool?)Convert.ToBoolean(Fields.FirstOrDefault(x => x.Name == "rejectBlocklistedTorrentHashesWhileGrabbing").Value);
            var otherRejectBlocklistedTorrentHashesWhileGrabbing = other.Fields.FirstOrDefault(x => x.Name == "rejectBlocklistedTorrentHashesWhileGrabbing")?.Value == null ? null : (bool?)Convert.ToBoolean(other.Fields.FirstOrDefault(x => x.Name == "rejectBlocklistedTorrentHashesWhileGrabbing").Value);
            var rejectBlocklistedTorrentHashesWhileGrabbingCompare = rejectBlocklistedTorrentHashesWhileGrabbing == otherRejectBlocklistedTorrentHashesWhileGrabbing;

            return other.EnableRss == EnableRss &&
                other.EnableAutomaticSearch == EnableAutomaticSearch &&
                other.EnableInteractiveSearch == EnableInteractiveSearch &&
                other.Name == Name &&
                other.Implementation == Implementation &&
                other.Priority == Priority &&
                other.Id == Id &&
                apiKeyCompare && apiPathCompare && baseUrl && cats && animeCats && animeStandardFormatSearchCompare && minimumSeedersCompare && seedRatioCompare && seedTimeCompare && seasonSeedTimeCompare && rejectBlocklistedTorrentHashesWhileGrabbingCompare;
        }
    }
}
