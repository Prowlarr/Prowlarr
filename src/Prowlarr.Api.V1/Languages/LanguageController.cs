using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Languages;
using Prowlarr.Http;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1.Languages
{
    [V1ApiController()]
    public class LanguageController : RestController<LanguageResource>
    {
        public override LanguageResource GetResourceById(int id)
        {
            var language = (Language)id;

            return new LanguageResource
            {
                Id = (int)language,
                Name = language.ToString()
            };
        }

        [HttpGet]
        [Produces("application/json")]
        public List<LanguageResource> GetAll()
        {
            return Language.All.Select(l => new LanguageResource
            {
                Id = (int)l,
                Name = l.ToString()
            })
                                    .OrderBy(l => l.Name)
                                    .ToList();
        }
    }
}
