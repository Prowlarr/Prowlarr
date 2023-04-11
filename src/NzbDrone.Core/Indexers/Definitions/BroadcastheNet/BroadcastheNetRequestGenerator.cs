using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
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
                parameters.Tvdb = string.Format("{0}", searchCriteria.TvdbId);
            }

            if (searchCriteria.RId > 0)
            {
                parameters.Tvrage = string.Format("{0}", searchCriteria.RId);
            }

            // If only the season/episode is searched for then change format to match expected format
            if (searchCriteria.Season > 0 && searchCriteria.Episode.IsNullOrWhiteSpace())
            {
                // Season Only
                // Search by Season and by Episode to cover all use cases
                // This mirrors Sonarr's logic
                var episodeParameters = parameters.Clone();

                // Search Season
                parameters.Category = "Season";
                parameters.Name = string.Format("Season {0}%", searchCriteria.Season);
                pageableRequests.Add(GetPagedRequests(parameters, btnResults, btnOffset));

                // Search Episode
                episodeParameters.Category = "Episode";
                episodeParameters.Name = string.Format("S{0:00}E%", searchCriteria.Season);

                pageableRequests.Add(GetPagedRequests(episodeParameters, btnResults, btnOffset));
            }
            else if (DateTime.TryParseExact($"{searchCriteria.Season} {searchCriteria.Episode}", "yyyy MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var showDate))
            {
                // Daily Episode
                parameters.Name = string.Format("{0:yyyy}.{0:MM}.{0:dd}", showDate);
                parameters.Category = "Episode";
                pageableRequests.Add(GetPagedRequests(parameters, btnResults, btnOffset));
            }
            else if (searchCriteria.Season > 0 && int.Parse(searchCriteria.Episode) > 0)
            {
                // Standard (S/E) Episode
                parameters.Name = string.Format("S{0:00}E{1:00}", searchCriteria.Season.Value, int.Parse(searchCriteria.Episode));
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
