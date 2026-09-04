using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Dapper;
using NLog;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Cache
{
    public class DownloadCacheService : IDownloadCacheService
    {
        private readonly ISqliteCacheDatabase _cacheDatabase;
        private readonly Logger _logger;

        public bool IsEnabled =>
            bool.TryParse(Environment.GetEnvironmentVariable("ENABLE_DOWNLOAD_CACHE"), out var enabled) && enabled;

        public DownloadCacheService(ISqliteCacheDatabase cacheDatabase,
                                    Logger logger)
        {
            _cacheDatabase = cacheDatabase;
            _logger = logger;
        }

        public async Task<byte[]> Get(string key)
        {
            if (!IsEnabled)
            {
                return null;
            }

            var hash = CacheKeyHasher.Hash(key);

            try
            {
                using var connection = _cacheDatabase.OpenConnection();

                const string selectSql = "SELECT Payload, FileName FROM DownloadCache WHERE KeyHash = @hash;";
                var result = await connection.QueryFirstOrDefaultAsync<DownloadCacheRecord>(selectSql, new { hash });

                if (result == null || result.Payload == null || result.Payload.Length == 0)
                {
                    return null;
                }

                _logger.Debug("Download cache hit for {0}: {1}", key, result.FileName.CleanFileName());

                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                const string updateSql = "UPDATE DownloadCache SET LastAccessedAt = @now WHERE KeyHash = @hash;";

                try
                {
                    await connection.ExecuteAsync(updateSql, new { now, hash });
                }
                catch (Exception ex)
                {
                    // Cache metadata update failure shouldn't invalidate a successful read.
                    _logger.Warn(ex, "Failed to update last access metadata for cache key {0}", key);
                }

                return BrotliCompressionHelper.Decompress(result.Payload);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to read download cache for key {0}", key);
                return null;
            }
        }

        public async Task Store(string key, byte[] value, string fileName)
        {
            if (!IsEnabled || value == null || value.Length == 0)
            {
                return;
            }

            var hash = CacheKeyHasher.Hash(key);
            var safeFileName = GetSafeFileName(fileName);

            try
            {
                var compressed = BrotliCompressionHelper.Compress(value, CompressionLevel.Fastest);
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                using var connection = _cacheDatabase.OpenConnection();

                const string upsertSql = @"
                    INSERT INTO DownloadCache (KeyHash, OriginalKey, Payload, CompressedSize, UncompressedSize, FileName, CreatedAt, LastAccessedAt)
                    VALUES (@hash, @key, @compressed, @compressedSize, @uncompressedSize, @safeFileName, @now, @now)
                    ON CONFLICT(KeyHash) DO UPDATE SET
                        OriginalKey = @key,
                        Payload = @compressed,
                        CompressedSize = @compressedSize,
                        UncompressedSize = @uncompressedSize,
                        FileName = @safeFileName,
                        LastAccessedAt = @now;
                ";

                await connection.ExecuteAsync(upsertSql, new
                {
                    hash,
                    key,
                    compressed,
                    compressedSize = compressed.Length,
                    uncompressedSize = value.Length,
                    safeFileName,
                    now
                });

                _logger.Debug("Stored download in SQLite cache for key {0}: {1}", key, safeFileName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to store download in cache for key {0}: {1}", key, fileName);
            }
        }

        public void Cleanup()
        {
            if (!IsEnabled)
            {
                return;
            }

            var cacheMaxSize = long.TryParse(
                Environment.GetEnvironmentVariable("DOWNLOAD_CACHE_MAX_SIZE_MB"),
                out var mega)
                ? mega
                : 1000;

            var maxBytes = cacheMaxSize * 1024 * 1024;

            try
            {
                using var connection = _cacheDatabase.OpenConnection();

                var currentTotalSize = connection.ExecuteScalar<long>("SELECT COALESCE(SUM(CompressedSize), 0) FROM DownloadCache;");

                _logger.Debug("Total compressed size of download cache: {0} MB, Limit: {1} MB",
                    currentTotalSize / 1024 / 1024,
                    maxBytes / 1024 / 1024);

                if (currentTotalSize <= maxBytes)
                {
                    return;
                }

                const string lruEvictionSql = @"
                    WITH Excess AS (
                        SELECT KeyHash,
                               SUM(CompressedSize) OVER (ORDER BY LastAccessedAt DESC) AS CumulativeSize
                        FROM DownloadCache
                    )
                    DELETE FROM DownloadCache
                    WHERE KeyHash IN (
                        SELECT KeyHash FROM Excess WHERE CumulativeSize > @maxBytes
                    );
                ";

                var deletedCount = connection.Execute(lruEvictionSql, new { maxBytes });

                _logger.Info("Cleaned up {0} entries from download cache", deletedCount);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to clean up download cache");
            }
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

        private class DownloadCacheRecord
        {
            public byte[] Payload { get; set; }
            public string FileName { get; set; }
        }
    }
}
