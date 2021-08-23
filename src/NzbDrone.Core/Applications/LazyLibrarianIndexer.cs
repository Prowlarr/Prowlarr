using System.Collections.Generic;

namespace NzbDrone.Core.Applications.LazyLibrarian
{
    public class LazyLibrarianIndexerResponse
    {
        public bool Success { get; set; }
        public LazyLibrarianIndexerData Data { get; set; }
        public LazyLibrarianError Error { get; set; }
    }

    public class LazyLibrarianIndexerData
    {
        public List<LazyLibrarianIndexer> Torznabs { get; set; }
        public List<LazyLibrarianIndexer> Newznabs { get; set; }
    }

    public enum LazyLibrarianProviderType
    {
        Newznab,
        Torznab
    }

    public class LazyLibrarianIndexer
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public string Apikey { get; set; }
        public string Categories { get; set; }
        public bool Enabled { get; set; }
        public string Altername { get; set; }
        public LazyLibrarianProviderType Type { get; set; }

        public bool Equals(LazyLibrarianIndexer other)
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
