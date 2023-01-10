using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Definitions.Avistaz
{
    public class AvistazRequestGenerator : IIndexerRequestGenerator
    {
        public AvistazSettings Settings { get; set; }
        public IIndexerHttpClient HttpClient { get; set; }
        public IndexerCapabilities Capabilities { get; set; }
        public Logger Logger { get; set; }
        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
        protected virtual string SearchUrl => Settings.BaseUrl + "api/v1/jackett/torrents";

        // hook to adjust the search category
        protected virtual List<KeyValuePair<string, string>> GetBasicSearchParameters(int[] categories, string genre)
        {
            var categoryMapping = Capabilities.Categories.MapTorznabCapsToTrackers(categories).Distinct().ToList();
            var qc = new List<KeyValuePair<string, string>> // NameValueCollection don't support cat[]=19&cat[]=6
            {
                { "in", "1" },
                { "type", categoryMapping.Any() ? categoryMapping.First() : "0" }
            };

            if (Settings.FreeleechOnly)
            {
                qc.Add("discount[]", "1");
            }

            if (genre.IsNotNullOrWhiteSpace())
            {
                qc.Add("tags", genre);
            }

            // resolution filter to improve the search
            if (!categories.Contains(NewznabStandardCategory.Movies.Id) &&
                !categories.Contains(NewznabStandardCategory.TV.Id) &&
                !categories.Contains(NewznabStandardCategory.Audio.Id))
            {
                if (categories.Contains(NewznabStandardCategory.MoviesUHD.Id) || categories.Contains(NewznabStandardCategory.TVUHD.Id))
                {
                    qc.Add("video_quality[]", "6"); // 2160p
                }

                if (categories.Contains(NewznabStandardCategory.MoviesHD.Id) || categories.Contains(NewznabStandardCategory.TVHD.Id))
                {
                    qc.Add("video_quality[]", "2"); // 720p
                    qc.Add("video_quality[]", "7"); // 1080i
                    qc.Add("video_quality[]", "3"); // 1080p
                }

                if (categories.Contains(NewznabStandardCategory.MoviesSD.Id) || categories.Contains(NewznabStandardCategory.TVSD.Id))
                {
                    qc.Add("video_quality[]", "1"); // SD
                }
            }

            return qc;
        }

        private IEnumerable<IndexerRequest> GetRequest(List<KeyValuePair<string, string>> searchParameters)
        {
            var searchUrl = SearchUrl + "?" + searchParameters.GetQueryString();

            // TODO: Change to HttpAccept.Json after they fix the issue with missing headers
            var request = new IndexerRequest(searchUrl, HttpAccept.Html);
            request.HttpRequest.Headers.Add("Authorization", $"Bearer {Settings.Token}");

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var parameters = GetBasicSearchParameters(searchCriteria.Categories, searchCriteria.Genre);

            if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace())
            {
                parameters.Add("imdb", searchCriteria.FullImdbId);
            }
            else if (searchCriteria.TmdbId.HasValue)
            {
                parameters.Add("tmdb", searchCriteria.TmdbId.Value.ToString());
            }
            else
            {
                parameters.Add("search", GetSearchTerm(searchCriteria.SanitizedSearchTerm).Trim());
            }

            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(parameters));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var parameters = GetBasicSearchParameters(searchCriteria.Categories, null);

            parameters.Add("search", GetSearchTerm(searchCriteria.SanitizedSearchTerm).Trim());

            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(parameters));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var parameters = GetBasicSearchParameters(searchCriteria.Categories, searchCriteria.Genre);

            if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace())
            {
                parameters.Add("imdb", searchCriteria.FullImdbId);
                parameters.Add("search", GetSearchTerm(searchCriteria.EpisodeSearchString).Trim());
            }
            else if (searchCriteria.TvdbId.HasValue)
            {
                parameters.Add("tvdb", searchCriteria.TvdbId.Value.ToString());
                parameters.Add("search", GetSearchTerm(searchCriteria.EpisodeSearchString).Trim());
            }
            else
            {
                parameters.Add("search", GetSearchTerm(searchCriteria.SanitizedTvSearchString).Trim());
            }

            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(parameters));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            throw new NotImplementedException();
        }

        // hook to adjust the search term
        protected virtual string GetSearchTerm(string term) => term;

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var parameters = GetBasicSearchParameters(searchCriteria.Categories, null);

            parameters.Add("search", GetSearchTerm(searchCriteria.SanitizedSearchTerm).Trim());

            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetRequest(parameters));
            return pageableRequests;
        }
    }
}
