using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.Newznab
{
    public class NewznabRequestGenerator : IIndexerRequestGenerator
    {
        private readonly INewznabCapabilitiesProvider _capabilitiesProvider;
        public int MaxPages { get; set; }
        public int PageSize { get; set; }
        public NewznabSettings Settings { get; set; }

        public NewznabRequestGenerator(INewznabCapabilitiesProvider capabilitiesProvider)
        {
            _capabilitiesProvider = capabilitiesProvider;

            MaxPages = 30;
            PageSize = 100;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

            var pageableRequests = new IndexerPageableRequestChain();
            var parameters = string.Empty;

            if (searchCriteria.TmdbId.HasValue && capabilities.MovieSearchTmdbAvailable)
            {
                parameters += string.Format("&tmdbid={0}", searchCriteria.TmdbId.Value);
            }

            if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace() && capabilities.MovieSearchImdbAvailable)
            {
                parameters += string.Format("&imdbid={0}", searchCriteria.ImdbId);
            }

            if (searchCriteria.TraktId.HasValue && capabilities.MovieSearchTraktAvailable)
            {
                parameters += string.Format("&traktid={0}", searchCriteria.ImdbId);
            }

            if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
            {
                parameters += string.Format("&q={0}", searchCriteria.SearchTerm);
            }

            pageableRequests.Add(GetPagedRequests(searchCriteria,
                parameters));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

            var pageableRequests = new IndexerPageableRequestChain();
            var parameters = string.Empty;

            if (searchCriteria.TvdbId.HasValue && capabilities.TvSearchTvdbAvailable)
            {
                parameters += string.Format("&tvdbid={0}", searchCriteria.TvdbId.Value);
            }

            if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace() && capabilities.TvSearchImdbAvailable)
            {
                parameters += string.Format("&imdbid={0}", searchCriteria.ImdbId);
            }

            if (searchCriteria.TvMazeId.HasValue && capabilities.TvSearchTvMazeAvailable)
            {
                parameters += string.Format("&tvmazeid={0}", searchCriteria.TvMazeId);
            }

            if (searchCriteria.RId.HasValue && capabilities.TvSearchTvRageAvailable)
            {
                parameters += string.Format("&rid={0}", searchCriteria.RId);
            }

            if (searchCriteria.Season.HasValue && capabilities.TvSearchSeasonAvailable)
            {
                parameters += string.Format("&season={0}", searchCriteria.Season);
            }

            if (searchCriteria.Ep.HasValue && capabilities.TvSearchEpAvailable)
            {
                parameters += string.Format("&ep={0}", searchCriteria.Ep);
            }

            if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
            {
                parameters += string.Format("&q={0}", searchCriteria.SearchTerm);
            }

            pageableRequests.Add(GetPagedRequests(searchCriteria,
                parameters));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var searchQuery = searchCriteria.SearchTerm;

            pageableRequests.Add(GetPagedRequests(searchCriteria,
                    searchQuery.IsNotNullOrWhiteSpace() ? string.Format("&q={0}", NewsnabifyTitle(searchCriteria.SearchTerm)) : string.Empty));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(SearchCriteriaBase searchCriteria, string parameters)
        {
            var baseUrl = string.Format("{0}{1}?t={2}&extended=1", Settings.BaseUrl.TrimEnd('/'), Settings.ApiPath.TrimEnd('/'), searchCriteria.SearchType);
            var categories = searchCriteria.Categories;

            if (categories != null && categories.Any())
            {
                var categoriesQuery = string.Join(",", categories.Distinct());
                baseUrl += string.Format("&cat={0}", categoriesQuery);
            }

            if (Settings.ApiKey.IsNotNullOrWhiteSpace())
            {
                baseUrl += "&apikey=" + Settings.ApiKey;
            }

            if (searchCriteria.Limit.HasValue)
            {
                parameters += string.Format("&limit={0}", searchCriteria.Limit);
            }

            if (searchCriteria.Offset.HasValue)
            {
                parameters += string.Format("&offset={0}", searchCriteria.Offset);
            }

            yield return new IndexerRequest(string.Format("{0}{1}", baseUrl, parameters), HttpAccept.Rss);
        }

        private static string NewsnabifyTitle(string title)
        {
            return title.Replace("+", "%20");
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
