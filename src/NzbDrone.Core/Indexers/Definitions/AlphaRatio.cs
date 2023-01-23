using System.Collections.Generic;
using System.Collections.Specialized;
using NLog;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.Gazelle;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions;

public class AlphaRatio : GazelleBase<AlphaRatioSettings>
{
    public override string Name => "AlphaRatio";
    public override string[] IndexerUrls => new[] { "https://alpharatio.cc/" };
    public override string Description => "AlphaRatio(AR) is a Private Torrent Tracker for 0DAY / GENERAL";
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

    public AlphaRatio(IIndexerHttpClient httpClient,
                      IEventAggregator eventAggregator,
                      IIndexerStatusService indexerStatusService,
                      IConfigService configService,
                      Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new AlphaRatioRequestGenerator(Settings, Capabilities, _httpClient, _logger);
    }

    protected override IndexerCapabilities SetCapabilities()
    {
        var caps = new IndexerCapabilities
        {
            TvSearchParams = new List<TvSearchParam>
            {
                TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
            },
            MovieSearchParams = new List<MovieSearchParam>
            {
                MovieSearchParam.Q
            }
        };

        caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.TVSD, "TvSD");
        caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVHD, "TvHD");
        caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.TVUHD, "TvUHD");
        caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.TVSD, "TvDVDRip");
        caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.TVSD, "TvPackSD");
        caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.TVHD, "TvPackHD");
        caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.TVUHD, "TvPackUHD");
        caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.MoviesSD, "MovieSD");
        caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.MoviesHD, "MovieHD");
        caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.MoviesUHD, "MovieUHD");
        caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.MoviesSD, "MoviePackSD");
        caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.MoviesHD, "MoviePackHD");
        caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.MoviesUHD, "MoviePackUHD");
        caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.XXX, "MovieXXX");
        caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.MoviesBluRay, "Bluray");
        caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.TVAnime, "AnimeSD");
        caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.TVAnime, "AnimeHD");
        caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.PCGames, "GamesPC");
        caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.ConsoleXBox, "GamesxBox");
        caps.Categories.AddCategoryMapping(20, NewznabStandardCategory.ConsolePS4, "GamesPS");
        caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.ConsoleWii, "GamesNin");
        caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.PC0day, "AppsWindows");
        caps.Categories.AddCategoryMapping(23, NewznabStandardCategory.PCMac, "AppsMAC");
        caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.PC0day, "AppsLinux");
        caps.Categories.AddCategoryMapping(25, NewznabStandardCategory.PCMobileOther, "AppsMobile");
        caps.Categories.AddCategoryMapping(26, NewznabStandardCategory.XXX, "0dayXXX");
        caps.Categories.AddCategoryMapping(27, NewznabStandardCategory.Books, "eBook");
        caps.Categories.AddCategoryMapping(28, NewznabStandardCategory.AudioAudiobook, "AudioBook");
        caps.Categories.AddCategoryMapping(29, NewznabStandardCategory.AudioOther, "Music");
        caps.Categories.AddCategoryMapping(30, NewznabStandardCategory.Other, "Misc");

        return caps;
    }
}

public class AlphaRatioRequestGenerator : GazelleRequestGenerator
{
    protected override bool ImdbInTags => true;
    private readonly AlphaRatioSettings _settings;

    public AlphaRatioRequestGenerator(AlphaRatioSettings settings,
                                      IndexerCapabilities capabilities,
                                      IIndexerHttpClient httpClient,
                                      Logger logger)
        : base(settings, capabilities, httpClient, logger)
    {
        _settings = settings;
    }

    protected override NameValueCollection GetBasicSearchParameters(string term, int[] categories)
    {
        var parameters = base.GetBasicSearchParameters(term, categories);

        if (_settings.FreeleechOnly)
        {
            parameters.Set("freetorrent", "1");
        }

        if (_settings.ExcludeScene)
        {
            parameters.Set("scene", "0");
        }

        return parameters;
    }
}

public class AlphaRatioSettings : GazelleSettings
{
    [FieldDefinition(5, Label = "Freeleech Only", Type = FieldType.Checkbox, HelpText = "Search freeleech torrents only")]
    public bool FreeleechOnly { get; set; }

    [FieldDefinition(6, Label = "Exclude Scene", Type = FieldType.Checkbox, HelpText = "Exclude Scene torrents from results")]
    public bool ExcludeScene { get; set; }
}
