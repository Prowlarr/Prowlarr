using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Definitions.FileList;

public class FileListRequestGenerator : IIndexerRequestGenerator
{
    public FileListSettings Settings { get; set; }
    public IndexerCapabilities Capabilities { get; set; }
    public Func<IDictionary<string, string>> GetCookies { get; set; }
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

    public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();
        var parameters = new NameValueCollection();

        if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace() || searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
        {
            parameters.Set("action", "search-torrents");

            var searchQuery = searchCriteria.SanitizedSearchTerm.Trim();

            if (DateTime.TryParseExact($"{searchCriteria.Season} {searchCriteria.Episode}", "yyyy MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var showDate))
            {
                searchQuery = $"{searchQuery} {showDate:yyyy.MM.dd}".Trim();
            }
            else
            {
                if (searchCriteria.Season.HasValue)
                {
                    parameters.Set("season", searchCriteria.Season.ToString());
                }

                if (searchCriteria.Episode.IsNotNullOrWhiteSpace())
                {
                    parameters.Set("episode", searchCriteria.Episode);
                }
            }

            if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace())
            {
                parameters.Set("type", "imdb");
                parameters.Set("query", searchCriteria.FullImdbId);
            }
            else if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
            {
                parameters.Set("type", "name");
                parameters.Set("query", searchQuery);
            }
        }

        pageableRequests.Add(GetPagedRequests(searchCriteria, parameters));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();
        var parameters = new NameValueCollection();

        if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace())
        {
            parameters.Set("action", "search-torrents");
            parameters.Set("type", "imdb");
            parameters.Set("query", searchCriteria.FullImdbId);
        }
        else if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
        {
            parameters.Set("action", "search-torrents");
            parameters.Set("type", "name");
            parameters.Set("query", searchCriteria.SanitizedSearchTerm.Trim());
        }

        pageableRequests.Add(GetPagedRequests(searchCriteria, parameters));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();
        var parameters = new NameValueCollection();

        if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
        {
            parameters.Set("action", "search-torrents");
            parameters.Set("type", "name");
            parameters.Set("query", searchCriteria.SanitizedSearchTerm.Trim());
        }

        pageableRequests.Add(GetPagedRequests(searchCriteria, parameters));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();
        var parameters = new NameValueCollection();

        if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
        {
            parameters.Set("action", "search-torrents");
            parameters.Set("type", "name");
            parameters.Set("query", searchCriteria.SanitizedSearchTerm.Trim());
        }

        pageableRequests.Add(GetPagedRequests(searchCriteria, parameters));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();
        var parameters = new NameValueCollection();

        if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
        {
            parameters.Set("action", "search-torrents");
            parameters.Set("type", "name");
            parameters.Set("query", searchCriteria.SanitizedSearchTerm.Trim());
        }

        pageableRequests.Add(GetPagedRequests(searchCriteria, parameters));

        return pageableRequests;
    }

    private IEnumerable<IndexerRequest> GetPagedRequests(SearchCriteriaBase searchCriteria, NameValueCollection parameters)
    {
        if (parameters.Get("action") is null)
        {
            parameters.Set("action", "latest-torrents");
        }

        if (searchCriteria.Categories != null && searchCriteria.Categories.Any())
        {
            parameters.Set("category", string.Join(",", Capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories)));
        }

        if (Settings.FreeleechOnly)
        {
            parameters.Set("freeleech", "1");
        }

        var searchUrl = $"{Settings.BaseUrl.TrimEnd('/')}/api.php?{parameters.GetQueryString()}";

        var request = new IndexerRequest(searchUrl, HttpAccept.Json)
        {
            HttpRequest =
            {
                Credentials = new BasicNetworkCredential(Settings.Username.Trim(), Settings.Passkey.Trim())
            }
        };

        yield return request;
    }
}
