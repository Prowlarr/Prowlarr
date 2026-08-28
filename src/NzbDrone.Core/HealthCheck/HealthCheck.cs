using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.HealthCheck
{
    public class HealthCheck : ModelBase
    {
        private static readonly Regex CleanFragmentRegex = new Regex("[^a-z ]", RegexOptions.Compiled);

        public Type Source { get; set; }
        public HealthCheckResult Type { get; set; }
        public string Message { get; set; }
        public HttpUri WikiUrl { get; set; }

        /// <summary>
        /// Ids of the indexers this health check relates to, when applicable. Used to scope
        /// notifications by tag; an empty collection means the check is not tied to specific
        /// indexers (e.g. a system check) and applies to every notification.
        /// </summary>
        public IEnumerable<int> RelatedProviders { get; set; } = Array.Empty<int>();

        public HealthCheck()
        {
        }

        public HealthCheck(Type source)
        {
            Source = source;
            Type = HealthCheckResult.Ok;
        }

        public HealthCheck(Type source, HealthCheckResult type, string message, string wikiFragment = null)
        {
            Source = source;
            Type = type;
            Message = message;
            WikiUrl = MakeWikiUrl(wikiFragment ?? MakeWikiFragment(message));
        }

        private static string MakeWikiFragment(string message)
        {
            return "#" + CleanFragmentRegex.Replace(message.ToLower(), string.Empty).Replace(' ', '-');
        }

        private static HttpUri MakeWikiUrl(string fragment)
        {
            return new HttpUri("https://wiki.servarr.com/prowlarr/system#") + new HttpUri(fragment);
        }
    }

    public enum HealthCheckResult
    {
        Ok = 0,
        Notice = 1,
        Warning = 2,
        Error = 3
    }
}
