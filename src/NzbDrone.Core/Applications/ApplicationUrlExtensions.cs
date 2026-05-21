using System;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Applications
{
    public static class ApplicationUrlExtensions
    {
        // HttpUri's parser rejects user:pass@ userinfo, so proxies that talk to *arr
        // apps behind a Basic-auth-protected reverse proxy need to peel the credentials
        // off the URL up-front. ManagedHttpDispatcher converts BasicNetworkCredential
        // into the Authorization header for the outgoing request.
        public static string StripCredentials(this string url, out BasicNetworkCredential credentials)
        {
            credentials = null;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || string.IsNullOrEmpty(parsed.UserInfo))
            {
                return url;
            }

            var parts = parsed.UserInfo.Split(':', 2);
            var user = Uri.UnescapeDataString(parts[0]);
            var pass = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;

            credentials = new BasicNetworkCredential(user, pass);

            return $"{parsed.Scheme}://{parsed.Authority}{parsed.PathAndQuery}{parsed.Fragment}";
        }
    }
}
