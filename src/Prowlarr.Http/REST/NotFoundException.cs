using System.Net;
using Prowlarr.Http.Exceptions;

namespace Prowlarr.Http.REST
{
    public class NotFoundException : ApiException
    {
        public NotFoundException(object content = null)
            : base(HttpStatusCode.NotFound, content)
        {
        }
    }
}
