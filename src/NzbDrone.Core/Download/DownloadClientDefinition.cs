using System.Collections.Generic;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Download
{
    public class DownloadClientDefinition : ProviderDefinition
    {
        public DownloadClientDefinition()
        {
            Categories = new List<DownloadClientCategory>();
        }

        public List<DownloadClientCategory> Categories { get; set; }
        public bool SupportsCategories { get; set; }
        public DownloadProtocol Protocol { get; set; }
        public int Priority { get; set; } = 1;
    }
}
