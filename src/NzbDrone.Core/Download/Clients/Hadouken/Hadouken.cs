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

namespace NzbDrone.Core.Download.Clients.Hadouken
{
    public class Hadouken : TorrentClientBase<HadoukenSettings>
    {
        private readonly IHadoukenProxy _proxy;

        public Hadouken(IHadoukenProxy proxy,
                        ITorrentFileInfoReader torrentFileInfoReader,
                        ISeedConfigProvider seedConfigProvider,
                        IConfigService configService,
                        IDiskProvider diskProvider,
                        Logger logger)
            : base(torrentFileInfoReader, seedConfigProvider, configService, diskProvider, logger)
        {
            _proxy = proxy;
        }

        public override string Name => "Hadouken";
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

        protected override string AddFromMagnetLink(TorrentInfo release, string hash, string magnetLink)
        {
            _proxy.AddTorrentUri(Settings, magnetLink, GetCategoryForRelease(release) ?? Settings.Category);

            return hash.ToUpper();
        }

        protected override string AddFromTorrentFile(TorrentInfo release, string hash, string filename, byte[] fileContent)
        {
            return _proxy.AddTorrentFile(Settings, fileContent, GetCategoryForRelease(release) ?? Settings.Category).ToUpper();
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                var sysInfo = _proxy.GetSystemInfo(Settings);
                var version = new Version(sysInfo.Versions["hadouken"]);

                if (version < new Version("5.1"))
                {
                    return new ValidationFailure(string.Empty,
                        "Old Hadouken client with unsupported API, need 5.1 or higher");
                }
            }
            catch (DownloadClientAuthenticationException ex)
            {
                _logger.Error(ex, ex.Message);

                return new NzbDroneValidationFailure("Password", "Authentication failed");
            }
            catch (Exception ex)
            {
                return new NzbDroneValidationFailure("Host", "Unable to connect to Hadouken")
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
                _logger.Error(ex, ex.Message);
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
