using Microsoft.AspNetCore.Builder;

namespace Prowlarr.Host.Middleware
{
    public interface IAspNetCoreMiddleware
    {
        int Order { get; }
        void Attach(IApplicationBuilder appBuilder);
    }
}
