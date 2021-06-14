using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Headphones
{
    public class HeadphonesRequestGenerator : IIndexerRequestGenerator
    {
        public int MaxPages { get; set; }
        public int PageSize { get; set; }
        public HeadphonesSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public HeadphonesRequestGenerator()
        {
            MaxPages = 30;
            PageSize = 100;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var capabilities = Capabilities;

            var pageableRequests = new IndexerPageableRequestChain();
            var parameters = new NameValueCollection();

            if (searchCriteria.Artist.IsNotNullOrWhiteSpace() && capabilities.MusicSearchArtistAvailable)
            {
                parameters.Add("artist", searchCriteria.Artist);
            }

            if (searchCriteria.Album.IsNotNullOrWhiteSpace() && capabilities.MusicSearchAlbumAvailable)
            {
                parameters.Add("album", searchCriteria.Album);
            }

            //Workaround issue with Sphinx search returning garbage results on some indexers. If we don't use id parameters, fallback to t=search
            if (parameters.Count == 0)
            {
                searchCriteria.SearchType = "search";

                if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.SearchAvailable)
                {
                    parameters.Add("q", NewsnabifyTitle(searchCriteria.SearchTerm));
                }
            }
            else
            {
                if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.MusicSearchAvailable)
                {
                    parameters.Add("q", NewsnabifyTitle(searchCriteria.SearchTerm));
                }
            }

            pageableRequests.Add(GetPagedRequests(searchCriteria,
                parameters));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var capabilities = Capabilities;
            var pageableRequests = new IndexerPageableRequestChain();

            var parameters = new NameValueCollection();

            if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace() && capabilities.SearchAvailable)
            {
                parameters.Add("q", NewsnabifyTitle(searchCriteria.SearchTerm));
            }

            pageableRequests.Add(GetPagedRequests(searchCriteria, parameters));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(SearchCriteriaBase searchCriteria, NameValueCollection parameters)
        {
            var baseUrl = string.Format("{0}{1}?t={2}&extended=1", Settings.BaseUrl.TrimEnd('/'), Settings.ApiPath.TrimEnd('/'), searchCriteria.SearchType);
            var categories = searchCriteria.Categories;

            if (categories != null && categories.Any())
            {
                var categoriesQuery = string.Join(",", categories.Distinct());
                baseUrl += string.Format("&cat={0}", categoriesQuery);
            }

            if (Settings.ApiKey.IsNotNullOrWhiteSpace())
            {
                baseUrl += "&apikey=" + Settings.ApiKey;
            }

            if (searchCriteria.Limit.HasValue)
            {
                parameters.Add("limit", searchCriteria.Limit.ToString());
            }

            if (searchCriteria.Offset.HasValue)
            {
                parameters.Add("offset", searchCriteria.Offset.ToString());
            }

            var request = new IndexerRequest(string.Format("{0}&{1}", baseUrl, parameters.GetQueryString()), HttpAccept.Rss);
            request.HttpRequest.AddBasicAuthentication(Settings.Username, Settings.Password);

            yield return request;
        }

        private static string NewsnabifyTitle(string title)
        {
            return title.Replace("+", "%20");
        }
    }
}
