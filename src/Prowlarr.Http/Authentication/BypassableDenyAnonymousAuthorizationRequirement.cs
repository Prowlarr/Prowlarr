using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Prowlarr.Http.Authentication
{
    public class BypassableDenyAnonymousAuthorizationRequirement : DenyAnonymousAuthorizationRequirement
    {
    }
}
