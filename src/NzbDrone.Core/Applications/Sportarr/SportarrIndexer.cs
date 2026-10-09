using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NzbDrone.Core.Applications.Sportarr
{
    public class SportarrIndexer
    {
        public int Id { get; set; }
        public bool EnableRss { get; set; }
        public bool EnableAutomaticSearch { get; set; }
        public bool EnableInteractiveSearch { get; set; }
        public int Priority { get; set; }
        public string Name { get; set; }
        public string Implementation { get; set; }
        public string ConfigContract { get; set; }
        public HashSet<int> Tags { get; set; }
        public List<SportarrField> Fields { get; set; }

        public bool Equals(SportarrIndexer other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            // Sportarr serializes every field value as a string (categories as
            // a comma-joined list), while the payloads built from the schema
            // carry JSON-typed values. Normalize both sides before comparing
            // so a sync only fires on real drift.
            var baseUrlCompare = FieldString("baseUrl")?.TrimEnd('/') == other.FieldString("baseUrl")?.TrimEnd('/');
            var apiPathCompare = FieldString("apiPath") == other.FieldString("apiPath");

            var apiKey = FieldString("apiKey");
            var otherApiKey = other.FieldString("apiKey");
            var apiKeyCompare = apiKey == otherApiKey || otherApiKey == "********";

            var catsCompare = FieldCategorySet("categories").SetEquals(other.FieldCategorySet("categories"));
            var minimumSeedersCompare = FieldInt("minimumSeeders") == other.FieldInt("minimumSeeders");
            var seedRatioCompare = FieldDouble("seedRatio") == other.FieldDouble("seedRatio");
            var seedTimeCompare = FieldInt("seedTime") == other.FieldInt("seedTime");
            var seasonPackSeedTimeCompare = FieldInt("seasonPackSeedTime") == other.FieldInt("seasonPackSeedTime");
            var rejectBlocklistedCompare = FieldBool("rejectBlocklistedTorrentHashes") == other.FieldBool("rejectBlocklistedTorrentHashes");

            return other.EnableRss == EnableRss &&
                other.EnableAutomaticSearch == EnableAutomaticSearch &&
                other.EnableInteractiveSearch == EnableInteractiveSearch &&
                other.Name == Name &&
                other.Implementation == Implementation &&
                other.Priority == Priority &&
                other.Id == Id &&
                baseUrlCompare && apiPathCompare && apiKeyCompare && catsCompare &&
                minimumSeedersCompare && seedRatioCompare && seedTimeCompare &&
                seasonPackSeedTimeCompare && rejectBlocklistedCompare;
        }

        private string FieldString(string name)
        {
            var value = Fields.FirstOrDefault(x => x.Name == name)?.Value;

            return value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : value?.ToString();
        }

        private HashSet<int> FieldCategorySet(string name)
        {
            var value = Fields.FirstOrDefault(x => x.Name == name)?.Value;

            var result = new HashSet<int>();

            if (value is JArray array)
            {
                foreach (var item in array)
                {
                    result.Add((int)item);
                }

                return result;
            }

            foreach (var part in (value?.ToString() ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(part.Trim(), out var category))
                {
                    result.Add(category);
                }
            }

            return result;
        }

        private int? FieldInt(string name)
        {
            var text = FieldString(name);

            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
        }

        private double? FieldDouble(string name)
        {
            var text = FieldString(name);

            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : null;
        }

        private bool? FieldBool(string name)
        {
            var text = FieldString(name);

            return bool.TryParse(text, out var value) ? value : null;
        }
    }
}
