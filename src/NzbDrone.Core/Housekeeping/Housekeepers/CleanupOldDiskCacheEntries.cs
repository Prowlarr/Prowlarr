using NzbDrone.Core.Cache;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOldDiskCacheEntries(IDiskCacheService diskCacheService) : IHousekeepingTask
    {
        public void Clean()
        {
            diskCacheService.Cleanup();
        }
    }
}
