using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.Transmission
{
    public abstract class TransmissionBase : TorrentClientBase<TransmissionSettings>
    {
        protected readonly ITransmissionProxy _proxy;

        public TransmissionBase(ITransmissionProxy proxy,
            ITorrentFileInfoReader torrentFileInfoReader,
            IHttpClient httpClient,
            IConfigService configService,
            IDiskProvider diskProvider,
            Logger logger)
            : base(torrentFileInfoReader, httpClient, configService, diskProvider, logger)
        {
            _proxy = proxy;
        }

        protected bool HasReachedSeedLimit(TransmissionTorrent torrent, double? ratio, Lazy<TransmissionConfig> config)
        {
            var isStopped = torrent.Status == TransmissionTorrentStatus.Stopped;
            var isSeeding = torrent.Status == TransmissionTorrentStatus.Seeding;

            if (torrent.SeedRatioMode == 1)
            {
                if (isStopped && ratio.HasValue && ratio >= torrent.SeedRatioLimit)
                {
                    return true;
                }
            }
            else if (torrent.SeedRatioMode == 0)
            {
                if (isStopped && config.Value.SeedRatioLimited && ratio >= config.Value.SeedRatioLimit)
                {
                    return true;
                }
            }

            // Transmission doesn't support SeedTimeLimit, use/abuse seed idle limit, but only if it was set per-torrent.
            if (torrent.SeedIdleMode == 1)
            {
                if ((isStopped || isSeeding) && torrent.SecondsSeeding > torrent.SeedIdleLimit * 60)
                {
                    return true;
                }
            }
            else if (torrent.SeedIdleMode == 0)
            {
                // The global idle limit is a real idle limit, if it's configured then 'Stopped' is enough.
                if (isStopped && config.Value.IdleSeedingLimitEnabled)
                {
                    return true;
                }
            }

            return false;
        }

        protected override string AddFromMagnetLink(ReleaseInfo release, string hash, string magnetLink)
        {
            _proxy.AddTorrentFromUrl(magnetLink, GetDownloadDirectory(), Settings);

            //_proxy.SetTorrentSeedingConfiguration(hash, release.SeedConfiguration, Settings);
            if (Settings.Priority == (int)TransmissionPriority.First)
            {
                _proxy.MoveTorrentToTopInQueue(hash, Settings);
            }

            return hash;
        }

        protected override string AddFromTorrentFile(ReleaseInfo release, string hash, string filename, byte[] fileContent)
        {
            _proxy.AddTorrentFromData(fileContent, GetDownloadDirectory(), Settings);

            //_proxy.SetTorrentSeedingConfiguration(hash, release.SeedConfiguration, Settings);
            if (Settings.Priority == (int)TransmissionPriority.First)
            {
                _proxy.MoveTorrentToTopInQueue(hash, Settings);
            }

            return hash;
        }

        protected override string AddFromTorrentLink(ReleaseInfo release, string hash, string torrentLink)
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

        protected string GetDownloadDirectory()
        {
            if (Settings.Directory.IsNotNullOrWhiteSpace())
            {
                return Settings.Directory;
            }

            if (!Settings.Category.IsNotNullOrWhiteSpace())
            {
                return null;
            }

            var config = _proxy.GetConfig(Settings);
            var destDir = config.DownloadDir;

            return $"{destDir.TrimEnd('/')}/{Settings.Category}";
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
