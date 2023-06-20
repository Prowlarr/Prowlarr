using System;
using System.Collections.Generic;
using System.Net;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.QBittorrent
{
    public class QBittorrent : TorrentClientBase<QBittorrentSettings>
    {
        private readonly IQBittorrentProxySelector _proxySelector;
        private readonly ICached<SeedingTimeCacheEntry> _seedingTimeCache;

        private class SeedingTimeCacheEntry
        {
            public DateTime LastFetched { get; set; }
            public long SeedingTime { get; set; }
        }

        public QBittorrent(IQBittorrentProxySelector proxySelector,
                           ITorrentFileInfoReader torrentFileInfoReader,
                           ISeedConfigProvider seedConfigProvider,
                           IConfigService configService,
                           IDiskProvider diskProvider,
                           ICacheManager cacheManager,
                           Logger logger)
            : base(torrentFileInfoReader, seedConfigProvider, configService, diskProvider, logger)
        {
            _proxySelector = proxySelector;

            _seedingTimeCache = cacheManager.GetCache<SeedingTimeCacheEntry>(GetType(), "seedingTime");
        }

        private IQBittorrentProxy Proxy => _proxySelector.GetProxy(Settings);
        private Version ProxyApiVersion => _proxySelector.GetApiVersion(Settings);

        protected override string AddFromMagnetLink(TorrentInfo release, string hash, string magnetLink)
        {
            if (!Proxy.GetConfig(Settings).DhtEnabled && !magnetLink.Contains("&tr="))
            {
                throw new NotSupportedException("Magnet Links without trackers not supported if DHT is disabled");
            }

            var setShareLimits = release.SeedConfiguration != null && (release.SeedConfiguration.Ratio.HasValue || release.SeedConfiguration.SeedTime.HasValue);
            var addHasSetShareLimits = setShareLimits && ProxyApiVersion >= new Version(2, 8, 1);
            var moveToTop = Settings.Priority == (int)QBittorrentPriority.First;
            var forceStart = (QBittorrentState)Settings.InitialState == QBittorrentState.ForceStart;
            var category = GetCategoryForRelease(release) ?? Settings.Category;

            Proxy.AddTorrentFromUrl(magnetLink, addHasSetShareLimits && setShareLimits ? release.SeedConfiguration : null, Settings, category);

            if ((!addHasSetShareLimits && setShareLimits) || moveToTop || forceStart)
            {
                if (!WaitForTorrent(hash))
                {
                    return hash;
                }

                if (!addHasSetShareLimits && setShareLimits)
                {
                    try
                    {
                        Proxy.SetTorrentSeedingConfiguration(hash.ToLower(), release.SeedConfiguration, Settings);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Failed to set the torrent seed criteria for {0}.", hash);
                    }
                }

                if (moveToTop)
                {
                    try
                    {
                        Proxy.MoveTorrentToTopInQueue(hash.ToLower(), Settings);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Failed to set the torrent priority for {0}.", hash);
                    }
                }

                if (forceStart)
                {
                    try
                    {
                        Proxy.SetForceStart(hash.ToLower(), true, Settings);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Failed to set ForceStart for {0}.", hash);
                    }
                }
            }

            return hash;
        }

        protected override string AddFromTorrentFile(TorrentInfo release, string hash, string filename, byte[] fileContent)
        {
            var setShareLimits = release.SeedConfiguration != null && (release.SeedConfiguration.Ratio.HasValue || release.SeedConfiguration.SeedTime.HasValue);
            var addHasSetShareLimits = setShareLimits && ProxyApiVersion >= new Version(2, 8, 1);
            var moveToTop = Settings.Priority == (int)QBittorrentPriority.First;
            var forceStart = (QBittorrentState)Settings.InitialState == QBittorrentState.ForceStart;
            var category = GetCategoryForRelease(release) ?? Settings.Category;

            Proxy.AddTorrentFromFile(filename, fileContent, addHasSetShareLimits ? release.SeedConfiguration : null, Settings, category);

            if ((!addHasSetShareLimits && setShareLimits) || moveToTop || forceStart)
            {
                if (!WaitForTorrent(hash))
                {
                    return hash;
                }

                if (!addHasSetShareLimits && setShareLimits)
                {
                    try
                    {
                        Proxy.SetTorrentSeedingConfiguration(hash.ToLower(), release.SeedConfiguration, Settings);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Failed to set the torrent seed criteria for {0}.", hash);
                    }
                }

                if (moveToTop)
                {
                    try
                    {
                        Proxy.MoveTorrentToTopInQueue(hash.ToLower(), Settings);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Failed to set the torrent priority for {0}.", hash);
                    }
                }

                if (forceStart)
                {
                    try
                    {
                        Proxy.SetForceStart(hash.ToLower(), true, Settings);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn(ex, "Failed to set ForceStart for {0}.", hash);
                    }
                }
            }

            return hash;
        }

        protected bool WaitForTorrent(string hash)
        {
            var count = 10;

            while (count != 0)
            {
                try
                {
                    if (Proxy.IsTorrentLoaded(hash.ToLower(), Settings))
                    {
                        return true;
                    }
                }
                catch
                {
                }

                _logger.Trace("Torrent '{0}' not yet visible in qbit, waiting 100ms.", hash);
                System.Threading.Thread.Sleep(100);
                count--;
            }

            _logger.Warn("Failed to load torrent '{0}' within 500 ms, skipping additional parameters.", hash);
            return false;
        }

        public override string Name => "qBittorrent";
        public override bool SupportsCategories => true;

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
            if (failures.HasErrors())
            {
                return;
            }

            failures.AddIfNotNull(TestCategory());
            failures.AddIfNotNull(TestPrioritySupport());
            failures.AddIfNotNull(TestGetTorrents());
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                var version = _proxySelector.GetProxy(Settings, true).GetApiVersion(Settings);
                if (version < Version.Parse("1.5"))
                {
                    // API version 5 introduced the "save_path" property in /query/torrents
                    return new NzbDroneValidationFailure("Host", "Unsupported client version")
                    {
                        DetailedDescription = "Please upgrade to qBittorrent version 3.2.4 or higher."
                    };
                }
                else if (version < Version.Parse("1.6"))
                {
                    // API version 6 introduced support for labels
                    if (Settings.Category.IsNotNullOrWhiteSpace() || Categories.Count > 0)
                    {
                        return new NzbDroneValidationFailure("Category", "Category is not supported")
                        {
                            DetailedDescription = "Labels are not supported until qBittorrent version 3.3.0. Please upgrade or try again with an empty Category."
                        };
                    }
                }
            }
            catch (DownloadClientAuthenticationException ex)
            {
                _logger.Error(ex, ex.Message);
                return new NzbDroneValidationFailure("Username", "Authentication failure")
                {
                    DetailedDescription = "Please verify your username and password."
                };
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Unable to connect to qBittorrent");
                if (ex.Status == WebExceptionStatus.ConnectFailure)
                {
                    return new NzbDroneValidationFailure("Host", "Unable to connect")
                    {
                        DetailedDescription = "Please verify the hostname and port."
                    };
                }

                return new NzbDroneValidationFailure(string.Empty, "Unknown exception: " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to test qBittorrent");

                return new NzbDroneValidationFailure("Host", "Unable to connect to qBittorrent")
                {
                    DetailedDescription = ex.Message
                };
            }

            return null;
        }

        private ValidationFailure TestCategory()
        {
            if (Settings.Category.IsNullOrWhiteSpace() && Categories.Count == 0)
            {
                return null;
            }

            // api v1 doesn't need to check/add categories as it's done on set
            var version = _proxySelector.GetProxy(Settings, true).GetApiVersion(Settings);
            if (version < Version.Parse("2.0"))
            {
                return null;
            }

            var labels = Proxy.GetLabels(Settings);

            foreach (var category in Categories)
            {
                if (category.ClientCategory.IsNotNullOrWhiteSpace() && !labels.ContainsKey(category.ClientCategory))
                {
                    Proxy.AddLabel(category.ClientCategory, Settings);
                    labels = Proxy.GetLabels(Settings);

                    if (!labels.ContainsKey(category.ClientCategory))
                    {
                        return new NzbDroneValidationFailure(string.Empty, "Configuration of label failed")
                        {
                            DetailedDescription = "Prowlarr was unable to add the label to qBittorrent."
                        };
                    }
                }
            }

            if (Settings.Category.IsNotNullOrWhiteSpace() && !labels.ContainsKey(Settings.Category))
            {
                Proxy.AddLabel(Settings.Category, Settings);
                labels = Proxy.GetLabels(Settings);

                if (!labels.ContainsKey(Settings.Category))
                {
                    return new NzbDroneValidationFailure("Category", "Configuration of label failed")
                    {
                        DetailedDescription = "Prowlarr was unable to add the label to qBittorrent."
                    };
                }
            }

            return null;
        }

        private ValidationFailure TestPrioritySupport()
        {
            var recentPriorityDefault = Settings.Priority == (int)QBittorrentPriority.Last;

            try
            {
                var config = Proxy.GetConfig(Settings);

                if (!config.QueueingEnabled)
                {
                    if (!recentPriorityDefault)
                    {
                        return new NzbDroneValidationFailure(nameof(Settings.Priority), "Queueing not enabled") { DetailedDescription = "Torrent Queueing is not enabled in your qBittorrent settings. Enable it in qBittorrent or select 'Last' as priority." };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to test qBittorrent");
                return new NzbDroneValidationFailure(string.Empty, "Unknown exception: " + ex.Message);
            }

            return null;
        }

        private ValidationFailure TestGetTorrents()
        {
            try
            {
                Proxy.GetTorrents(Settings);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get torrents");
                return new NzbDroneValidationFailure(string.Empty, "Failed to get the list of torrents: " + ex.Message);
            }

            return null;
        }

        protected TimeSpan? GetRemainingTime(QBittorrentTorrent torrent)
        {
            if (torrent.Eta < 0 || torrent.Eta > 365 * 24 * 3600)
            {
                return null;
            }

            // qBittorrent sends eta=8640000 if unknown such as queued
            if (torrent.Eta == 8640000)
            {
                return null;
            }

            return TimeSpan.FromSeconds((int)torrent.Eta);
        }

        protected bool HasReachedSeedLimit(QBittorrentTorrent torrent, QBittorrentPreferences config)
        {
            if (torrent.RatioLimit >= 0)
            {
                if (torrent.Ratio >= torrent.RatioLimit)
                {
                    return true;
                }
            }
            else if (torrent.RatioLimit == -2 && config.MaxRatioEnabled)
            {
                if (torrent.Ratio >= config.MaxRatio)
                {
                    return true;
                }
            }

            if (HasReachedSeedingTimeLimit(torrent, config))
            {
                return true;
            }

            return false;
        }

        protected bool HasReachedSeedingTimeLimit(QBittorrentTorrent torrent, QBittorrentPreferences config)
        {
            long seedingTimeLimit;

            if (torrent.SeedingTimeLimit >= 0)
            {
                seedingTimeLimit = torrent.SeedingTimeLimit;
            }
            else if (torrent.SeedingTimeLimit == -2 && config.MaxSeedingTimeEnabled)
            {
                seedingTimeLimit = config.MaxSeedingTime;
            }
            else
            {
                return false;
            }

            if (torrent.SeedingTime.HasValue)
            {
                // SeedingTime can't be available here, but use it if the api starts to provide it.
                return torrent.SeedingTime.Value >= seedingTimeLimit;
            }

            var cacheKey = Settings.Host + Settings.Port + torrent.Hash;
            var cacheSeedingTime = _seedingTimeCache.Find(cacheKey);

            if (cacheSeedingTime != null)
            {
                var togo = seedingTimeLimit - cacheSeedingTime.SeedingTime;
                var elapsed = (DateTime.UtcNow - cacheSeedingTime.LastFetched).TotalSeconds;

                if (togo <= 0)
                {
                    // Already reached the limit, keep the cache alive
                    _seedingTimeCache.Set(cacheKey, cacheSeedingTime, TimeSpan.FromMinutes(5));
                    return true;
                }
                else if (togo > elapsed)
                {
                    // SeedingTime cannot have reached the required value since the last check, preserve the cache
                    _seedingTimeCache.Set(cacheKey, cacheSeedingTime, TimeSpan.FromMinutes(5));
                    return false;
                }
            }

            FetchTorrentDetails(torrent);

            cacheSeedingTime = new SeedingTimeCacheEntry
            {
                LastFetched = DateTime.UtcNow,
                SeedingTime = torrent.SeedingTime.Value
            };

            _seedingTimeCache.Set(cacheKey, cacheSeedingTime, TimeSpan.FromMinutes(5));

            if (cacheSeedingTime.SeedingTime >= seedingTimeLimit)
            {
                // Reached the limit, keep the cache alive
                return true;
            }

            return false;
        }

        protected void FetchTorrentDetails(QBittorrentTorrent torrent)
        {
            var torrentProperties = Proxy.GetTorrentProperties(torrent.Hash, Settings);

            torrent.SeedingTime = torrentProperties.SeedingTime;
        }

        protected override string AddFromTorrentLink(TorrentInfo release, string hash, string torrentLink)
        {
            throw new NotImplementedException();
        }
    }
}
