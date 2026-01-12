using System.Collections.Generic;

namespace NzbDrone.Core.Applications.Listenarr
{
    public class ListenarrStatus
    {
        public string Version { get; set; }
    }

    public class ListenarrIndexer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool EnableRss { get; set; }
        public bool EnableAutomaticSearch { get; set; }
        public bool EnableInteractiveSearch { get; set; }
        public int Priority { get; set; }
        public string ConfigContract { get; set; }
        public string Implementation { get; set; }
        public string Protocol { get; set; }
        public string BaseUrl { get; set; }
        public List<ListenarrField> Fields { get; set; }
        public List<int> Tags { get; set; }
    }

    public class ListenarrField
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }

    public class ListenarrTag
    {
        public int Id { get; set; }
        public string Label { get; set; }
    }
}
