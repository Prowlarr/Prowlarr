using Prowlarr.Http;

namespace Prowlarr.Api.V1
{
    public abstract class ProwlarrV1Module : ProwlarrModule
    {
        protected ProwlarrV1Module(string resource)
            : base("/api/v1/" + resource.Trim('/'))
        {
        }
    }
}
