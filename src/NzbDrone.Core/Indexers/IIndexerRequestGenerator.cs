using System;
using System.Collections.Generic;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers
{
    public interface IIndexerRequestGenerator
    {
        IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria);
        IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria);
        IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria);
        IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria);
        IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria);
        Func<IDictionary<string, string>> GetCookies { get; set; }
        Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
