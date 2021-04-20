using System.Collections.Generic;
using Newtonsoft.Json;

namespace NzbDrone.Core.Migration
{
    public class JackettIndexerDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Configured { get; set; }
        [JsonProperty("site_link")]
        public string SiteLink { get; set; }
        public List<string> AlternativeSiteLinks { get; set; }
        public string language { get; set; }

        //ignoring last_error, potatoenabled, caps
    }
}
