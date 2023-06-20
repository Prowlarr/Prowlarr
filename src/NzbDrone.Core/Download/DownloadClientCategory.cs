using System.Collections.Generic;

namespace NzbDrone.Core.Download
{
    public class DownloadClientCategory
    {
        public string ClientCategory { get; set; }
        public List<int> Categories { get; set; }
    }
}
