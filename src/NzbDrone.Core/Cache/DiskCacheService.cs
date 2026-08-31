using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Cache
{
    public interface IDiskCacheService
    {
        Task<byte[]> Get(string key);

        Task Store(string key, byte[] value, string fileName);

        void Cleanup();
    }

    public class DiskCacheService : IDiskCacheService
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly Logger _logger;

        public DiskCacheService(IAppFolderInfo appFolderInfo, Logger logger)
        {
            _appFolderInfo = appFolderInfo;
            _logger = logger;
            Directory.CreateDirectory(GetDiskCacheDir());
        }

        public async Task<byte[]> Get(string key)
        {
            var hash = GenerateHash(key);

            var directory = Path.Combine(GetDiskCacheDir(), hash[..2], hash);

            if (!Directory.Exists(directory))
            {
                return null;
            }

            var files = Directory.GetFiles(directory);

            if (files.Length == 0)
            {
                return null;
            }

            var cachedFile = files[0];
            _logger.Debug("Download cache hit for {0}: {1}", key, cachedFile.CleanFileName());

            var value = await File.ReadAllBytesAsync(cachedFile);

            try
            {
                File.SetLastWriteTimeUtc(cachedFile, DateTime.UtcNow);
            }
            catch
            {
                // Cache metadata update failure shouldn't invalidate a successful read.
            }

            return value;
        }

        public async Task Store(string key, byte[] value, string fileName)
        {
            var hash = GenerateHash(key);

            var directory = Path.Combine(GetDiskCacheDir(), hash[..2], hash);

            Directory.CreateDirectory(directory);

            var safeFileName = GetSafeFileName(fileName);
            var path = Path.Combine(directory, safeFileName);

            var tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                await File.WriteAllBytesAsync(tempPath, value);
                File.Move(tempPath, path, overwrite: true);
                _logger.Debug("Stored file in disk cache for key {0}: {1}", key, path);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to store file in disk cache for key {0}: {1}", key, path);
            }
            finally
            {
                // Clean up if the move/write failed.
                File.Delete(tempPath);
            }
        }

        public void Cleanup()
        {
            var cacheMaxSize = int.TryParse(
                Environment.GetEnvironmentVariable("DOWNLOAD_CACHE_MAX_SIZE_MB"),
                out var mega)
                ? mega
                : 1000;

            var maxBytes = cacheMaxSize * 1024 * 1024;

            var files = Directory
                .EnumerateFiles(GetDiskCacheDir(), "*", SearchOption.AllDirectories)
                .Select(path =>
                {
                    var info = new FileInfo(path);

                    return new
                    {
                        Path = path,
                        Size = info.Length,
                        LastAccessed = info.LastWriteTimeUtc
                    };
                })
                .OrderBy(x => x.LastAccessed)
                .ToList();

            var totalSize = files.Sum(x => x.Size);
            var deletedCount = 0;

            foreach (var file in files)
            {
                if (totalSize <= maxBytes)
                {
                    break;
                }

                try
                {
                    var entryDirectory = Path.GetDirectoryName(file.Path);

                    if (entryDirectory is null)
                    {
                        _logger.Warn("Unable to determine parent directory for cache file {0}", file.Path);
                        continue;
                    }

                    Directory.Delete(entryDirectory, recursive: true);

                    totalSize -= file.Size;
                    deletedCount += 1;
                }
                catch (DirectoryNotFoundException)
                {
                    // Already deleted, possibly by another operation.
                }
            }

            _logger.Info("Cleaned up {0} files from disk cache", deletedCount);
        }

        private string GetDiskCacheDir()
        {
            return Path.Combine(_appFolderInfo.AppDataFolder, "download-cache");
        }

        private static string GenerateHash(string key)
        {
            return Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(key)))
                .ToLowerInvariant();
        }

        private static string GetSafeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "data";
            }

            var safeFileName = Path.GetFileName(fileName);

            if (string.IsNullOrWhiteSpace(safeFileName) ||
                safeFileName is "." or "..")
            {
                return "data";
            }

            if (safeFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return "data";
            }

            return safeFileName;
        }
    }
}
