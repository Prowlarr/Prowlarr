using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using NLog;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Cache
{
    public class SqliteCacheDatabase : ISqliteCacheDatabase
    {
        private readonly IAppFolderInfo _appFolderInfo;
        private readonly Logger _logger;
        private readonly string _connectionString;
        private readonly object _initLock = new object();
        private bool _isInitialized;

        public SqliteCacheDatabase(IAppFolderInfo appFolderInfo, Logger logger)
        {
            _appFolderInfo = appFolderInfo;
            _logger = logger;

            var dbPath = _appFolderInfo.GetCacheDatabase();
            var connectionBuilder = new SQLiteConnectionStringBuilder
            {
                DataSource = dbPath,
                CacheSize = (int)-20000,
                DateTimeKind = DateTimeKind.Utc,
                JournalMode = OsInfo.IsOsx ? SQLiteJournalModeEnum.Truncate : SQLiteJournalModeEnum.Wal,
                Pooling = true,
                Version = 3,
                BusyTimeout = 5000
            };

            if (OsInfo.IsOsx)
            {
                connectionBuilder.Add("Full FSync", true);
            }

            _connectionString = connectionBuilder.ConnectionString;
        }

        public IDbConnection OpenConnection()
        {
            Initialize();

            var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            return connection;
        }

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            lock (_initLock)
            {
                if (_isInitialized)
                {
                    return;
                }

                try
                {
                    var dbPath = _appFolderInfo.GetCacheDatabase();
                    var directory = Path.GetDirectoryName(dbPath);

                    if (directory != null && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (var connection = new SQLiteConnection(_connectionString))
                    {
                        connection.Open();

                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = @"
                                PRAGMA synchronous = NORMAL;

                                CREATE TABLE IF NOT EXISTS DownloadCache (
                                    KeyHash TEXT PRIMARY KEY NOT NULL,
                                    OriginalKey TEXT,
                                    Payload BLOB NOT NULL,
                                    CompressedSize INTEGER NOT NULL,
                                    UncompressedSize INTEGER NOT NULL,
                                    FileName TEXT,
                                    CreatedAt INTEGER NOT NULL,
                                    LastAccessedAt INTEGER NOT NULL
                                );
                                CREATE INDEX IF NOT EXISTS IX_DownloadCache_LastAccessedAt ON DownloadCache (LastAccessedAt);

                                CREATE TABLE IF NOT EXISTS OutputCache (
                                    KeyHash TEXT PRIMARY KEY NOT NULL,
                                    OriginalKey TEXT,
                                    Payload BLOB NOT NULL,
                                    CompressedSize INTEGER NOT NULL,
                                    UncompressedSize INTEGER NOT NULL,
                                    CreatedAt INTEGER NOT NULL,
                                    ExpiresAt INTEGER NOT NULL
                                );
                                CREATE INDEX IF NOT EXISTS IX_OutputCache_ExpiresAt ON OutputCache (ExpiresAt);
                            ";
                            cmd.ExecuteNonQuery();
                        }
                    }

                    _isInitialized = true;
                    _logger.Debug("Initialized cache database at {0}", dbPath);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to initialize SQLite cache database");
                    throw;
                }
            }
        }
    }
}
