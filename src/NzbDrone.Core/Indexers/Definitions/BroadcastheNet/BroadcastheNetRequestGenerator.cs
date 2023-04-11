using System;
using System.Collections.Generic;
using System.Globalization;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.BroadcastheNet
{
    public class BroadcastheNetRequestGenerator : IIndexerRequestGenerator
    {
        public int MaxPages { get; set; }
        public int PageSize { get; set; }
        public BroadcastheNetSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public int? LastRecentTorrentID { get; set; }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public BroadcastheNetRequestGenerator()
        {
            MaxPages = 10;
            PageSize = 100;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(BroadcastheNetTorrentQuery parameters, int results, int offset)
        {
            var builder = new JsonRpcRequestBuilder(Settings.BaseUrl)
                .Call("getTorrents", Settings.ApiKey, parameters, results, offset);
            builder.SuppressHttpError = true;

            yield return new IndexerRequest(builder.Build());
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var parameters = new BroadcastheNetTorrentQuery();

            var searchString = searchCriteria.SearchTerm ?? "";

            var btnResults = searchCriteria.Limit.GetValueOrDefault();
            if (btnResults == 0)
            {
                btnResults = (int)Capabilities.LimitsDefault;
            }

            var btnOffset = searchCriteria.Offset.GetValueOrDefault();

            if (searchCriteria.TvdbId > 0)
            {
                parameters.Tvdb = $"{searchCriteria.TvdbId}";
            }

            if (searchCriteria.RId > 0)
            {
                parameters.Tvrage = $"{searchCriteria.RId}";
            }

            // If only the season/episode is searched for then change format to match expected format
            if (searchCriteria.Season > 0 && searchCriteria.Episode.IsNullOrWhiteSpace())
            {
                // Search Season
                parameters.Category = "Season";
                parameters.Name = $"Season {searchCriteria.Season}%";
                pageableRequests.Add(GetPagedRequests(parameters, btnResults, btnOffset));

                parameters = parameters.Clone();

                // Search Episode
                parameters.Category = "Episode";
                parameters.Name = $"S{searchCriteria.Season:00}E%";
                pageableRequests.Add(GetPagedRequests(parameters, btnResults, btnOffset));
            }
            else if (DateTime.TryParseExact($"{searchCriteria.Season} {searchCriteria.Episode}", "yyyy MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var showDate))
            {
                // Daily Episode
                parameters.Name = showDate.ToString("yyyy.MM.dd");
                parameters.Category = "Episode";
                pageableRequests.Add(GetPagedRequests(parameters, btnResults, btnOffset));
            }
            else if (searchCriteria.Season > 0 && int.TryParse(searchCriteria.Episode, out var episode) && episode > 0)
            {
                // Standard (S/E) Episode
                parameters.Name = $"S{searchCriteria.Season:00}E{episode:00}";
                parameters.Category = "Episode";
                pageableRequests.Add(GetPagedRequests(parameters, btnResults, btnOffset));
            }
            else
            {
                // Neither a season only search nor daily nor standard, fall back to query
                parameters.Search = searchString.Replace(" ", "%");
                pageableRequests.Add(GetPagedRequests(parameters, btnResults, btnOffset));
            }

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var parameters = new BroadcastheNetTorrentQuery();

            var searchString = searchCriteria.SearchTerm ?? "";

            var btnResults = searchCriteria.Limit.GetValueOrDefault();
            if (btnResults == 0)
            {
                btnResults = (int)Capabilities.LimitsDefault;
            }

            parameters.Search = searchString.Replace(" ", "%");

            var btnOffset = searchCriteria.Offset.GetValueOrDefault();

            pageableRequests.Add(GetPagedRequests(parameters, btnResults, btnOffset));

            return pageableRequests;
        }
    }
}
