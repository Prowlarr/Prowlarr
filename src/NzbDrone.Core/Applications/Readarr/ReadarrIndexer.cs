using System.Collections.Generic;

namespace NzbDrone.Core.Applications.Readarr
{
    public class ReadarrIndexer
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
        public List<ReadarrField> Fields { get; set; }
    }
}
