using System;
using System.Collections.Generic;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers
{
    public interface IIndexerRequestGenerator
    {
        IEnumerable<IndexerRequest> GetSearchRequests(MovieSearchCriteria searchCriteria);
        IEnumerable<IndexerRequest> GetSearchRequests(MusicSearchCriteria searchCriteria);
        IEnumerable<IndexerRequest> GetSearchRequests(TvSearchCriteria searchCriteria);
        IEnumerable<IndexerRequest> GetSearchRequests(BookSearchCriteria searchCriteria);
        IEnumerable<IndexerRequest> GetSearchRequests(BasicSearchCriteria searchCriteria);
        Func<IDictionary<string, string>> GetCookies { get; set; }
        Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
