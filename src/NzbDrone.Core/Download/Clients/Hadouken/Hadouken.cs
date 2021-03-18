using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download.Clients.Hadouken.Models;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.Hadouken
{
    public class Hadouken : TorrentClientBase<HadoukenSettings>
    {
        private readonly IHadoukenProxy _proxy;

        public Hadouken(IHadoukenProxy proxy,
                        ITorrentFileInfoReader torrentFileInfoReader,
                        IHttpClient httpClient,
                        IConfigService configService,
                        IDiskProvider diskProvider,
                        Logger logger)
            : base(torrentFileInfoReader, httpClient, configService, diskProvider, logger)
        {
            _proxy = proxy;
        }

        public override string Name => "Hadouken";

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
            if (failures.HasErrors())
            {
                return;
            }

            failures.AddIfNotNull(TestGetTorrents());
        }

        protected override string AddFromMagnetLink(ReleaseInfo release, string hash, string magnetLink)
        {
            _proxy.AddTorrentUri(Settings, magnetLink);

            return hash.ToUpper();
        }

        protected override string AddFromTorrentFile(ReleaseInfo release, string hash, string filename, byte[] fileContent)
        {
            return _proxy.AddTorrentFile(Settings, fileContent).ToUpper();
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

        protected override string AddFromTorrentLink(ReleaseInfo release, string hash, string torrentLink)
        {
            throw new NotImplementedException();
        }
    }
}
