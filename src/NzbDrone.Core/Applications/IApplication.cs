using NzbDrone.Core.Indexers;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Applications
{
    public interface IApplication : IProvider
    {
        void SyncIndexers();
        void AddIndexer(IndexerDefinition indexer);
        void UpdateIndexer(IndexerDefinition indexer);
        void RemoveIndexer(int indexerId);
    }
}
