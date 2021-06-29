using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.Rarbg
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

        private IEnumerable<IndexerRequest> GetRequest(string term, int[] categories, string imdbId = null, int? tmdbId = null)
        {
            var requestBuilder = new HttpRequestBuilder(Settings.BaseUrl)
                .Resource("/pubapi_v2.php")
                .Accept(HttpAccept.Json);

            if (Settings.CaptchaToken.IsNotNullOrWhiteSpace())
            {
                requestBuilder.UseSimplifiedUserAgent = true;
                requestBuilder.SetCookie("cf_clearance", Settings.CaptchaToken);
            }

            requestBuilder.AddQueryParam("mode", "search");

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                requestBuilder.AddQueryParam("search_imdb", imdbId);
            }
            else if (tmdbId.HasValue && tmdbId > 0)
            {
                requestBuilder.AddQueryParam("search_themoviedb", tmdbId);
            }
            else if (term.IsNotNullOrWhiteSpace())
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
            requestBuilder.AddQueryParam("token", _tokenProvider.GetToken(Settings, Settings.BaseUrl));
            requestBuilder.AddQueryParam("format", "json_extended");
            requestBuilder.AddQueryParam("app_id", BuildInfo.AppName);

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
            pageableRequests.Add(GetRequest(searchCriteria.SanitizedTvSearchString, searchCriteria.Categories, searchCriteria.FullImdbId));
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
