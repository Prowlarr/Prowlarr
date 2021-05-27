using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Profiles
{
    public class AppSyncProfile : ModelBase
    {
        public string Name { get; set; }
        public bool EnableRss { get; set; }
        public bool EnableAutomaticSearch { get; set; }
        public bool EnableInteractiveSearch { get; set; }
    }
}
