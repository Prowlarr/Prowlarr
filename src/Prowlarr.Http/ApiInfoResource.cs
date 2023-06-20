using System.Collections.Generic;

namespace Prowlarr.Http
{
    public class ApiInfoResource
    {
        public string Current { get; set; }
        public List<string> Deprecated { get; set; }
    }
}
