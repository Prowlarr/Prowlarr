using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
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

        public virtual IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            var query = new TorrentQuery();

            var imdbId = ParseUtil.GetImdbId(searchCriteria.ImdbId).GetValueOrDefault(0);

            if (imdbId == 0 && searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
            {
                query.Search = Regex.Replace(searchCriteria.SanitizedSearchTerm, "[\\W]+", " ").Trim();
            }

            if (imdbId != 0)
            {
                query.ImdbInfo ??= new ImdbInfo();
                query.ImdbInfo.Id = imdbId;
            }

            pageableRequests.Add(GetRequest(query, searchCriteria));

            return pageableRequests;
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
            var imdbId = ParseUtil.GetImdbId(searchCriteria.ImdbId).GetValueOrDefault(0);

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
                    query.Search = showDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
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

            pageableRequests.Add(GetRequest(query, searchCriteria));

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

            if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
            {
                query.Search = searchCriteria.SanitizedSearchTerm;
            }

            pageableRequests.Add(GetRequest(query, searchCriteria));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        private IEnumerable<IndexerRequest> GetRequest(TorrentQuery query, SearchCriteriaBase searchCriteria)
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

            if (Settings.Codecs.Any())
            {
                query.Codec = Settings.Codecs.ToArray();
            }

            if (Settings.Mediums.Any())
            {
                query.Medium = Settings.Mediums.ToArray();
            }

            if (Settings.Origins.Any())
            {
                query.Origin = Settings.Origins.ToArray();
            }

            if (Settings.Exclusive.Any())
            {
                query.Exclusive = Settings.Exclusive.ToArray();
            }

            if (searchCriteria.Categories?.Length > 0)
            {
                query.Category = Capabilities.Categories
                    .MapTorznabCapsToTrackers(searchCriteria.Categories)
                    .Distinct()
                    .Select(int.Parse)
                    .ToArray();
            }

            query.Limit = 100;

            if (searchCriteria.Limit is > 0 && searchCriteria.Offset is > 0)
            {
                query.Page = (int)(searchCriteria.Offset / searchCriteria.Limit);
            }

            request.SetContent(query.ToJson());
            request.ContentSummary = query.ToJson(Formatting.None);

            yield return new IndexerRequest(request);
        }
    }
}
