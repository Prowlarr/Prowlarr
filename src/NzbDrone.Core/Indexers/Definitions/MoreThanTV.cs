using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using FluentValidation;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions;

public class MoreThanTV : TorrentIndexerBase<CookieTorrentBaseSettings>
{
    public override string Name => "MoreThanTV";
    public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
    public override bool FollowRedirect => true;

    public MoreThanTV(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
        => new MoreThanTVRequestGenerator(Settings, Capabilities);

    public override IParseIndexerResponse GetParser()
        => new MoreThanTVParser
        {
            Settings = Settings
        };

    protected override IDictionary<string, string> GetCookies()
    {
        return CookieUtil.CookieHeaderToDictionary(Settings.Cookie);
    }
}

public class MoreThanTVRequestGenerator : IIndexerRequestGenerator
{
    private CookieTorrentBaseSettings Settings { get; }
    private IndexerCapabilities Capabilities { get; }

    private NameValueCollection BrowserHeaders { get; }

    public MoreThanTVRequestGenerator(CookieTorrentBaseSettings settings, IndexerCapabilities capabilities)
    {
        Settings = settings;
        Capabilities = capabilities;
        BrowserHeaders = new NameValueCollection()
        {
            { "referer", settings.BaseUrl },
            { "Upgrade-Insecure-Requests", "1" },
            { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.72 Safari/537.36" }
        };
    }

    public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        => PerformRequest(searchCriteria);

    public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        => PerformRequest(searchCriteria);

    public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        => PerformRequest(searchCriteria);

    public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        => PerformRequest(searchCriteria);

    public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        => PerformRequest(searchCriteria);

    public Func<IDictionary<string, string>> GetCookies { get; set; }
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

    private IndexerPageableRequestChain PerformRequest(SearchCriteriaBase query)
    {
        var chain = new IndexerPageableRequestChain();

        var requests = new List<IndexerRequest> { new (new HttpRequest(GetTorrentSearchUrl(query)) { Headers = new HttpHeader(BrowserHeaders), AllowAutoRedirect = true }) };

        if (query is TvSearchCriteria tvSearchCriteria)
        {
            // Always search for torrent groups (complete seasons) too
            var seasonRegex = new Regex(@".*\s[Ss]{1}\d{2}([Ee]{1}\d{2,3})?$", RegexOptions.Compiled);
            var seasonMatch = seasonRegex.Match(query.SanitizedSearchTerm);
            if (seasonMatch.Success)
            {
                var seasonReplaceRegex = new Regex(@"[Ss]{1}\d{2}([Ee]{1}\d{2,3})?", RegexOptions.Compiled);
                var newSearchQuery = seasonReplaceRegex.Replace(query.SanitizedSearchTerm, $"Season {tvSearchCriteria.Season}");
                requests.Add(new IndexerRequest(new HttpRequest(GetTorrentSearchUrl(query, newSearchQuery)) { Headers = new HttpHeader(BrowserHeaders), AllowAutoRedirect = true }));
            }
        }

        chain.Add(requests);

        return chain;
    }

    private string GetTorrentSearchUrl(SearchCriteriaBase query, string overrideSearchTerm = null)
    {
        var qc = new NameValueCollection
        {
            { "action", "advanced" },
            { "sizetype", "gb" },
            { "sizerange", "0.01" },
            { "title", overrideSearchTerm ?? GetSearchString(query.SanitizedSearchTerm) }
        };

        switch (query)
        {
            case MovieSearchCriteria:
                qc.Add("filter_cat[1]", "1"); // HD Movies
                qc.Add("filter_cat[2]", "1"); // SD Movies
                break;
            case TvSearchCriteria:
                qc.Add("filter_cat[3]", "1"); // HD Episode
                qc.Add("filter_cat[4]", "1"); // SD Episode
                qc.Add("filter_cat[5]", "1"); // HD Season
                qc.Add("filter_cat[6]", "1"); // SD Season
                break;
        }

        return $"{Settings.BaseUrl}torrents/browse?{qc.GetQueryString()}";
    }

    private string GetSearchString(string input)
    {
        input = input.Replace("Marvels", "Marvel"); // strip 's for better results
        var regex = new Regex(@"(S\d{2})$", RegexOptions.Compiled);
        return regex.Replace(input, "$1*"); // If we're just seaching for a season (no episode) append an * to include all episodes of that season.
    }
}

public class MoreThanTVParser : IParseIndexerResponse
{
    public CookieTorrentBaseSettings Settings { get; init; }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        var releases = new List<ReleaseInfo>();

        try
        {
            var parser = new HtmlParser();
            var document = parser.ParseDocument(indexerResponse.Content);
            var torrents = document.QuerySelectorAll("#torrent_table > tbody > tr.torrent");
            var movies = new[] { "movie" };
            var tv = new[] { "season", "episode" };

            // Loop through all torrents checking for groups
            foreach (var torrent in torrents)
            {
                // Parse required data
                var downloadAnchor = torrent.QuerySelector("span a[href^=\"/torrents.php?action=download\"]");
                var title = downloadAnchor.ParentElement.ParentElement.ParentElement.QuerySelector("a[class=\"overlay_torrent\"]").TextContent.Trim();
                title = CleanUpTitle(title);

                var category = torrent.QuerySelector(".cats_col div").GetAttribute("title");

                // default to Other
                var indexerCategory = NewznabStandardCategory.Other;

                if (movies.Any(category.Contains))
                {
                    indexerCategory = NewznabStandardCategory.Movies;
                }
                else if (tv.Any(category.Contains))
                {
                    indexerCategory = NewznabStandardCategory.TV;
                }

                releases.Add(GetReleaseInfo(torrent, downloadAnchor, title, indexerCategory));
            }

            return releases;
        }
        catch (Exception ex)
        {
            throw new Exception("Error while parsing torrent response", ex);
        }
    }

    /// <summary>
    /// Gather Release info from torrent table. Target using css
    /// </summary>
    /// <param name="row"></param>
    /// <param name="downloadAnchor"></param>
    /// <param name="title"></param>
    /// <param name="category"></param>
    /// <returns></returns>
    private ReleaseInfo GetReleaseInfo(IElement row, IElement downloadAnchor, string title, IndexerCategory category)
    {
            // count from bottom
            const int FILES_COL = 7;
            /*const int COMMENTS_COL = 7;*/
            const int DATE_COL = 6;
            const int FILESIZE_COL = 5;
            const int SNATCHED_COL = 4;
            const int SEEDS_COL = 3;
            const int LEECHERS_COL = 2;
            /*const int USER_COL = 1;*/

            var downloadAnchorHref = (downloadAnchor as IHtmlAnchorElement).Href;
            var queryParams = HttpUtility.ParseQueryString(downloadAnchorHref, Encoding.UTF8);
            var torrentId = queryParams["id"];

            var qFiles = row.QuerySelector("td:nth-last-child(" + FILES_COL + ")").TextContent;

            var fileCount = ParseUtil.CoerceInt(qFiles);
            var qPublishDate = row.QuerySelector("td:nth-last-child(" + DATE_COL + ") .time").Attributes["title"].Value;
            var publishDate = DateTime.ParseExact(qPublishDate, "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToLocalTime();
            var qPoster = row.QuerySelector("div.tp-banner img")?.GetAttribute("src");
            var poster = (qPoster != null && !qPoster.Contains("caticons")) ? qPoster : null;
            var description = row.QuerySelector("div.tags")?.TextContent.Trim();
            var fileSize = row.QuerySelector("td:nth-last-child(" + FILESIZE_COL + ")").TextContent.Trim();
            var snatched = row.QuerySelector("td:nth-last-child(" + SNATCHED_COL + ")").TextContent.Trim();
            var seeds = row.QuerySelector("td:nth-last-child(" + SEEDS_COL + ")").TextContent.Trim();
            var leechs = row.QuerySelector("td:nth-last-child(" + LEECHERS_COL + ")").TextContent.Trim();

            if (fileSize.Length <= 0 || snatched.Length <= 0 || seeds.Length <= 0 || leechs.Length <= 0)
            {
                // Size (xx.xx GB[ (Max)]) Snatches (xx) Seeders (xx) Leechers (xx)
                throw new Exception($"We expected 4 torrent datas.");
            }

            var detailUrl = $"{Settings.BaseUrl}details.php";

            var size = ParseUtil.GetBytes(fileSize);
            var grabs = int.Parse(snatched, NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
            var seeders = int.Parse(seeds, NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
            var leechers = int.Parse(leechs, NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
            var detailsUrl = $"{detailUrl}?torrentid={torrentId}";
            var downloadUrl = $"{detailUrl}?action=download&id={torrentId}";
            var categories = new List<IndexerCategory> { category };

            return new TorrentInfo
            {
                Title = title,
                Categories = categories,
                DownloadUrl = downloadUrl,
                PublishDate = publishDate,
                PosterUrl = poster,
                Description = description,
                Seeders = seeders,
                Peers = seeders + leechers,
                Files = fileCount,
                Size = size,
                Grabs = grabs,
                Guid = downloadUrl,
                InfoUrl = detailsUrl,
                DownloadVolumeFactor = 0, // ratioless tracker
                UploadVolumeFactor = 1
            };
        }

    /// <summary>
    /// Clean Up any title stuff
    /// </summary>
    /// <param name="title"></param>
    /// <returns></returns>
    private string CleanUpTitle(string title)
    {
        return title
            .Replace(".", " ")
            .Replace("4K", "2160p"); // sonarr cleanup
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}
