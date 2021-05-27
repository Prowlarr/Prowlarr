using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Profiles
{
    public interface IProfileService
    {
        AppSyncProfile Add(AppSyncProfile profile);
        void Update(AppSyncProfile profile);
        void Delete(int id);
        List<AppSyncProfile> All();
        AppSyncProfile Get(int id);
        bool Exists(int id);
        AppSyncProfile GetDefaultProfile(string name);
    }

    public class AppSyncProfileService : IProfileService,
        IHandle<ApplicationStartedEvent>
    {
        private readonly IAppProfileRepository _profileRepository;
        private readonly IIndexerFactory _indexerFactory;
        private readonly Logger _logger;

        public AppSyncProfileService(IAppProfileRepository profileRepository,
                              IIndexerFactory movieService,
                              Logger logger)
        {
            _profileRepository = profileRepository;
            _indexerFactory = movieService;
            _logger = logger;
        }

        public AppSyncProfile Add(AppSyncProfile profile)
        {
            return _profileRepository.Insert(profile);
        }

        public void Update(AppSyncProfile profile)
        {
            _profileRepository.Update(profile);
        }

        public void Delete(int id)
        {
            if (_indexerFactory.All().Any(c => c.AppProfileId == id))
            {
                throw new ProfileInUseException(id);
            }

            _profileRepository.Delete(id);
        }

        public List<AppSyncProfile> All()
        {
            return _profileRepository.All().ToList();
        }

        public AppSyncProfile Get(int id)
        {
            return _profileRepository.Get(id);
        }

        public bool Exists(int id)
        {
            return _profileRepository.Exists(id);
        }

        public void Handle(ApplicationStartedEvent message)
        {
            if (All().Any())
            {
                return;
            }

            _logger.Info("Setting up default app profile");

            AddDefaultProfile("Standard");
        }

        public AppSyncProfile GetDefaultProfile(string name)
        {
            var qualityProfile = new AppSyncProfile
            {
                Name = name,
                EnableAutomaticSearch = true,
                EnableInteractiveSearch = true,
                EnableRss = true
            };

            return qualityProfile;
        }

        private AppSyncProfile AddDefaultProfile(string name)
        {
            var profile = GetDefaultProfile(name);

            return Add(profile);
        }
    }
}
