using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.HDBits
{
    public class HDBitsRequestGenerator : IIndexerRequestGenerator
    {
        public IndexerCapabilities Capabilities { get; set; }
        public HDBitsSettings Settings { get; set; }

        public virtual IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            var query = new TorrentQuery();
            var imdbId = ParseUtil.GetImdbID(searchCriteria.ImdbId).GetValueOrDefault(0);

            if (searchCriteria.Categories?.Length > 0)
            {
                query.Category = Capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories).Select(int.Parse).ToArray();
            }

            if (imdbId == 0 && searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
            {
                query.Search = searchCriteria.SanitizedSearchTerm;
            }

            if (imdbId != 0)
            {
                query.ImdbInfo = query.ImdbInfo ?? new ImdbInfo();
                query.ImdbInfo.Id = imdbId;
            }

            pageableRequests.Add(GetRequest(query));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        private IEnumerable<IndexerRequest> GetRequest(TorrentQuery query)
        {
            var request = new HttpRequestBuilder(Settings.BaseUrl)
                .Resource("/api/torrents")
                .Build();

            request.Method = HttpMethod.Post;
            const string appJson = "application/json";
            request.Headers.Accept = appJson;
            request.Headers.ContentType = appJson;

            query.Username = Settings.Username;
            query.Passkey = Settings.ApiKey;

            query.Codec = Settings.Codecs.ToArray();
            query.Medium = Settings.Mediums.ToArray();

            request.SetContent(query.ToJson());

            yield return new IndexerRequest(request);
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            var query = new TorrentQuery();
            var tvdbId = searchCriteria.TvdbId.GetValueOrDefault(0);
            var imdbId = ParseUtil.GetImdbID(searchCriteria.ImdbId).GetValueOrDefault(0);

            if (searchCriteria.Categories?.Length > 0)
            {
                query.Category = Capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories).Select(int.Parse).ToArray();
            }

            if (tvdbId == 0 && imdbId == 0 && searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
            {
                query.Search = searchCriteria.SanitizedTvSearchString;
            }

            if (tvdbId != 0)
            {
                query.TvdbInfo = query.TvdbInfo ?? new TvdbInfo();
                query.TvdbInfo.Id = tvdbId;
                query.TvdbInfo.Season = searchCriteria.Season;
                query.TvdbInfo.Episode = searchCriteria.Episode;
            }

            if (imdbId != 0)
            {
                query.ImdbInfo = query.ImdbInfo ?? new ImdbInfo();
                query.ImdbInfo.Id = imdbId;
            }

            pageableRequests.Add(GetRequest(query));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            var query = new TorrentQuery();

            if (searchCriteria.Categories?.Length > 0)
            {
                query.Category = Capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories).Select(int.Parse).ToArray();
            }

            if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
            {
                query.Search = searchCriteria.SanitizedSearchTerm;
            }

            pageableRequests.Add(GetRequest(query));

            return pageableRequests;
        }
    }
}
