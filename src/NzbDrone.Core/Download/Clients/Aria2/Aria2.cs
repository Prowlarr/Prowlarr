using System;
using System.Collections.Generic;
using System.Threading;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.Aria2
{
    public class Aria2 : TorrentClientBase<Aria2Settings>
    {
        private readonly IAria2Proxy _proxy;

        public override string Name => "Aria2";

        public override bool SupportsCategories => false;

        public Aria2(IAria2Proxy proxy,
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
            var gid = _proxy.AddUri(Settings, magnetLink);

            var tries = 10;
            var retryDelay = 500;

            // Wait a bit for the magnet to be resolved.
            if (!WaitForTorrent(gid, hash, tries, retryDelay))
            {
                _logger.Warn($"Aria2 could not add magnent within {tries * retryDelay / 1000} seconds, download may remain stuck: {magnetLink}.");
                return hash;
            }

            _logger.Debug($"Äria2 AddFromMagnetLink '{hash}' -> '{gid}'");

            return hash;
        }

        protected override string AddFromTorrentFile(TorrentInfo release, string hash, string filename, byte[] fileContent)
        {
            var gid = _proxy.AddTorrent(Settings, fileContent);

            var tries = 10;
            var retryDelay = 500;

            // Wait a bit for the magnet to be resolved.
            if (!WaitForTorrent(gid, hash, tries, retryDelay))
            {
                _logger.Warn($"Aria2 could not add torrent within {tries * retryDelay / 1000} seconds, download may remain stuck: {filename}.");
                return hash;
            }

            return hash;
        }

        private bool WaitForTorrent(string gid, string hash, int tries, int retryDelay)
        {
            for (var i = 0; i < tries; i++)
            {
                var found = _proxy.GetFromGID(Settings, gid);

                if (found?.InfoHash?.ToLower() == hash?.ToLower())
                {
                    return true;
                }

                Thread.Sleep(retryDelay);
            }

            _logger.Debug("Could not find hash {0} in {1} tries at {2} ms intervals.", hash, tries, retryDelay);

            return false;
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());

            if (failures.HasErrors())
            {
                return;
            }
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                var version = _proxy.GetVersion(Settings);

                if (new Version(version) < new Version("1.34.0"))
                {
                    return new ValidationFailure(string.Empty, "Aria2 version should be at least 1.34.0. Version reported is {0}", version);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to test Aria2");

                return new NzbDroneValidationFailure("Host", "Unable to connect to Aria2")
                {
                    DetailedDescription = ex.Message
                };
            }

            return null;
        }

        protected override string AddFromTorrentLink(TorrentInfo release, string hash, string torrentLink)
        {
            var gid = _proxy.AddUri(Settings, torrentLink);

            var tries = 10;
            var retryDelay = 500;

            // Wait a bit for the magnet to be resolved.
            if (!WaitForTorrent(gid, hash, tries, retryDelay))
            {
                _logger.Warn($"Aria2 could not add torrent within {tries * retryDelay / 1000} seconds, download may remain stuck: {torrentLink}.");
                return hash;
            }

            _logger.Debug($"Aria2 AddFromTorrentLink '{hash}' -> '{gid}'");

            return hash;
        }
    }
}
