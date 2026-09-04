using System;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.OutputCaching;
using NLog;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Cache;

namespace NzbDrone.Host.Caching
{
    public class SqliteOutputCacheStore : IOutputCacheStore
    {
        private readonly ISqliteCacheDatabase _cacheDatabase;
        private readonly Logger _logger = NzbDroneLogger.GetLogger(typeof(SqliteOutputCacheStore));

        public SqliteOutputCacheStore(ISqliteCacheDatabase cacheDatabase)
        {
            _cacheDatabase = cacheDatabase;
        }

        public async ValueTask<byte[]> GetAsync(string key, CancellationToken cancellationToken)
        {
            var hash = CacheKeyHasher.Hash(key);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            try
            {
                using var connection = _cacheDatabase.OpenConnection();

                const string selectSql = "SELECT Payload, ExpiresAt FROM OutputCache WHERE KeyHash = @hash;";
                var entry = await connection.QueryFirstOrDefaultAsync<OutputCacheRecord>(selectSql, new { hash });

                if (entry == null || entry.Payload == null || entry.Payload.Length == 0)
                {
                    return null;
                }

                if (entry.ExpiresAt <= now)
                {
                    // Expired entry
                    return null;
                }

                return BrotliCompressionHelper.Decompress(entry.Payload);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to read OutputCache for key: {0}", key);
                return null;
            }
        }

        public async ValueTask SetAsync(string key, byte[] value, string[] tags, TimeSpan validFor, CancellationToken cancellationToken)
        {
            if (value == null || value.Length == 0)
            {
                return;
            }

            var hash = CacheKeyHasher.Hash(key);

            try
            {
                var compressed = BrotliCompressionHelper.Compress(value, CompressionLevel.Fastest);
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var expiresAt = now + (long)validFor.TotalSeconds;

                using var connection = _cacheDatabase.OpenConnection();

                const string upsertSql = @"
                    INSERT INTO OutputCache (KeyHash, OriginalKey, Payload, CompressedSize, UncompressedSize, CreatedAt, ExpiresAt)
                    VALUES (@hash, @key, @compressed, @compressedSize, @uncompressedSize, @now, @expiresAt)
                    ON CONFLICT(KeyHash) DO UPDATE SET
                        OriginalKey = @key,
                        Payload = @compressed,
                        CompressedSize = @compressedSize,
                        UncompressedSize = @uncompressedSize,
                        ExpiresAt = @expiresAt;
                ";

                await connection.ExecuteAsync(upsertSql, new
                {
                    hash,
                    key,
                    compressed,
                    compressedSize = compressed.Length,
                    uncompressedSize = value.Length,
                    now,
                    expiresAt
                });
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to store into OutputCache for key: {0}", key);
            }
        }

        public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
        {
            // Tag-based eviction is not used
            return ValueTask.CompletedTask;
        }

        private class OutputCacheRecord
        {
            public byte[] Payload { get; set; }
            public long ExpiresAt { get; set; }
        }
    }
}
