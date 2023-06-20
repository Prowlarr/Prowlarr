using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Disk;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.FileSystem
{
    [V1ApiController]
    public class FileSystemController : Controller
    {
        private readonly IFileSystemLookupService _fileSystemLookupService;
        private readonly IDiskProvider _diskProvider;

        public FileSystemController(IFileSystemLookupService fileSystemLookupService,
                                IDiskProvider diskProvider)
        {
            _fileSystemLookupService = fileSystemLookupService;
            _diskProvider = diskProvider;
        }

        [HttpGet]
        [Produces("application/json")]
        public IActionResult GetContents(string path, bool includeFiles = false, bool allowFoldersWithoutTrailingSlashes = false)
        {
            return Ok(_fileSystemLookupService.LookupContents(path, includeFiles, allowFoldersWithoutTrailingSlashes));
        }

        [HttpGet("type")]
        [Produces("application/json")]
        public object GetEntityType(string path)
        {
            if (_diskProvider.FileExists(path))
            {
                return new { type = "file" };
            }

            //Return folder even if it doesn't exist on disk to avoid leaking anything from the UI about the underlying system
            return new { type = "folder" };
        }
    }
}
