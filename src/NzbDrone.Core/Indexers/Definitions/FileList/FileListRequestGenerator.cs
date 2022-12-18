using System;
using System.Collections.Generic;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.FileList
{
    public class FileListRequestGenerator : IIndexerRequestGenerator
    {
        public FileListSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }
        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public virtual IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace())
            {
                pageableRequests.Add(GetRequest("search-torrents", searchCriteria.Categories, string.Format("&type=imdb&query={0}", searchCriteria.FullImdbId)));
            }
            else if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
            {
                var titleYearSearchQuery = string.Format("{0}", searchCriteria.SanitizedSearchTerm);
                pageableRequests.Add(GetRequest("search-torrents", searchCriteria.Categories, string.Format("&type=name&query={0}", titleYearSearchQuery.Trim())));
            }
            else
            {
                pageableRequests.Add(GetRequest("latest-torrents", searchCriteria.Categories, ""));
            }

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
            {
                var titleYearSearchQuery = string.Format("{0}", searchCriteria.SanitizedSearchTerm);
                pageableRequests.Add(GetRequest("search-torrents", searchCriteria.Categories, string.Format("&type=name&query={0}", titleYearSearchQuery.Trim())));
            }
            else
            {
                pageableRequests.Add(GetRequest("latest-torrents", searchCriteria.Categories, ""));
            }

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace())
            {
                pageableRequests.Add(GetRequest("search-torrents", searchCriteria.Categories, string.Format("&type=imdb&query={0}&season={1}&episode={2}", searchCriteria.FullImdbId, searchCriteria.Season, searchCriteria.Episode)));
            }
            else if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
            {
                var titleYearSearchQuery = string.Format("{0}", searchCriteria.SanitizedSearchTerm);
                pageableRequests.Add(GetRequest("search-torrents", searchCriteria.Categories, string.Format("&type=name&query={0}&season={1}&episode={2}", titleYearSearchQuery.Trim(), searchCriteria.Season, searchCriteria.Episode)));
            }
            else
            {
                pageableRequests.Add(GetRequest("latest-torrents", searchCriteria.Categories, ""));
            }

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
            {
                var titleYearSearchQuery = string.Format("{0}", searchCriteria.SanitizedSearchTerm);
                pageableRequests.Add(GetRequest("search-torrents", searchCriteria.Categories, string.Format("&type=name&query={0}", titleYearSearchQuery.Trim())));
            }
            else
            {
                pageableRequests.Add(GetRequest("latest-torrents", searchCriteria.Categories, ""));
            }

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
            {
                var titleYearSearchQuery = string.Format("{0}", searchCriteria.SanitizedSearchTerm);
                pageableRequests.Add(GetRequest("search-torrents", searchCriteria.Categories, string.Format("&type=name&query={0}", titleYearSearchQuery.Trim())));
            }
            else
            {
                pageableRequests.Add(GetRequest("latest-torrents", searchCriteria.Categories, ""));
            }

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetRequest(string searchType, int[] categories, string parameters)
        {
            var categoriesQuery = string.Join(",", Capabilities.Categories.MapTorznabCapsToTrackers(categories));

            var baseUrl = string.Format("{0}/api.php?action={1}&category={2}&username={3}&passkey={4}{5}", Settings.BaseUrl.TrimEnd('/'), searchType, categoriesQuery, Settings.Username.Trim(), Settings.Passkey.Trim(), parameters);

            if (Settings.FreeleechOnly)
            {
                baseUrl += "&freeleech=1";
            }

            yield return new IndexerRequest(baseUrl, HttpAccept.Json);
        }
    }
}
