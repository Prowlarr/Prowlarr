using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions;

public class AudioBookBay : TorrentIndexerBase<NoAuthTorrentBaseSettings>
{
    public override string Name => "AudioBook Bay";
    public override string[] IndexerUrls => new[]
    {
        "https://audiobookbay.li/",
        "https://audiobookbay.se/"
    };
    public override string[] LegacyUrls => new[]
    {
        "https://audiobookbay.la/",
        "http://audiobookbay.net/",
        "https://audiobookbay.unblockit.tv/",
        "http://audiobookbay.nl/",
        "http://audiobookbay.ws/",
        "https://audiobookbay.unblockit.how/",
        "https://audiobookbay.unblockit.cam/",
        "https://audiobookbay.unblockit.biz/",
        "https://audiobookbay.unblockit.day/",
        "https://audiobookbay.unblockit.llc/",
        "https://audiobookbay.unblockit.blue/",
        "https://audiobookbay.unblockit.name/",
        "http://audiobookbay.fi/",
        "http://audiobookbay.se/",
        "http://audiobookbayabb.com/",
        "https://audiobookbay.unblockit.ist/",
        "https://audiobookbay.unblockit.bet/",
        "https://audiobookbay.unblockit.cat/",
        "https://audiobookbay.unblockit.nz/",
        "https://audiobookbay.fi/",
        "https://audiobookbay.unblockit.page/",
        "https://audiobookbay.unblockit.pet/",
        "https://audiobookbay.unblockit.ink/",
        "https://audiobookbay.unblockit.bio/" // error 502
    };
    public override string Description => "AudioBook Bay (ABB) is a public Torrent Tracker for AUDIOBOOKS";
    public override string Language => "en-US";
    public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
    public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
    public override int PageSize => 15;
    public override IndexerCapabilities Capabilities => SetCapabilities();

    public AudioBookBay(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new AudioBookBayRequestGenerator(Settings, Capabilities);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new AudioBookBayParser(Settings, Capabilities.Categories);
    }

    public override async Task<byte[]> Download(Uri link)
    {
        var request = new HttpRequestBuilder(link.ToString())
            .SetCookies(GetCookies() ?? new Dictionary<string, string>())
            .Accept(HttpAccept.Html)
            .Build();

        var response = await _httpClient.ExecuteProxiedAsync(request, Definition);

        var parser = new HtmlParser();
        var dom = parser.ParseDocument(response.Content);

        var hash = dom.QuerySelector("td:contains(\"Info Hash:\") ~ td")?.TextContent.Trim();
        if (hash == null)
        {
            throw new Exception($"Failed to fetch hash from {link}");
        }

        var title = dom.QuerySelector("div.postTitle h1")?.TextContent.Trim();
        if (title == null)
        {
            throw new Exception($"Failed to fetch title from {link}");
        }

        title = StringUtil.MakeValidFileName(title, '_', false);

        var magnet = MagnetLinkBuilder.BuildPublicMagnetLink(hash, title);

        return await base.Download(new Uri(magnet));
    }

    private IndexerCapabilities SetCapabilities()
    {
        var caps = new IndexerCapabilities
        {
            BookSearchParams = new List<BookSearchParam>
            {
                BookSearchParam.Q
            }
        };

        // Age
        caps.Categories.AddCategoryMapping("children", NewznabStandardCategory.AudioAudiobook, "Children");
        caps.Categories.AddCategoryMapping("teen-young-adult", NewznabStandardCategory.AudioAudiobook, "Teen & Young Adult");
        caps.Categories.AddCategoryMapping("adults", NewznabStandardCategory.AudioAudiobook, "Adults");

        // Category
        caps.Categories.AddCategoryMapping("postapocalyptic", NewznabStandardCategory.AudioAudiobook, "(Post)apocalyptic");
        caps.Categories.AddCategoryMapping("action", NewznabStandardCategory.AudioAudiobook, "Action");
        caps.Categories.AddCategoryMapping("adventure", NewznabStandardCategory.AudioAudiobook, "Adventure");
        caps.Categories.AddCategoryMapping("art", NewznabStandardCategory.AudioAudiobook, "Art");
        caps.Categories.AddCategoryMapping("autobiography-biographies", NewznabStandardCategory.AudioAudiobook, "Autobiography & Biographies");
        caps.Categories.AddCategoryMapping("business", NewznabStandardCategory.AudioAudiobook, "Business");
        caps.Categories.AddCategoryMapping("computer", NewznabStandardCategory.AudioAudiobook, "Computer");
        caps.Categories.AddCategoryMapping("contemporary", NewznabStandardCategory.AudioAudiobook, "Contemporary");
        caps.Categories.AddCategoryMapping("crime", NewznabStandardCategory.AudioAudiobook, "Crime");
        caps.Categories.AddCategoryMapping("detective", NewznabStandardCategory.AudioAudiobook, "Detective");
        caps.Categories.AddCategoryMapping("doctor-who-sci-fi", NewznabStandardCategory.AudioAudiobook, "Doctor Who");
        caps.Categories.AddCategoryMapping("education", NewznabStandardCategory.AudioAudiobook, "Education");
        caps.Categories.AddCategoryMapping("fantasy", NewznabStandardCategory.AudioAudiobook, "Fantasy");
        caps.Categories.AddCategoryMapping("general-fiction", NewznabStandardCategory.AudioAudiobook, "General Fiction");
        caps.Categories.AddCategoryMapping("historical-fiction", NewznabStandardCategory.AudioAudiobook, "Historical Fiction");
        caps.Categories.AddCategoryMapping("history", NewznabStandardCategory.AudioAudiobook, "History");
        caps.Categories.AddCategoryMapping("horror", NewznabStandardCategory.AudioAudiobook, "Horror");
        caps.Categories.AddCategoryMapping("humor", NewznabStandardCategory.AudioAudiobook, "Humor");
        caps.Categories.AddCategoryMapping("lecture", NewznabStandardCategory.AudioAudiobook, "Lecture");
        caps.Categories.AddCategoryMapping("lgbt", NewznabStandardCategory.AudioAudiobook, "LGBT");
        caps.Categories.AddCategoryMapping("literature", NewznabStandardCategory.AudioAudiobook, "Literature");
        caps.Categories.AddCategoryMapping("litrpg", NewznabStandardCategory.AudioAudiobook, "LitRPG");
        caps.Categories.AddCategoryMapping("general-non-fiction", NewznabStandardCategory.AudioAudiobook, "Misc. Non-fiction");
        caps.Categories.AddCategoryMapping("mystery", NewznabStandardCategory.AudioAudiobook, "Mystery");
        caps.Categories.AddCategoryMapping("paranormal", NewznabStandardCategory.AudioAudiobook, "Paranormal");
        caps.Categories.AddCategoryMapping("plays-theater", NewznabStandardCategory.AudioAudiobook, "Plays & Theater");
        caps.Categories.AddCategoryMapping("poetry", NewznabStandardCategory.AudioAudiobook, "Poetry");
        caps.Categories.AddCategoryMapping("political", NewznabStandardCategory.AudioAudiobook, "Political");
        caps.Categories.AddCategoryMapping("radio-productions", NewznabStandardCategory.AudioAudiobook, "Radio Productions");
        caps.Categories.AddCategoryMapping("romance", NewznabStandardCategory.AudioAudiobook, "Romance");
        caps.Categories.AddCategoryMapping("sci-fi", NewznabStandardCategory.AudioAudiobook, "Sci-Fi");
        caps.Categories.AddCategoryMapping("science", NewznabStandardCategory.AudioAudiobook, "Science");
        caps.Categories.AddCategoryMapping("self-help", NewznabStandardCategory.AudioAudiobook, "Self-help");
        caps.Categories.AddCategoryMapping("spiritual", NewznabStandardCategory.AudioAudiobook, "Spiritual & Religious");
        caps.Categories.AddCategoryMapping("sports", NewznabStandardCategory.AudioAudiobook, "Sport & Recreation");
        caps.Categories.AddCategoryMapping("suspense", NewznabStandardCategory.AudioAudiobook, "Suspense");
        caps.Categories.AddCategoryMapping("thriller", NewznabStandardCategory.AudioAudiobook, "Thriller");
        caps.Categories.AddCategoryMapping("true-crime", NewznabStandardCategory.AudioAudiobook, "True Crime");
        caps.Categories.AddCategoryMapping("tutorial", NewznabStandardCategory.AudioAudiobook, "Tutorial");
        caps.Categories.AddCategoryMapping("westerns", NewznabStandardCategory.AudioAudiobook, "Westerns");
        caps.Categories.AddCategoryMapping("zombies", NewznabStandardCategory.AudioAudiobook, "Zombies");

        // Category Modifiers
        caps.Categories.AddCategoryMapping("anthology", NewznabStandardCategory.AudioAudiobook, "Anthology");
        caps.Categories.AddCategoryMapping("bestsellers", NewznabStandardCategory.AudioAudiobook, "Bestsellers");
        caps.Categories.AddCategoryMapping("classic", NewznabStandardCategory.AudioAudiobook, "Classic");
        caps.Categories.AddCategoryMapping("documentary", NewznabStandardCategory.AudioAudiobook, "Documentary");
        caps.Categories.AddCategoryMapping("full-cast", NewznabStandardCategory.AudioAudiobook, "Full Cast");
        caps.Categories.AddCategoryMapping("libertarian", NewznabStandardCategory.AudioAudiobook, "Libertarian");
        caps.Categories.AddCategoryMapping("military", NewznabStandardCategory.AudioAudiobook, "Military");
        caps.Categories.AddCategoryMapping("novel", NewznabStandardCategory.AudioAudiobook, "Novel");
        caps.Categories.AddCategoryMapping("short-story", NewznabStandardCategory.AudioAudiobook, "Short Story");

        return caps;
    }
}

public class AudioBookBayRequestGenerator : IIndexerRequestGenerator
{
    private readonly NoAuthTorrentBaseSettings _settings;
    private readonly IndexerCapabilities _capabilities;

    public AudioBookBayRequestGenerator(NoAuthTorrentBaseSettings settings, IndexerCapabilities capabilities)
    {
        _settings = settings;
        _capabilities = capabilities;
    }

    public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        return new IndexerPageableRequestChain();
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
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}"));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}"));

        return pageableRequests;
    }

    private IEnumerable<IndexerRequest> GetPagedRequests(string term)
    {
        var searchUrl = _settings.BaseUrl;

        var parameters = new NameValueCollection();

        term = Regex.Replace(term, @"[\W]+", " ").Trim();

        if (term.IsNotNullOrWhiteSpace())
        {
            parameters.Set("s", term);
            parameters.Set("tt", "1");
        }

        if (parameters.Count > 0)
        {
            searchUrl += $"?{parameters.GetQueryString()}";
        }

        yield return new IndexerRequest(new UriBuilder(searchUrl) { Path = "/" }.Uri.AbsoluteUri, HttpAccept.Html);
        yield return new IndexerRequest(new UriBuilder(searchUrl) { Path = "/page/2/" }.Uri.AbsoluteUri, HttpAccept.Html);
        yield return new IndexerRequest(new UriBuilder(searchUrl) { Path = "/page/3/" }.Uri.AbsoluteUri, HttpAccept.Html);
    }

    public Func<IDictionary<string, string>> GetCookies { get; set; }
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class AudioBookBayParser : IParseIndexerResponse
{
    private readonly NoAuthTorrentBaseSettings _settings;
    private readonly IndexerCapabilitiesCategories _categories;

    public AudioBookBayParser(NoAuthTorrentBaseSettings settings, IndexerCapabilitiesCategories categories)
    {
        _settings = settings;
        _categories = categories;
    }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        var releaseInfos = new List<ReleaseInfo>();

        var doc = ParseHtmlDocument(indexerResponse.Content);

        var rows = doc.QuerySelectorAll("div.post:has(div[class=\"postTitle\"])");
        foreach (var row in rows)
        {
            var infoUrl = _settings.BaseUrl + row.QuerySelector("div.postTitle h2 a")?.GetAttribute("href")?.Trim().TrimStart('/');

            var title = row.QuerySelector("div.postTitle")?.TextContent.Trim();

            var infoString = row.QuerySelector("div.postContent")?.TextContent.Trim() ?? string.Empty;

            var matchFormat = Regex.Match(infoString, @"Format: (.+) \/", RegexOptions.IgnoreCase);
            if (matchFormat.Groups[1].Success && matchFormat.Groups[1].Value.Length > 0 && matchFormat.Groups[1].Value != "?")
            {
                title += $" [{matchFormat.Groups[1].Value.Trim()}]";
            }

            var matchBitrate = Regex.Match(infoString, @"Bitrate: (.+)File", RegexOptions.IgnoreCase);
            if (matchBitrate.Groups[1].Success && matchBitrate.Groups[1].Value.Length > 0 && matchBitrate.Groups[1].Value != "?")
            {
                title += $" [{matchBitrate.Groups[1].Value.Trim()}]";
            }

            var matchSize = Regex.Match(infoString, @"File Size: (.+?)s?$", RegexOptions.IgnoreCase);
            var size = matchSize.Groups[1].Success ? ParseUtil.GetBytes(matchSize.Groups[1].Value) : 0;

            var matchDateAdded = Regex.Match(infoString, @"Posted: (\d{1,2} \D{3} \d{4})", RegexOptions.IgnoreCase);
            var publishDate = matchDateAdded.Groups[1].Success && DateTime.TryParseExact(matchDateAdded.Groups[1].Value, "d MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedDate) ? parsedDate : DateTime.Now;

            var postInfo = row.QuerySelector("div.postInfo")?.FirstChild?.TextContent.Trim().Replace("\xA0", ";") ?? string.Empty;
            var matchCategory = Regex.Match(postInfo, @"Category: (.+)$", RegexOptions.IgnoreCase);
            var category = matchCategory.Groups[1].Success ? matchCategory.Groups[1].Value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList() : new List<string>();
            var categories = category.SelectMany(_categories.MapTrackerCatDescToNewznab).Distinct().ToList();

            var release = new TorrentInfo
            {
                Guid = infoUrl,
                InfoUrl = infoUrl,
                DownloadUrl = infoUrl,
                Title = CleanTitle(title),
                Categories = categories,
                Size = size,
                Seeders = 1,
                Peers = 1,
                PublishDate = publishDate,
                DownloadVolumeFactor = 0,
                UploadVolumeFactor = 1
            };

            var cover = row.QuerySelector("img[src]")?.GetAttribute("src")?.Trim();
            if (!string.IsNullOrEmpty(cover))
            {
                release.PosterUrl = cover.StartsWith("http") ? cover : _settings.BaseUrl + cover;
            }

            releaseInfos.Add(release);
        }

        return releaseInfos;
    }

    private static IHtmlDocument ParseHtmlDocument(string response)
    {
        var parser = new HtmlParser();
        var doc = parser.ParseDocument(response);

        var hidden = doc.QuerySelectorAll("div.post.re-ab");
        foreach (var element in hidden)
        {
            var body = doc.CreateElement("div");
            body.ClassList.Add("post");
            body.InnerHtml = Encoding.UTF8.GetString(Convert.FromBase64String(element.TextContent));
            element.Parent.ReplaceChild(body, element);
        }

        return doc;
    }

    private static string CleanTitle(string title)
    {
        title = Regex.Replace(title, @"[\u0000-\u0008\u000A-\u001F\u0100-\uFFFF]", string.Empty, RegexOptions.Compiled);
        title = Regex.Replace(title, @"\s+", " ", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        return title.Trim();
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}
