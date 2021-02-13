using System;
using System.Collections.Generic;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.AwesomeHD
{
    public class AwesomeHDRequestGenerator : IIndexerRequestGenerator
    {
        public AwesomeHDSettings Settings { get; set; }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(null));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            var parameters = string.Empty;

            if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace())
            {
                parameters = string.Format("&action=imdbsearch&imdb={0}", searchCriteria.ImdbId);
            }
            else if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
            {
                parameters = string.Format("&action=titlesearch&title={0}", searchCriteria.SearchTerm);
            }
            else
            {
                parameters = "&action=latestmovies";
            }

            pageableRequests.Add(GetRequest(parameters));
            return pageableRequests;
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
            return new IndexerPageableRequestChain();
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        private IEnumerable<IndexerRequest> GetRequest(string searchParameters)
        {
            if (searchParameters != null)
            {
                yield return new IndexerRequest($"{Settings.BaseUrl.Trim().TrimEnd('/')}/searchapi.php?passkey={Settings.Passkey.Trim()}{searchParameters}", HttpAccept.Rss);
            }
        }
    }
}
