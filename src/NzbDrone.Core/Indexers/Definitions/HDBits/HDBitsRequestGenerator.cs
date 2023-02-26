using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Definitions.HDBits
{
    public class HDBitsRequestGenerator : IIndexerRequestGenerator
    {
        public IndexerCapabilities Capabilities { get; set; }
        public HDBitsSettings Settings { get; set; }

        public virtual IEnumerable<IndexerRequest> GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
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
                query.ImdbInfo ??= new ImdbInfo();
                query.ImdbInfo.Id = imdbId;
            }

            return GetRequest(query);
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

        public IEnumerable<IndexerRequest> GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return new List<IndexerRequest>();
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(TvSearchCriteria searchCriteria)
        {
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
                query.TvdbInfo ??= new TvdbInfo();
                query.TvdbInfo.Id = tvdbId;

                if (DateTime.TryParseExact($"{searchCriteria.Season} {searchCriteria.Episode}", "yyyy MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var showDate))
                {
                    query.Search = showDate.ToString("yyyy-MM-dd");
                }
                else
                {
                    query.TvdbInfo.Season = searchCriteria.Season;
                    query.TvdbInfo.Episode = searchCriteria.Episode;
                }
            }

            if (imdbId != 0)
            {
                query.ImdbInfo ??= new ImdbInfo();
                query.ImdbInfo.Id = imdbId;
            }

            return GetRequest(query);
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new List<IndexerRequest>();
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var query = new TorrentQuery();

            if (searchCriteria.Categories?.Length > 0)
            {
                query.Category = Capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories).Select(int.Parse).ToArray();
            }

            if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
            {
                query.Search = searchCriteria.SanitizedSearchTerm;
            }

            return GetRequest(query);
        }
    }
}
