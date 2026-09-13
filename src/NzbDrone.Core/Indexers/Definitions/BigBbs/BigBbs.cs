using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using AngleSharp.Html.Parser;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions.BigBbs;

public class BigBbs : TorrentIndexerBase<BigBbsSettings>
{
    public override string Name => "BigBBS";
    public override string[] IndexerUrls => ["https://bigbbs.eu/"];
    public override string Description => "BigBBS is a POLISH Private Torrent Tracker for MOVIES / TV / GENERAL";
    public override string Language => "pl-PL";
    public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
    public override IndexerCapabilities Capabilities => SetCapabilities();
    private string LoginUrl => Settings.BaseUrl + "?p=home&pid=1";

    public BigBbs(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new BigBbsRequestGenerator(Settings, Capabilities);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new BigBbsParser(Settings, Capabilities.Categories);
    }

    protected override async Task DoLogin()
    {
        Cookies = null;

        var loginPage = await ExecuteAuth(new HttpRequest(LoginUrl));
        var loginPageCookies = loginPage.GetCookies();

        var securityToken = await ExtractSecurityTokenAsync(loginPage.Content);

        if (securityToken.IsNullOrWhiteSpace())
        {
            throw new IndexerAuthException("Could not find security token");
        }

        var loginUrl = Settings.BaseUrl + "ajax/login.php";

        var requestBuilder = new HttpRequestBuilder(loginUrl)
        {
            LogResponseContent = true,
            Method = HttpMethod.Post
        };

        var authLoginRequest = requestBuilder
            .AddFormParameter("action", "login")
            .AddFormParameter("loginbox_membername", Settings.Username)
            .AddFormParameter("loginbox_password", Settings.Password)
            .AddFormParameter("loginbox_remember", "1")
            .AddFormParameter("securitytoken", securityToken)
            .SetCookies(loginPageCookies)
            .SetHeader("Content-Type", "application/x-www-form-urlencoded")
            .SetHeader("Referer", loginUrl)
            .Build();

        var response = await ExecuteAuth(authLoginRequest);

        if (CheckIfLoginNeeded(response))
        {
            throw new IndexerAuthException("Invalid username or password");
        }

        var cookies = response.GetCookies();
        if (cookies == null || !cookies.Any())
        {
            cookies = loginPageCookies;
        }

        UpdateCookies(cookies, DateTime.Now.AddDays(30));

        _logger.Debug("BigBbs authentication succeeded.");
    }

    protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
    {
        return httpResponse.Content.Contains("error") || httpResponse.Content.Contains("-ERROR-");
    }

    private static async Task<string> ExtractSecurityTokenAsync(string content)
    {
        if (content.IsNullOrWhiteSpace())
        {
            return string.Empty;
        }

        var parser = new HtmlParser();
        using var dom = await parser.ParseDocumentAsync(content);

        var scripts = dom.QuerySelectorAll("script");

        var securityToken =
            (from script in scripts
             where script.TextContent.Contains("stKey:")
             select Regex.Match(script.TextContent, "stKey: \"(.+?)\",")
                into match
             where match.Success
             select match.Groups[1].Value).FirstOrDefault();

        return securityToken ?? string.Empty;
    }

    public override async Task<IndexerDownloadResponse> Download(Uri link)
    {
        var torrentId = ExtractTorrentIdFromLink(link);

        if (torrentId.IsNotNullOrWhiteSpace())
        {
            await SendThankYouAsync(torrentId);
        }

        return await base.Download(link);
    }

    private static string ExtractTorrentIdFromLink(Uri link)
    {
        if (link.Query.IsNullOrWhiteSpace())
        {
            return null;
        }

        return HttpUtility.ParseQueryString(link.Query).Get("tid");
    }

    private async Task SendThankYouAsync(string torrentId)
    {
        try
        {
            var sessionCookies = GetCookies() ?? new Dictionary<string, string>();

            var tokenPageRequest = new HttpRequestBuilder(LoginUrl)
                .SetCookies(sessionCookies)
                .Build();

            var tokenPage = await ExecuteAuth(tokenPageRequest);
            var securityToken = await ExtractSecurityTokenAsync(tokenPage.Content);

            if (securityToken.IsNullOrWhiteSpace())
            {
                _logger.Warn("Could not retrieve security token for thank you request");
                return;
            }

            var thankUrl = Settings.BaseUrl + "ajax/torrents.php";

            var thankRequest = new HttpRequestBuilder(thankUrl)
                {
                    Method = HttpMethod.Post
                }
                .AddFormParameter("action", "thank")
                .AddFormParameter("tid", torrentId)
                .AddFormParameter("securitytoken", securityToken)
                .SetCookies(sessionCookies)
                .Build();

            var response = await _httpClient.ExecuteProxiedAsync(thankRequest, Definition);

            if (response.Content.Contains("error") || response.Content.Contains("-ERROR-"))
            {
                _logger.Warn("Failed to send thank you for torrent {0}", torrentId);
            }
            else
            {
                _logger.Debug("Thank you sent successfully for torrent {0}", torrentId);
            }
        }
        catch (Exception ex)
        {
            _logger.Warn(ex, "Error sending thank you for torrent {0}", torrentId);
        }
    }

    private IndexerCapabilities SetCapabilities()
    {
        var caps = new IndexerCapabilities
        {
            TvSearchParams =
            [
                TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
            ],
            MovieSearchParams =
            [
                MovieSearchParam.Q
            ],
            MusicSearchParams =
            [
                MusicSearchParam.Q
            ],
            BookSearchParams =
            [
                BookSearchParam.Q
            ]
        };

        // Movies
        caps.Categories.AddCategoryMapping("1", NewznabStandardCategory.Movies, "Filmi");
        caps.Categories.AddCategoryMapping("14", NewznabStandardCategory.MoviesSD, "Filmi DivX / XviD");
        caps.Categories.AddCategoryMapping("41", NewznabStandardCategory.MoviesHD, "Filmi x264");
        caps.Categories.AddCategoryMapping("40", NewznabStandardCategory.Movies, "Filmi Al / Lektor Amatorski");
        caps.Categories.AddCategoryMapping("39", NewznabStandardCategory.MoviesBluRay, "Filmi BluRay");
        caps.Categories.AddCategoryMapping("147", NewznabStandardCategory.Movies, "Filmi Xmas");
        caps.Categories.AddCategoryMapping("37", NewznabStandardCategory.MoviesDVD, "Filmi DVD 5 / 9");
        caps.Categories.AddCategoryMapping("52", NewznabStandardCategory.Movies3D, "Filmi 3D");
        caps.Categories.AddCategoryMapping("17", NewznabStandardCategory.MoviesHD, "Filmi HD 1080p , 720p");
        caps.Categories.AddCategoryMapping("99", NewznabStandardCategory.MoviesHD, "Filmi x265");
        caps.Categories.AddCategoryMapping("62", NewznabStandardCategory.XXX, "Filmi XXX");
        caps.Categories.AddCategoryMapping("16", NewznabStandardCategory.MoviesSD, "Filmi CAM / TS");
        caps.Categories.AddCategoryMapping("65", NewznabStandardCategory.MoviesHD, "Filmi Rmvb");
        caps.Categories.AddCategoryMapping("66", NewznabStandardCategory.Movies, "Filmi TV RIP");
        caps.Categories.AddCategoryMapping("84", NewznabStandardCategory.TVDocumentary, "Filmi Dokumentalne");
        caps.Categories.AddCategoryMapping("100", NewznabStandardCategory.Movies, "Filmi Seriale");
        caps.Categories.AddCategoryMapping("103", NewznabStandardCategory.Movies, "Filmi FILMY GSM / PDA");
        caps.Categories.AddCategoryMapping("107", NewznabStandardCategory.MoviesUHD, "Filmi 4K-UHD");
        caps.Categories.AddCategoryMapping("109", NewznabStandardCategory.Movies, "Filmi Biblijny");
        caps.Categories.AddCategoryMapping("113", NewznabStandardCategory.Movies, "Filmi Prawniczy");
        caps.Categories.AddCategoryMapping("61", NewznabStandardCategory.Movies, "Bajki");
        caps.Categories.AddCategoryMapping("88", NewznabStandardCategory.Movies3D, "Filmi 3D");
        caps.Categories.AddCategoryMapping("108", NewznabStandardCategory.MoviesBluRay, "Filmi BluRay");
        caps.Categories.AddCategoryMapping("89", NewznabStandardCategory.MoviesHD, "Filmi HD x264");
        caps.Categories.AddCategoryMapping("90", NewznabStandardCategory.MoviesHD, "Filmi HD DivX / XviD");
        caps.Categories.AddCategoryMapping("91", NewznabStandardCategory.MoviesDVD, "Filmi DVD 5 / DVD 9");
        caps.Categories.AddCategoryMapping("92", NewznabStandardCategory.MoviesSD, "Filmi SD DivX / XviD");
        caps.Categories.AddCategoryMapping("93", NewznabStandardCategory.MoviesSD, "Filmi SD x264");
        caps.Categories.AddCategoryMapping("96", NewznabStandardCategory.Movies, "Filmi TVRip");
        caps.Categories.AddCategoryMapping("101", NewznabStandardCategory.Movies, "Filmi Boxset");
        caps.Categories.AddCategoryMapping("98", NewznabStandardCategory.Movies, "Filmi Seriale");
        caps.Categories.AddCategoryMapping("95", NewznabStandardCategory.Movies, "Bajki");
        caps.Categories.AddCategoryMapping("97", NewznabStandardCategory.AudioVideo, "Kabarety");
        caps.Categories.AddCategoryMapping("102", NewznabStandardCategory.MoviesHD, "Filmi x265");
        caps.Categories.AddCategoryMapping("104", NewznabStandardCategory.Movies, "Filmi FILMY GSM / PDA");
        caps.Categories.AddCategoryMapping("110", NewznabStandardCategory.Movies, "Filmi Biblijny");
        caps.Categories.AddCategoryMapping("114", NewznabStandardCategory.MoviesUHD, "Filmi 4K-UHD");
        caps.Categories.AddCategoryMapping("115", NewznabStandardCategory.Movies, "Filmi Fan BBRG");
        caps.Categories.AddCategoryMapping("112", NewznabStandardCategory.Movies, "Filmi ENG");

        // Comics & Books
        caps.Categories.AddCategoryMapping("106", NewznabStandardCategory.BooksComics, "Manga");
        caps.Categories.AddCategoryMapping("47", NewznabStandardCategory.BooksEBook, "EEbooki");
        caps.Categories.AddCategoryMapping("50", NewznabStandardCategory.BooksEBook, "Ebook Pdf");
        caps.Categories.AddCategoryMapping("67", NewznabStandardCategory.BooksComics, "Komiksy");

        // Anime
        caps.Categories.AddCategoryMapping("53", NewznabStandardCategory.TVAnime, "Anime");

        // TV
        caps.Categories.AddCategoryMapping("56", NewznabStandardCategory.TV, "TV");
        caps.Categories.AddCategoryMapping("57", NewznabStandardCategory.TV, "TV BOXSETS");
        caps.Categories.AddCategoryMapping("58", NewznabStandardCategory.TV, "TV EPIZODY");

        // Applications
        caps.Categories.AddCategoryMapping("6", NewznabStandardCategory.PC, "Aplikacje");
        caps.Categories.AddCategoryMapping("18", NewznabStandardCategory.PC0day, "Aplikacje Windows");
        caps.Categories.AddCategoryMapping("64", NewznabStandardCategory.PCMobileOther, "Aplikacje GSM/PDA");
        caps.Categories.AddCategoryMapping("19", NewznabStandardCategory.PCMac, "Aplikacje Macintosh");
        caps.Categories.AddCategoryMapping("42", NewznabStandardCategory.PC, "Aplikacje Linux");

        // Sport
        caps.Categories.AddCategoryMapping("63", NewznabStandardCategory.TVSport, "Sport");

        // Music
        caps.Categories.AddCategoryMapping("7", NewznabStandardCategory.Audio, "Muzyka");
        caps.Categories.AddCategoryMapping("20", NewznabStandardCategory.AudioVideo, "Koncert");
        caps.Categories.AddCategoryMapping("144", NewznabStandardCategory.Audio, "BiGBBS RMG (Rellase Music Group)");
        caps.Categories.AddCategoryMapping("21", NewznabStandardCategory.AudioMP3, "MP3");
        caps.Categories.AddCategoryMapping("38", NewznabStandardCategory.AudioLossless, "FLAC");
        caps.Categories.AddCategoryMapping("135", NewznabStandardCategory.Audio, "RetroRemix,ClubDance");
        caps.Categories.AddCategoryMapping("43", NewznabStandardCategory.Audio, "Soundtrack");
        caps.Categories.AddCategoryMapping("136", NewznabStandardCategory.Audio, "Rock");
        caps.Categories.AddCategoryMapping("80", NewznabStandardCategory.AudioLossless, "lossless");
        caps.Categories.AddCategoryMapping("81", NewznabStandardCategory.Audio, "MusicVid");
        caps.Categories.AddCategoryMapping("82", NewznabStandardCategory.Audio, "Radio");
        caps.Categories.AddCategoryMapping("138", NewznabStandardCategory.Audio, "Dyskografie Płytowe");
        caps.Categories.AddCategoryMapping("117", NewznabStandardCategory.Audio, "Metal Rock");
        caps.Categories.AddCategoryMapping("139", NewznabStandardCategory.Audio, "Kolekcje Muzyczne");
        caps.Categories.AddCategoryMapping("118", NewznabStandardCategory.Audio, "Disco Polo");
        caps.Categories.AddCategoryMapping("119", NewznabStandardCategory.Audio, "Clubbing");
        caps.Categories.AddCategoryMapping("120", NewznabStandardCategory.Audio, "House");
        caps.Categories.AddCategoryMapping("116", NewznabStandardCategory.Audio, "PHC");
        caps.Categories.AddCategoryMapping("125", NewznabStandardCategory.Audio, "Elektro");
        caps.Categories.AddCategoryMapping("127", NewznabStandardCategory.Audio, "Tranc");
        caps.Categories.AddCategoryMapping("128", NewznabStandardCategory.Audio, "Dance");
        caps.Categories.AddCategoryMapping("130", NewznabStandardCategory.Audio, "Opus");
        caps.Categories.AddCategoryMapping("129", NewznabStandardCategory.Audio, "Pop");
        caps.Categories.AddCategoryMapping("131", NewznabStandardCategory.Audio, "Italo");
        caps.Categories.AddCategoryMapping("133", NewznabStandardCategory.Audio, "ClubDance");
        caps.Categories.AddCategoryMapping("134", NewznabStandardCategory.Audio, "Retro Remixes");
        caps.Categories.AddCategoryMapping("146", NewznabStandardCategory.Audio, "Techno");
        caps.Categories.AddCategoryMapping("132", NewznabStandardCategory.Audio, "eurodance");
        caps.Categories.AddCategoryMapping("145", NewznabStandardCategory.Audio, "Chillout");
        caps.Categories.AddCategoryMapping("83", NewznabStandardCategory.Audio, "BLUES / REGGAE/ ROCK / METAL/CLASSIC/");
        caps.Categories.AddCategoryMapping("86", NewznabStandardCategory.Audio, "Muzyka BBRG");

        // Games
        caps.Categories.AddCategoryMapping("2", NewznabStandardCategory.Console, "Gry");
        caps.Categories.AddCategoryMapping("3", NewznabStandardCategory.ConsolePS3, "Sony PS");
        caps.Categories.AddCategoryMapping("4", NewznabStandardCategory.ConsoleWii, "Wii");
        caps.Categories.AddCategoryMapping("26", NewznabStandardCategory.ConsoleXBox, "XboX");
        caps.Categories.AddCategoryMapping("27", NewznabStandardCategory.PCGames, "Gry PC");
        caps.Categories.AddCategoryMapping("28", NewznabStandardCategory.ConsoleNDS, "Nintendo");

        // Audiobooks
        caps.Categories.AddCategoryMapping("48", NewznabStandardCategory.AudioAudiobook, "Audio Book");

        // Other
        caps.Categories.AddCategoryMapping("59", NewznabStandardCategory.Other, "BBRG");

        return caps;
    }
}
