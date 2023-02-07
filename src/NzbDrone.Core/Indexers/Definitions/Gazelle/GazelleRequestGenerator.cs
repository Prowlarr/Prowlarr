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

    public virtual IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        var parameters = GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories);

        if (searchCriteria.ImdbId != null)
        {
            parameters.Set(ImdbInTags ? "taglist" : "cataloguenumber", searchCriteria.FullImdbId);
        }

        pageableRequests.Add(GetRequest(parameters));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        var parameters = GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories);

        if (Capabilities.SupportsRawSearch)
        {
            if (searchCriteria.SanitizedArtist.IsNotNullOrWhiteSpace() && searchCriteria.SanitizedArtist != "VA")
            {
                parameters.Set("artistname", searchCriteria.SanitizedArtist);
            }
        }
        else if (searchCriteria.Artist.IsNotNullOrWhiteSpace() && searchCriteria.Artist != "VA")
        {
            parameters.Set("artistname", searchCriteria.Artist);
        }

        if (Capabilities.SupportsRawSearch && searchCriteria.SanitizedAlbum.IsNotNullOrWhiteSpace())
        {
            parameters.Set("groupname", searchCriteria.SanitizedAlbum);
        }
        else if (searchCriteria.Album.IsNotNullOrWhiteSpace())
        {
            parameters.Set("groupname", searchCriteria.Album);
        }

        if (Capabilities.SupportsRawSearch && searchCriteria.SanitizedLabel.IsNotNullOrWhiteSpace())
        {
            parameters.Set("recordlabel", searchCriteria.SanitizedLabel);
        }
        else if (searchCriteria.Label.IsNotNullOrWhiteSpace())
        {
            parameters.Set("recordlabel", searchCriteria.Label);
        }

        pageableRequests.Add(GetRequest(parameters));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        var parameters = GetBasicSearchParameters(searchCriteria.SanitizedTvSearchString, searchCriteria.Categories);

        if (searchCriteria.ImdbId != null)
        {
            parameters.Set(ImdbInTags ? "taglist" : "cataloguenumber", searchCriteria.FullImdbId);
        }

        pageableRequests.Add(GetRequest(parameters));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        var parameters = GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories);
        pageableRequests.Add(GetRequest(parameters));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        var parameters = GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories);
        pageableRequests.Add(GetRequest(parameters));

        return pageableRequests;
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
