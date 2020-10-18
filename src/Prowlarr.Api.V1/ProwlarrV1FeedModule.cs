using Prowlarr.Http;

namespace Prowlarr.Api.V1
{
    public abstract class RadarrV3FeedModule : RadarrModule
    {
        protected RadarrV3FeedModule(string resource)
            : base("/feed/v1/" + resource.Trim('/'))
        {
        }
    }
}
