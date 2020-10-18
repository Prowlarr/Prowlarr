using System.Collections.Generic;
using Newtonsoft.Json;
using NzbDrone.Core.Languages;

namespace NzbDrone.Core.Parser.Model
{
    public class ParsedMovieInfo
    {
        public string MovieTitle { get; set; }
        public string OriginalTitle { get; set; }
        public string ReleaseTitle { get; set; }
        public string SimpleReleaseTitle { get; set; }
        public List<Language> Languages { get; set; } = new List<Language>();
        public string ReleaseGroup { get; set; }
        public string ReleaseHash { get; set; }
        public string Edition { get; set; }
        public int Year { get; set; }
        public string ImdbId { get; set; }
        [JsonIgnore]
        public Dictionary<string, object> ExtraInfo { get; set; } = new Dictionary<string, object>();

        public override string ToString()
        {
            return string.Format("{0} - {1}", MovieTitle, Year);
        }
    }
}
