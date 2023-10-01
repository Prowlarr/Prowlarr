using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Definitions.PassThePopcorn
{
    public class PassThePopcornRequestGenerator : IIndexerRequestGenerator
    {
        private readonly PassThePopcornSettings _settings;

        public PassThePopcornRequestGenerator(PassThePopcornSettings settings)
        {
            _settings = settings;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace())
            {
                pageableRequests.Add(GetRequest(searchCriteria.FullImdbId, searchCriteria));
            }
            else
            {
                pageableRequests.Add(GetRequest($"{searchCriteria.SearchTerm}", searchCriteria));
            }

            return pageableRequests;
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
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest($"{searchCriteria.SearchTerm}", searchCriteria));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetRequest(string searchParameters, SearchCriteriaBase searchCriteria)
        {
            var parameters = new NameValueCollection
            {
                { "action", "advanced" },
                { "json", "noredirect" },
                { "grouping", "0" },
                { "searchstr", searchParameters }
            };

            if (searchCriteria.Limit is > 0 && searchCriteria.Offset is > 0)
            {
                var page = (int)(searchCriteria.Offset / searchCriteria.Limit) + 1;
                parameters.Set("page", page.ToString());
            }

            if (_settings.FreeleechOnly)
            {
                parameters.Set("freetorrent", "1");
            }

            var searchUrl = $"{_settings.BaseUrl.Trim().TrimEnd('/')}/torrents.php?{parameters.GetQueryString()}";

            var request = new IndexerRequest(searchUrl, HttpAccept.Json);
            request.HttpRequest.Headers.Add("ApiUser", _settings.APIUser);
            request.HttpRequest.Headers.Add("ApiKey", _settings.APIKey);

            yield return request;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
