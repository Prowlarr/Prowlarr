using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Localization;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Localization
{
    [V1ApiController]
    public class LocalizationController : Controller
    {
        private readonly ILocalizationService _localizationService;
        private readonly JsonSerializerOptions _serializerSettings;

        public LocalizationController(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
            _serializerSettings = STJson.GetSerializerSettings();
            _serializerSettings.DictionaryKeyPolicy = null;
            _serializerSettings.PropertyNamingPolicy = null;
        }

        [HttpGet]
        [Produces("application/json")]
        public IActionResult GetLocalizationDictionary()
        {
            return Json(_localizationService.GetLocalizationDictionary().ToResource(), _serializerSettings);
        }

        [HttpGet("Options")]
        [Produces("application/json")]
        public ActionResult<IEnumerable<LocalizationOption>> GetLocalizationOptions()
        {
            return Ok(_localizationService.GetLocalizationOptions());
        }
    }
}
