using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class BrokenStones : Gazelle.Gazelle
    {
        public override string Name => "BrokenStones";
        public override string[] IndexerUrls => new string[] { "https://brokenstones.club/" };
        public override string Description => "Broken Stones is a Private site for MacOS and iOS APPS / GAMES";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public BrokenStones(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        protected override IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.PCMac, "MacOS Apps");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.PCMac, "MacOS Games");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.PCMobileiOS, "iOS Apps");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.PCMobileiOS, "iOS Games");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.Other, "Graphics");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.Audio, "Audio");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.Other, "Tutorials");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.Other, "Other");

            return caps;
        }
    }
}
