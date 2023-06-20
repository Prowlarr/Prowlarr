using System;
using System.Collections.Generic;
using System.Net;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.UTorrent
{
    public class UTorrent : TorrentClientBase<UTorrentSettings>
    {
        private readonly IUTorrentProxy _proxy;

        public UTorrent(IUTorrentProxy proxy,
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
            _proxy.AddTorrentFromUrl(magnetLink, Settings);
            _proxy.SetTorrentSeedingConfiguration(hash, release.SeedConfiguration, Settings);

            var category = GetCategoryForRelease(release) ?? Settings.Category;

            if (category.IsNotNullOrWhiteSpace())
            {
                _proxy.SetTorrentLabel(hash, category, Settings);
            }

            if (Settings.Priority == (int)UTorrentPriority.First)
            {
                _proxy.MoveTorrentToTopInQueue(hash, Settings);
            }

            _proxy.SetState(hash, (UTorrentState)Settings.IntialState, Settings);

            return hash;
        }

        protected override string AddFromTorrentFile(TorrentInfo release, string hash, string filename, byte[] fileContent)
        {
            _proxy.AddTorrentFromFile(filename, fileContent, Settings);
            _proxy.SetTorrentSeedingConfiguration(hash, release.SeedConfiguration, Settings);

            var category = GetCategoryForRelease(release) ?? Settings.Category;

            if (category.IsNotNullOrWhiteSpace())
            {
                _proxy.SetTorrentLabel(hash, category, Settings);
            }

            if (Settings.Priority == (int)UTorrentPriority.First)
            {
                _proxy.MoveTorrentToTopInQueue(hash, Settings);
            }

            _proxy.SetState(hash, (UTorrentState)Settings.IntialState, Settings);

            return hash;
        }

        public override string Name => "uTorrent";
        public override bool SupportsCategories => true;

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
            if (failures.HasErrors())
            {
                return;
            }

            failures.AddIfNotNull(TestGetTorrents());
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                var version = _proxy.GetVersion(Settings);

                if (version < 25406)
                {
                    return new ValidationFailure(string.Empty, "Old uTorrent client with unsupported API, need 3.0 or higher");
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
                _logger.Error(ex, "Unable to connect to uTorrent");
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
                _logger.Error(ex, "Failed to test uTorrent");

                return new NzbDroneValidationFailure("Host", "Unable to connect to uTorrent")
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
                _proxy.GetTorrents(null, Settings);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get torrents");
                return new NzbDroneValidationFailure(string.Empty, "Failed to get the list of torrents: " + ex.Message);
            }

            return null;
        }

        protected override string AddFromTorrentLink(TorrentInfo release, string hash, string torrentLink)
        {
            throw new NotImplementedException();
        }
    }
}
