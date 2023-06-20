using System.Collections.Generic;
using AngleSharp.Html.Parser;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.Gazelle;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions;

public class AroLol : GazelleBase<AroLolSettings>
{
    public override string Name => "aro.lol";
    public override string[] IndexerUrls => new[] { "https://aro.lol/" };
    public override string Description => "aro.lol is a SERBIAN/ENGLISH Private Torrent Tracker for ANIME";
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

    public AroLol(IIndexerHttpClient httpClient,
                        IEventAggregator eventAggregator,
                        IIndexerStatusService indexerStatusService,
                        IConfigService configService,
                        Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    protected override HttpRequestBuilder AuthLoginRequestBuilder()
    {
        return base.AuthLoginRequestBuilder()
            .AddFormParameter("twofa", Settings.TwoFactorAuthCode?.Trim() ?? "");
    }

    protected override bool CheckForLoginError(HttpResponse response)
    {
        if (response.Content.Contains("loginform"))
        {
            var parser = new HtmlParser();
            var dom = parser.ParseDocument(response.Content);
            var errorMessage = dom.QuerySelector("#loginform > .warning")?.TextContent.Trim();

            throw new IndexerAuthException(errorMessage ?? "Unknown error message, please report.");
        }

        return true;
    }

    protected override IndexerCapabilities SetCapabilities()
    {
        var caps = new IndexerCapabilities
        {
            TvSearchParams = new List<TvSearchParam>
            {
                TvSearchParam.Q
            },
            MovieSearchParams = new List<MovieSearchParam>
            {
                MovieSearchParam.Q
            },
            BookSearchParams = new List<BookSearchParam>
            {
                BookSearchParam.Q
            }
        };

        caps.Categories.AddCategoryMapping("1", NewznabStandardCategory.Movies, "Movies");
        caps.Categories.AddCategoryMapping("2", NewznabStandardCategory.TVAnime, "Anime");
        caps.Categories.AddCategoryMapping("3", NewznabStandardCategory.Books, "Manga");
        caps.Categories.AddCategoryMapping("4", NewznabStandardCategory.Console, "Games");
        caps.Categories.AddCategoryMapping("5", NewznabStandardCategory.Other, "Other");

        return caps;
    }
}

public class AroLolSettings : GazelleSettings
{
    [FieldDefinition(4, Label = "2FA code", Type = FieldType.Textbox, HelpText = "Only fill in the <b>2FA code</b> box if you have enabled <b>2FA</b> on the aro.lol Web Site. Otherwise just leave it empty.")]
    public string TwoFactorAuthCode { get; set; }
}
