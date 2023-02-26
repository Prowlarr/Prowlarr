using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Definitions.Xthor
{
    public class XthorRequestGenerator : IIndexerRequestGenerator
    {
        public XthorSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public XthorRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term,
            int[] categories,
            int pageNumber,
            string tmdbid = null,
            int forced_accent = 0)
        {
            var searchUrl = string.Format("{0}", Settings.BaseUrl.TrimEnd('/'));

            var searchString = term;

            var trackerCats = Capabilities.Categories.MapTorznabCapsToTrackers(categories) ?? new List<string>();

            var queryCollection = new NameValueCollection();

            queryCollection.Add("passkey", Settings.Passkey);

            if (tmdbid.IsNotNullOrWhiteSpace())
            {
                queryCollection.Add("tmdbid", tmdbid);
            }
            else if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Replace("'", ""); // ignore ' (e.g. search for america's Next Top Model)
                if (Settings.EnhancedAnime &&
                    (trackerCats.Contains("101") || trackerCats.Contains("32") || trackerCats.Contains("110")))
                {
                    var regex = new Regex(" ([0-9]+)");
                    searchString = regex.Replace(searchString, " E$1");
                }

                queryCollection.Add("search", searchString);
            }

            if (Settings.FreeleechOnly)
            {
                queryCollection.Add("freeleech", "1");
            }

            if (trackerCats.Count >= 1)
            {
                queryCollection.Add("category", string.Join("+", trackerCats));
            }

            if (Settings.Accent >= 1 && forced_accent == 0)
            {
                queryCollection.Add("accent", Settings.Accent.ToString());
            }

            if (forced_accent != 0)
            {
                queryCollection.Add("accent", forced_accent.ToString());
            }

            if (pageNumber > 0)
            {
                queryCollection.Add("page", pageNumber.ToString());
            }

            searchUrl = searchUrl + "?" + queryCollection.GetQueryString();

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
        }

        public IEnumerable<IndexerRequest> GetSearchRequestsCommon(SearchCriteriaBase searchCriteria, string searchTerm, string tmdbid = null)
        {
            var actualPage = 0;

            var requests = new List<IndexerRequest>();

            while (actualPage < Settings.MaxPages)
            {
                requests.AddRange(GetPagedRequests(searchTerm, searchCriteria.Categories, actualPage, tmdbid));

                if (Settings.EnhancedFrenchAccent && (Settings.Accent == 1 || Settings.Accent == 2))
                {
                    requests.AddRange(GetPagedRequests(searchTerm, searchCriteria.Categories, actualPage, tmdbid, 47));
                }

                if (tmdbid.IsNotNullOrWhiteSpace() && Settings.ByPassPageForTmDbid)
                {
                    break;
                }

                ++actualPage;
            }

            return requests;
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            return GetSearchRequestsCommon(searchCriteria,
                string.Format("{0}", searchCriteria.SanitizedSearchTerm),
                searchCriteria.TmdbId.ToString());
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return GetSearchRequestsCommon(searchCriteria, string.Format("{0}", searchCriteria.SanitizedSearchTerm));
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            return GetSearchRequestsCommon(searchCriteria,
                string.Format("{0}", searchCriteria.SanitizedTvSearchString));
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return GetSearchRequestsCommon(searchCriteria, string.Format("{0}", searchCriteria.SanitizedSearchTerm));
        }

        public IEnumerable<IndexerRequest> GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            return GetSearchRequestsCommon(searchCriteria, string.Format("{0}", searchCriteria.SanitizedSearchTerm));
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
