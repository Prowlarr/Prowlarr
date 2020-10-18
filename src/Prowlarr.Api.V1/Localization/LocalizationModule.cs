using Newtonsoft.Json;
using NzbDrone.Core.Localization;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Localization
{
    public class LocalizationModule : ProwlarrRestModule<LocalizationResource>
    {
        private readonly ILocalizationService _localizationService;

        public LocalizationModule(ILocalizationService localizationService)
        {
            _localizationService = localizationService;

            Get("/", x => GetLocalizationDictionary());
        }

        private string GetLocalizationDictionary()
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
