using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

public class PreToMe : TorrentIndexerBase<PreToMeSettings>
{
    public override string Name => "PreToMe";
    public override string[] IndexerUrls => new[] { "https://pretome.info/" };
    public override string Description => "PreToMe is a ratioless 0Day/General tracker.";
    public override string Language => "en-US";
    public override Encoding Encoding => Encoding.GetEncoding("iso-8859-1");
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
    public override IndexerCapabilities Capabilities => SetCapabilities();

    public PreToMe(IIndexerHttpClient httpClient,
                   IEventAggregator eventAggregator,
                   IIndexerStatusService indexerStatusService,
                   IConfigService configService,
                   Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new PreToMeRequestGenerator(Settings, Capabilities);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new PreToMeParser(Settings, Capabilities.Categories);
    }

    protected override async Task DoLogin()
    {
        UpdateCookies(null, null);

        var loginPage = await ExecuteAuth(new HttpRequest(Settings.BaseUrl + "login.php"));

        var loginUrl = Settings.BaseUrl + "takelogin.php";
        var requestBuilder = new HttpRequestBuilder(loginUrl)
        {
            LogResponseContent = true,
            AllowAutoRedirect = true,
            Method = HttpMethod.Post
        };

        var authLoginRequest = requestBuilder
            .SetCookies(loginPage.GetCookies())
            .AddFormParameter("username", Settings.Username)
            .AddFormParameter("password", Settings.Password)
            .AddFormParameter("login_pin", Settings.Pin)
            .AddFormParameter("returnto", "%2F")
            .AddFormParameter("login", "Login")
            .SetHeader("Content-Type", "application/x-www-form-urlencoded")
            .Build();

        var response = await ExecuteAuth(authLoginRequest);

        if (response.Content == null)
        {
            throw new IndexerAuthException("Authentication failed. Reason: empty response.");
        }

        if (CheckIfLoginNeeded(response))
        {
            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(response.Content);
            var errorMessage = dom.QuerySelector("table.body_table font[color~=\"red\"]")?.TextContent.Trim();

            throw new IndexerAuthException(errorMessage ?? "Unknown error message, please report.");
        }

        UpdateCookies(response.GetCookies(), DateTime.Now.AddDays(30));

        _logger.Debug("Authentication succeeded");
    }

    protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
    {
        return httpResponse.HasHttpRedirect || !httpResponse.Content.Contains("logout.php");
    }

    private IndexerCapabilities SetCapabilities()
    {
        var caps = new IndexerCapabilities
        {
            TvSearchParams = new List<TvSearchParam>
            {
                TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
            },
            MovieSearchParams = new List<MovieSearchParam>
            {
                MovieSearchParam.Q, MovieSearchParam.ImdbId
            },
            MusicSearchParams = new List<MusicSearchParam>
            {
                MusicSearchParam.Q
            },
            BookSearchParams = new List<BookSearchParam>
            {
                BookSearchParam.Q
            }
        };

        caps.Categories.AddCategoryMapping("cat[]=22", NewznabStandardCategory.PC, "Applications");
        caps.Categories.AddCategoryMapping("cat[]=22&tags=Windows", NewznabStandardCategory.PC0day, "Applications/Windows");
        caps.Categories.AddCategoryMapping("cat[]=22&tags=MAC", NewznabStandardCategory.PCMac, "Applications/MAC");
        caps.Categories.AddCategoryMapping("cat[]=22&tags=Linux", NewznabStandardCategory.PC, "Applications/Linux");

        caps.Categories.AddCategoryMapping("cat[]=27", NewznabStandardCategory.BooksEBook, "Ebooks");

        caps.Categories.AddCategoryMapping("cat[]=4", NewznabStandardCategory.Console, "Games");
        caps.Categories.AddCategoryMapping("cat[]=4&tags=PC", NewznabStandardCategory.PCGames, "Games/PC");
        caps.Categories.AddCategoryMapping("cat[]=4&tags=RIP", NewznabStandardCategory.PCGames, "Games/RIP");
        caps.Categories.AddCategoryMapping("cat[]=4&tags=ISO", NewznabStandardCategory.PCGames, "Games/ISO");
        caps.Categories.AddCategoryMapping("cat[]=4&tags=XBOX360", NewznabStandardCategory.ConsoleXBox360, "Games/XBOX360");
        caps.Categories.AddCategoryMapping("cat[]=4&tags=PS3", NewznabStandardCategory.ConsolePS3, "Games/PS3");
        caps.Categories.AddCategoryMapping("cat[]=4&tags=Wii", NewznabStandardCategory.ConsoleWii, "Games/Wii");
        caps.Categories.AddCategoryMapping("cat[]=4&tags=PSP", NewznabStandardCategory.ConsolePSP, "Games/PSP");
        caps.Categories.AddCategoryMapping("cat[]=4&tags=GAMES-NSW", NewznabStandardCategory.ConsoleOther, "Games/NSW");
        caps.Categories.AddCategoryMapping("cat[]=4&tags=NDS", NewznabStandardCategory.ConsoleNDS, "Games/NDS");
        caps.Categories.AddCategoryMapping("cat[]=4&tags=Xbox", NewznabStandardCategory.ConsoleXBox, "Games/Xbox");
        caps.Categories.AddCategoryMapping("cat[]=4&tags=NSW", NewznabStandardCategory.ConsoleOther, "Games/NSW");
        caps.Categories.AddCategoryMapping("cat[]=4&tags=PS2", NewznabStandardCategory.ConsoleOther, "Games/PS2");

        caps.Categories.AddCategoryMapping("cat[]=31", NewznabStandardCategory.Other, "Miscellaneous");
        caps.Categories.AddCategoryMapping("cat[]=31&tags=Ebook", NewznabStandardCategory.BooksEBook, "Miscellaneous/Ebook");
        caps.Categories.AddCategoryMapping("cat[]=31&tags=RARFiX", NewznabStandardCategory.Other, "Miscellaneous/RARFiX");

        caps.Categories.AddCategoryMapping("cat[]=19", NewznabStandardCategory.Movies, "Movies");
        caps.Categories.AddCategoryMapping("cat[]=19&tags=x264", NewznabStandardCategory.Movies, "Movies/x264");
        caps.Categories.AddCategoryMapping("cat[]=19&tags=720p", NewznabStandardCategory.MoviesHD, "Movies/720p");
        caps.Categories.AddCategoryMapping("cat[]=19&tags=XviD", NewznabStandardCategory.MoviesSD, "Movies/XviD");
        caps.Categories.AddCategoryMapping("cat[]=19&tags=BluRay", NewznabStandardCategory.MoviesHD, "Movies/BluRay");
        caps.Categories.AddCategoryMapping("cat[]=19&tags=DVDRiP", NewznabStandardCategory.MoviesSD, "Movies/DVDRiP");
        caps.Categories.AddCategoryMapping("cat[]=19&tags=1080p", NewznabStandardCategory.MoviesHD, "Movies/1080p");
        caps.Categories.AddCategoryMapping("cat[]=19&tags=DVD", NewznabStandardCategory.MoviesSD, "Movies/DVD");
        caps.Categories.AddCategoryMapping("cat[]=19&tags=DVDR", NewznabStandardCategory.MoviesSD, "Movies/DVDR");
        caps.Categories.AddCategoryMapping("cat[]=19&tags=WMV", NewznabStandardCategory.Movies, "Movies/WMV");
        caps.Categories.AddCategoryMapping("cat[]=19&tags=CAM", NewznabStandardCategory.Movies, "Movies/CAM");

        caps.Categories.AddCategoryMapping("cat[]=6", NewznabStandardCategory.Audio, "Music");
        caps.Categories.AddCategoryMapping("cat[]=6&tags=MP3", NewznabStandardCategory.AudioMP3, "Music/MP3");
        caps.Categories.AddCategoryMapping("cat[]=6&tags=V2", NewznabStandardCategory.AudioMP3, "Music/V2");
        caps.Categories.AddCategoryMapping("cat[]=6&tags=FLAC", NewznabStandardCategory.AudioLossless, "Music/FLAC");
        caps.Categories.AddCategoryMapping("cat[]=6&tags=320kbps", NewznabStandardCategory.AudioMP3, "Music/320kbps");

        caps.Categories.AddCategoryMapping("cat[]=7", NewznabStandardCategory.TV, "TV");
        caps.Categories.AddCategoryMapping("cat[]=7&tags=x264", NewznabStandardCategory.TVHD, "TV/x264");
        caps.Categories.AddCategoryMapping("cat[]=7&tags=720p", NewznabStandardCategory.TVHD, "TV/720p");
        caps.Categories.AddCategoryMapping("cat[]=7&tags=HDTV", NewznabStandardCategory.TVHD, "TV/HDTV");
        caps.Categories.AddCategoryMapping("cat[]=7&tags=XviD", NewznabStandardCategory.TVSD, "TV/XviD");
        caps.Categories.AddCategoryMapping("cat[]=7&tags=BluRay", NewznabStandardCategory.TVHD, "TV/BluRay");
        caps.Categories.AddCategoryMapping("cat[]=7&tags=DVDRiP", NewznabStandardCategory.TVSD, "TV/DVDRiP");
        caps.Categories.AddCategoryMapping("cat[]=7&tags=DVD", NewznabStandardCategory.TVSD, "TV/DVD");
        caps.Categories.AddCategoryMapping("cat[]=7&tags=Documentary", NewznabStandardCategory.TVDocumentary, "TV/Documentary");
        caps.Categories.AddCategoryMapping("cat[]=7&tags=PDTV", NewznabStandardCategory.TVSD, "TV/PDTV");
        caps.Categories.AddCategoryMapping("cat[]=7&tags=HD-DVD", NewznabStandardCategory.TVSD, "TV/HD-DVD");

        caps.Categories.AddCategoryMapping("cat[]=51", NewznabStandardCategory.XXX, "XXX");
        caps.Categories.AddCategoryMapping("cat[]=51&tags=XviD", NewznabStandardCategory.XXXXviD, "XXX/XviD");
        caps.Categories.AddCategoryMapping("cat[]=51&tags=DVDRiP", NewznabStandardCategory.XXXDVD, "XXX/DVDRiP");

        return caps;
    }
}

public class PreToMeRequestGenerator : IIndexerRequestGenerator
{
    private readonly PreToMeSettings _settings;
    private readonly IndexerCapabilities _capabilities;

    public PreToMeRequestGenerator(PreToMeSettings settings, IndexerCapabilities capabilities)
    {
        _settings = settings;
        _capabilities = capabilities;
    }

    private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
    {
        // NameValueCollection don't support cat[]=19&cat[]=6
        var parameters = new List<KeyValuePair<string, string>>
        {
            { "st", "1" } // search in title
        };

        if (imdbId.IsNotNullOrWhiteSpace())
        {
            parameters.Add("search", imdbId);
            parameters.Add("sd", "1"); // search in description
        }
        else
        {
            parameters.Add("search", term);
        }

        // parse categories and tags
        var catGroups = new HashSet<string>();
        var tagGroups = new HashSet<string>();

        var cats = _capabilities.Categories.MapTorznabCapsToTrackers(categories);
        foreach (var cat in cats)
        {
            // "cat[]=7&tags=x264"
            var cSplit = cat.Split('&');

            var gSplit = cSplit[0].Split('=');
            if (gSplit.Length > 1)
            {
                catGroups.Add(gSplit[1]); // category = 7
            }

            if (cSplit.Length > 1)
            {
                var tSplit = cSplit[1].Split('=');
                if (tSplit.Length > 1)
                {
                    tagGroups.Add(tSplit[1]); // tag = x264
                }
            }
        }

        // add categories
        foreach (var cat in catGroups)
        {
            parameters.Add("cat[]", cat);
        }

        // do not include too many tags as it'll mess with their servers
        if (tagGroups.Count < 7)
        {
            parameters.Add("tags", string.Join(",", tagGroups));

            // if tags are specified match any
            // if no tags are specified match all, with any we get random results
            parameters.Add("tf", tagGroups.Any() ? "any" : "all");
        }

        var searchUrl = $"{_settings.BaseUrl}browse.php";

        if (parameters.Count > 0)
        {
            searchUrl += $"?{parameters.GetQueryString()}";
        }

        var request = new IndexerRequest(searchUrl, HttpAccept.Html)
        {
            HttpRequest =
            {
                AllowAutoRedirect = false
            }
        };

        yield return request;
    }

    public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories, searchCriteria.FullImdbId));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedTvSearchString}", searchCriteria.Categories, searchCriteria.FullImdbId));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories));

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories));

        return pageableRequests;
    }

    public Func<IDictionary<string, string>> GetCookies { get; set; }
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class PreToMeParser : IParseIndexerResponse
{
    private readonly PreToMeSettings _settings;
    private readonly IndexerCapabilitiesCategories _categories;

    public PreToMeParser(PreToMeSettings settings, IndexerCapabilitiesCategories categories)
    {
        _settings = settings;
        _categories = categories;
    }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        var releaseInfos = new List<ReleaseInfo>();

        var parser = new HtmlParser();
        using var dom = parser.ParseDocument(indexerResponse.Content);

        var rows = dom.QuerySelectorAll("table > tbody > tr.browse");
        foreach (var row in rows)
        {
            var qDetails = row.QuerySelector("a[href^=\"details.php?id=\"]");
            var title = qDetails?.GetAttribute("title");

            var infoUrl = _settings.BaseUrl + qDetails.GetAttribute("href");
            var downloadUrl = _settings.BaseUrl + row.QuerySelector("a[href^=\"download.php\"]")?.GetAttribute("href");

            var dateAdded = Regex.Replace(row.QuerySelector("td:nth-of-type(6)").InnerHtml, @"\<br[\s]{0,1}[\/]{0,1}\>", " ").Trim();

            var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(10)")?.TextContent);
            var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(11)")?.TextContent);

            var cat = row.QuerySelector("td:nth-of-type(1) a[href^=\"browse.php\"]")?.GetAttribute("href")?.Split('?').Last();

            var release = new TorrentInfo
            {
                Guid = infoUrl,
                InfoUrl = infoUrl,
                DownloadUrl = downloadUrl,
                Title = title,
                Categories = _categories.MapTrackerCatToNewznab(cat),
                PublishDate = DateTimeUtil.FromTimeAgo(dateAdded),
                Size = ParseUtil.GetBytes(row.QuerySelector("td:nth-of-type(8)")?.TextContent),
                Files = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(4)")?.TextContent),
                Grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-of-type(9)")?.TextContent),
                Seeders = seeders,
                Peers = leechers + seeders,
                DownloadVolumeFactor = 0, // ratioless
                UploadVolumeFactor = 1,
                MinimumRatio = 0.75,
                MinimumSeedTime = 216000 // 60 hours
            };

            releaseInfos.Add(release);
        }

        return releaseInfos.ToArray();
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class PreToMeSettingsValidator : UserPassBaseSettingsValidator<PreToMeSettings>
{
    public PreToMeSettingsValidator()
    {
        RuleFor(c => c.Pin).NotEmpty();
    }
}

public class PreToMeSettings : UserPassTorrentBaseSettings
{
    private static readonly PreToMeSettingsValidator Validator = new ();

    [FieldDefinition(4, Label = "Pin", HelpText = "Site Pin", Privacy = PrivacyLevel.Password)]
    public string Pin { get; set; }

    public override NzbDroneValidationResult Validate()
    {
        return new NzbDroneValidationResult(Validator.Validate(this));
    }
}
