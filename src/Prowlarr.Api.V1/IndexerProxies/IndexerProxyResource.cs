using NzbDrone.Core.IndexerProxies;

namespace Prowlarr.Api.V1.IndexerProxies
{
    public class IndexerProxyResource : ProviderResource<IndexerProxyResource>
    {
        public string Link { get; set; }
        public bool OnHealthIssue { get; set; }
        public bool SupportsOnHealthIssue { get; set; }
        public bool IncludeHealthWarnings { get; set; }
        public string TestCommand { get; set; }
    }

    public class IndexerProxyResourceMapper : ProviderResourceMapper<IndexerProxyResource, IndexerProxyDefinition>
    {
        public override IndexerProxyResource ToResource(IndexerProxyDefinition definition)
        {
            if (definition == null)
            {
                return default(IndexerProxyResource);
            }

            var resource = base.ToResource(definition);

            return resource;
        }

        public override IndexerProxyDefinition ToModel(IndexerProxyResource resource, IndexerProxyDefinition existingDefinition)
        {
            if (resource == null)
            {
                return default(IndexerProxyDefinition);
            }

            var definition = base.ToModel(resource, existingDefinition);

            return definition;
        }
    }
}
