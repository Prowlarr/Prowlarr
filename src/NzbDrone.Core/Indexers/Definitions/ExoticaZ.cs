using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.Avistaz;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class ExoticaZ : AvistazBase
    {
        public override string Name => "ExoticaZ";
        public override string[] IndexerUrls => new[] { "https://exoticaz.to/" };
        public override string Description => "ExoticaZ (YourExotic) is a Private Torrent Tracker for 3X";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public ExoticaZ(IIndexerRepository indexerRepository,
                        IIndexerHttpClient httpClient,
                        IEventAggregator eventAggregator,
                        IIndexerStatusService indexerStatusService,
                        IConfigService configService,
                        Logger logger)
            : base(indexerRepository, httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IParseIndexerResponse GetParser()
        {
            return new ExoticaZParser(Capabilities.Categories);
        }

        protected override IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                LimitsDefault = PageSize,
                LimitsMax = PageSize
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.XXXx264, "Video Clip");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.XXXPack, "Video Pack");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.XXXPack, "Siterip Pack");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.XXXPack, "Pornstar Pack");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.XXXDVD, "DVD");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.XXXx264, "BluRay");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.XXXImageSet, "Photo Pack");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.XXXImageSet, "Books & Magazines");

            return caps;
        }
    }

    public class ExoticaZParser : AvistazParserBase
    {
        private readonly IndexerCapabilitiesCategories _categories;

        protected override string TimezoneOffset => "+01:00";

        public ExoticaZParser(IndexerCapabilitiesCategories categories)
        {
            _categories = categories;
        }

        protected override List<IndexerCategory> ParseCategories(AvistazRelease row)
        {
            var cat = row.Category;

            return cat.SelectMany(c => _categories.MapTrackerCatToNewznab(c.Key)).ToList();
        }
    }
}
