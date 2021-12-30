using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.CustomFilters;
using NzbDrone.Http.REST.Attributes;
using Prowlarr.Http;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1.CustomFilters
{
    [V1ApiController]
    public class CustomFilterController : RestController<CustomFilterResource>
    {
        private readonly ICustomFilterService _customFilterService;

        public CustomFilterController(ICustomFilterService customFilterService)
        {
            _customFilterService = customFilterService;
        }

        public override CustomFilterResource GetResourceById(int id)
        {
            return _customFilterService.Get(id).ToResource();
        }

        [HttpGet]
        [Produces("application/json")]
        public List<CustomFilterResource> GetCustomFilters()
        {
            return _customFilterService.All().ToResource();
        }

        [RestPostById]
        [Produces("application/json")]
        public ActionResult<CustomFilterResource> AddCustomFilter(CustomFilterResource resource)
        {
            var customFilter = _customFilterService.Add(resource.ToModel());

            return Created(customFilter.Id);
        }

        [RestPutById]
        [Produces("application/json")]
        public ActionResult<CustomFilterResource> UpdateCustomFilter(CustomFilterResource resource)
        {
            _customFilterService.Update(resource.ToModel());
            return Accepted(resource.Id);
        }

        [RestDeleteById]
        public void DeleteCustomResource(int id)
        {
            _customFilterService.Delete(id);
        }
    }
}
