using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.Gazelle
{
    public class GazelleRequestGenerator : IIndexerRequestGenerator
    {
        public GazelleSettings Settings { get; set; }

        public IDictionary<string, string> AuthCookieCache { get; set; }
        public IIndexerHttpClient HttpClient { get; set; }
        public IndexerCapabilities Capabilities { get; set; }
        public Logger Logger { get; set; }

        protected virtual string APIUrl => Settings.BaseUrl + "ajax.php";
        protected virtual bool ImdbInTags => false;

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public virtual IndexerPageableRequestChain GetRecentRequests()
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest(null));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetRequest(string searchParameters)
        {
            var filter = "";
            if (searchParameters == null)
            {
            }

            var request =
                new IndexerRequest(
                    $"{APIUrl}?{searchParameters}{filter}",
                    HttpAccept.Json);

            yield return request;
        }

        private string GetBasicSearchParameters(string searchTerm, int[] categories)
        {
            var searchString = GetSearchTerm(searchTerm);

            var parameters = "action=browse&order_by=time&order_way=desc";

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                parameters += string.Format("&searchstr={0}", searchString);
            }

            if (categories != null)
            {
                foreach (var cat in Capabilities.Categories.MapTorznabCapsToTrackers(categories))
                {
                    parameters += string.Format("&filter_cat[{0}]=1", cat);
                }
            }

            return parameters;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var parameters = GetBasicSearchParameters(searchCriteria.SearchTerm, searchCriteria.Categories);

            if (searchCriteria.ImdbId != null)
            {
                if (ImdbInTags)
                {
                    parameters += string.Format("&taglist={0}", searchCriteria.FullImdbId);
                }
                else
                {
                    parameters += string.Format("&cataloguenumber={0}", searchCriteria.FullImdbId);
                }
            }

            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(parameters));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var parameters = GetBasicSearchParameters(searchCriteria.SearchTerm, searchCriteria.Categories);

            if (searchCriteria.Artist != null)
            {
                parameters += string.Format("&artistname={0}", searchCriteria.Artist);
            }

            if (searchCriteria.Label != null)
            {
                parameters += string.Format("&recordlabel={0}", searchCriteria.Label);
            }

            if (searchCriteria.Album != null)
            {
                parameters += string.Format("&groupname={0}", searchCriteria.Album);
            }

            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(parameters));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var parameters = GetBasicSearchParameters(searchCriteria.SanitizedTvSearchString, searchCriteria.Categories);

            if (searchCriteria.ImdbId != null)
            {
                if (ImdbInTags)
                {
                    parameters += string.Format("&taglist={0}", searchCriteria.FullImdbId);
                }
                else
                {
                    parameters += string.Format("&cataloguenumber={0}", searchCriteria.FullImdbId);
                }
            }

            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(parameters));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var parameters = GetBasicSearchParameters(searchCriteria.SearchTerm, searchCriteria.Categories);

            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(parameters));
            return pageableRequests;
        }

        // hook to adjust the search term
        protected virtual string GetSearchTerm(string term) => term;

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var parameters = GetBasicSearchParameters(searchCriteria.SearchTerm, searchCriteria.Categories);

            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(parameters));
            return pageableRequests;
        }
    }
}
