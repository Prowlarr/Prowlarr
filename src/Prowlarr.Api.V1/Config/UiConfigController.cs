using System.Linq;
using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Localization;
using NzbDrone.Http.REST.Attributes;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Config
{
    [V1ApiController("config/ui")]
    public class UiConfigController : ConfigController<UiConfigResource>
    {
        private readonly IConfigFileProvider _configFileProvider;

        public UiConfigController(IConfigFileProvider configFileProvider, IConfigService configService, ILocalizationService localizationService)
            : base(configService)
        {
            _configFileProvider = configFileProvider;

            SharedValidator.RuleFor(c => c.UILanguage)
                           .NotEmpty()
                           .WithMessage("The UI Language value cannot be empty");

            SharedValidator.RuleFor(c => c.UILanguage).Custom((value, context) =>
            {
                if (!localizationService.GetLocalizationOptions().Any(o => o.Value == value))
                {
                    context.AddFailure("Invalid UI Language value");
                }
            });
        }

        [RestPutById]
        [Consumes("application/json")]
        [Produces("application/json")]
        public override ActionResult<UiConfigResource> SaveConfig(UiConfigResource resource)
        {
            var dictionary = resource.GetType()
                                     .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                     .ToDictionary(prop => prop.Name, prop => prop.GetValue(resource, null));

            _configFileProvider.SaveConfigDictionary(dictionary);
            _configService.SaveConfigDictionary(dictionary);

            return Accepted(resource.Id);
        }

        protected override UiConfigResource ToResource(IConfigService model)
        {
            return UiConfigResourceMapper.ToResource(_configFileProvider, model);
        }
    }
}
