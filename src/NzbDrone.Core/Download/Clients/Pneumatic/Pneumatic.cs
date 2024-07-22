using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.Clients.Pneumatic
{
    public class Pneumatic : DownloadClientBase<PneumaticSettings>
    {
        public Pneumatic(IConfigService configService,
                         IDiskProvider diskProvider,
                         Logger logger)
            : base(configService, diskProvider, logger)
        {
        }

        public override string Name => "Pneumatic";
        public override bool SupportsCategories => false;

        public override DownloadProtocol Protocol => DownloadProtocol.Usenet;

        public override async Task<string> Download(ReleaseInfo release, bool redirect, IIndexer indexer)
        {
            var url = new Uri(release.DownloadUrl);
            var title = release.Title;

            title = StringUtil.CleanFileName(title);

            //Save to the Pneumatic directory (The user will need to ensure its accessible by XBMC)
            var nzbFile = Path.Combine(Settings.NzbFolder, title + ".nzb");

            _logger.Debug("Downloading NZB from: {0} to: {1}", url, nzbFile);

            var downloadResponse = await indexer.Download(url);

            await File.WriteAllBytesAsync(nzbFile, downloadResponse.Data);

            _logger.Debug("NZB Download succeeded, saved to: {0}", nzbFile);

            var strmFile = WriteStrmFile(title, nzbFile);

            return GetDownloadClientId(strmFile);
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(Settings.NzbFolder);

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestFolder(Settings.NzbFolder, "NzbFolder"));
            failures.AddIfNotNull(TestFolder(Settings.StrmFolder, "StrmFolder"));
        }

        private string WriteStrmFile(string title, string nzbFile)
        {
            if (Settings.StrmFolder.IsNullOrWhiteSpace())
            {
                throw new DownloadClientException("Strm Folder needs to be set for Pneumatic Downloader");
            }

            var contents = string.Format("plugin://plugin.program.pneumatic/?mode=strm&type=add_file&nzb={0}&nzbname={1}", nzbFile, title);
            var filename = Path.Combine(Settings.StrmFolder, title + ".strm");

            _diskProvider.WriteAllText(filename, contents);

            return filename;
        }

        private string GetDownloadClientId(string filename)
        {
            return Definition.Name + "_" + Path.GetFileName(filename) + "_" + _diskProvider.FileGetLastWrite(filename).Ticks;
        }
    }
}
