using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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

    public IEnumerable<IndexerRequest> GetSearchRequests(TvSearchCriteria searchCriteria)
    {
        var parameters = new NameValueCollection();

        if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace() || searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
        {
            parameters.Set("action", "search-torrents");

            if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace())
            {
                parameters.Set("type", "imdb");
                parameters.Set("query", searchCriteria.FullImdbId);
            }
            else if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
            {
                parameters.Set("type", "name");
                parameters.Set("query", searchCriteria.SanitizedSearchTerm.Trim());
            }

            if (searchCriteria.Season.HasValue)
            {
                parameters.Set("season", searchCriteria.Season.ToString());
            }

            if (searchCriteria.Episode.IsNotNullOrWhiteSpace())
            {
                parameters.Set("episode", searchCriteria.Episode);
            }
        }

        return GetPagedRequests(searchCriteria, parameters);
    }

    public IEnumerable<IndexerRequest> GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
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

        return GetPagedRequests(searchCriteria, parameters);
    }

    public IEnumerable<IndexerRequest> GetSearchRequests(MusicSearchCriteria searchCriteria)
    {
        var parameters = new NameValueCollection();

        if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
        {
            parameters.Set("action", "search-torrents");
            parameters.Set("type", "name");
            parameters.Set("query", searchCriteria.SanitizedSearchTerm.Trim());
        }

        return GetPagedRequests(searchCriteria, parameters);
    }

    public IEnumerable<IndexerRequest> GetSearchRequests(BookSearchCriteria searchCriteria)
    {
        var parameters = new NameValueCollection();

        if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
        {
            parameters.Set("action", "search-torrents");
            parameters.Set("type", "name");
            parameters.Set("query", searchCriteria.SanitizedSearchTerm.Trim());
        }

        return GetPagedRequests(searchCriteria, parameters);
    }

    public IEnumerable<IndexerRequest> GetSearchRequests(BasicSearchCriteria searchCriteria)
    {
        var parameters = new NameValueCollection();

        if (searchCriteria.SearchTerm.IsNotNullOrWhiteSpace())
        {
            parameters.Set("action", "search-torrents");
            parameters.Set("type", "name");
            parameters.Set("query", searchCriteria.SanitizedSearchTerm.Trim());
        }

        return GetPagedRequests(searchCriteria, parameters);
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
