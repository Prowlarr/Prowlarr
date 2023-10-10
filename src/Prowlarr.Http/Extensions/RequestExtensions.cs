using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.Datastore;

namespace Prowlarr.Http.Extensions
{
    public static class RequestExtensions
    {
        public static bool IsApiRequest(this HttpRequest request)
        {
            return request.Path.StartsWithSegments("/api", StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool GetBooleanQueryParameter(this HttpRequest request, string parameter, bool defaultValue = false)
        {
            var parameterValue = request.Query[parameter];

            if (parameterValue.Any())
            {
                return bool.Parse(parameterValue.ToString());
            }

            return defaultValue;
        }

        public static PagingResource<TResource> ApplyToPage<TResource, TModel>(this PagingSpec<TModel> pagingSpec, Func<PagingSpec<TModel>, PagingSpec<TModel>> function, Converter<TModel, TResource> mapper)
        {
            pagingSpec = function(pagingSpec);

            return new PagingResource<TResource>
            {
                Page = pagingSpec.Page,
                PageSize = pagingSpec.PageSize,
                SortDirection = pagingSpec.SortDirection,
                SortKey = pagingSpec.SortKey,
                TotalRecords = pagingSpec.TotalRecords,
                Records = pagingSpec.Records.ConvertAll(mapper)
            };
        }

        public static string GetRemoteIP(this HttpContext context)
        {
            return context?.Request?.GetRemoteIP() ?? "Unknown";
        }

        public static string GetRemoteIP(this HttpRequest request)
        {
            if (request == null)
            {
                return "Unknown";
            }

            var remoteIP = request.HttpContext.Connection.RemoteIpAddress;

            if (remoteIP.IsIPv4MappedToIPv6)
            {
                remoteIP = remoteIP.MapToIPv4();
            }

            return remoteIP.ToString();
        }

        public static string GetSource(this HttpRequest request)
        {
            if (request.Headers.TryGetValue("X-Prowlarr-Client", out var source))
            {
                return "Prowlarr";
            }

            return NzbDrone.Common.Http.UserAgentParser.ParseSource(request.Headers["User-Agent"]);
        }

        public static string GetHostName(this HttpRequest request)
        {
            var ip = request.GetRemoteIP();

            try
            {
                var myIP = IPAddress.Parse(ip);
                var getIPHost = Dns.GetHostEntry(myIP);
                var compName = getIPHost.HostName.ToString().Split('.').ToList();
                return compName.First();
            }
            catch
            {
                return ip;
            }
        }

        public static string GetServerUrl(this HttpRequest request)
        {
            var scheme = request.Scheme;
            var port = request.HttpContext.Request.Host.Port;

            // Check for protocol headers added by reverse proxys
            // X-Forwarded-Proto: A de facto standard for identifying the originating protocol of an HTTP request
            var xForwardedProto = request.Headers.Where(x => x.Key == "X-Forwarded-Proto").Select(x => x.Value).FirstOrDefault();

            if (xForwardedProto.Any())
            {
                scheme = xForwardedProto.First();
            }

            // Front-End-Https: Non-standard header field used by Microsoft applications and load-balancers
            else if (request.Headers.Any(x => x.Key == "Front-End-Https" && x.Value.FirstOrDefault() == "on"))
            {
                scheme = "https";
            }

            //default to 443 if the Host header doesn't contain the port (needed for reverse proxy setups)
            if (scheme == "https" && !request.Host.Port.HasValue)
            {
                port = 443;
            }

            return $"{scheme}://{request.Host.Host}:{port}";
        }

        public static void DisableCache(this IHeaderDictionary headers)
        {
            headers.Remove("Last-Modified");
            headers["Cache-Control"] = "no-cache, no-store";
            headers["Expires"] = "-1";
            headers["Pragma"] = "no-cache";
        }

        public static void EnableCache(this IHeaderDictionary headers)
        {
            headers["Cache-Control"] = "max-age=31536000, public";
            headers["Last-Modified"] = BuildInfo.BuildDateTime.ToString("r");
        }
    }
}
