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

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace())
            {
                pageableRequests.Add(GetRequest(searchCriteria.FullImdbId, searchCriteria));
            }
            else
            {
                pageableRequests.Add(GetRequest($"{searchCriteria.SearchTerm}", searchCriteria));
            }

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        private IEnumerable<IndexerRequest> GetRequest(string searchParameters, SearchCriteriaBase searchCriteria)
        {
            var queryParams = new NameValueCollection
            {
                { "action", "advanced" },
                { "json", "noredirect" },
                { "grouping", "0" },
                { "searchstr", searchParameters }
            };

            if (searchCriteria.Limit is > 0 && searchCriteria.Offset is > 0)
            {
                var page = (int)(searchCriteria.Offset / searchCriteria.Limit) + 1;
                queryParams.Set("page", page.ToString());
            }

            if (Settings.FreeleechOnly)
            {
                queryParams.Set("freetorrent", "1");
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

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest($"{searchCriteria.SearchTerm}", searchCriteria));

            return pageableRequests;
        }
    }
}
