using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.Definitions.Rarbg
{
    public class RarbgRequestGenerator : IIndexerRequestGenerator
    {
        private readonly IRarbgTokenProvider _tokenProvider;

        public RarbgSettings Settings { get; set; }
        public IndexerCapabilitiesCategories Categories { get; set; }

        public RarbgRequestGenerator(IRarbgTokenProvider tokenProvider)
        {
            _tokenProvider = tokenProvider;
        }

        private IEnumerable<IndexerRequest> GetRequest(string term, int[] categories, string imdbId = null, int? tmdbId = null, int? tvdbId = null)
        {
            var requestBuilder = new HttpRequestBuilder(Settings.BaseUrl.Trim('/'))
                .Resource("/pubapi_v2.php")
                .AddQueryParam("mode", "search")
                .Accept(HttpAccept.Json);

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                requestBuilder.AddQueryParam("search_imdb", imdbId);
            }
            else if (tmdbId.HasValue && tmdbId > 0)
            {
                requestBuilder.AddQueryParam("search_themoviedb", tmdbId);
            }
            else if (tvdbId.HasValue && tvdbId > 0)
            {
                requestBuilder.AddQueryParam("search_tvdb", tvdbId);
            }

            if (term.IsNotNullOrWhiteSpace())
            {
                requestBuilder.AddQueryParam("search_string", $"{term}");
            }

            if (!Settings.RankedOnly)
            {
                requestBuilder.AddQueryParam("ranked", "0");
            }

            var cats = Categories.MapTorznabCapsToTrackers(categories);

            if (cats != null && cats.Count > 0)
            {
                var categoryParam = string.Join(";", cats.Distinct());
                requestBuilder.AddQueryParam("category", categoryParam);
            }

            requestBuilder.AddQueryParam("limit", "100");
            requestBuilder.AddQueryParam("token", _tokenProvider.GetToken(Settings));
            requestBuilder.AddQueryParam("format", "json_extended");
            requestBuilder.AddQueryParam("app_id", $"{BuildInfo.AppName}_{BuildInfo.Version}");

            yield return new IndexerRequest(requestBuilder.Build());
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories, searchCriteria.FullImdbId, searchCriteria.TmdbId));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(searchCriteria.SanitizedTvSearchString, searchCriteria.Categories, searchCriteria.FullImdbId, tvdbId: searchCriteria.TvdbId));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
