using System;
using System.Text.RegularExpressions;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Download.Clients.Transmission
{
    public class Transmission : TransmissionBase
    {
        public Transmission(ITransmissionProxy proxy,
                            ITorrentFileInfoReader torrentFileInfoReader,
                            ISeedConfigProvider seedConfigProvider,
                            IConfigService configService,
                            IDiskProvider diskProvider,
                            Logger logger)
            : base(proxy, torrentFileInfoReader, seedConfigProvider, configService, diskProvider, logger)
        {
        }

        protected override ValidationFailure ValidateVersion()
        {
            var versionString = _proxy.GetClientVersion(Settings);

            _logger.Debug("Transmission version information: {0}", versionString);

            var versionResult = Regex.Match(versionString, @"(?<!\(|(\d|\.)+)(\d|\.)+(?!\)|(\d|\.)+)").Value;
            var version = Version.Parse(versionResult);

            if (version < new Version(2, 40))
            {
                return new ValidationFailure(string.Empty, "Transmission version not supported, should be 2.40 or higher.");
            }

            return null;
        }

        public override string Name => "Transmission";
        public override bool SupportsCategories => true;
    }
}
