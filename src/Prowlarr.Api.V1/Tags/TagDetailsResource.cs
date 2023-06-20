using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Tags;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1.Tags
{
    public class TagDetailsResource : RestResource
    {
        public string Label { get; set; }
        public List<int> NotificationIds { get; set; }
        public List<int> IndexerIds { get; set; }
        public List<int> IndexerProxyIds { get; set; }
        public List<int> ApplicationIds { get; set; }
    }

    public static class TagDetailsResourceMapper
    {
        public static TagDetailsResource ToResource(this TagDetails model)
        {
            if (model == null)
            {
                return null;
            }

            return new TagDetailsResource
            {
                Id = model.Id,
                Label = model.Label,
                NotificationIds = model.NotificationIds,
                IndexerIds = model.IndexerIds,
                IndexerProxyIds = model.IndexerProxyIds,
                ApplicationIds = model.ApplicationIds
            };
        }

        public static List<TagDetailsResource> ToResource(this IEnumerable<TagDetails> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
