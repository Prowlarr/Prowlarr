using System.Linq;
using System.Reflection;
using NzbDrone.Core.Authentication;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Validation.Paths;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Config
{
    public class DevelopmentConfigModule : ProwlarrRestModule<DevelopmentConfigResource>
    {
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IConfigService _configService;

        public DevelopmentConfigModule(IConfigFileProvider configFileProvider,
                                IConfigService configService)
            : base("/config/development")
        {
            _configFileProvider = configFileProvider;
            _configService = configService;

            GetResourceSingle = GetDevelopmentConfig;
            GetResourceById = GetDevelopmentConfig;
            UpdateResource = SaveDevelopmentConfig;
        }

        private DevelopmentConfigResource GetDevelopmentConfig()
        {
            var resource = DevelopmentConfigResourceMapper.ToResource(_configFileProvider, _configService);
            resource.Id = 1;

            return resource;
        }

        private DevelopmentConfigResource GetDevelopmentConfig(int id)
        {
            return GetDevelopmentConfig();
        }

        private void SaveDevelopmentConfig(DevelopmentConfigResource resource)
        {
            var dictionary = resource.GetType()
                                     .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                     .ToDictionary(prop => prop.Name, prop => prop.GetValue(resource, null));

            _configFileProvider.SaveConfigDictionary(dictionary);
            _configService.SaveConfigDictionary(dictionary);
        }
    }
}
