using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using AngleSharp.Html.Parser;
using FluentValidation;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions;

public class PixelHD : TorrentIndexerBase<PixelHDSettings>
{
    public override string Name => "PiXELHD";
    public override string[] IndexerUrls => new[] { "https://pixelhd.me/" };
    public override string Description => "PixelHD (PxHD) is a ratioless Private Torrent Tracker for HD .MP4 MOVIES / TV";
    public override string Language => "en-US";
    public override Encoding Encoding => Encoding.UTF8;
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
    public override IndexerCapabilities Capabilities => SetCapabilities();

    public PixelHD(IIndexerHttpClient httpClient,
                   IEventAggregator eventAggregator,
                   IIndexerStatusService indexerStatusService,
                   IConfigService configService,
                   Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new PixelHDRequestGenerator(Settings);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new PixelHDParser(Settings);
    }

    protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
    {
        return false;
    }

    protected override IDictionary<string, string> GetCookies()
    {
        return CookieUtil.CookieHeaderToDictionary(Settings.Cookie);
    }

    private IndexerCapabilities SetCapabilities()
    {
        var caps = new IndexerCapabilities
        {
            MovieSearchParams = new List<MovieSearchParam>
            {
                MovieSearchParam.Q, MovieSearchParam.ImdbId
            }
        };

        caps.Categories.AddCategoryMapping("1", NewznabStandardCategory.MoviesHD);

        return caps;
    }
}

public class PixelHDRequestGenerator : IIndexerRequestGenerator
{
    private readonly PixelHDSettings _settings;

    public PixelHDRequestGenerator(PixelHDSettings settings)
    {
        _settings = settings;
    }

    public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests(searchCriteria.SanitizedSearchTerm, searchCriteria.FullImdbId));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
    {
        return new IndexerPageableRequestChain();
    }

    public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
    {
        return new IndexerPageableRequestChain();
    }

    public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
    {
        return new IndexerPageableRequestChain();
    }

    public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests(searchCriteria.SanitizedSearchTerm));

        return pageableRequests;
    }

    private IEnumerable<IndexerRequest> GetPagedRequests(string term, string imdbId = null)
    {
        var parameters = new NameValueCollection
        {
            { "order_by", "time" },
            { "order_way", "desc" }
        };

        if (imdbId.IsNotNullOrWhiteSpace())
        {
            parameters.Set("imdbid", imdbId);
        }

        if (term.IsNotNullOrWhiteSpace())
        {
            parameters.Set("groupname", term);
        }

        var searchUrl = $"{_settings.BaseUrl}torrents.php?{parameters.GetQueryString()}";

        var request = new IndexerRequest(searchUrl, HttpAccept.Html);

        if (_settings.UserAgent.IsNotNullOrWhiteSpace())
        {
            request.HttpRequest.Headers.UserAgent = _settings.UserAgent;
        }

        yield return request;
    }

    public Func<IDictionary<string, string>> GetCookies { get; set; }
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class PixelHDParser : IParseIndexerResponse
{
    private readonly PixelHDSettings _settings;

    public PixelHDParser(PixelHDSettings settings)
    {
        _settings = settings;
    }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new IndexerException(indexerResponse, $"Search returned unexpected result. Expected 200 OK but got {indexerResponse.HttpResponse.StatusCode}.");
        }

        var releaseInfos = new List<ReleaseInfo>();

        var parser = new HtmlParser();
        using var dom = parser.ParseDocument(indexerResponse.Content);

        var groups = dom.QuerySelectorAll("div.browsePoster");
        foreach (var group in groups)
        {
            var groupName = group.QuerySelector("strong:has(a[title=\"View Torrent\"])")?.TextContent.Replace(" ]", "]");

            var imdbId = ParseUtil.GetImdbId(group.QuerySelector("a[href*=\"imdb.com/title/tt\"]")?.GetAttribute("href")?.TrimEnd('/')?.Split('/')?.LastOrDefault()) ?? 0;

            var rows = group.QuerySelectorAll("tr.group_torrent:has(a[href^=\"torrents.php?id=\"])");
            foreach (var row in rows)
            {
                var downloadUrl = _settings.BaseUrl + row.QuerySelector("a[href^=\"torrents.php?action=download\"]")?.GetAttribute("href");
                var infoUrl = _settings.BaseUrl + row.QuerySelector("a[href^=\"torrents.php?id=\"]")?.GetAttribute("href");

                var title = row.QuerySelector("a[href^=\"torrents.php?id=\"]")?.TextContent.Trim();

                var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(7)")?.TextContent);
                var peers = seeders + ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(8)")?.TextContent.Trim());

                var release = new TorrentInfo
                {
                    Guid = infoUrl,
                    InfoUrl = infoUrl,
                    DownloadUrl = downloadUrl,
                    Title = $"{groupName} {title}".Trim(),
                    Categories = new List<IndexerCategory> { NewznabStandardCategory.Movies, NewznabStandardCategory.MoviesHD },
                    Seeders = seeders,
                    Peers = peers,
                    Size =  ParseUtil.GetBytes(row.QuerySelector("td:nth-child(4)")?.TextContent.Trim()),
                    Grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(6)")?.TextContent),
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1,
                    ImdbId = imdbId,
                    MinimumRatio = 1,
                    MinimumSeedTime = 259200 // 72 hours
                };

                try
                {
                    release.PublishDate = DateTime.ParseExact(
                        row.QuerySelector("td:nth-child(3) span[class=\"time\"]")?.GetAttribute("title")?.Trim(),
                        "MMM dd yyyy, HH:mm",
                        CultureInfo.InvariantCulture);
                }
                catch (Exception)
                {
                    release.PublishDate = DateTimeUtil.FromTimeAgo(row.QuerySelector("td:nth-child(3)")?.TextContent.Trim());
                }

                releaseInfos.Add(release);
            }
        }

        return releaseInfos.ToArray();
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class PixelHDSettingsValidator : CookieBaseSettingsValidator<PixelHDSettings>
{
    public PixelHDSettingsValidator()
    {
        RuleFor(c => c.UserAgent).NotEmpty();
    }
}

public class PixelHDSettings : CookieTorrentBaseSettings
{
    private static readonly PixelHDSettingsValidator Validator = new ();

    [FieldDefinition(3, Label = "Cookie User-Agent", Type = FieldType.Textbox, HelpText = "User-Agent associated with cookie used from Browser")]
    public string UserAgent { get; set; }

    public override NzbDroneValidationResult Validate()
    {
        return new NzbDroneValidationResult(Validator.Validate(this));
    }
}
