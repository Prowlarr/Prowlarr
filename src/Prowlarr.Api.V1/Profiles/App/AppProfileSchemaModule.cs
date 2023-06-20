using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Profiles;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Profiles.App
{
    [V1ApiController("appprofile/schema")]
    public class QualityProfileSchemaController : Controller
    {
        private readonly IProfileService _profileService;

        public QualityProfileSchemaController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet]
        [Produces("application/json")]
        public AppProfileResource GetSchema()
        {
            var qualityProfile = _profileService.GetDefaultProfile(string.Empty);

            return qualityProfile.ToResource();
        }
    }
}
