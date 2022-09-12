using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.FileList
{
    public class FileList : TorrentIndexerBase<FileListSettings>
    {
        public override string Name => "FileList.io";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;
        public override bool SupportsRedirect => true;

        public FileList(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new FileListRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new FileListParser(Settings, Capabilities.Categories);
        }
    }
}
