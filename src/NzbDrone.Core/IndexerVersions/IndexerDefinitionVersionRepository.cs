using System.Linq;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.IndexerVersions
{
    public interface IIndexerDefinitionVersionRepository : IBasicRepository<IndexerDefinitionVersion>
    {
        public IndexerDefinitionVersion GetByDefId(string defId);
    }

    public class IndexerDefinitionVersionRepository : BasicRepository<IndexerDefinitionVersion>, IIndexerDefinitionVersionRepository
    {
        public IndexerDefinitionVersionRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public IndexerDefinitionVersion GetByDefId(string defId)
        {
            return Query(x => x.DefinitionId == defId).SingleOrDefault();
        }
    }
}
