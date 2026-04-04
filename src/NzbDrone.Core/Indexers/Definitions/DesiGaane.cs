using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.Gazelle;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions;

public class DesiGaane : GazelleBase<GazelleSettings>
{
    public override string Name => "DesiGaane";
    public override string[] IndexerUrls => new[] { "https://desigaane.rocks/" };
    public override string Description => "DesiGaane is a Private Torrent Tracker for DESI MUSIC";
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

    public DesiGaane(IIndexerHttpClient httpClient,
                        IEventAggregator eventAggregator,
                        IIndexerStatusService indexerStatusService,
                        IConfigService configService,
                        Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    protected override IndexerCapabilities SetCapabilities()
    {
        var caps = new IndexerCapabilities();

        caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Audio, "Music");

        return caps;
    }
}
