using System;
using Dapper;
using NLog;
using NzbDrone.Core.Cache;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOldDownloadCacheEntries(IDownloadCacheService downloadCacheService,
                                                ISqliteCacheDatabase cacheDatabase,
                                                Logger logger) : IHousekeepingTask
    {
        public void Clean()
        {
            if (downloadCacheService.IsEnabled)
            {
                downloadCacheService.Cleanup();
            }

            try
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                using var connection = cacheDatabase.OpenConnection();
                var expiredOutputCount = connection.Execute("DELETE FROM OutputCache WHERE ExpiresAt <= @now;", new { now });

                if (expiredOutputCount > 0)
                {
                    logger.Debug("Evicted {0} expired records from OutputCache", expiredOutputCount);
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Failed to evict expired records from OutputCache");
            }
        }
    }
}
