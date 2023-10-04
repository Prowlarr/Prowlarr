using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
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

namespace NzbDrone.Core.Indexers.Definitions;

public class Libble : TorrentIndexerBase<LibbleSettings>
{
    public override string Name => "Libble";
    public override string[] IndexerUrls => new[] { "https://libble.me/" };
    public override string Description => "Libble is a Private Torrent Tracker for MUSIC";
    private string LoginUrl => Settings.BaseUrl + "login.php";
    public override string Language => "en-US";
    public override Encoding Encoding => Encoding.UTF8;
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
    public override bool SupportsPagination => true;
    public override int PageSize => 50;
    public override IndexerCapabilities Capabilities => SetCapabilities();

    public Libble(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new LibbleRequestGenerator(Settings, Capabilities);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new LibbleParser(Settings);
    }

    protected override async Task DoLogin()
    {
        var requestBuilder = new HttpRequestBuilder(LoginUrl)
        {
            AllowAutoRedirect = true,
            Method = HttpMethod.Post
        };

        var cookies = Cookies;
        Cookies = null;

        var authLoginRequest = requestBuilder
            .AddFormParameter("username", Settings.Username)
            .AddFormParameter("password", Settings.Password)
            .AddFormParameter("code", Settings.TwoFactorAuthCode)
            .AddFormParameter("keeplogged", "1")
            .AddFormParameter("login", "Login")
            .SetHeader("Content-Type", "application/x-www-form-urlencoded")
            .SetHeader("Referer", LoginUrl)
            .Build();

        var response = await ExecuteAuth(authLoginRequest);

        if (CheckIfLoginNeeded(response))
        {
            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(response.Content);
            var errorMessage = dom.QuerySelector("#loginform > .warning")?.TextContent.Trim();

            throw new IndexerAuthException(errorMessage ?? "Unknown error message, please report.");
        }

        cookies = response.GetCookies();
        UpdateCookies(cookies, DateTime.Now.AddDays(30));

        _logger.Debug("Authentication succeeded.");
    }

    protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
    {
        return !httpResponse.Content.Contains("logout.php");
    }

    private IndexerCapabilities SetCapabilities()
    {
        var caps = new IndexerCapabilities
        {
            MusicSearchParams = new List<MusicSearchParam>
            {
                MusicSearchParam.Q, MusicSearchParam.Artist, MusicSearchParam.Album, MusicSearchParam.Label, MusicSearchParam.Year, MusicSearchParam.Genre
            }
        };

        caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.Audio, "Music");
        caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.Audio, "Libble Mixtapes");
        caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.AudioVideo, "Music Videos");

        return caps;
    }
}

public class LibbleRequestGenerator : IIndexerRequestGenerator
{
    private readonly LibbleSettings _settings;
    private readonly IndexerCapabilities _capabilities;

    public LibbleRequestGenerator(LibbleSettings settings, IndexerCapabilities capabilities)
    {
        _settings = settings;
        _capabilities = capabilities;
    }

    public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();
        var parameters = new NameValueCollection();

        if (searchCriteria.Artist.IsNotNullOrWhiteSpace() && searchCriteria.Artist != "VA")
        {
            parameters.Set("artistname", searchCriteria.Artist);
        }

        if (searchCriteria.Album.IsNotNullOrWhiteSpace())
        {
            // Remove year
            var album = Regex.Replace(searchCriteria.Album, @"(.+)\b\d{4}$", "$1");

            parameters.Set("groupname", album.Trim());
        }

        if (searchCriteria.Label.IsNotNullOrWhiteSpace())
        {
            parameters.Set("recordlabel", searchCriteria.Label);
        }

        if (searchCriteria.Year.HasValue)
        {
            parameters.Set("year", searchCriteria.Year.ToString());
        }

        if (searchCriteria.Genre.IsNotNullOrWhiteSpace())
        {
            parameters.Set("taglist", searchCriteria.Genre);
            parameters.Set("tags_type", "0");
        }

        pageableRequests.Add(GetPagedRequests(searchCriteria, parameters));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();
        var parameters = new NameValueCollection();

        pageableRequests.Add(GetPagedRequests(searchCriteria, parameters));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
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

    private IEnumerable<IndexerRequest> GetPagedRequests(SearchCriteriaBase searchCriteria, NameValueCollection parameters)
    {
        var term = searchCriteria.SanitizedSearchTerm.Trim();

        parameters.Set("action", "advanced");
        parameters.Set("order_by", "time");
        parameters.Set("order_way", "desc");

        if (term.IsNotNullOrWhiteSpace())
        {
            parameters.Set("searchstr", term);
        }

        var queryCats = _capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories);
        if (queryCats.Any())
        {
            queryCats.ForEach(cat => parameters.Set($"filter_cat[{cat}]", "1"));
        }

        if (searchCriteria.Limit is > 0 && searchCriteria.Offset is > 0)
        {
            var page = (int)(searchCriteria.Offset / searchCriteria.Limit) + 1;
            parameters.Set("page", page.ToString());
        }

        var searchUrl = $"{_settings.BaseUrl.TrimEnd('/')}/torrents.php?{parameters.GetQueryString()}";

        var request = new IndexerRequest(searchUrl, HttpAccept.Html);

        yield return request;
    }

    public Func<IDictionary<string, string>> GetCookies { get; set; }
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class LibbleParser : IParseIndexerResponse
{
    private readonly LibbleSettings _settings;
    private static Regex ReleaseYearRegex => new (@"\[(\d{4})\]$", RegexOptions.Compiled);

    public LibbleParser(LibbleSettings settings)
    {
        _settings = settings;
    }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        var releaseInfos = new List<ReleaseInfo>();

        var parser = new HtmlParser();
        using var doc = parser.ParseDocument(indexerResponse.Content);

        var groups = doc.QuerySelectorAll("table#torrent_table > tbody > tr.group:has(strong > a[href*=\"torrents.php?id=\"])");
        foreach (var group in groups)
        {
            var albumLinkNode = group.QuerySelector("strong > a[href*=\"torrents.php?id=\"]");
            var groupId = ParseUtil.GetArgumentFromQueryString(albumLinkNode.GetAttribute("href"), "id");

            var artistsNodes = group.QuerySelectorAll("strong > a[href*=\"artist.php?id=\"]");

            var releaseArtist = "Various Artists";
            if (artistsNodes.Any())
            {
                releaseArtist = artistsNodes.Select(artist => artist.TextContent.Trim()).ToList().Join(", ");
            }

            var releaseAlbumName = group.QuerySelector("strong > a[href*=\"torrents.php?id=\"]")?.TextContent.Trim();

            var title = group.QuerySelector("td:nth-child(4) > strong")?.TextContent.Trim();
            var releaseAlbumYear = ReleaseYearRegex.Match(title);

            var releaseDescription = group.QuerySelector("div.tags")?.TextContent.Trim();
            var releaseThumbnailUrl = group.QuerySelector(".thumbnail")?.GetAttribute("title")?.Trim();

            var releaseGenres = new List<string>();
            if (!string.IsNullOrEmpty(releaseDescription))
            {
                releaseGenres = releaseDescription.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            var rows = doc.QuerySelectorAll($"table#torrent_table > tbody > tr.group_torrent.groupid_{groupId}:has(a[href*=\"torrents.php?id=\"])");
            foreach (var row in rows)
            {
                var detailsNode = row.QuerySelector("a[href^=\"torrents.php?id=\"]");

                var infoUrl = _settings.BaseUrl + detailsNode.GetAttribute("href").Trim();
                var downloadLink = _settings.BaseUrl + row.QuerySelector("a[href^=\"torrents.php?action=download&id=\"]").GetAttribute("href").Trim();

                var releaseTags = detailsNode.FirstChild?.TextContent.Trim(' ', '/');
                var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(6)").TextContent);

                var release = new TorrentInfo
                {
                    Guid = infoUrl,
                    InfoUrl = infoUrl,
                    DownloadUrl = downloadLink,
                    Title = $"{releaseArtist} - {releaseAlbumName} {releaseAlbumYear.Value} {releaseTags}".Trim(' ', '-'),
                    Artist = releaseArtist,
                    Album = releaseAlbumName,
                    Categories = ParseCategories(group),
                    Description = releaseDescription,
                    Size = ParseUtil.GetBytes(row.QuerySelector("td:nth-child(4)").TextContent.Trim()),
                    Files = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(2)").TextContent),
                    Grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(5)").TextContent),
                    Seeders = seeders,
                    Peers = seeders + ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(7)").TextContent),
                    DownloadVolumeFactor = 1,
                    UploadVolumeFactor = 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 259200, // 72 hours,
                    Genres = releaseGenres,
                    PosterUrl = releaseThumbnailUrl,
                };

                try
                {
                    var dateAdded = row.QuerySelector("td:nth-child(3) > span[title]").GetAttribute("title").Trim();
                    release.PublishDate = DateTime.ParseExact(dateAdded, "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                }
                catch (Exception)
                {
                    release.PublishDate = DateTimeUtil.FromTimeAgo(row.QuerySelector("td:nth-child(3)")?.TextContent.Trim());
                }

                switch (row.QuerySelector("a[href^=\"torrents.php?id=\"] strong")?.TextContent.ToLower().Trim(' ', '!'))
                {
                    case "neutral":
                        release.DownloadVolumeFactor = 0;
                        release.UploadVolumeFactor = 0;
                        break;
                    case "freeleech":
                        release.DownloadVolumeFactor = 0;
                        release.UploadVolumeFactor = 1;
                        break;
                }

                releaseInfos.Add(release);
            }
        }

        return releaseInfos.ToArray();
    }

    private IList<IndexerCategory> ParseCategories(IElement group)
    {
        var cat = group.QuerySelector("td.cats_col div.cat_icon")?.GetAttribute("class")?.Trim();

        var matchCategory = Regex.Match(cat, @"\bcats_(.*?)\b");
        if (matchCategory.Success)
        {
            cat = matchCategory.Groups[1].Value.Trim();
        }

        return new List<IndexerCategory>
        {
            cat switch
            {
                "music" => NewznabStandardCategory.Audio,
                "libblemixtapes" => NewznabStandardCategory.Audio,
                "musicvideos" => NewznabStandardCategory.AudioVideo,
                _ => NewznabStandardCategory.Other,
            }
        };
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class LibbleSettings : UserPassTorrentBaseSettings
{
    public LibbleSettings()
    {
        TwoFactorAuthCode = "";
    }

    [FieldDefinition(4, Label = "2FA code", Type = FieldType.Textbox, HelpText = "Only fill in the <b>2FA code</b> box if you have enabled <b>2FA</b> on the Libble Web Site. Otherwise just leave it empty.")]
    public string TwoFactorAuthCode { get; set; }
}
