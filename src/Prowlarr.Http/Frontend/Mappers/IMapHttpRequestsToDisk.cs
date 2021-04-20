using Microsoft.AspNetCore.Mvc;

namespace Prowlarr.Http.Frontend.Mappers
{
    public interface IMapHttpRequestsToDisk
    {
        string Map(string resourceUrl);
        bool CanHandle(string resourceUrl);
        FileStreamResult GetResponse(string resourceUrl);
    }
}
