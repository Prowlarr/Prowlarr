using System.Collections.Generic;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.Gazelle;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions;

public class DICMusic : GazelleBase<DICMusicSettings>
{
    public override string Name => "DICMusic";
    public override string[] IndexerUrls => new[] { "https://dicmusic.com/" };
    public override string[] LegacyUrls => new[] { "https://dicmusic.club/" };
    public override string Description => "DICMusic is a CHINESE Private Torrent Tracker for MUSIC";
    public override string Language => "zh-CN";
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

    public DICMusic(IIndexerHttpClient httpClient,
                    IEventAggregator eventAggregator,
                    IIndexerStatusService indexerStatusService,
                    IConfigService configService,
                    Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    protected override IndexerCapabilities SetCapabilities()
    {
        var caps = new IndexerCapabilities
        {
            MusicSearchParams = new List<MusicSearchParam>
            {
                MusicSearchParam.Q, MusicSearchParam.Album, MusicSearchParam.Artist, MusicSearchParam.Label, MusicSearchParam.Year
            }
        };

        caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Audio, "Music");
        caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.PC, "Applications");

        return caps;
    }
}

public class DICMusicSettings : GazelleUserPassOrCookieSettings
{
}
