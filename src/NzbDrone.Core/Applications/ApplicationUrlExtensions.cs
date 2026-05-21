using System;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Applications
{
    public static class ApplicationUrlExtensions
    {
        // Returns the URL with any user:pass@ userinfo removed.
        // Used to normalize callback URLs read back from the external app before
        // matching them against the user-entered ProwlarrUrl.
        public static string StripCredentials(this string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || string.IsNullOrEmpty(parsed.UserInfo))
            {
                return url;
            }

            return $"{parsed.Scheme}://{parsed.Authority}{parsed.PathAndQuery}{parsed.Fragment}";
        }

        // Returns the URL with user:pass@ embedded into the authority.
        // External apps store the Prowlarr callback URL as a single string with no
        // separate auth fields, so credentials need to be inlined when Prowlarr
        // sits behind a Basic-auth-protected reverse proxy.
        public static string WithBasicAuth(this string url, string user, string pass)
        {
            if (user.IsNullOrWhiteSpace() && pass.IsNullOrWhiteSpace())
            {
                return url;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed))
            {
                return url;
            }

            var userInfo = $"{Uri.EscapeDataString(user ?? string.Empty)}:{Uri.EscapeDataString(pass ?? string.Empty)}";

            return $"{parsed.Scheme}://{userInfo}@{parsed.Authority}{parsed.PathAndQuery}{parsed.Fragment}";
        }
    }
}
