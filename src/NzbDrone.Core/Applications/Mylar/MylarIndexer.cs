using System.Collections.Generic;

namespace NzbDrone.Core.Applications.Mylar
{
    public class MylarIndexerResponse
    {
        public bool Success { get; set; }
        public MylarIndexerData Data { get; set; }
        public MylarError Error { get; set; }
    }

    public class MylarIndexerData
    {
        public List<MylarIndexer> Torznabs { get; set; }
        public List<MylarIndexer> Newznabs { get; set; }
    }

    public enum MylarProviderType
    {
        Newznab,
        Torznab
    }

    public class MylarIndexer
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public string Apikey { get; set; }
        public string Categories { get; set; }
        public bool Enabled { get; set; }
        public string Altername { get; set; }
        public MylarProviderType Type { get; set; }

        public bool Equals(MylarIndexer other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            return other.Host == Host &&
                other.Apikey == Apikey &&
                other.Name == Name &&
                other.Categories == Categories &&
                other.Enabled == Enabled &&
                other.Altername == Altername;
        }
    }
}
