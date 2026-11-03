using NzbDrone.Core.Configuration;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1.Config
{
    public class IndexerConfigResource : RestResource
    {
        public string IndexerUserAgent { get; set; }
    }

    public static class IndexerConfigResourceMapper
    {
        public static IndexerConfigResource ToResource(IConfigService model)
        {
            return new IndexerConfigResource
            {
                IndexerUserAgent = model.IndexerUserAgent
            };
        }
    }
}
