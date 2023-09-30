using System;
using System.Collections.Generic;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.Definitions.TorrentPotato
{
    public class TorrentPotatoRequestGenerator : IIndexerRequestGenerator
    {
        private readonly TorrentPotatoSettings _settings;

        public TorrentPotatoRequestGenerator(TorrentPotatoSettings settings)
        {
            _settings = settings;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(BuildRequest(searchCriteria.SearchTerm, searchCriteria.FullImdbId));

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

            pageableRequests.Add(BuildRequest(searchCriteria.SearchTerm));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> BuildRequest(string searchTerm, string imdbId = null)
        {
            var requestBuilder = new HttpRequestBuilder(_settings.BaseUrl)
                .Accept(HttpAccept.Json);

            requestBuilder.AddQueryParam("passkey", _settings.Passkey);
            requestBuilder.AddQueryParam("user", _settings.User.IsNotNullOrWhiteSpace() ? _settings.User : "");

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                requestBuilder.AddQueryParam("imdbid", imdbId);
            }

            requestBuilder.AddQueryParam("search", searchTerm.IsNotNullOrWhiteSpace() ? $"{searchTerm}" : " ");

            yield return new IndexerRequest(requestBuilder.Build());
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
