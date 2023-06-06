using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download.Clients.rTorrent;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.RTorrent
{
    public class RTorrent : TorrentClientBase<RTorrentSettings>
    {
        private readonly IRTorrentProxy _proxy;
        private readonly IRTorrentDirectoryValidator _rTorrentDirectoryValidator;

        public RTorrent(IRTorrentProxy proxy,
                        ITorrentFileInfoReader torrentFileInfoReader,
                        ISeedConfigProvider seedConfigProvider,
                        IConfigService configService,
                        IDiskProvider diskProvider,
                        IRTorrentDirectoryValidator rTorrentDirectoryValidator,
                        Logger logger)
            : base(torrentFileInfoReader, seedConfigProvider, configService, diskProvider, logger)
        {
            _proxy = proxy;
            _rTorrentDirectoryValidator = rTorrentDirectoryValidator;
        }

        protected override string AddFromMagnetLink(TorrentInfo release, string hash, string magnetLink)
        {
            var priority = (RTorrentPriority)Settings.Priority;

            _proxy.AddTorrentFromUrl(magnetLink, GetCategoryForRelease(release) ?? Settings.Category, priority, Settings.Directory, Settings);

            var tries = 10;
            var retryDelay = 500;

            // Wait a bit for the magnet to be resolved.
            if (!WaitForTorrent(hash, tries, retryDelay))
            {
                _logger.Warn("rTorrent could not resolve magnet within {0} seconds, download may remain stuck: {1}.", tries * retryDelay / 1000, magnetLink);

                return hash;
            }

            return hash;
        }

        protected override string AddFromTorrentFile(TorrentInfo release, string hash, string filename, byte[] fileContent)
        {
            var priority = (RTorrentPriority)Settings.Priority;

            _proxy.AddTorrentFromFile(filename, fileContent, GetCategoryForRelease(release) ?? Settings.Category, priority, Settings.Directory, Settings);

            var tries = 10;
            var retryDelay = 500;
            if (!WaitForTorrent(hash, tries, retryDelay))
            {
                _logger.Debug("rTorrent didn't add the torrent within {0} seconds: {1}.", tries * retryDelay / 1000, filename);

                throw new ReleaseDownloadException("Downloading torrent failed");
            }

            return hash;
        }

        public override string Name => "rTorrent";
        public override bool SupportsCategories => true;

        public override ProviderMessage Message => new ProviderMessage("Prowlarr is unable to remove torrents that have finished seeding when using rTorrent", ProviderMessageType.Warning);

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
            if (failures.HasErrors())
            {
                return;
            }

            failures.AddIfNotNull(TestGetTorrents());
            failures.AddIfNotNull(TestDirectory());
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                var version = _proxy.GetVersion(Settings);

                if (new Version(version) < new Version("0.9.0"))
                {
                    return new ValidationFailure(string.Empty, "rTorrent version should be at least 0.9.0. Version reported is {0}", version);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to test rTorrent");

                return new NzbDroneValidationFailure("Host", "Unable to connect to rTorrent")
                       {
                           DetailedDescription = ex.Message
                       };
            }

            return null;
        }

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

        private ValidationFailure TestDirectory()
        {
            var result = _rTorrentDirectoryValidator.Validate(Settings);

            if (result.IsValid)
            {
                return null;
            }

            return result.Errors.First();
        }

        private bool WaitForTorrent(string hash, int tries, int retryDelay)
        {
            for (var i = 0; i < tries; i++)
            {
                if (_proxy.HasHashTorrent(hash, Settings))
                {
                    return true;
                }

                Thread.Sleep(retryDelay);
            }

            _logger.Debug("Could not find hash {0} in {1} tries at {2} ms intervals.", hash, tries, retryDelay);

            return false;
        }

        protected override string AddFromTorrentLink(TorrentInfo release, string hash, string torrentLink)
        {
            throw new NotImplementedException();
        }
    }
}
