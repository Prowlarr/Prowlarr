using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.PassThePopcorn
{
    public class PassThePopcornRequestGenerator : IIndexerRequestGenerator
    {
        public PassThePopcornSettings Settings { get; set; }

        public IDictionary<string, string> Cookies { get; set; }

        public IIndexerHttpClient HttpClient { get; set; }
        public Logger Logger { get; set; }

        public IEnumerable<IndexerRequest> GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace())
            {
                return GetRequest(searchCriteria.FullImdbId);
            }

            return GetRequest(string.Format("{0}", searchCriteria.SearchTerm));
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        private IEnumerable<IndexerRequest> GetRequest(string searchParameters)
        {
            var queryParams = new NameValueCollection
            {
                { "action", "advanced" },
                { "json", "noredirect" },
                { "searchstr", searchParameters }
            };

            if (Settings.FreeleechOnly)
            {
                queryParams.Add("freetorrent", "1");
            }

            var request =
                new IndexerRequest(
                    $"{Settings.BaseUrl.Trim().TrimEnd('/')}/torrents.php?{queryParams.GetQueryString()}",
                    HttpAccept.Json);

            request.HttpRequest.Headers["ApiUser"] = Settings.APIUser;
            request.HttpRequest.Headers["ApiKey"] = Settings.APIKey;

            if (Settings.APIKey.IsNullOrWhiteSpace())
            {
                foreach (var cookie in Cookies)
                {
                    request.HttpRequest.Cookies[cookie.Key] = cookie.Value;
                }

                CookiesUpdater(Cookies, DateTime.Now.AddDays(30));
            }

            yield return request;
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return new List<IndexerRequest>();
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            return new List<IndexerRequest>();
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new List<IndexerRequest>();
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            return GetRequest(string.Format("{0}", searchCriteria.SearchTerm));
        }
    }
}
