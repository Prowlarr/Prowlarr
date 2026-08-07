using System;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.Gazelle;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions;

public class Simurg : GazelleBase<GazelleSettings>
{
    public override string Name => "Simurg";
    public override string[] IndexerUrls => new[] { "https://simurg.world/" };
    public override string Description => "Simurg is a Private Torrent Tracker for EBOOKS and AUDIOBOOKS";
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

    public Simurg(IIndexerHttpClient httpClient,
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

        caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.BooksEBook, "E-Books");
        caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.AudioAudiobook, "Audiobooks");

        return caps;
    }

    protected override Task<HttpRequest> GetDownloadRequest(Uri link)
    {
        var requestBuilder = new HttpRequestBuilder(link.AbsoluteUri)
        {
            AllowAutoRedirect = FollowRedirect
        };

        var request = requestBuilder
            .SetHeader("Authorization", $"token {Settings.ApiKey}")
            .Build();

        return Task.FromResult(request);
    }
}
