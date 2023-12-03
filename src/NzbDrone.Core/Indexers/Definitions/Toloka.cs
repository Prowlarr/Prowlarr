using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

namespace NzbDrone.Core.Indexers.Definitions
{
    public class Toloka : TorrentIndexerBase<TolokaSettings>
    {
        public override string Name => "Toloka.to";
        public override string[] IndexerUrls => new[] { "https://toloka.to/" };
        public override string Description => "Toloka.to is a Semi-Private Ukrainian torrent site with a thriving file-sharing community";
        public override string Language => "uk-UA";
        public override Encoding Encoding => Encoding.UTF8;
        public override IndexerPrivacy Privacy => IndexerPrivacy.SemiPrivate;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public Toloka(IIndexerHttpClient httpClient,
            IEventAggregator eventAggregator,
            IIndexerStatusService indexerStatusService,
            IConfigService configService,
            Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new TolokaRequestGenerator(Settings, Capabilities);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new TolokaParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            var loginUrl = $"{Settings.BaseUrl}login.php";

            var requestBuilder = new HttpRequestBuilder(loginUrl)
            {
                LogResponseContent = true,
                AllowAutoRedirect = true
            };

            var authLoginRequest = requestBuilder.Post()
                .AddFormParameter("username", Settings.Username)
                .AddFormParameter("password", Settings.Password)
                .AddFormParameter("autologin", "on")
                .AddFormParameter("ssl", "on")
                .AddFormParameter("redirect", "")
                .AddFormParameter("login", "Вхід")
                .SetHeader("Content-Type", "application/x-www-form-urlencoded")
                .SetHeader("Referer", loginUrl)
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            if (CheckIfLoginNeeded(response))
            {
                var parser = new HtmlParser();
                using var dom = await parser.ParseDocumentAsync(response.Content);
                var errorMessage = dom.QuerySelector("table.forumline table span.gen")?.FirstChild?.TextContent;

                throw new IndexerAuthException(errorMessage ?? "Unknown error message, please report.");
            }

            var cookies = response.GetCookies();
            UpdateCookies(cookies, DateTime.Now.AddDays(30));

            _logger.Debug("Toloka.to authentication succeeded.");
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            return !httpResponse.Content.Contains("logout=true");
        }

        private IndexerCapabilities SetCapabilities()
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
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                },
                SupportsRawSearch = true
            };

            caps.Categories.AddCategoryMapping("117", NewznabStandardCategory.Movies, "Українське кіно");
            caps.Categories.AddCategoryMapping("84", NewznabStandardCategory.Movies, "|-Мультфільми і казки");
            caps.Categories.AddCategoryMapping("42", NewznabStandardCategory.Movies, "|-Художні фільми");
            caps.Categories.AddCategoryMapping("124", NewznabStandardCategory.TV, "|-Телесеріали");
            caps.Categories.AddCategoryMapping("125", NewznabStandardCategory.TV, "|-Мультсеріали");
            caps.Categories.AddCategoryMapping("129", NewznabStandardCategory.Movies, "|-АртХаус");
            caps.Categories.AddCategoryMapping("219", NewznabStandardCategory.Movies, "|-Аматорське відео");
            caps.Categories.AddCategoryMapping("118", NewznabStandardCategory.Movies, "Українське озвучення");
            caps.Categories.AddCategoryMapping("16", NewznabStandardCategory.Movies, "|-Фільми");
            caps.Categories.AddCategoryMapping("32", NewznabStandardCategory.TV, "|-Телесеріали");
            caps.Categories.AddCategoryMapping("19", NewznabStandardCategory.Movies, "|-Мультфільми");
            caps.Categories.AddCategoryMapping("44", NewznabStandardCategory.TV, "|-Мультсеріали");
            caps.Categories.AddCategoryMapping("127", NewznabStandardCategory.TVAnime, "|-Аніме");
            caps.Categories.AddCategoryMapping("55", NewznabStandardCategory.Movies, "|-АртХаус");
            caps.Categories.AddCategoryMapping("94", NewznabStandardCategory.MoviesOther, "|-Трейлери");
            caps.Categories.AddCategoryMapping("144", NewznabStandardCategory.Movies, "|-Короткометражні");

            caps.Categories.AddCategoryMapping("190", NewznabStandardCategory.Movies, "Українські субтитри");
            caps.Categories.AddCategoryMapping("70", NewznabStandardCategory.Movies, "|-Фільми");
            caps.Categories.AddCategoryMapping("192", NewznabStandardCategory.TV, "|-Телесеріали");
            caps.Categories.AddCategoryMapping("193", NewznabStandardCategory.Movies, "|-Мультфільми");
            caps.Categories.AddCategoryMapping("195", NewznabStandardCategory.TV, "|-Мультсеріали");
            caps.Categories.AddCategoryMapping("194", NewznabStandardCategory.TVAnime, "|-Аніме");
            caps.Categories.AddCategoryMapping("196", NewznabStandardCategory.Movies, "|-АртХаус");
            caps.Categories.AddCategoryMapping("197", NewznabStandardCategory.Movies, "|-Короткометражні");

            caps.Categories.AddCategoryMapping("225", NewznabStandardCategory.TVDocumentary, "Документальні фільми українською");
            caps.Categories.AddCategoryMapping("21", NewznabStandardCategory.TVDocumentary, "|-Українські наукові документальні фільми");
            caps.Categories.AddCategoryMapping("131", NewznabStandardCategory.TVDocumentary, "|-Українські історичні документальні фільми");
            caps.Categories.AddCategoryMapping("226", NewznabStandardCategory.TVDocumentary, "|-BBC");
            caps.Categories.AddCategoryMapping("227", NewznabStandardCategory.TVDocumentary, "|-Discovery");
            caps.Categories.AddCategoryMapping("228", NewznabStandardCategory.TVDocumentary, "|-National Geographic");
            caps.Categories.AddCategoryMapping("229", NewznabStandardCategory.TVDocumentary, "|-History Channel");
            caps.Categories.AddCategoryMapping("230", NewznabStandardCategory.TVDocumentary, "|-Інші іноземні документальні фільми");

            caps.Categories.AddCategoryMapping("119", NewznabStandardCategory.TVOther, "Телепередачі українською");
            caps.Categories.AddCategoryMapping("18", NewznabStandardCategory.TVOther, "|-Музичне відео");
            caps.Categories.AddCategoryMapping("132", NewznabStandardCategory.TVOther, "|-Телевізійні шоу та програми");

            caps.Categories.AddCategoryMapping("157", NewznabStandardCategory.TVSport, "Український спорт");
            caps.Categories.AddCategoryMapping("235", NewznabStandardCategory.TVSport, "|-Олімпіада");
            caps.Categories.AddCategoryMapping("170", NewznabStandardCategory.TVSport, "|-Чемпіонати Європи з футболу");
            caps.Categories.AddCategoryMapping("162", NewznabStandardCategory.TVSport, "|-Чемпіонати світу з футболу");
            caps.Categories.AddCategoryMapping("166", NewznabStandardCategory.TVSport, "|-Чемпіонат та Кубок України з футболу");
            caps.Categories.AddCategoryMapping("167", NewznabStandardCategory.TVSport, "|-Єврокубки");
            caps.Categories.AddCategoryMapping("168", NewznabStandardCategory.TVSport, "|-Збірна України");
            caps.Categories.AddCategoryMapping("169", NewznabStandardCategory.TVSport, "|-Закордонні чемпіонати");
            caps.Categories.AddCategoryMapping("54", NewznabStandardCategory.TVSport, "|-Футбольне відео");
            caps.Categories.AddCategoryMapping("158", NewznabStandardCategory.TVSport, "|-Баскетбол, хоккей, волейбол, гандбол, футзал");
            caps.Categories.AddCategoryMapping("159", NewznabStandardCategory.TVSport, "|-Бокс, реслінг, бойові мистецтва");
            caps.Categories.AddCategoryMapping("160", NewznabStandardCategory.TVSport, "|-Авто, мото");
            caps.Categories.AddCategoryMapping("161", NewznabStandardCategory.TVSport, "|-Інший спорт, активний відпочинок");

            // caps.Categories.AddCategoryMapping("136", NewznabStandardCategory.Other, "HD українською");
            caps.Categories.AddCategoryMapping("96", NewznabStandardCategory.MoviesHD, "|-Фільми в HD");
            caps.Categories.AddCategoryMapping("173", NewznabStandardCategory.TVHD, "|-Серіали в HD");
            caps.Categories.AddCategoryMapping("139", NewznabStandardCategory.MoviesHD, "|-Мультфільми в HD");
            caps.Categories.AddCategoryMapping("174", NewznabStandardCategory.TVHD, "|-Мультсеріали в HD");
            caps.Categories.AddCategoryMapping("140", NewznabStandardCategory.TVDocumentary, "|-Документальні фільми в HD");
            caps.Categories.AddCategoryMapping("120", NewznabStandardCategory.MoviesDVD, "DVD українською");
            caps.Categories.AddCategoryMapping("66", NewznabStandardCategory.MoviesDVD, "|-Художні фільми та серіали в DVD");
            caps.Categories.AddCategoryMapping("137", NewznabStandardCategory.MoviesDVD, "|-Мультфільми та мультсеріали в DVD");
            caps.Categories.AddCategoryMapping("137", NewznabStandardCategory.TV, "|-Мультфільми та мультсеріали в DVD");
            caps.Categories.AddCategoryMapping("138", NewznabStandardCategory.MoviesDVD, "|-Документальні фільми в DVD");

            caps.Categories.AddCategoryMapping("237", NewznabStandardCategory.Movies, "Відео для мобільних (iOS, Android, Windows Phone)");

            caps.Categories.AddCategoryMapping("33", NewznabStandardCategory.AudioVideo, "Звукові доріжки та субтитри");

            caps.Categories.AddCategoryMapping("8", NewznabStandardCategory.Audio, "Українська музика (lossy)");
            caps.Categories.AddCategoryMapping("23", NewznabStandardCategory.Audio, "|-Поп, Естрада");
            caps.Categories.AddCategoryMapping("24", NewznabStandardCategory.Audio, "|-Джаз, Блюз");
            caps.Categories.AddCategoryMapping("43", NewznabStandardCategory.Audio, "|-Етно, Фольклор, Народна, Бардівська");
            caps.Categories.AddCategoryMapping("35", NewznabStandardCategory.Audio, "|-Інструментальна, Класична та неокласична");
            caps.Categories.AddCategoryMapping("37", NewznabStandardCategory.Audio, "|-Рок, Метал, Альтернатива, Панк, СКА");
            caps.Categories.AddCategoryMapping("36", NewznabStandardCategory.Audio, "|-Реп, Хіп-хоп, РнБ");
            caps.Categories.AddCategoryMapping("38", NewznabStandardCategory.Audio, "|-Електронна музика");
            caps.Categories.AddCategoryMapping("56", NewznabStandardCategory.Audio, "|-Невидане");

            caps.Categories.AddCategoryMapping("98", NewznabStandardCategory.AudioLossless, "Українська музика (lossless)");
            caps.Categories.AddCategoryMapping("100", NewznabStandardCategory.AudioLossless, "|-Поп, Естрада");
            caps.Categories.AddCategoryMapping("101", NewznabStandardCategory.AudioLossless, "|-Джаз, Блюз");
            caps.Categories.AddCategoryMapping("102", NewznabStandardCategory.AudioLossless, "|-Етно, Фольклор, Народна, Бардівська");
            caps.Categories.AddCategoryMapping("103", NewznabStandardCategory.AudioLossless, "|-Інструментальна, Класична та неокласична");
            caps.Categories.AddCategoryMapping("104", NewznabStandardCategory.AudioLossless, "|-Рок, Метал, Альтернатива, Панк, СКА");
            caps.Categories.AddCategoryMapping("105", NewznabStandardCategory.AudioLossless, "|-Реп, Хіп-хоп, РнБ");
            caps.Categories.AddCategoryMapping("106", NewznabStandardCategory.AudioLossless, "|-Електронна музика");

            caps.Categories.AddCategoryMapping("11", NewznabStandardCategory.Books, "Друкована література");
            caps.Categories.AddCategoryMapping("134", NewznabStandardCategory.Books, "|-Українська художня література (до 1991 р.)");
            caps.Categories.AddCategoryMapping("177", NewznabStandardCategory.Books, "|-Українська художня література (після 1991 р.)");
            caps.Categories.AddCategoryMapping("178", NewznabStandardCategory.Books, "|-Зарубіжна художня література");
            caps.Categories.AddCategoryMapping("179", NewznabStandardCategory.Books, "|-Наукова література (гуманітарні дисципліни)");
            caps.Categories.AddCategoryMapping("180", NewznabStandardCategory.Books, "|-Наукова література (природничі дисципліни)");
            caps.Categories.AddCategoryMapping("183", NewznabStandardCategory.Books, "|-Навчальна та довідкова");
            caps.Categories.AddCategoryMapping("181", NewznabStandardCategory.BooksMags, "|-Періодика");
            caps.Categories.AddCategoryMapping("182", NewznabStandardCategory.Books, "|-Батькам та малятам");
            caps.Categories.AddCategoryMapping("184", NewznabStandardCategory.BooksComics, "|-Графіка (комікси, манґа, BD та інше)");

            caps.Categories.AddCategoryMapping("185", NewznabStandardCategory.AudioAudiobook, "Аудіокниги українською");
            caps.Categories.AddCategoryMapping("135", NewznabStandardCategory.AudioAudiobook, "|-Українська художня література");
            caps.Categories.AddCategoryMapping("186", NewznabStandardCategory.AudioAudiobook, "|-Зарубіжна художня література");
            caps.Categories.AddCategoryMapping("187", NewznabStandardCategory.AudioAudiobook, "|-Історія, біографістика, спогади");
            caps.Categories.AddCategoryMapping("189", NewznabStandardCategory.AudioAudiobook, "|-Сирий матеріал");

            caps.Categories.AddCategoryMapping("9", NewznabStandardCategory.PC, "Windows");
            caps.Categories.AddCategoryMapping("25", NewznabStandardCategory.PC, "|-Windows");
            caps.Categories.AddCategoryMapping("199", NewznabStandardCategory.PC, "|-Офіс");
            caps.Categories.AddCategoryMapping("200", NewznabStandardCategory.PC, "|-Антивіруси та безпека");
            caps.Categories.AddCategoryMapping("201", NewznabStandardCategory.PC, "|-Мультимедія");
            caps.Categories.AddCategoryMapping("202", NewznabStandardCategory.PC, "|-Утиліти, обслуговування, мережа");
            caps.Categories.AddCategoryMapping("239", NewznabStandardCategory.PC, "Linux, Mac OS");
            caps.Categories.AddCategoryMapping("26", NewznabStandardCategory.PC, "|-Linux");
            caps.Categories.AddCategoryMapping("27", NewznabStandardCategory.PCMac, "|-Mac OS");

            // caps.Categories.AddCategoryMapping("240", NewznabStandardCategory.PC, "Інші OS");
            caps.Categories.AddCategoryMapping("211", NewznabStandardCategory.PCMobileAndroid, "|-Android");
            caps.Categories.AddCategoryMapping("122", NewznabStandardCategory.PCMobileiOS, "|-iOS");
            caps.Categories.AddCategoryMapping("40", NewznabStandardCategory.PCMobileOther, "|-Інші мобільні платформи");

            // caps.Categories.AddCategoryMapping("241", NewznabStandardCategory.Other, "Інше");
            // caps.Categories.AddCategoryMapping("203", NewznabStandardCategory.Other, "|-Інфодиски, електронні підручники, відеоуроки");
            // caps.Categories.AddCategoryMapping("12", NewznabStandardCategory.Other, "|-Шпалери, фотографії та зображення");
            // caps.Categories.AddCategoryMapping("249", NewznabStandardCategory.Other, "|-Веб-скрипти");
            caps.Categories.AddCategoryMapping("10", NewznabStandardCategory.PCGames, "Ігри українською");
            caps.Categories.AddCategoryMapping("28", NewznabStandardCategory.PCGames, "|-PC ігри");
            caps.Categories.AddCategoryMapping("259", NewznabStandardCategory.PCGames, "|-Mac ігри");
            caps.Categories.AddCategoryMapping("29", NewznabStandardCategory.PCGames, "|-Українізації, доповнення, патчі...");
            caps.Categories.AddCategoryMapping("30", NewznabStandardCategory.PCGames, "|-Мобільні та консольні ігри");
            caps.Categories.AddCategoryMapping("41", NewznabStandardCategory.PCMobileiOS, "|-iOS");
            caps.Categories.AddCategoryMapping("212", NewznabStandardCategory.PCMobileAndroid, "|-Android");
            caps.Categories.AddCategoryMapping("205", NewznabStandardCategory.PCGames, "Переклад ігор українською");

            return caps;
        }
    }

    public class TolokaRequestGenerator : IIndexerRequestGenerator
    {
        private readonly TolokaSettings _settings;
        private readonly IndexerCapabilities _capabilities;

        public TolokaRequestGenerator(TolokaSettings settings, IndexerCapabilities capabilities)
        {
            _settings = settings;
            _capabilities = capabilities;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories));

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

            var term = $"{searchCriteria.SanitizedSearchTerm}";

            if (searchCriteria.Season is > 0)
            {
                term += $" Сезон {searchCriteria.Season}";
            }

            pageableRequests.Add(GetPagedRequests(term, searchCriteria.Categories));

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

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                { "o", "1" },
                { "s", "2" },
                { "nm", term.IsNotNullOrWhiteSpace() ? term.Replace("-", " ") : "" }
            };

            if (_settings.FreeleechOnly)
            {
                parameters.Add("sds", "1");
            }

            var queryCats = _capabilities.Categories.MapTorznabCapsToTrackers(categories);
            if (queryCats.Any())
            {
                queryCats.ForEach(cat => parameters.Add("f[]", $"{cat}"));
            }

            var searchUrl = $"{_settings.BaseUrl}tracker.php";

            if (parameters.Count > 0)
            {
                searchUrl += $"?{parameters.GetQueryString()}";
            }

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class TolokaParser : IParseIndexerResponse
    {
        private readonly TolokaSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        private readonly TolokaTitleParser _titleParser = new ();

        public TolokaParser(TolokaSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releaseInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(indexerResponse.Content);

            var rows = dom.QuerySelectorAll("table.forumline > tbody > tr[class*=prow]");
            foreach (var row in rows)
            {
                var downloadUrl = row.QuerySelector("td:nth-child(6) > a")?.GetAttribute("href");

                // Expects moderation
                if (downloadUrl == null)
                {
                    continue;
                }

                var infoUrl = _settings.BaseUrl + row.QuerySelector("td:nth-child(3) > a")?.GetAttribute("href");
                var title = row.QuerySelector("td:nth-child(3) > a")?.TextContent.Trim() ?? string.Empty;

                var categoryLink = row.QuerySelector("td:nth-child(2) > a")?.GetAttribute("href") ?? string.Empty;
                var cat = ParseUtil.GetArgumentFromQueryString(categoryLink, "f");
                var categories = _categories.MapTrackerCatToNewznab(cat);

                var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(10) > b")?.TextContent);
                var peers = seeders + ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(11) > b")?.TextContent.Trim());

                // 2023-01-21
                var added = row.QuerySelector("td:nth-child(13)")?.TextContent.Trim() ?? string.Empty;

                var release = new TorrentInfo
                {
                    Guid = infoUrl,
                    InfoUrl = infoUrl,
                    DownloadUrl = _settings.BaseUrl + downloadUrl,
                    Title = _titleParser.Parse(title, categories, _settings.StripCyrillicLetters),
                    Description = title,
                    Categories = categories,
                    Seeders = seeders,
                    Peers = peers,
                    Size =  ParseUtil.GetBytes(row.QuerySelector("td:nth-child(7)")?.TextContent.Trim()),
                    Grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(9)")?.TextContent),
                    PublishDate = DateTimeUtil.FromFuzzyTime(added),
                    DownloadVolumeFactor = 1,
                    UploadVolumeFactor = 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 0
                };

                if (row.QuerySelector("img[src=\"images/gold.gif\"], img[src=\"images/authors.gif\"]") != null)
                {
                    release.DownloadVolumeFactor = 0;
                }
                else if (row.QuerySelector("img[src=\"images/silver.gif\"]") != null)
                {
                    release.DownloadVolumeFactor = 0.5;
                }
                else if (row.QuerySelector("img[src=\"images/bronze.gif\"]") != null)
                {
                    release.DownloadVolumeFactor = 0.75;
                }

                releaseInfos.Add(release);
            }

            return releaseInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class TolokaTitleParser
    {
        private static readonly List<Regex> FindTagsInTitlesRegexList = new ()
        {
            new Regex(@"\((?>\((?<c>)|[^()]+|\)(?<-c>))*(?(c)(?!))\)"),
            new Regex(@"\[(?>\[(?<c>)|[^\[\]]+|\](?<-c>))*(?(c)(?!))\]")
        };

        private readonly Regex _tvTitleCommaRegex = new (@"\s(\d+),(\d+)", RegexOptions.Compiled);
        private readonly Regex _tvTitleCyrillicXRegex = new (@"([\s-])Х+([\)\]])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Regex _tvTitleMultipleSeasonsRegex = new (@"(?:Сезон|Seasons?)\s*[:]*\s+(\d+-\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Regex _tvTitleUkrSeasonEpisodeOfRegex = new (@"Сезон\s*[:]*\s+(\d+).+(?:Серії|Серія|Серій|Епізод)+\s*[:]*\s+(\d+(?:-\d+)?)\s*з\s*([\w?])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _tvTitleUkrSeasonEpisodeRegex = new (@"Сезон\s*[:]*\s+(\d+).+(?:Серії|Серія|Серій|Епізод)+\s*[:]*\s+(\d+(?:-\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _tvTitleUkrSeasonRegex = new (@"Сезон\s*[:]*\s+(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _tvTitleUkrEpisodeOfRegex = new (@"(?:Серії|Серія|Серій|Епізод)+\s*[:]*\s+(\d+(?:-\d+)?)\s*з\s*([\w?])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _tvTitleUkrEpisodeRegex = new (@"(?:Серії|Серія|Серій|Епізод)+\s*[:]*\s+(\d+(?:-\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Regex _tvTitleEngSeasonEpisodeOfRegex = new (@"Season\s*[:]*\s+(\d+).+(?:Episodes?)+\s*[:]*\s+(\d+(?:-\d+)?)\s*of\s*([\w?])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _tvTitleEngSeasonEpisodeRegex = new (@"Season\s*[:]*\s+(\d+).+(?:Episodes?)+\s*[:]*\s+(\d+(?:-\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _tvTitleEngSeasonRegex = new (@"Season\s*[:]*\s+(\d+(?:-\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _tvTitleEngEpisodeOfRegex = new (@"(?:Episodes?)+\s*[:]*\s+(\d+(?:-\d+)?)\s*of\s*([\w?])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex _tvTitleEngEpisodeRegex = new (@"(?:Episodes?)+\s*[:]+\s*[:]*\s+(\d+(?:-\d+)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Regex _stripCyrillicRegex = new (@"(\([\p{IsCyrillic}\W]+\))|(^[\p{IsCyrillic}\W\d]+\/ )|([\p{IsCyrillic} \-]+,+)|([\p{IsCyrillic}]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string Parse(string title, ICollection<IndexerCategory> categories, bool stripCyrillicLetters = true)
        {
            // https://www.fileformat.info/info/unicode/category/Pd/list.htm
            title = Regex.Replace(title, @"\p{Pd}", "-", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            if (IsAnyTvCategory(categories))
            {
                title = _tvTitleCommaRegex.Replace(title, " $1-$2");
                title = _tvTitleCyrillicXRegex.Replace(title, "$1XX$2");

                // special case for multiple seasons
                title = _tvTitleMultipleSeasonsRegex.Replace(title, "S$1");

                title = _tvTitleUkrSeasonEpisodeOfRegex.Replace(title, "S$1E$2 of $3");
                title = _tvTitleUkrSeasonEpisodeRegex.Replace(title, "S$1E$2");
                title = _tvTitleUkrSeasonRegex.Replace(title, "S$1");
                title = _tvTitleUkrEpisodeOfRegex.Replace(title, "E$1 of $2");
                title = _tvTitleUkrEpisodeRegex.Replace(title, "E$1");

                title = _tvTitleEngSeasonEpisodeOfRegex.Replace(title, "S$1E$2 of $3");
                title = _tvTitleEngSeasonEpisodeRegex.Replace(title, "S$1E$2");
                title = _tvTitleEngSeasonRegex.Replace(title, "S$1");
                title = _tvTitleEngEpisodeOfRegex.Replace(title, "E$1 of $2");
                title = _tvTitleEngEpisodeRegex.Replace(title, "E$1");
            }

            if (stripCyrillicLetters)
            {
                title = _stripCyrillicRegex.Replace(title, string.Empty).Trim(' ', '-');
            }

            title = Regex.Replace(title, @"\b-Rip\b", "Rip", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            title = Regex.Replace(title, @"\bHDTVRip\b", "HDTV", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            title = Regex.Replace(title, @"\bWEB-DLRip\b", "WEB-DL", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            title = Regex.Replace(title, @"\bWEBDLRip\b", "WEB-DL", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            title = Regex.Replace(title, @"\bWEBDL\b", "WEB-DL", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            title = MoveFirstTagsToEndOfReleaseTitle(title);

            title = Regex.Replace(title, @"\(\s*\/\s*", "(", RegexOptions.Compiled);
            title = Regex.Replace(title, @"\s*\/\s*\)", ")", RegexOptions.Compiled);

            title = Regex.Replace(title, @"[\[\(]\s*[\)\]]", "", RegexOptions.Compiled);

            title = title.Trim(' ', '&', ',', '.', '!', '?', '+', '-', '_', '|', '/', '\\', ':');

            // replace multiple spaces with a single space
            title = Regex.Replace(title, @"\s+", " ");

            return title.Trim();
        }

        private static bool IsAnyTvCategory(ICollection<IndexerCategory> category)
        {
            return category.Contains(NewznabStandardCategory.TV) || NewznabStandardCategory.TV.SubCategories.Any(subCategory => category.Contains(subCategory));
        }

        private static string MoveFirstTagsToEndOfReleaseTitle(string input)
        {
            var output = input;
            foreach (var findTagsRegex in FindTagsInTitlesRegexList)
            {
                var expectedIndex = 0;
                foreach (Match match in findTagsRegex.Matches(output))
                {
                    if (match.Index > expectedIndex)
                    {
                        var substring = output.Substring(expectedIndex, match.Index - expectedIndex);
                        if (string.IsNullOrWhiteSpace(substring))
                        {
                            expectedIndex = match.Index;
                        }
                        else
                        {
                            break;
                        }
                    }

                    var tag = match.ToString();
                    var regex = new Regex(Regex.Escape(tag));
                    output = $"{regex.Replace(output, string.Empty, 1)} {tag}".Trim();
                    expectedIndex += tag.Length;
                }
            }

            return output.Trim();
        }
    }

    public class TolokaSettings : UserPassTorrentBaseSettings
    {
        public TolokaSettings()
        {
            StripCyrillicLetters = true;
        }

        [FieldDefinition(4, Label = "Freeleech Only", HelpText = "Search Freeleech torrents only", Type = FieldType.Checkbox)]
        public bool FreeleechOnly { get; set; }

        [FieldDefinition(5, Label = "Strip Cyrillic Letters", Type = FieldType.Checkbox)]
        public bool StripCyrillicLetters { get; set; }
    }
}
