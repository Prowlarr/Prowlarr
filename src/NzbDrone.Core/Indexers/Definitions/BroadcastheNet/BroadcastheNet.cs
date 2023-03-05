using System;
using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.BroadcastheNet
{
    public class BroadcastheNet : TorrentIndexerBase<BroadcastheNetSettings>
    {
        public override string Name => "BroadcasTheNet";

        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;
        public override bool SupportsPagination => true;
        public override int PageSize => 100;
        public override IndexerCapabilities Capabilities => SetCapabilities();
        public override TimeSpan RateLimit => TimeSpan.FromSeconds(5);

        public override string[] IndexerUrls => new string[] { "https://api.broadcasthe.net/" };
        public override string[] LegacyUrls => new string[] { "http://api.broadcasthe.net/" };

        public override string Description => "BroadcasTheNet (BTN) is an invite-only torrent tracker focused on TV shows";

        public BroadcastheNet(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
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

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                LimitsDefault = 100,
                LimitsMax = 1000,
                TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.TvdbId, TvSearchParam.RId
                       }
            };

            caps.Categories.AddCategoryMapping("SD", NewznabStandardCategory.TVSD, "SD");
            caps.Categories.AddCategoryMapping("720p", NewznabStandardCategory.TVHD, "720p");
            caps.Categories.AddCategoryMapping("1080p", NewznabStandardCategory.TVHD, "1080p");
            caps.Categories.AddCategoryMapping("1080i", NewznabStandardCategory.TVHD, "1080i");
            caps.Categories.AddCategoryMapping("2160p", NewznabStandardCategory.TVHD, "2160p");
            caps.Categories.AddCategoryMapping("Portable Device", NewznabStandardCategory.TVSD, "Portable Device");

            return caps;
        }
    }
}
