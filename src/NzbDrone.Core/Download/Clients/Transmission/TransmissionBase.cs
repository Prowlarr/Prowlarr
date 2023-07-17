using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.Transmission
{
    public abstract class TransmissionBase : TorrentClientBase<TransmissionSettings>
    {
        protected readonly ITransmissionProxy _proxy;

        public TransmissionBase(ITransmissionProxy proxy,
            ITorrentFileInfoReader torrentFileInfoReader,
            ISeedConfigProvider seedConfigProvider,
            IConfigService configService,
            IDiskProvider diskProvider,
            Logger logger)
            : base(torrentFileInfoReader, seedConfigProvider, configService, diskProvider, logger)
        {
            _proxy = proxy;
        }

        protected override string AddFromMagnetLink(TorrentInfo release, string hash, string magnetLink)
        {
            var category = GetCategoryForRelease(release) ?? Settings.Category;
            var downloadDirectory = GetDownloadDirectory(category);

            _proxy.AddTorrentFromUrl(magnetLink, downloadDirectory, Settings);
            _proxy.SetTorrentSeedingConfiguration(hash, release.SeedConfiguration, Settings);

            if (Settings.Priority == (int)TransmissionPriority.First)
            {
                _proxy.MoveTorrentToTopInQueue(hash, Settings);
            }

            return hash;
        }

        protected override string AddFromTorrentFile(TorrentInfo release, string hash, string filename, byte[] fileContent)
        {
            var category = GetCategoryForRelease(release) ?? Settings.Category;
            var downloadDirectory = GetDownloadDirectory(category);

            _proxy.AddTorrentFromData(fileContent, downloadDirectory, Settings);
            _proxy.SetTorrentSeedingConfiguration(hash, release.SeedConfiguration, Settings);

            if (Settings.Priority == (int)TransmissionPriority.First)
            {
                _proxy.MoveTorrentToTopInQueue(hash, Settings);
            }

            return hash;
        }

        protected override string AddFromTorrentLink(TorrentInfo release, string hash, string torrentLink)
        {
            throw new NotImplementedException();
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
            if (failures.HasErrors())
            {
                return;
            }

            failures.AddIfNotNull(TestGetTorrents());
        }

        protected virtual OsPath GetOutputPath(OsPath outputPath, TransmissionTorrent torrent)
        {
            return outputPath + torrent.Name.Replace(":", "_");
        }

        protected string GetDownloadDirectory(string category)
        {
            if (Settings.Directory.IsNotNullOrWhiteSpace())
            {
                return Settings.Directory;
            }

            if (category.IsNullOrWhiteSpace())
            {
                return null;
            }

            var config = _proxy.GetConfig(Settings);
            var destDir = config.DownloadDir;

            return $"{destDir.TrimEnd('/')}/{category}";
        }

        protected ValidationFailure TestConnection()
        {
            try
            {
                return ValidateVersion();
            }
            catch (DownloadClientAuthenticationException ex)
            {
                _logger.Error(ex, ex.Message);
                return new NzbDroneValidationFailure("Username", "Authentication failure")
                {
                    DetailedDescription = string.Format("Please verify your username and password. Also verify if the host running Prowlarr isn't blocked from accessing {0} by WhiteList limitations in the {0} configuration.", Name)
                };
            }
            catch (DownloadClientUnavailableException ex)
            {
                _logger.Error(ex, ex.Message);

                return new NzbDroneValidationFailure("Host", "Unable to connect to Transmission")
                       {
                           DetailedDescription = ex.Message
                       };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to test");

                return new NzbDroneValidationFailure(string.Empty, "Unknown exception: " + ex.Message);
            }
        }

        protected abstract ValidationFailure ValidateVersion();

        private ValidationFailure TestGetTorrents()
        {
            try
            {
                _proxy.GetTorrents(Settings);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get torrents");
                return new NzbDroneValidationFailure(string.Empty, "Failed to get the list of torrents: " + ex.Message);
            }

            return null;
        }
    }
}
