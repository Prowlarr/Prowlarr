using System;
using System.Web;

namespace NzbDrone.Common.Extensions
{
    public static class UrlExtensions
    {
        public static bool IsValidUrl(this string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (path.StartsWith(" ") || path.EndsWith(" "))
            {
                return false;
            }

            return Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.IsWellFormedOriginalString();
        }

        public static Uri RemoveQueryParam(this Uri url, string name)
        {
            var uriBuilder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            query.Remove(name);
            uriBuilder.Query = query.ToString() ?? string.Empty;

            return uriBuilder.Uri;
        }

        public static string GetQueryParam(this Uri url, string name)
        {
            var uriBuilder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);

            return query[name];
        }
    }
}
