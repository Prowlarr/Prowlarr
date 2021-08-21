using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class CGPeers : Gazelle.Gazelle
    {
        public override string Name => "CGPeers";
        public override string[] IndexerUrls => new string[] { "https://cgpeers.to/" };
        public override string Description => "CGPeers is a Private Torrent Tracker for GRAPHICS SOFTWARE / TUTORIALS / ETC";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public CGPeers(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        protected override IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.PCISO, "Full Applications");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.PC0day, "Plugins");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.Other, "Tutorials");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.Other, "Models");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.Other, "Materials");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.OtherMisc, "Misc");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.Other, "GameDev");

            return caps;
        }
    }
}
