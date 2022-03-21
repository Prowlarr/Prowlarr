using System.Collections.Generic;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Profiles;
using NzbDrone.Http.REST.Attributes;
using Prowlarr.Http;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1.Profiles.App
{
    [V1ApiController]
    public class AppProfileController : RestController<AppProfileResource>
    {
        private readonly IProfileService _profileService;

        public AppProfileController(IProfileService profileService)
        {
            _profileService = profileService;
            SharedValidator.RuleFor(c => c.Name).NotEmpty();
        }

        [RestPostById]
        [Produces("application/json")]
        public ActionResult<AppProfileResource> Create(AppProfileResource resource)
        {
            var model = resource.ToModel();
            model = _profileService.Add(model);
            return Created(model.Id);
        }

        [RestDeleteById]
        [Produces("application/json")]
        public object DeleteProfile(int id)
        {
            _profileService.Delete(id);
            return new { };
        }

        [RestPutById]
        [Produces("application/json")]
        public ActionResult<AppProfileResource> Update(AppProfileResource resource)
        {
            var model = resource.ToModel();

            _profileService.Update(model);

            return Accepted(model.Id);
        }

        [Produces("application/json")]
        [ProducesResponseType(typeof(AppProfileResource), 200)]
        [ProducesResponseType(typeof(IDictionary<string, string>), 404)]
        [ProducesResponseType(500)]
        public override AppProfileResource GetResourceById(int id)
        {
            return _profileService.Get(id).ToResource();
        }

        [HttpGet]
        [Produces("application/json")]
        public List<AppProfileResource> GetAll()
        {
            return _profileService.All().ToResource();
        }
    }
}
