using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.Clients.Blackhole
{
    public class TorrentBlackhole : TorrentClientBase<TorrentBlackholeSettings>
    {
        public override bool PreferTorrentFile => true;

        public TorrentBlackhole(ITorrentFileInfoReader torrentFileInfoReader,
                                ISeedConfigProvider seedConfigProvider,
                                IConfigService configService,
                                IDiskProvider diskProvider,
                                Logger logger)
            : base(torrentFileInfoReader, seedConfigProvider, configService, diskProvider, logger)
        {
        }

        protected override string AddFromTorrentLink(TorrentInfo release, string hash, string torrentLink)
        {
            throw new NotImplementedException("Blackhole does not support redirected indexers.");
        }

        protected override string AddFromMagnetLink(TorrentInfo release, string hash, string magnetLink)
        {
            if (!Settings.SaveMagnetFiles)
            {
                throw new NotSupportedException("Blackhole does not support magnet links.");
            }

            var title = release.Title;

            title = title.CleanFileName();

            var filepath = Path.Combine(Settings.TorrentFolder, $"{title}.{Settings.MagnetFileExtension.Trim('.')}");

            var fileContent = Encoding.UTF8.GetBytes(magnetLink);
            using (var stream = _diskProvider.OpenWriteStream(filepath))
            {
                stream.Write(fileContent, 0, fileContent.Length);
            }

            _logger.Debug("Saving magnet link succeeded, saved to: {0}", filepath);

            return null;
        }

        protected override string AddFromTorrentFile(TorrentInfo release, string hash, string filename, byte[] fileContent)
        {
            var title = release.Title;

            title = title.CleanFileName();

            var filepath = Path.Combine(Settings.TorrentFolder, string.Format("{0}.torrent", title));

            using (var stream = _diskProvider.OpenWriteStream(filepath))
            {
                stream.Write(fileContent, 0, fileContent.Length);
            }

            _logger.Debug("Torrent Download succeeded, saved to: {0}", filepath);

            return null;
        }

        public override string Name => "Torrent Blackhole";

        public override bool SupportsCategories => false;

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestFolder(Settings.TorrentFolder, "TorrentFolder"));
        }
    }
}
