using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.BroadcastheNet
{
    public class BroadcastheNet : TorrentIndexerBase<BroadcastheNetSettings>
    {
        public override string Name => "BroadcasTheNet";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;
        public override int PageSize => 100;

        public override TimeSpan RateLimit => TimeSpan.FromSeconds(5);

        public BroadcastheNet(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            var requestGenerator = new BroadcastheNetRequestGenerator() { Settings = Settings, PageSize = PageSize, Capabilities = Capabilities };

            var releaseInfo = _indexerStatusService.GetLastRssSyncReleaseInfo(Definition.Id);
            if (releaseInfo != null)
            {
                int torrentID;
                if (int.TryParse(releaseInfo.Guid.Replace("BTN-", string.Empty), out torrentID))
                {
                    requestGenerator.LastRecentTorrentID = torrentID;
                }
            }

            return requestGenerator;
        }

        public override IParseIndexerResponse GetParser()
        {
            return new BroadcastheNetParser(Capabilities.Categories);
        }
    }
}
