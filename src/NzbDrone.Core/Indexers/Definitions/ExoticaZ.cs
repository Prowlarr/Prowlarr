using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.Avistaz;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class ExoticaZ : AvistazBase
    {
        public override string Name => "ExoticaZ";
        public override string[] IndexerUrls => new string[] { "https://exoticaz.to/" };
        public override string Description => "ExoticaZ (YourExotic) is a Private Torrent Tracker for 3X";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public ExoticaZ(IIndexerRepository indexerRepository, IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(indexerRepository, httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AvistazRequestGenerator()
            {
                Settings = Settings,
                HttpClient = _httpClient,
                Logger = _logger,
                Capabilities = Capabilities,
            };
        }

        protected override IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q
                       }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.XXXx264);
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.XXXPack);
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.XXXPack);
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.XXXPack);
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.XXXDVD);
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.XXXx264);
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.XXXImageSet);
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.XXXImageSet);

            return caps;
        }
    }
}
