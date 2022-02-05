using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.HDBits
{
    public class HDBits : TorrentIndexerBase<HDBitsSettings>
    {
        public override string Name => "HDBits";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override bool SupportsRedirect => true;

        public override int PageSize => 30;

        public HDBits(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new HDBitsRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new HDBitsParser(Settings, Capabilities.Categories);
        }
    }
}
