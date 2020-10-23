using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider.Events;

namespace NzbDrone.Core.Applications
{
    public interface IAppIndexerMapService
    {
        List<AppIndexerMap> GetMappingsForApp(int appId);
        AppIndexerMap Insert(AppIndexerMap appIndexerMap);
        void DeleteAllForApp(int appId);
    }

    public class AppIndexerMapService : IAppIndexerMapService, IHandle<ProviderDeletedEvent<IApplication>>
    {
        private readonly IAppIndexerMapRepository _appIndexerMapRepository;

        public AppIndexerMapService(IAppIndexerMapRepository appIndexerMapRepository)
        {
            _appIndexerMapRepository = appIndexerMapRepository;
        }

        public void DeleteAllForApp(int appId)
        {
            _appIndexerMapRepository.DeleteAllForApp(appId);
        }

        public List<AppIndexerMap> GetMappingsForApp(int appId)
        {
            return _appIndexerMapRepository.GetMappingsForApp(appId);
        }

        public AppIndexerMap Insert(AppIndexerMap appIndexerMap)
        {
            return _appIndexerMapRepository.Insert(appIndexerMap);
        }

        public void Handle(ProviderDeletedEvent<IApplication> message)
        {
            _appIndexerMapRepository.DeleteAllForApp(message.ProviderId);
        }
    }
}
