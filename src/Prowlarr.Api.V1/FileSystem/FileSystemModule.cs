using System;
using System.IO;
using System.Linq;
using Nancy;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using Prowlarr.Http.Extensions;

namespace Prowlarr.Api.V1.FileSystem
{
    public class FileSystemModule : RadarrV3Module
    {
        private readonly IFileSystemLookupService _fileSystemLookupService;
        private readonly IDiskProvider _diskProvider;

        public FileSystemModule(IFileSystemLookupService fileSystemLookupService,
                                IDiskProvider diskProvider)
            : base("/filesystem")
        {
            _fileSystemLookupService = fileSystemLookupService;
            _diskProvider = diskProvider;
            Get("/", x => GetContents());
            Get("/type", x => GetEntityType());
        }

        private object GetContents()
        {
            var pathQuery = Request.Query.path;
            var includeFiles = Request.GetBooleanQueryParameter("includeFiles");
            var allowFoldersWithoutTrailingSlashes = Request.GetBooleanQueryParameter("allowFoldersWithoutTrailingSlashes");

            return _fileSystemLookupService.LookupContents((string)pathQuery.Value, includeFiles, allowFoldersWithoutTrailingSlashes);
        }

        private object GetEntityType()
        {
            var pathQuery = Request.Query.path;
            var path = (string)pathQuery.Value;

            if (_diskProvider.FileExists(path))
            {
                return new { type = "file" };
            }

            //Return folder even if it doesn't exist on disk to avoid leaking anything from the UI about the underlying system
            return new { type = "folder" };
        }
    }
}
