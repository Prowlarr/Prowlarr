using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers.Definitions.Avistaz;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions;

public class AnimeZ : AvistazBase
{
    public override string Name => "AnimeZ";
    public override string[] IndexerUrls => new[] { "https://animez.to/" };
    public override string Description => "AnimeZ (ex-AnimeTorrents) is a Private Torrent Tracker for ANIME / MANGA";
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

    public AnimeZ(IIndexerRepository indexerRepository,
        IIndexerHttpClient httpClient,
        IEventAggregator eventAggregator,
        IIndexerStatusService indexerStatusService,
        IConfigService configService,
        Logger logger)
        : base(indexerRepository, httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new AnimeZRequestGenerator
        {
            Settings = Settings,
            Capabilities = Capabilities,
            PageSize = PageSize,
            HttpClient = _httpClient,
            Logger = _logger
        };
    }

    public override IParseIndexerResponse GetParser()
    {
        return new AnimeZParser(Capabilities.Categories);
    }

    public override async Task<IndexerDownloadResponse> Download(Uri link)
    {
        try
        {
            return await base.Download(link).ConfigureAwait(false);
        }
        catch (ReleaseDownloadException ex) when (ex.InnerException is HttpException httpException &&
                                                  httpException.Response.StatusCode is HttpStatusCode.Unauthorized)
        {
            await DoLogin().ConfigureAwait(false);
        }

        return await base.Download(link).ConfigureAwait(false);
    }

    protected override Task<HttpRequest> GetDownloadRequest(Uri link)
    {
        var request = new HttpRequestBuilder(link.AbsoluteUri)
            .Accept(HttpAccept.Json)
            .SetHeader("Authorization", $"Bearer {Settings.Token}")
            .Build();

        return Task.FromResult(request);
    }

    protected override IndexerCapabilities SetCapabilities()
    {
        var caps = new IndexerCapabilities
        {
            LimitsDefault = PageSize,
            LimitsMax = PageSize,
            TvSearchParams = new List<TvSearchParam>
            {
                TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
            },
            MovieSearchParams = new List<MovieSearchParam>
            {
                MovieSearchParam.Q
            },
            BookSearchParams = new List<BookSearchParam>
            {
                BookSearchParam.Q,
            }
        };

        caps.Categories.AddCategoryMapping("TV", NewznabStandardCategory.TVAnime, "Anime > TV");
        caps.Categories.AddCategoryMapping("TV_SHORT", NewznabStandardCategory.TVAnime, "Anime > TV Short");
        caps.Categories.AddCategoryMapping("MOVIE", NewznabStandardCategory.Movies, "Anime > Movie");
        caps.Categories.AddCategoryMapping("SPECIAL", NewznabStandardCategory.TVAnime, "Anime > Special");
        caps.Categories.AddCategoryMapping("OVA", NewznabStandardCategory.TVAnime, "Anime > OVA");
        caps.Categories.AddCategoryMapping("ONA", NewznabStandardCategory.TVAnime, "Anime > ONA");
        caps.Categories.AddCategoryMapping("MUSIC", NewznabStandardCategory.TVAnime, "Anime > Music");
        caps.Categories.AddCategoryMapping("MANGA", NewznabStandardCategory.BooksComics, "Manga > Manga");
        caps.Categories.AddCategoryMapping("NOVEL", NewznabStandardCategory.BooksForeign, "Manga > Novel");
        caps.Categories.AddCategoryMapping("ONE_SHOT", NewznabStandardCategory.BooksForeign, "Manga > One-Shot");

        return caps;
    }
}

public class AnimeZRequestGenerator : AvistazRequestGenerator
{
    protected override List<KeyValuePair<string, string>> GetBasicSearchParameters(SearchCriteriaBase searchCriteria, string genre = null)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            { "limit", Math.Min(PageSize, searchCriteria.Limit.GetValueOrDefault(PageSize)).ToString() }
        };

        var categoryMappings = Capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories).Distinct().ToList();

        if (categoryMappings.Any())
        {
            foreach (var category in categoryMappings)
            {
                parameters.Add("format[]", category);
            }
        }

        if (searchCriteria.Limit is > 0 && searchCriteria.Offset is > 0)
        {
            var page = (int)(searchCriteria.Offset / searchCriteria.Limit) + 1;
            parameters.Add("page", page.ToString());
        }

        if (Settings.FreeleechOnly)
        {
            parameters.Add("freeleech", "1");
        }

        return parameters;
    }
}

public class AnimeZParser(IndexerCapabilitiesCategories categories) : AvistazParserBase
{
    protected override List<IndexerCategory> ParseCategories(AvistazRelease row)
    {
        return categories.MapTrackerCatToNewznab(row.Format).ToList();
    }

    protected override string ParseTitle(AvistazRelease row)
    {
        return row.ReleaseTitle.IsNotNullOrWhiteSpace() ? row.ReleaseTitle : row.FileName;
    }
}
