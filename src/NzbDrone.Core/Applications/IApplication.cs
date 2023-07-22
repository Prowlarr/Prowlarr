using System.Collections.Generic;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Applications
{
    public interface IApplication : IProvider
    {
        void AddIndexer(IndexerDefinition indexer);
        void UpdateIndexer(IndexerDefinition indexer, bool forceSync = false);
        void RemoveIndexer(int indexerId);
        List<AppIndexerMap> GetIndexerMappings();
    }
}
