using System.Threading.Tasks;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Download
{
    public interface IDownloadClient : IProvider
    {
        bool SupportsCategories { get; }
        DownloadProtocol Protocol { get; }
        Task<string> Download(ReleaseInfo release, bool redirect, IIndexer indexer);
    }
}
