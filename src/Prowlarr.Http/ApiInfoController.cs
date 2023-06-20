using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace Prowlarr.Http
{
    public class ApiInfoController : Controller
    {
        [HttpGet("/api")]
        [Produces("application/json")]
        public ApiInfoResource GetApiInfo()
        {
            return new ApiInfoResource
            {
                Current = "v1",
                Deprecated = new List<string>()
            };
        }
    }
}
