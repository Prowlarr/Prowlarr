using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.IndexerStats;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1.Indexers
{
    public class IndexerStatsResource : RestResource
    {
        public int IndexerId { get; set; }
        public string IndexerName { get; set; }
        public int NumberOfQueries { get; set; }
        public int AverageResponseTime { get; set; }
        public int NumberOfGrabs { get; set; }
    }

    public static class IndexerStatsResourceMapper
    {
        public static IndexerStatsResource ToResource(this IndexerStatistics model)
        {
            if (model == null)
            {
                return null;
            }

            return new IndexerStatsResource
            {
                IndexerId = model.IndexerId,
                IndexerName = model.IndexerName,
                NumberOfQueries = model.NumberOfQueries,
                AverageResponseTime = model.AverageResponseTime,
                NumberOfGrabs = model.NumberOfGrabs
            };
        }

        public static List<IndexerStatsResource> ToResource(this IEnumerable<IndexerStatistics> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
