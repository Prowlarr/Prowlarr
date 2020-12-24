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

        public RarbgRequestGenerator(IRarbgTokenProvider tokenProvider)
        {
            _tokenProvider = tokenProvider;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetMovieRequest(searchCriteria));
            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetMovieRequest(MovieSearchCriteria searchCriteria)
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

            if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace())
            {
                requestBuilder.AddQueryParam("search_imdb", searchCriteria.ImdbId);
            }
            else if (searchCriteria.TmdbId > 0)
            {
                requestBuilder.AddQueryParam("search_themoviedb", searchCriteria.TmdbId);
            }
            else if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
            {
                requestBuilder.AddQueryParam("search_string", $"{searchCriteria.SearchTerm}");
            }

            if (!Settings.RankedOnly)
            {
                requestBuilder.AddQueryParam("ranked", "0");
            }

            var categoryParam = string.Join(";", searchCriteria.Categories.Distinct());

            requestBuilder.AddQueryParam("category", categoryParam);
            requestBuilder.AddQueryParam("limit", "100");
            requestBuilder.AddQueryParam("token", _tokenProvider.GetToken(Settings));
            requestBuilder.AddQueryParam("format", "json_extended");
            requestBuilder.AddQueryParam("app_id", BuildInfo.AppName);

            yield return new IndexerRequest(requestBuilder.Build());
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
    }
}
