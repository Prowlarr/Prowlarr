using Prowlarr.Http;

namespace Prowlarr.Api.V1
{
    public abstract class ProwlarrV3FeedModule : ProwlarrModule
    {
        protected ProwlarrV3FeedModule(string resource)
            : base("/feed/v1/" + resource.Trim('/'))
        {
        }
    }
}
