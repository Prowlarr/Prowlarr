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
        private readonly IAppProfileService _appProfileService;

        public AppProfileController(IAppProfileService appProfileService)
        {
            _appProfileService = appProfileService;
            SharedValidator.RuleFor(c => c.Name).NotEmpty();
        }

        [RestPostById]
        [Consumes("application/json")]
        [Produces("application/json")]
        public ActionResult<AppProfileResource> Create(AppProfileResource resource)
        {
            var model = resource.ToModel();
            model = _appProfileService.Add(model);
            return Created(model.Id);
        }

        [RestDeleteById]
        [Produces("application/json")]
        public object DeleteProfile(int id)
        {
            _appProfileService.Delete(id);
            return new { };
        }

        [RestPutById]
        [Consumes("application/json")]
        [Produces("application/json")]
        public ActionResult<AppProfileResource> Update(AppProfileResource resource)
        {
            var model = resource.ToModel();

            _appProfileService.Update(model);

            return Accepted(model.Id);
        }

        [Produces("application/json")]
        [ProducesResponseType(typeof(AppProfileResource), 200)]
        [ProducesResponseType(typeof(IDictionary<string, string>), 404)]
        [ProducesResponseType(500)]
        public override AppProfileResource GetResourceById(int id)
        {
            return _appProfileService.Get(id).ToResource();
        }

        [HttpGet]
        [Produces("application/json")]
        public List<AppProfileResource> GetAll()
        {
            return _appProfileService.All().ToResource();
        }
    }
}
