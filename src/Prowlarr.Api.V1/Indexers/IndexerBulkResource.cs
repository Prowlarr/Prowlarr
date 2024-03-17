using System.Collections.Generic;
using NzbDrone.Core.Indexers;

namespace Prowlarr.Api.V1.Indexers
{
    public class IndexerBulkResource : ProviderBulkResource<IndexerBulkResource>
    {
        public bool? Enable { get; set; }
        public int? AppProfileId { get; set; }
        public int? Priority { get; set; }
        public int? MinimumSeeders { get; set; }
        public double? SeedRatio { get; set; }
        public int? SeedTime { get; set; }
        public int? PackSeedTime { get; set; }
    }

    public class IndexerBulkResourceMapper : ProviderBulkResourceMapper<IndexerBulkResource, IndexerDefinition>
    {
        public override List<IndexerDefinition> UpdateModel(IndexerBulkResource resource, List<IndexerDefinition> existingDefinitions)
        {
            if (resource == null)
            {
                return new List<IndexerDefinition>();
            }

            existingDefinitions.ForEach(existing =>
            {
                existing.Enable = resource.Enable ?? existing.Enable;
                existing.AppProfileId = resource.AppProfileId ?? existing.AppProfileId;
                existing.Priority = resource.Priority ?? existing.Priority;

                if (existing.Protocol == DownloadProtocol.Torrent)
                {
                    ((ITorrentIndexerSettings)existing.Settings).TorrentBaseSettings.AppMinimumSeeders = resource.MinimumSeeders ?? ((ITorrentIndexerSettings)existing.Settings).TorrentBaseSettings.AppMinimumSeeders;
                    ((ITorrentIndexerSettings)existing.Settings).TorrentBaseSettings.SeedRatio = resource.SeedRatio ?? ((ITorrentIndexerSettings)existing.Settings).TorrentBaseSettings.SeedRatio;
                    ((ITorrentIndexerSettings)existing.Settings).TorrentBaseSettings.SeedTime = resource.SeedTime ?? ((ITorrentIndexerSettings)existing.Settings).TorrentBaseSettings.SeedTime;
                    ((ITorrentIndexerSettings)existing.Settings).TorrentBaseSettings.PackSeedTime = resource.PackSeedTime ?? ((ITorrentIndexerSettings)existing.Settings).TorrentBaseSettings.PackSeedTime;
                }
            });

            return existingDefinitions;
        }
    }
}
