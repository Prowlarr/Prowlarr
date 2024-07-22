using System;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Download
{
    public abstract class UsenetClientBase<TSettings> : DownloadClientBase<TSettings>
        where TSettings : IProviderConfig, new()
    {
        protected readonly IHttpClient _httpClient;

        protected UsenetClientBase(IHttpClient httpClient,
                                   IConfigService configService,
                                   IDiskProvider diskProvider,
                                   Logger logger)
            : base(configService, diskProvider, logger)
        {
            _httpClient = httpClient;
        }

        public override DownloadProtocol Protocol => DownloadProtocol.Usenet;

        protected abstract string AddFromNzbFile(ReleaseInfo release, string filename, byte[] fileContents);
        protected abstract string AddFromLink(ReleaseInfo release);

        public override async Task<string> Download(ReleaseInfo release, bool redirect, IIndexer indexer)
        {
            var url = new Uri(release.DownloadUrl);

            if (redirect)
            {
                return AddFromLink(release);
            }

            var filename = StringUtil.CleanFileName(release.Title) + ".nzb";

            var downloadResponse = await indexer.Download(url);

            _logger.Info("Adding report [{0}] to the queue.", release.Title);
            return AddFromNzbFile(release, filename, downloadResponse.Data);
        }
    }
}
