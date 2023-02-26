using System;
using System.Collections.Generic;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.TorrentPotato
{
    public class TorrentPotatoRequestGenerator : IIndexerRequestGenerator
    {
        public TorrentPotatoSettings Settings { get; set; }

        public TorrentPotatoRequestGenerator()
        {
        }

        public virtual IEnumerable<IndexerRequest> GetRecentRequests()
        {
            return GetPagedRequests("list", null, null);
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string mode, int? tvdbId, string query, params object[] args)
        {
            var requestBuilder = new HttpRequestBuilder(Settings.BaseUrl)
                .Accept(HttpAccept.Json);

            requestBuilder.AddQueryParam("passkey", Settings.Passkey);
            if (!string.IsNullOrWhiteSpace(Settings.User))
            {
                requestBuilder.AddQueryParam("user", Settings.User);
            }
            else
            {
                requestBuilder.AddQueryParam("user", "");
            }

            requestBuilder.AddQueryParam("search", "-");

            yield return new IndexerRequest(requestBuilder.Build());
        }

        private IEnumerable<IndexerRequest> GetMovieRequest(MovieSearchCriteria searchCriteria)
        {
            var requestBuilder = new HttpRequestBuilder(Settings.BaseUrl)
                 .Accept(HttpAccept.Json);

            requestBuilder.AddQueryParam("passkey", Settings.Passkey);

            if (!string.IsNullOrWhiteSpace(Settings.User))
            {
                requestBuilder.AddQueryParam("user", Settings.User);
            }
            else
            {
                requestBuilder.AddQueryParam("user", "");
            }

            if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace())
            {
                requestBuilder.AddQueryParam("imdbid", searchCriteria.ImdbId);
            }
            else if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
            {
                //TODO: Hack for now
                requestBuilder.AddQueryParam("search", $"{searchCriteria.SearchTerm}");
            }

            yield return new IndexerRequest(requestBuilder.Build());
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            return GetMovieRequest(searchCriteria);
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return new List<IndexerRequest>();
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            return new List<IndexerRequest>();
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new List<IndexerRequest>();
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            return new List<IndexerRequest>();
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
