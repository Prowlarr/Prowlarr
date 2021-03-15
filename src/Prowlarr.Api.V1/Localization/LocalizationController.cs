using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NzbDrone.Core.Localization;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Localization
{
    [V1ApiController]
    public class LocalizationController : Controller
    {
        private readonly ILocalizationService _localizationService;

        public LocalizationController(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        [HttpGet]
        public string GetLocalizationDictionary()
        {
            // We don't want camel case for transation strings, create new serializer settings
            var serializerSettings = new JsonSerializerSettings
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
                DefaultValueHandling = DefaultValueHandling.Include
            };

            return JsonConvert.SerializeObject(_localizationService.GetLocalizationDictionary().ToResource(), serializerSettings);
        }
    }
}
