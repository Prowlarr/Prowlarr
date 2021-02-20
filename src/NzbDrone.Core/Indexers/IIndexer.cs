using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers
{
    public interface IIndexer : IProvider
    {
        bool SupportsRss { get; }
        bool SupportsSearch { get; }
        IndexerCapabilities Capabilities { get; }

        string BaseUrl { get; }
        DownloadProtocol Protocol { get; }
        IndexerPrivacy Privacy { get; }

        IndexerPageableQueryResult Fetch(MovieSearchCriteria searchCriteria);
        IndexerPageableQueryResult Fetch(MusicSearchCriteria searchCriteria);
        IndexerPageableQueryResult Fetch(TvSearchCriteria searchCriteria);
        IndexerPageableQueryResult Fetch(BookSearchCriteria searchCriteria);
        IndexerPageableQueryResult Fetch(BasicSearchCriteria searchCriteria);

        byte[] Download(HttpUri link);

        IndexerCapabilities GetCapabilities();
    }
}
