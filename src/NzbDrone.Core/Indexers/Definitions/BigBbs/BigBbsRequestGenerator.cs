using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Definitions.BigBbs;

public class BigBbsRequestGenerator : IIndexerRequestGenerator
{
    private readonly BigBbsSettings _settings;
    private readonly IndexerCapabilities _capabilities;

    public BigBbsRequestGenerator(BigBbsSettings settings, IndexerCapabilities capabilities)
    {
        _settings = settings;
        _capabilities = capabilities;
    }

    private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
    {
        var queryCollection = new NameValueCollection();

        var catList = _capabilities.Categories.MapTorznabCapsToTrackers(categories);
        foreach (var cat in catList)
        {
            queryCollection.Add("cid[]", cat);
        }

        if (term.IsNotNullOrWhiteSpace())
        {
            var keywords = Regex.Replace(term, "[^a-zA-Z0-9]+", "%25").Trim();
            queryCollection.Set("keywords", keywords);
        }

        queryCollection.Set("search_type", "name");
        queryCollection.Set("sortOptions[sortBy]", _settings.SortBy.ToString().ToLowerInvariant());
        queryCollection.Set("sortOptions[sortOrder]", _settings.SortOrder.ToString().ToLowerInvariant());

        var searchUrl = $"{_settings.BaseUrl.TrimEnd('/')}/?p=torrents&pid=10&{queryCollection.GetQueryString()}";

        yield return new IndexerRequest(searchUrl, HttpAccept.Html);
    }

    public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests(searchCriteria.SanitizedTvSearchString, searchCriteria.Categories));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests(searchCriteria.SanitizedSearchTerm, searchCriteria.Categories));

        return pageableRequests;
    }

    public Func<IDictionary<string, string>> GetCookies { get; set; }
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}
