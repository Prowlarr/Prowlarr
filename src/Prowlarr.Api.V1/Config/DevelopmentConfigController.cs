using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Configuration;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Config
{
    [V1ApiController("config/development")]
    public class DevelopmentConfigController : ConfigController<DevelopmentConfigResource>
    {
        private readonly IConfigFileProvider _configFileProvider;

        public DevelopmentConfigController(IConfigFileProvider configFileProvider,
                                IConfigService configService)
        : base(configService)
        {
            _configFileProvider = configFileProvider;
        }

        public override ActionResult<DevelopmentConfigResource> SaveConfig(DevelopmentConfigResource resource)
        {
            var dictionary = resource.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(prop => prop.Name, prop => prop.GetValue(resource, null));

            _configFileProvider.SaveConfigDictionary(dictionary);
            _configService.SaveConfigDictionary(dictionary);

            return Accepted(resource.Id);
        }

        protected override DevelopmentConfigResource ToResource(IConfigService model)
        {
            return DevelopmentConfigResourceMapper.ToResource(_configFileProvider, model);
        }
    }
}
