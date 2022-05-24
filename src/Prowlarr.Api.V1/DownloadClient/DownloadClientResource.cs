using System.Collections.Generic;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;

namespace Prowlarr.Api.V1.DownloadClient
{
    public class DownloadClientResource : ProviderResource<DownloadClientResource>
    {
        public bool Enable { get; set; }
        public DownloadProtocol Protocol { get; set; }
        public int Priority { get; set; }
        public List<DownloadClientCategory> Categories { get; set; }
        public bool SupportsCategories { get; set; }
    }

    public class DownloadClientResourceMapper : ProviderResourceMapper<DownloadClientResource, DownloadClientDefinition>
    {
        public override DownloadClientResource ToResource(DownloadClientDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            var resource = base.ToResource(definition);

            resource.Enable = definition.Enable;
            resource.Protocol = definition.Protocol;
            resource.Priority = definition.Priority;
            resource.Categories = definition.Categories;
            resource.SupportsCategories = definition.SupportsCategories;

            return resource;
        }

        public override DownloadClientDefinition ToModel(DownloadClientResource resource, DownloadClientDefinition existingDefinition)
        {
            if (resource == null)
            {
                return null;
            }

            var definition = base.ToModel(resource, existingDefinition);

            definition.Enable = resource.Enable;
            definition.Protocol = resource.Protocol;
            definition.Priority = resource.Priority;
            definition.Categories = resource.Categories;

            return definition;
        }
    }
}
