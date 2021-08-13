using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Applications
{
    public class AppIndexerMap : ModelBase
    {
        public int IndexerId { get; set; }
        public int AppId { get; set; }
        public int RemoteIndexerId { get; set; }
        public string RemoteIndexerName { get; set; }
    }
}
