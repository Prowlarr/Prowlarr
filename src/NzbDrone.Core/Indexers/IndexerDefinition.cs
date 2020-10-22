using System;
using System.Collections.Generic;
using NzbDrone.Core.Indexers.Cardigann;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers
{
    public class IndexerDefinition : ProviderDefinition
    {
        public bool EnableRss { get; set; }
        public bool EnableAutomaticSearch { get; set; }
        public bool EnableInteractiveSearch { get; set; }
        public DownloadProtocol Protocol { get; set; }
        public IndexerPrivacy Privacy { get; set; }
        public bool SupportsRss { get; set; }
        public bool SupportsSearch { get; set; }
        public IndexerCapabilities Capabilities { get; set; }
        public int Priority { get; set; } = 25;
        public DateTime Added { get; set; }

        public override bool Enable => EnableRss || EnableAutomaticSearch || EnableInteractiveSearch;

        public IndexerStatus Status { get; set; }

        public List<SettingsField> ExtraFields { get; set; } = new List<SettingsField>();
    }
}
