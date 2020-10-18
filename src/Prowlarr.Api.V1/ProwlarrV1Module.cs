using Prowlarr.Http;

namespace Prowlarr.Api.V1
{
    public abstract class RadarrV3Module : RadarrModule
    {
        protected RadarrV3Module(string resource)
            : base("/api/v1/" + resource.Trim('/'))
        {
        }
    }
}
