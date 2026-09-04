using System;
using System.IO;
using Dapper;
using NLog;
using NzbDrone.Common.EnvironmentInfo;

namespace NzbDrone.Core.Cache
{
    public class DownloadCacheMigrator : IDownloadCacheMigrator
    {
        private readonly ISqliteCacheDatabase _cacheDatabase;
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly Logger _logger;
        private static readonly object MigrationLock = new object();
        private static bool _migrated;

        public DownloadCacheMigrator(ISqliteCacheDatabase cacheDatabase, IAppFolderInfo appFolderInfo, Logger logger)
        {
            _cacheDatabase = cacheDatabase;
            _appFolderInfo = appFolderInfo;
            _logger = logger;
        }

        public void Migrate()
        {
            if (_migrated)
            {
                return;
            }

            lock (MigrationLock)
            {
                if (_migrated)
                {
                    return;
                }

                var legacyCacheDir = Path.Combine(_appFolderInfo.AppDataFolder, "download-cache");

                if (!Directory.Exists(legacyCacheDir))
                {
                    _migrated = true;
                    return;
                }

                try
                {
                    _logger.Info("Starting migration of legacy download cache directory from {0}...", legacyCacheDir);

                    var files = Directory.GetFiles(legacyCacheDir, "*", SearchOption.AllDirectories);
                    var migratedCount = 0;

                    using (var connection = _cacheDatabase.OpenConnection())
                    using (var transaction = connection.BeginTransaction())
                    {
                        const string insertSql = @"
                            INSERT OR IGNORE INTO DownloadCache (KeyHash, OriginalKey, Payload, CompressedSize, UncompressedSize, FileName, CreatedAt, LastAccessedAt)
                            VALUES (@hash, NULL, @compressed, @compressedSize, @uncompressedSize, @fileName, @createdAt, @lastAccessedAt);
                        ";

                        foreach (var filePath in files)
                        {
                            try
                            {
                                var fileInfo = new FileInfo(filePath);
                                var hash = fileInfo.Directory?.Name;

                                if (string.IsNullOrWhiteSpace(hash) || hash.Length != 64)
                                {
                                    continue;
                                }

                                var uncompressedBytes = File.ReadAllBytes(filePath);
                                if (uncompressedBytes.Length == 0)
                                {
                                    continue;
                                }

                                var compressedBytes = BrotliCompressionHelper.Compress(uncompressedBytes);
                                var createdAt = new DateTimeOffset(fileInfo.CreationTimeUtc).ToUnixTimeSeconds();
                                var lastAccessedAt = new DateTimeOffset(fileInfo.LastWriteTimeUtc).ToUnixTimeSeconds();

                                connection.Execute(insertSql,
                                    new
                                    {
                                        hash,
                                        compressed = compressedBytes,
                                        compressedSize = compressedBytes.Length,
                                        uncompressedSize = uncompressedBytes.Length,
                                        fileName = fileInfo.Name,
                                        createdAt,
                                        lastAccessedAt
                                    },
                                    transaction);

                                migratedCount++;
                            }
                            catch (Exception ex)
                            {
                                _logger.Warn(ex, "Failed to migrate legacy download cache file {0}", filePath);
                            }
                        }

                        transaction.Commit();
                    }

                    _logger.Info("Successfully migrated {0} files from legacy download cache into SQLite.", migratedCount);

                    try
                    {
                        Directory.Delete(legacyCacheDir, recursive: true);
                        _logger.Debug("Removed legacy download cache directory: {0}", legacyCacheDir);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Failed to remove legacy download cache directory after migration: {0}", legacyCacheDir);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "An error occurred during legacy download cache migration.");
                }
                finally
                {
                    _migrated = true;
                }
            }
        }
    }
}
