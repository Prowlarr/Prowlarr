using Prowlarr.Http;

namespace Prowlarr.Api.V1
{
    public abstract class ProwlarrV3Module : ProwlarrModule
    {
        protected ProwlarrV3Module(string resource)
            : base("/api/v1/" + resource.Trim('/'))
        {
        }
    }
}
