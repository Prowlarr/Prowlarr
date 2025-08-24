using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.RQBit
{
    public class RQBit : TorrentClientBase<RQbitSettings>
    {
        private readonly IRQbitProxy _proxy;

        public override string Name => "RQBit";

        public RQBit(IRQbitProxy proxy,
            ITorrentFileInfoReader torrentFileInfoReader,
            ISeedConfigProvider seedConfigProvider,
            IConfigService configService,
            IDiskProvider diskProvider,
            ILocalizationService localizationService,
            Logger logger)
            : base(torrentFileInfoReader, seedConfigProvider, configService, diskProvider, localizationService, logger)
        {
            _proxy = proxy;
        }

        public override IEnumerable<DownloadClientItem> GetItems()
        {
            var torrents = _proxy.GetTorrents(Settings);

            _logger.Debug("Retrieved metadata of {0} torrents in client", torrents.Count);

            var items = new List<DownloadClientItem>();
            foreach (var torrent in torrents)
            {
                // Ignore torrents with an empty path
                if (torrent.Path.IsNullOrWhiteSpace())
                {
                    _logger.Warn("Torrent '{0}' has an empty download path and will not be processed", torrent.Name);
                    continue;
                }

                if (torrent.Path.StartsWith("."))
                {
                    _logger.Warn("Torrent '{0}' has a download path starting with '.' and will not be processed", torrent.Name);
                    continue;
                }

                var item = new DownloadClientItem();
                item.DownloadClientInfo = DownloadClientItemClientInfo.FromDownloadClient(this, false);
                item.Title = torrent.Name;
                item.DownloadId = torrent.Hash;
                item.Message = torrent.Message;

                item.OutputPath = new OsPath(torrent.Path);
                item.TotalSize = torrent.TotalSize;
                item.RemainingSize = torrent.RemainingSize;
                item.SeedRatio = torrent.Ratio;

                if (torrent.DownRate > 0)
                {
                    var secondsLeft = torrent.RemainingSize / torrent.DownRate;
                    item.RemainingTime = TimeSpan.FromSeconds(secondsLeft);
                }
                else
                {
                    item.RemainingTime = TimeSpan.Zero;
                }

                if (torrent.IsFinished)
                {
                    item.Status = DownloadItemStatus.Completed;
                }
                else if (torrent.IsActive)
                {
                    item.Status = DownloadItemStatus.Downloading;
                }
                else if (!torrent.IsActive)
                {
                    item.Status = DownloadItemStatus.Paused;
                }

                // Get cached seedConfig for this torrent
                var seedConfig = _seedConfigProvider.GetSeedConfiguration(torrent.Hash);

                if (item.DownloadClientInfo.RemoveCompletedDownloads && torrent.IsFinished && seedConfig != null)
                {
                    var canRemove = false;

                    if (torrent.Ratio / 1000.0 >= seedConfig.SeedRatio)
                    {
                        _logger.Trace($"{item} has met seed ratio goal of {seedConfig.SeedRatio}");
                        canRemove = true;
                    }
                    else if (DateTimeOffset.Now - DateTimeOffset.FromUnixTimeSeconds(torrent.FinishedTime) >= seedConfig.SeedTime)
                    {
                        _logger.Trace($"{item} has met seed time goal of {seedConfig.SeedTime} minutes");
                        canRemove = true;
                    }
                    else
                    {
                        _logger.Trace($"{item} seeding goals have not yet been reached");
                    }

                    // Check if torrent is finished and if it exceeds cached seedConfig
                    item.CanMoveFiles = item.CanBeRemoved = canRemove;
                }

                items.Add(item);
            }

            return items;
        }

        public override void RemoveItem(DownloadClientItem item, bool deleteData)
        {
            _proxy.RemoveTorrent(item.DownloadId, deleteData, Settings);
        }

        public override DownloadClientInfo GetStatus()
        {
            return new DownloadClientInfo
            {
                IsLocalhost = Settings.Host == "127.0.0.1" || Settings.Host == "::1" || Settings.Host == "localhost",
            };
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
            
            if (failures.HasErrors())
            {
                return;
            }

            failures.AddIfNotNull(TestVersion());
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                var version = _proxy.GetVersion(Settings);
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to connect to RQBit");
                return new ValidationFailure("", "Unable to connect to RQBit. Please check that RQBit is running and accessible.");
            }
        }

        private ValidationFailure TestVersion()
        {
            try
            {
                var versionString = _proxy.GetVersion(Settings);
                
                if (string.IsNullOrWhiteSpace(versionString))
                {
                    return new ValidationFailure("", "Unable to determine RQBit version. Please check that RQBit is running and accessible.");
                }

                // Parse version string to Version object
                var versionMatch = System.Text.RegularExpressions.Regex.Match(versionString, @"(\d+)\.(\d+)\.(\d+)");

                if (!versionMatch.Success)
                {
                    return new ValidationFailure("", $"Unable to parse RQBit version '{versionString}'. Please check that RQBit is running and accessible.");
                }

                var major = int.Parse(versionMatch.Groups[1].Value);
                var minor = int.Parse(versionMatch.Groups[2].Value);
                var patch = int.Parse(versionMatch.Groups[3].Value);
                var apiVersion = new Version(major, minor, patch);

                var minimumVersion = new Version(8, 0, 0);

                if (apiVersion < minimumVersion)
                {
                    return new ValidationFailure("", $"RQBit version {apiVersion} is not supported. Please upgrade to version {minimumVersion} or higher.");
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to determine RQBit version");
                return new ValidationFailure("", "Unable to determine RQBit version. Please check that RQBit is running and accessible.");
            }
        }

        protected override string AddFromMagnetLink(TorrentInfo release, string hash, string magnetLink)
        {
            return _proxy.AddTorrentFromUrl(magnetLink, Settings);
        }

        protected override string AddFromTorrentFile(TorrentInfo release, string hash, string filename, byte[] fileContent)
        {
            return _proxy.AddTorrentFromFile(filename, fileContent, Settings);
        }

        protected override string AddFromTorrentLink(TorrentInfo release, string hash, string torrentLink)
        {
            return _proxy.AddTorrentFromUrl(torrentLink, Settings);
        }
    }
}
