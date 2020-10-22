using System.Collections.Generic;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Applications
{
    public interface IAppIndexerMapRepository : IBasicRepository<AppIndexerMap>
    {
        List<AppIndexerMap> GetMappingsForApp(int appId);
        void DeleteAllForApp(int appId);
    }

    public class TagRepository : BasicRepository<AppIndexerMap>, IAppIndexerMapRepository
    {
        public TagRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public void DeleteAllForApp(int appId)
        {
            Delete(x => x.AppId == appId);
        }

        public List<AppIndexerMap> GetMappingsForApp(int appId)
        {
            return Query(x => x.AppId == appId);
        }
    }
}
