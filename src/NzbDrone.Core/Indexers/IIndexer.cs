using System.Threading.Tasks;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers
{
    public interface IIndexer : IProvider
    {
        bool SupportsRss { get; }
        bool SupportsSearch { get; }
        bool SupportsRedirect { get; }
        IndexerCapabilities Capabilities { get; }

        string BaseUrl { get; }
        DownloadProtocol Protocol { get; }
        IndexerPrivacy Privacy { get; }

        Task<IndexerPageableQueryResult> Fetch(MovieSearchCriteria searchCriteria);
        Task<IndexerPageableQueryResult> Fetch(MusicSearchCriteria searchCriteria);
        Task<IndexerPageableQueryResult> Fetch(TvSearchCriteria searchCriteria);
        Task<IndexerPageableQueryResult> Fetch(BookSearchCriteria searchCriteria);
        Task<IndexerPageableQueryResult> Fetch(BasicSearchCriteria searchCriteria);

        Task<byte[]> Download(HttpUri link);

        IndexerCapabilities GetCapabilities();
    }
}
