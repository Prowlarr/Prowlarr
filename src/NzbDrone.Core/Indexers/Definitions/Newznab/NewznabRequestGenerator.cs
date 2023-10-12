using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers.Newznab
{
    public class NewznabRequestGenerator : IIndexerRequestGenerator
    {
        private readonly INewznabCapabilitiesProvider _capabilitiesProvider;

        public ProviderDefinition Definition { get; set; }
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
            var capabilities = _capabilitiesProvider.GetCapabilities(Settings, Definition);

            var pageableRequests = new IndexerPageableRequestChain();
            var parameters = new NameValueCollection();

            if (searchCriteria.TmdbId.HasValue && capabilities.MovieSearchTmdbAvailable)
            {
                parameters.Set("tmdbid", searchCriteria.TmdbId.Value.ToString());
            }

            if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace() && capabilities.MovieSearchImdbAvailable)
            {
                parameters.Set("imdbid", searchCriteria.ImdbId);
            }

            if (searchCriteria.TraktId.HasValue && capabilities.MovieSearchTraktAvailable)
            {
                parameters.Set("traktid", searchCriteria.TraktId.ToString());
            }

            //Workaround issue with Sphinx search returning garbage results on some indexers. If we don't use id parameters, fallback to t=search
            if (parameters.Count == 0)
            {
                searchCriteria.SearchType = "search";

                if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.SearchAvailable)
                {
                    parameters.Set("q", NewsnabifyTitle(searchCriteria.SearchTerm));
                }
            }
            else
            {
                if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.MovieSearchAvailable)
                {
                    parameters.Set("q", NewsnabifyTitle(searchCriteria.SearchTerm));
                }
            }

            pageableRequests.Add(GetPagedRequests(searchCriteria,
                capabilities,
                parameters));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var capabilities = _capabilitiesProvider.GetCapabilities(Settings, Definition);

            var pageableRequests = new IndexerPageableRequestChain();
            var parameters = new NameValueCollection();

            if (searchCriteria.Artist.IsNotNullOrWhiteSpace() && capabilities.MusicSearchArtistAvailable)
            {
                parameters.Set("artist", searchCriteria.Artist);
            }

            if (searchCriteria.Album.IsNotNullOrWhiteSpace() && capabilities.MusicSearchAlbumAvailable)
            {
                parameters.Set("album", searchCriteria.Album);
            }

            //Workaround issue with Sphinx search returning garbage results on some indexers. If we don't use id parameters, fallback to t=search
            if (parameters.Count == 0)
            {
                searchCriteria.SearchType = "search";

                if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.SearchAvailable)
                {
                    parameters.Set("q", NewsnabifyTitle(searchCriteria.SearchTerm));
                }
            }
            else
            {
                if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.MusicSearchAvailable)
                {
                    parameters.Set("q", NewsnabifyTitle(searchCriteria.SearchTerm));
                }
            }

            pageableRequests.Add(GetPagedRequests(searchCriteria,
                capabilities,
                parameters));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var capabilities = _capabilitiesProvider.GetCapabilities(Settings, Definition);

            var pageableRequests = new IndexerPageableRequestChain();
            var parameters = new NameValueCollection();

            if (searchCriteria.TvdbId.HasValue && capabilities.TvSearchTvdbAvailable)
            {
                parameters.Set("tvdbid", searchCriteria.TvdbId.Value.ToString());
            }

            if (searchCriteria.TmdbId.HasValue && capabilities.TvSearchTvdbAvailable)
            {
                parameters.Set("tmdbid", searchCriteria.TmdbId.Value.ToString());
            }

            if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace() && capabilities.TvSearchImdbAvailable)
            {
                parameters.Set("imdbid", searchCriteria.ImdbId);
            }

            if (searchCriteria.TvMazeId.HasValue && capabilities.TvSearchTvMazeAvailable)
            {
                parameters.Set("tvmazeid", searchCriteria.TvMazeId.ToString());
            }

            if (searchCriteria.RId.HasValue && capabilities.TvSearchTvRageAvailable)
            {
                parameters.Set("rid", searchCriteria.RId.ToString());
            }

            if (searchCriteria.Season.HasValue && capabilities.TvSearchSeasonAvailable)
            {
                parameters.Set("season", NewznabifySeasonNumber(searchCriteria.Season.Value));
            }

            if (searchCriteria.Episode.IsNotNullOrWhiteSpace() && capabilities.TvSearchEpAvailable)
            {
                parameters.Set("ep", searchCriteria.Episode);
            }

            //Workaround issue with Sphinx search returning garbage results on some indexers. If we don't use id parameters, fallback to t=search
            if (parameters.Count == 0)
            {
                searchCriteria.SearchType = "search";

                if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.SearchAvailable)
                {
                    parameters.Set("q", NewsnabifyTitle(searchCriteria.SearchTerm));
                }
            }
            else
            {
                if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.TvSearchAvailable)
                {
                    parameters.Set("q", NewsnabifyTitle(searchCriteria.SearchTerm));
                }
            }

            pageableRequests.Add(GetPagedRequests(searchCriteria,
                capabilities,
                parameters));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var capabilities = _capabilitiesProvider.GetCapabilities(Settings, Definition);

            var pageableRequests = new IndexerPageableRequestChain();
            var parameters = new NameValueCollection();

            if (searchCriteria.Author.IsNotNullOrWhiteSpace() && capabilities.BookSearchAuthorAvailable)
            {
                parameters.Set("author", searchCriteria.Author);
            }

            if (searchCriteria.Title.IsNotNullOrWhiteSpace() && capabilities.BookSearchTitleAvailable)
            {
                parameters.Set("title", searchCriteria.Title);
            }

            //Workaround issue with Sphinx search returning garbage results on some indexers. If we don't use id parameters, fallback to t=search
            if (parameters.Count == 0)
            {
                searchCriteria.SearchType = "search";

                if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.SearchAvailable)
                {
                    parameters.Set("q", NewsnabifyTitle(searchCriteria.SearchTerm));
                }
            }
            else
            {
                if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.BookSearchAvailable)
                {
                    parameters.Set("q", NewsnabifyTitle(searchCriteria.SearchTerm));
                }
            }

            pageableRequests.Add(GetPagedRequests(searchCriteria,
                capabilities,
                parameters));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var capabilities = _capabilitiesProvider.GetCapabilities(Settings, Definition);
            var pageableRequests = new IndexerPageableRequestChain();

            var parameters = new NameValueCollection();

            if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.SearchAvailable)
            {
                parameters.Set("q", NewsnabifyTitle(searchCriteria.SearchTerm));
            }

            pageableRequests.Add(GetPagedRequests(searchCriteria, capabilities, parameters));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(SearchCriteriaBase searchCriteria, IndexerCapabilities capabilities, NameValueCollection parameters)
        {
            var searchUrl = string.Format("{0}{1}?t={2}&extended=1", Settings.BaseUrl.TrimEnd('/'), Settings.ApiPath.TrimEnd('/'), searchCriteria.SearchType);
            var categories = capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories);

            if (categories != null && categories.Any())
            {
                var categoriesQuery = string.Join(",", categories.Distinct());
                searchUrl += string.Format("&cat={0}", categoriesQuery);
            }

            if (Settings.AdditionalParameters.IsNotNullOrWhiteSpace())
            {
                searchUrl += Settings.AdditionalParameters;
            }

            if (Settings.ApiKey.IsNotNullOrWhiteSpace())
            {
                searchUrl += "&apikey=" + Settings.ApiKey;
            }

            if (searchCriteria.Limit.HasValue)
            {
                parameters.Set("limit", searchCriteria.Limit.ToString());
            }

            if (searchCriteria.Offset.HasValue)
            {
                parameters.Set("offset", searchCriteria.Offset.ToString());
            }

            if (searchCriteria.MinAge.HasValue)
            {
                parameters.Set("minage", searchCriteria.MaxAge.ToString());
            }

            if (searchCriteria.MaxAge.HasValue)
            {
                parameters.Set("maxage", searchCriteria.MaxAge.ToString());
            }

            if (searchCriteria.MinSize.HasValue)
            {
                parameters.Set("minsize", searchCriteria.MaxAge.ToString());
            }

            if (searchCriteria.MaxSize.HasValue)
            {
                parameters.Set("maxsize", searchCriteria.MaxAge.ToString());
            }

            if (parameters.Count > 0)
            {
                searchUrl += $"&{parameters.GetQueryString()}";
            }

            var request = new IndexerRequest(searchUrl, HttpAccept.Rss)
            {
                HttpRequest =
                {
                    AllowAutoRedirect = true
                }
            };

            yield return request;
        }

        private static string NewsnabifyTitle(string title)
        {
            return title.Replace("+", "%20");
        }

        // Temporary workaround for NNTMux considering season=0 -> null. '00' should work on existing newznab indexers.
        private static string NewznabifySeasonNumber(int seasonNumber)
        {
            return seasonNumber == 0 ? "00" : seasonNumber.ToString();
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
