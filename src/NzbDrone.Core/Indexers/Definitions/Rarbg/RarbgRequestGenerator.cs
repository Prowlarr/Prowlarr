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
        private readonly TimeSpan _rateLimit;

        public RarbgSettings Settings { get; set; }
        public IndexerCapabilitiesCategories Categories { get; set; }

        public RarbgRequestGenerator(IRarbgTokenProvider tokenProvider, TimeSpan rateLimit)
        {
            _tokenProvider = tokenProvider;
            _rateLimit = rateLimit;
        }

        private IEnumerable<IndexerRequest> GetRequest(bool isRssSearch, string term, int[] categories, string imdbId = null, int? tmdbId = null, int? tvdbId = null)
        {
            var requestBuilder = new HttpRequestBuilder(Settings.BaseUrl.Trim('/'))
                .Resource("/pubapi_v2.php")
                .AddQueryParam("limit", "100")
                .AddQueryParam("token", _tokenProvider.GetToken(Settings, _rateLimit))
                .AddQueryParam("format", "json_extended")
                .AddQueryParam("app_id", $"rralworP_{BuildInfo.Version}")
                .Accept(HttpAccept.Json);

            if (isRssSearch)
            {
                requestBuilder
                    .AddQueryParam("mode", "list")
                    .WithRateLimit(31);
            }
            else
            {
                requestBuilder.AddQueryParam("mode", "search");

                if (imdbId.IsNotNullOrWhiteSpace())
                {
                    requestBuilder.AddQueryParam("search_imdb", imdbId);
                }
                else if (tmdbId is > 0)
                {
                    requestBuilder.AddQueryParam("search_themoviedb", tmdbId);
                }
                else if (tvdbId is > 0)
                {
                    requestBuilder.AddQueryParam("search_tvdb", tvdbId);
                }

                if (term.IsNotNullOrWhiteSpace())
                {
                    requestBuilder.AddQueryParam("search_string", $"{term}");
                }
            }

            if (!Settings.RankedOnly)
            {
                requestBuilder.AddQueryParam("ranked", "0");
            }

            var cats = Categories.MapTorznabCapsToTrackers(categories);
            if (cats == null || !cats.Any())
            {
                // default to all, without specifying it some categories are missing (e.g. games), see #4146
                cats = Categories.GetTrackerCategories();
            }

            requestBuilder.AddQueryParam("category", string.Join(";", cats.Distinct()));

            yield return new IndexerRequest(requestBuilder.Build());
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(searchCriteria.IsRssSearch, searchCriteria.SanitizedSearchTerm, searchCriteria.Categories, searchCriteria.FullImdbId, searchCriteria.TmdbId));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(searchCriteria.IsRssSearch, searchCriteria.SanitizedSearchTerm, searchCriteria.Categories));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(searchCriteria.IsRssSearch, searchCriteria.SanitizedTvSearchString, searchCriteria.Categories, searchCriteria.FullImdbId, tvdbId: searchCriteria.TvdbId));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(searchCriteria.IsRssSearch, searchCriteria.SanitizedSearchTerm, searchCriteria.Categories));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(searchCriteria.IsRssSearch, searchCriteria.SanitizedSearchTerm, searchCriteria.Categories));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
