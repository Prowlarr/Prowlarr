using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;

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
            var parameters = new NameValueCollection();

            if (searchCriteria.TmdbId.HasValue && capabilities.MovieSearchTmdbAvailable)
            {
                parameters.Add("tmdbid", searchCriteria.TmdbId.Value.ToString());
            }

            if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace() && capabilities.MovieSearchImdbAvailable)
            {
                parameters.Add("imdbid", searchCriteria.ImdbId);
            }

            if (searchCriteria.TraktId.HasValue && capabilities.MovieSearchTraktAvailable)
            {
                parameters.Add("traktid", searchCriteria.TraktId.ToString());
            }

            //Workaround issue with Sphinx search returning garbage results on some indexers. If we don't use id parameters, fallback to t=search
            if (parameters.Count == 0)
            {
                searchCriteria.SearchType = "search";

                if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.SearchAvailable)
                {
                    parameters.Add("q", NewsnabifyTitle(searchCriteria.SearchTerm));
                }
            }
            else
            {
                if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.MovieSearchAvailable)
                {
                    parameters.Add("q", NewsnabifyTitle(searchCriteria.SearchTerm));
                }
            }

            pageableRequests.Add(GetPagedRequests(searchCriteria,
                parameters));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

            var pageableRequests = new IndexerPageableRequestChain();
            var parameters = new NameValueCollection();

            if (searchCriteria.Artist.IsNotNullOrWhiteSpace() && capabilities.MusicSearchArtistAvailable)
            {
                parameters.Add("artist", searchCriteria.Artist);
            }

            if (searchCriteria.Album.IsNotNullOrWhiteSpace() && capabilities.MusicSearchAlbumAvailable)
            {
                parameters.Add("album", searchCriteria.Album);
            }

            //Workaround issue with Sphinx search returning garbage results on some indexers. If we don't use id parameters, fallback to t=search
            if (parameters.Count == 0)
            {
                searchCriteria.SearchType = "search";

                if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.SearchAvailable)
                {
                    parameters.Add("q", NewsnabifyTitle(searchCriteria.SearchTerm));
                }
            }
            else
            {
                if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.MusicSearchAvailable)
                {
                    parameters.Add("q", NewsnabifyTitle(searchCriteria.SearchTerm));
                }
            }

            pageableRequests.Add(GetPagedRequests(searchCriteria,
                parameters));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

            var pageableRequests = new IndexerPageableRequestChain();
            var parameters = new NameValueCollection();

            if (searchCriteria.TvdbId.HasValue && capabilities.TvSearchTvdbAvailable)
            {
                parameters.Add("tvdbid", searchCriteria.TvdbId.Value.ToString());
            }

            if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace() && capabilities.TvSearchImdbAvailable)
            {
                parameters.Add("imdbid", searchCriteria.ImdbId);
            }

            if (searchCriteria.TvMazeId.HasValue && capabilities.TvSearchTvMazeAvailable)
            {
                parameters.Add("tvmazeid", searchCriteria.TvMazeId.ToString());
            }

            if (searchCriteria.RId.HasValue && capabilities.TvSearchTvRageAvailable)
            {
                parameters.Add("rid", searchCriteria.RId.ToString());
            }

            if (searchCriteria.Season.HasValue && capabilities.TvSearchSeasonAvailable)
            {
                // Pad seasons to two decimals due to issues with NNTmux handling season = 0
                parameters.Add("season", searchCriteria.Season.Value.ToString("00"));
            }

            if (searchCriteria.Episode.IsNotNullOrWhiteSpace() && capabilities.TvSearchEpAvailable)
            {
                parameters.Add("ep", searchCriteria.Episode);
            }

            //Workaround issue with Sphinx search returning garbage results on some indexers. If we don't use id parameters, fallback to t=search
            if (parameters.Count == 0)
            {
                searchCriteria.SearchType = "search";

                if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.SearchAvailable)
                {
                    parameters.Add("q", NewsnabifyTitle(searchCriteria.SearchTerm));
                }
            }
            else
            {
                if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.TvSearchAvailable)
                {
                    parameters.Add("q", NewsnabifyTitle(searchCriteria.SearchTerm));
                }
            }

            pageableRequests.Add(GetPagedRequests(searchCriteria,
                parameters));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

            var pageableRequests = new IndexerPageableRequestChain();
            var parameters = new NameValueCollection();

            if (searchCriteria.Author.IsNotNullOrWhiteSpace() && capabilities.BookSearchAuthorAvailable)
            {
                parameters.Add("author", searchCriteria.Author);
            }

            if (searchCriteria.Title.IsNotNullOrWhiteSpace() && capabilities.BookSearchTitleAvailable)
            {
                parameters.Add("title", searchCriteria.Title);
            }

            //Workaround issue with Sphinx search returning garbage results on some indexers. If we don't use id parameters, fallback to t=search
            if (parameters.Count == 0)
            {
                searchCriteria.SearchType = "search";

                if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.SearchAvailable)
                {
                    parameters.Add("q", NewsnabifyTitle(searchCriteria.SearchTerm));
                }
            }
            else
            {
                if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.BookSearchAvailable)
                {
                    parameters.Add("q", NewsnabifyTitle(searchCriteria.SearchTerm));
                }
            }

            pageableRequests.Add(GetPagedRequests(searchCriteria,
                parameters));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var capabilities = _capabilitiesProvider.GetCapabilities(Settings);
            var pageableRequests = new IndexerPageableRequestChain();

            var parameters = new NameValueCollection();

            if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.SearchAvailable)
            {
                parameters.Add("q", NewsnabifyTitle(searchCriteria.SearchTerm));
            }

            pageableRequests.Add(GetPagedRequests(searchCriteria, parameters));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(SearchCriteriaBase searchCriteria, NameValueCollection parameters)
        {
            var baseUrl = string.Format("{0}{1}?t={2}&extended=1", Settings.BaseUrl.TrimEnd('/'), Settings.ApiPath.TrimEnd('/'), searchCriteria.SearchType);
            var categories = searchCriteria.Categories;

            if (categories != null && categories.Any())
            {
                var categoriesQuery = string.Join(",", categories.Distinct());
                baseUrl += string.Format("&cat={0}", categoriesQuery);
            }

            if (Settings.AdditionalParameters.IsNotNullOrWhiteSpace())
            {
                baseUrl += Settings.AdditionalParameters;
            }

            if (Settings.ApiKey.IsNotNullOrWhiteSpace())
            {
                baseUrl += "&apikey=" + Settings.ApiKey;
            }

            if (searchCriteria.Limit.HasValue)
            {
                parameters.Add("limit", searchCriteria.Limit.ToString());
            }

            if (searchCriteria.Offset.HasValue)
            {
                parameters.Add("offset", searchCriteria.Offset.ToString());
            }

            yield return new IndexerRequest(string.Format("{0}&{1}", baseUrl, parameters.GetQueryString()), HttpAccept.Rss);
        }

        private static string NewsnabifyTitle(string title)
        {
            return title.Replace("+", "%20");
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
