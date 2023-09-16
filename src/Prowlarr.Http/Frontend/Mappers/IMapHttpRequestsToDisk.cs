using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Prowlarr.Http.Frontend.Mappers
{
    public interface IMapHttpRequestsToDisk
    {
        string Map(string resourceUrl);
        bool CanHandle(string resourceUrl);
        Task<FileStreamResult> GetResponse(string resourceUrl);
    }
}
