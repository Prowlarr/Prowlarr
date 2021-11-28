using System.Collections.Generic;

namespace NzbDrone.Core.Indexers.Cardigann
{
    public class CardigannMetaDefinition
    {
        public string Id { get; set; }
        public string File { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Language { get; set; }
        public string Encoding { get; set; }
        public List<string> Links { get; set; }
        public List<string> Legacylinks { get; set; }
        public List<SettingsField> Settings { get; set; }
        public string Sha { get; set; }
        public LoginBlock Login { get; set; }
        public string Protocol { get; set; }
        public bool Allowdownloadredirect { get; set; }
    }
}
