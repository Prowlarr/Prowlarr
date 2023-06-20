using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Indexers;
using Prowlarr.Http;

namespace NzbDrone.Api.V1.Indexers
{
    [V1ApiController("indexer/categories")]
    public class IndexerDefaultCategoriesController : Controller
    {
        [HttpGet]
        [Produces("application/json")]
        public IndexerCategory[] GetAll()
        {
            return NewznabStandardCategory.ParentCats;
        }
    }
}
