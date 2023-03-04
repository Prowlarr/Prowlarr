using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Definitions.Gazelle;

public class GazelleRequestGenerator : IIndexerRequestGenerator
{
    public GazelleSettings Settings { get; }
    public IndexerCapabilities Capabilities { get; }
    public IIndexerHttpClient HttpClient { get; }
    public Logger Logger { get; }
    public Func<IDictionary<string, string>> GetCookies { get; set; }
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    protected virtual bool ImdbInTags => false;
    protected virtual string ApiUrl => Settings.BaseUrl + "ajax.php";

    public GazelleRequestGenerator(GazelleSettings settings, IndexerCapabilities capabilities, IIndexerHttpClient httpClient, Logger logger)
    {
        Settings = settings;
        Capabilities = capabilities;
        HttpClient = httpClient;
        Logger = logger;
    }

    protected IEnumerable<IndexerRequest> GetRequest(NameValueCollection parameters)
    {
        var request = new IndexerRequest($"{ApiUrl}?{parameters.GetQueryString()}", HttpAccept.Json)
        {
            HttpRequest =
            {
                AllowAutoRedirect = false
            }
        };

        yield return request;
    }

    public virtual IEnumerable<IndexerRequest> GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        var parameters = GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories);

        if (searchCriteria.ImdbId != null)
        {
            parameters.Set(ImdbInTags ? "taglist" : "cataloguenumber", searchCriteria.FullImdbId);
        }

        return GetRequest(parameters);
    }

    public IEnumerable<IndexerRequest> GetSearchRequests(MusicSearchCriteria searchCriteria)
    {
        var parameters = GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories);

        if (searchCriteria.Artist.IsNotNullOrWhiteSpace() && searchCriteria.Artist != "VA")
        {
            parameters.Set("artistname", searchCriteria.Artist);
        }

        if (searchCriteria.Album.IsNotNullOrWhiteSpace())
        {
            parameters.Set("groupname", searchCriteria.Album);
        }

        if (searchCriteria.Label.IsNotNullOrWhiteSpace())
        {
            parameters.Set("recordlabel", searchCriteria.Label);
        }

        return GetRequest(parameters);
    }

    public IEnumerable<IndexerRequest> GetSearchRequests(TvSearchCriteria searchCriteria)
    {
        var parameters = GetBasicSearchParameters(searchCriteria.SanitizedTvSearchString, searchCriteria.Categories);

        if (searchCriteria.ImdbId != null)
        {
            parameters.Set(ImdbInTags ? "taglist" : "cataloguenumber", searchCriteria.FullImdbId);
        }

        return GetRequest(parameters);
    }

    public IEnumerable<IndexerRequest> GetSearchRequests(BookSearchCriteria searchCriteria)
    {
        var parameters = GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories);

        return GetRequest(parameters);
    }

    public IEnumerable<IndexerRequest> GetSearchRequests(BasicSearchCriteria searchCriteria)
    {
        var parameters = GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories);

        return GetRequest(parameters);
    }

    // hook to adjust the search term
    protected virtual string GetSearchTerm(string term) => term;

    protected virtual NameValueCollection GetBasicSearchParameters(string term, int[] categories)
    {
        var parameters = new NameValueCollection
        {
            { "action", "browse" },
            { "order_by", "time" },
            { "order_way", "desc" }
        };

        var searchTerm = GetSearchTerm(term);

        if (searchTerm.IsNotNullOrWhiteSpace())
        {
            parameters.Set("searchstr", searchTerm.Replace(".", " "));
        }

        if (categories != null)
        {
            var queryCats = Capabilities.Categories.MapTorznabCapsToTrackers(categories);
            if (queryCats.Any())
            {
                queryCats.ForEach(cat => parameters.Set($"filter_cat[{cat}]", "1"));
            }
        }

        return parameters;
    }
}
