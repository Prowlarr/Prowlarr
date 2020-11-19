using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NzbDrone.Core.Indexers;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1.Indexers
{
    public class IndexerCapabilityResource : RestResource
    {
        public int? LimitsMax { get; set; }
        public int? LimitsDefault { get; set; }

        public List<IndexerCategory> Categories;
    }

    public static class IndexerCapabilitiesResourceMapper
    {
        public static IndexerCapabilityResource ToResource(this IndexerCapabilities model)
        {
            if (model == null)
            {
                return null;
            }

            return new IndexerCapabilityResource
            {
                LimitsMax = model.LimitsMax,
                LimitsDefault = model.LimitsDefault,
                Categories = model.Categories.GetTorznabCategoryTree()
            };
        }

        public static List<IndexerCapabilityResource> ToResource(this IEnumerable<IndexerCapabilities> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
