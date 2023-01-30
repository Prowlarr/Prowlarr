using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        var parameters = GetDefaultParameters();

        if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace() || searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
        {
            parameters.Add("action", "search-torrents");

            if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace())
            {
                parameters.Add("type", "imdb");
                parameters.Add("query", searchCriteria.FullImdbId);
            }
            else if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
            {
                parameters.Add("type", "name");
                parameters.Add("query", searchCriteria.SanitizedSearchTerm.Trim());
            }

            if (searchCriteria.Season.HasValue)
            {
                parameters.Add("season", searchCriteria.Season.ToString());
                parameters.Add("episode", searchCriteria.Episode);
            }
        }

        pageableRequests.Add(GetRequest(searchCriteria, parameters));

        return pageableRequests;
    }

    public virtual IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();
        var parameters = GetDefaultParameters();

        if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace())
        {
            parameters.Add("action", "search-torrents");
            parameters.Add("type", "imdb");
            parameters.Add("query", searchCriteria.FullImdbId);
        }
        else if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
        {
            parameters.Add("action", "search-torrents");
            parameters.Add("type", "name");
            parameters.Add("query", searchCriteria.SanitizedSearchTerm.Trim());
        }

        pageableRequests.Add(GetRequest(searchCriteria, parameters));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();
        var parameters = GetDefaultParameters();

        if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
        {
            parameters.Add("action", "search-torrents");
            parameters.Add("type", "name");
            parameters.Add("query", searchCriteria.SanitizedSearchTerm.Trim());
        }

        pageableRequests.Add(GetRequest(searchCriteria, parameters));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();
        var parameters = GetDefaultParameters();

        if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
        {
            parameters.Add("action", "search-torrents");
            parameters.Add("type", "name");
            parameters.Add("query", searchCriteria.SanitizedSearchTerm.Trim());
        }

        pageableRequests.Add(GetRequest(searchCriteria, parameters));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();
        var parameters = GetDefaultParameters();

        if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
        {
            parameters.Add("action", "search-torrents");
            parameters.Add("type", "name");
            parameters.Add("query", searchCriteria.SanitizedSearchTerm.Trim());
        }

        pageableRequests.Add(GetRequest(searchCriteria, parameters));

        return pageableRequests;
    }

    private IEnumerable<IndexerRequest> GetRequest(SearchCriteriaBase searchCriteria, NameValueCollection parameters)
    {
        if (parameters.Get("action") is null)
        {
            parameters.Add("action", "latest-torrents");
        }

        parameters.Add("category", string.Join(",", Capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories)));

        var searchUrl = $"{Settings.BaseUrl.TrimEnd('/')}/api.php?{parameters.GetQueryString()}";

        yield return new IndexerRequest(searchUrl, HttpAccept.Json);
    }

    private NameValueCollection GetDefaultParameters()
    {
        var parameters = new NameValueCollection
        {
            { "username", Settings.Username.Trim() },
            { "passkey", Settings.Passkey.Trim() }
        };

        if (Settings.FreeleechOnly)
        {
            parameters.Add("freeleech", "1");
        }

        return parameters;
    }
}
