using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Languages;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Languages
{
    public class LanguageModule : ProwlarrRestModule<LanguageResource>
    {
        public LanguageModule()
        {
            GetResourceAll = GetAll;
            GetResourceById = GetById;
        }

        private LanguageResource GetById(int id)
        {
            var language = (Language)id;

            return new LanguageResource
            {
                Id = (int)language,
                Name = language.ToString()
            };
        }

        private List<LanguageResource> GetAll()
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
