using System;
using System.Collections.Generic;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.Definitions.TorrentRss
{
    public class TorrentRssIndexerRequestGenerator : IIndexerRequestGenerator
    {
        public TorrentRssIndexerSettings Settings { get; set; }

        public virtual IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            return BuildPageableRssRequests(searchCriteria);
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return BuildPageableRssRequests(searchCriteria);
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            return BuildPageableRssRequests(searchCriteria);
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return BuildPageableRssRequests(searchCriteria);
        }

        public virtual IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            return BuildPageableRssRequests(searchCriteria);
        }

        private IndexerPageableRequestChain BuildPageableRssRequests(SearchCriteriaBase searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            if (searchCriteria.IsRssSearch)
            {
                pageableRequests.Add(GetRssRequests());
            }

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetRssRequests()
        {
            var request = new IndexerRequest(Settings.BaseUrl.Trim().TrimEnd('/'), HttpAccept.Rss);

            if (Settings.Cookie.IsNotNullOrWhiteSpace())
            {
                foreach (var cookie in HttpHeader.ParseCookies(Settings.Cookie))
                {
                    request.HttpRequest.Cookies[cookie.Key] = cookie.Value;
                }
            }

            yield return request;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
