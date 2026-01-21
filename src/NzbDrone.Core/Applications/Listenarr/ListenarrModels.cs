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
        public bool EnableRss { get; set; }
        public bool EnableAutomaticSearch { get; set; }
        public bool EnableInteractiveSearch { get; set; }
        public int Priority { get; set; }
        public string Name { get; set; }
        public string ImplementationName { get; set; }
        public string Implementation { get; set; }
        public List<string> Implementations { get; set; }
        public string ConfigContract { get; set; }
        public string InfoLink { get; set; }
        public int? DownloadClientId { get; set; }
        public HashSet<int> Tags { get; set; }
        public List<ListenarrField> Fields { get; set; }
    }

    public class ListenarrField
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public string Type { get; set; }
        public bool Advanced { get; set; }
        public string Section { get; set; }
        public string Hidden { get; set; }

        public ListenarrField Clone()
        {
            return (ListenarrField)MemberwiseClone();
        }
    }

    public class ListenarrTag
    {
        public int Id { get; set; }
        public string Label { get; set; }
    }
}
