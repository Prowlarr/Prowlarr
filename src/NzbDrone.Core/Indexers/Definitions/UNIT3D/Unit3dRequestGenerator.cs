using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Definitions.UNIT3D
{
    public class Unit3dRequestGenerator : IIndexerRequestGenerator
    {
        public Unit3dSettings Settings { get; set; }

        public IIndexerHttpClient HttpClient { get; set; }
        public IndexerCapabilities Capabilities { get; set; }
        public Logger Logger { get; set; }

        protected virtual string SearchUrl => Settings.BaseUrl + "api/torrents/filter";
        protected virtual bool ImdbInTags => false;

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var parameters = GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories);

            if (searchCriteria.ImdbId != null)
            {
                parameters.Add("imdb", searchCriteria.ImdbId);
                parameters.Add("imdbId", searchCriteria.ImdbId);
            }

            if (searchCriteria.TmdbId > 0)
            {
                parameters.Add("tmdb", searchCriteria.TmdbId.ToString());
                parameters.Add("tmdbId", searchCriteria.TmdbId.ToString());
            }

            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(parameters));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var parameters = GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories);

            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(parameters));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var parameters = GetBasicSearchParameters(searchCriteria.SanitizedTvSearchString, searchCriteria.Categories);

            if (searchCriteria.ImdbId != null)
            {
                parameters.Add("imdb", searchCriteria.ImdbId);
                parameters.Add("imdbId", searchCriteria.ImdbId);
            }

            if (searchCriteria.TvdbId > 0)
            {
                parameters.Add("tvdb", searchCriteria.TvdbId.ToString());
                parameters.Add("tvdbId", searchCriteria.TvdbId.ToString());
            }

            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(parameters));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var parameters = GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories);

            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(parameters));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var parameters = GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories);

            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(parameters));
            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetRequest(List<KeyValuePair<string, string>> searchParameters)
        {
            var searchUrl = SearchUrl + "?" + searchParameters.GetQueryString();

            var request = new IndexerRequest(searchUrl, HttpAccept.Json);

            yield return request;
        }

        private List<KeyValuePair<string, string>> GetBasicSearchParameters(string searchTerm, int[] categories)
        {
            var searchString = searchTerm;

            var qc = new List<KeyValuePair<string, string>>
            {
                { "api_token", Settings.ApiKey }
            };

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                qc.Add("name", searchString);
            }

            if (categories != null)
            {
                foreach (var cat in Capabilities.Categories.MapTorznabCapsToTrackers(categories))
                {
                    qc.Add("categories[]", cat);
                }
            }

            return qc;
        }
    }
}
