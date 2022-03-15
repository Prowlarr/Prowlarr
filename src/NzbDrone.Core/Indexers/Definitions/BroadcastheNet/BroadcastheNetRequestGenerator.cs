using System;
using System.Collections.Generic;
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
            var builder = new JsonRpcRequestBuilder(Settings.BaseUrl).Call("getTorrents", Settings.ApiKey, parameters, results, offset);

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

            var searchString = searchCriteria.SearchTerm != null ? searchCriteria.SearchTerm : "";

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
            if (searchCriteria.Season > 0)
            {
                if (searchCriteria.Episode.IsNullOrWhiteSpace())
                {
                    // Season Only
                    parameters.Search = searchString.Trim() + string.Format(" S{0:00}", searchCriteria.Season.Value);
                    parameters.Category = "Season";
                }
                else if (Regex.IsMatch(searchCriteria.EpisodeSearchString, "(\\d{4}\\.\\d{2}\\.\\d{2})"))
                {
                    // Daily Episode
                    parameters.Name = searchCriteria.EpisodeSearchString;
                    parameters.Category = "Episode";
                }
            }
            else
            {
                // Neither a season only search nor daily nor standard, fall back to query
                parameters.Search = searchString.Replace(" ", "%");
            }

            pageableRequests.Add(GetPagedRequests(parameters, btnResults, btnOffset));

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

            var searchString = searchCriteria.SearchTerm != null ? searchCriteria.SearchTerm : "";

            var btnResults = searchCriteria.Limit.GetValueOrDefault();
            if (btnResults == 0)
            {
                btnResults = (int)Capabilities.LimitsDefault;
            }

            parameters.Search = searchString.Replace(" ", "+");

            var btnOffset = searchCriteria.Offset.GetValueOrDefault();

            pageableRequests.Add(GetPagedRequests(parameters, btnResults, btnOffset));

            return pageableRequests;
        }
    }
}
