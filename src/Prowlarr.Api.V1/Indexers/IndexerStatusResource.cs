using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Indexers;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1.Indexers
{
    public class IndexerStatusResource : RestResource
    {
        public int IndexerId { get; set; }
        public DateTime? DisabledTill { get; set; }
        public DateTime? MostRecentFailure { get; set; }
        public DateTime? InitialFailure { get; set; }
    }

    public static class IndexerStatusResourceMapper
    {
        public static IndexerStatusResource ToResource(this IndexerStatus model)
        {
            if (model == null)
            {
                return null;
            }

            return new IndexerStatusResource
            {
                IndexerId = model.ProviderId,
                DisabledTill = model.DisabledTill,
                MostRecentFailure = model.MostRecentFailure,
                InitialFailure = model.InitialFailure,
            };
        }

        public static List<IndexerStatusResource> ToResource(this IEnumerable<IndexerStatus> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
