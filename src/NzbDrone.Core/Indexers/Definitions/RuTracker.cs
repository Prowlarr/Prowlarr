using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using FluentValidation;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.Cardigann;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class RuTracker : TorrentIndexerBase<RuTrackerSettings>
    {
        public override string Name => "RuTracker";
        public override string[] IndexerUrls => new string[] { "https://rutracker.org/" };

        private string LoginUrl => Settings.BaseUrl + "forum/login.php";
        public override string Description => "RuTracker is a Semi-Private Russian torrent site with a thriving file-sharing community";
        public override string Language => "ru-org";
        public override Encoding Encoding => Encoding.GetEncoding("windows-1251");
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public RuTracker(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new RuTrackerRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new RuTrackerParser(Settings, Capabilities.Categories);
        }

        protected override async Task DoLogin()
        {
            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true,
                AllowAutoRedirect = true
            };

            requestBuilder.Method = HttpMethod.POST;
            requestBuilder.PostProcess += r => r.RequestTimeout = TimeSpan.FromSeconds(15);

            var cookies = Cookies;

            Cookies = null;
            requestBuilder.AddFormParameter("login_username", Settings.Username)
                .AddFormParameter("login_password", Settings.Password)
                .AddFormParameter("login", "Login")
                .SetHeader("Content-Type", "multipart/form-data");

            var authLoginRequest = requestBuilder.Build();

            authLoginRequest.Encoding = Encoding;

            var response = await ExecuteAuth(authLoginRequest);

            if (!response.Content.Contains("id=\"logged-in-username\""))
            {
                throw new IndexerAuthException("RuTracker Auth Failed");
            }

            cookies = response.GetCookies();
            UpdateCookies(cookies, DateTime.Now + TimeSpan.FromDays(30));

            _logger.Debug("RuTracker authentication succeeded.");
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            if (httpResponse.RedirectUrl.Contains("login.php") || !httpResponse.Content.Contains("id=\"logged-in-username\""))
            {
                return true;
            }

            return false;
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
                                   }
            };

            caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.Movies, "Наше кино");
            caps.Categories.AddCategoryMapping(941, NewznabStandardCategory.Movies, "|- Кино СССР");
            caps.Categories.AddCategoryMapping(1666, NewznabStandardCategory.Movies, "|- Детские отечественные фильмы");
            caps.Categories.AddCategoryMapping(376, NewznabStandardCategory.Movies, "|- Авторские дебюты");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.MoviesForeign, "Зарубежное кино");
            caps.Categories.AddCategoryMapping(187, NewznabStandardCategory.MoviesForeign, "|- Классика мирового кинематографа");
            caps.Categories.AddCategoryMapping(2090, NewznabStandardCategory.MoviesForeign, "|- Фильмы до 1990 года");
            caps.Categories.AddCategoryMapping(2221, NewznabStandardCategory.MoviesForeign, "|- Фильмы 1991-2000");
            caps.Categories.AddCategoryMapping(2091, NewznabStandardCategory.MoviesForeign, "|- Фильмы 2001-2005");
            caps.Categories.AddCategoryMapping(2092, NewznabStandardCategory.MoviesForeign, "|- Фильмы 2006-2010");
            caps.Categories.AddCategoryMapping(2093, NewznabStandardCategory.MoviesForeign, "|- Фильмы 2011-2015");
            caps.Categories.AddCategoryMapping(2200, NewznabStandardCategory.MoviesForeign, "|- Фильмы 2016-2019");
            caps.Categories.AddCategoryMapping(1950, NewznabStandardCategory.MoviesForeign, "|- Фильмы 2020");
            caps.Categories.AddCategoryMapping(2540, NewznabStandardCategory.MoviesForeign, "|- Фильмы Ближнего Зарубежья");
            caps.Categories.AddCategoryMapping(934, NewznabStandardCategory.MoviesForeign, "|- Азиатские фильмы");
            caps.Categories.AddCategoryMapping(505, NewznabStandardCategory.MoviesForeign, "|- Индийское кино");
            caps.Categories.AddCategoryMapping(212, NewznabStandardCategory.MoviesForeign, "|- Сборники фильмов");
            caps.Categories.AddCategoryMapping(2459, NewznabStandardCategory.MoviesForeign, "|- Короткий метр");
            caps.Categories.AddCategoryMapping(1235, NewznabStandardCategory.MoviesForeign, "|- Грайндхаус");
            caps.Categories.AddCategoryMapping(185, NewznabStandardCategory.Audio, "|- Звуковые дорожки и Переводы");
            caps.Categories.AddCategoryMapping(124, NewznabStandardCategory.MoviesOther, "Арт-хаус и авторское кино");
            caps.Categories.AddCategoryMapping(1543, NewznabStandardCategory.MoviesOther, "|- Короткий метр (Арт-хаус и авторское кино)");
            caps.Categories.AddCategoryMapping(709, NewznabStandardCategory.MoviesOther, "|- Документальные фильмы (Арт-хаус и авторское кино)");
            caps.Categories.AddCategoryMapping(1577, NewznabStandardCategory.MoviesOther, "|- Анимация (Арт-хаус и авторское кино)");
            caps.Categories.AddCategoryMapping(511, NewznabStandardCategory.TVOther, "Театр");
            caps.Categories.AddCategoryMapping(93, NewznabStandardCategory.MoviesDVD, "DVD Video");
            caps.Categories.AddCategoryMapping(905, NewznabStandardCategory.MoviesDVD, "|- Классика мирового кинематографа (DVD Video)");
            caps.Categories.AddCategoryMapping(101, NewznabStandardCategory.MoviesDVD, "|- Зарубежное кино (DVD Video)");
            caps.Categories.AddCategoryMapping(100, NewznabStandardCategory.MoviesDVD, "|- Наше кино (DVD Video)");
            caps.Categories.AddCategoryMapping(877, NewznabStandardCategory.MoviesDVD, "|- Фильмы Ближнего Зарубежья (DVD Video)");
            caps.Categories.AddCategoryMapping(1576, NewznabStandardCategory.MoviesDVD, "|- Азиатские фильмы (DVD Video)");
            caps.Categories.AddCategoryMapping(572, NewznabStandardCategory.MoviesDVD, "|- Арт-хаус и авторское кино (DVD Video)");
            caps.Categories.AddCategoryMapping(2220, NewznabStandardCategory.MoviesDVD, "|- Индийское кино (DVD Video)");
            caps.Categories.AddCategoryMapping(1670, NewznabStandardCategory.MoviesDVD, "|- Грайндхаус (DVD Video)");
            caps.Categories.AddCategoryMapping(2198, NewznabStandardCategory.MoviesHD, "HD Video");
            caps.Categories.AddCategoryMapping(1457, NewznabStandardCategory.MoviesUHD, "|- UHD Video");
            caps.Categories.AddCategoryMapping(2199, NewznabStandardCategory.MoviesHD, "|- Классика мирового кинематографа (HD Video)");
            caps.Categories.AddCategoryMapping(313, NewznabStandardCategory.MoviesHD, "|- Зарубежное кино (HD Video)");
            caps.Categories.AddCategoryMapping(312, NewznabStandardCategory.MoviesHD, "|- Наше кино (HD Video)");
            caps.Categories.AddCategoryMapping(1247, NewznabStandardCategory.MoviesHD, "|- Фильмы Ближнего Зарубежья (HD Video)");
            caps.Categories.AddCategoryMapping(2201, NewznabStandardCategory.MoviesHD, "|- Азиатские фильмы (HD Video)");
            caps.Categories.AddCategoryMapping(2339, NewznabStandardCategory.MoviesHD, "|- Арт-хаус и авторское кино (HD Video)");
            caps.Categories.AddCategoryMapping(140, NewznabStandardCategory.MoviesHD, "|- Индийское кино (HD Video)");
            caps.Categories.AddCategoryMapping(194, NewznabStandardCategory.MoviesHD, "|- Грайндхаус (HD Video)");
            caps.Categories.AddCategoryMapping(352, NewznabStandardCategory.Movies3D, "3D/Стерео КиноВидео, TV и Спорт");
            caps.Categories.AddCategoryMapping(549, NewznabStandardCategory.Movies3D, "|- 3D Кинофильмы");
            caps.Categories.AddCategoryMapping(1213, NewznabStandardCategory.Movies3D, "|- 3D Мультфильмы");
            caps.Categories.AddCategoryMapping(2109, NewznabStandardCategory.Movies3D, "|- 3D Документальные фильмы");
            caps.Categories.AddCategoryMapping(514, NewznabStandardCategory.Movies3D, "|- 3D Спорт");
            caps.Categories.AddCategoryMapping(2097, NewznabStandardCategory.Movies3D, "|- 3D РоликиМузыкальное видео, Трейлеры к фильмам");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.Movies, "Мультфильмы");
            caps.Categories.AddCategoryMapping(2343, NewznabStandardCategory.MoviesHD, "|- Отечественные мультфильмы (HD Video)");
            caps.Categories.AddCategoryMapping(930, NewznabStandardCategory.MoviesHD, "|- Иностранные мультфильмы (HD Video)");
            caps.Categories.AddCategoryMapping(2365, NewznabStandardCategory.MoviesHD, "|- Иностранные короткометражные мультфильмы (HD Video)");
            caps.Categories.AddCategoryMapping(1900, NewznabStandardCategory.MoviesDVD, "|- Отечественные мультфильмы (DVD)");
            caps.Categories.AddCategoryMapping(521, NewznabStandardCategory.MoviesDVD, "|- Иностранные мультфильмы (DVD)");
            caps.Categories.AddCategoryMapping(2258, NewznabStandardCategory.MoviesDVD, "|- Иностранные короткометражные мультфильмы (DVD)");
            caps.Categories.AddCategoryMapping(208, NewznabStandardCategory.Movies, "|- Отечественные мультфильмы");
            caps.Categories.AddCategoryMapping(539, NewznabStandardCategory.Movies, "|- Отечественные полнометражные мультфильмы");
            caps.Categories.AddCategoryMapping(209, NewznabStandardCategory.MoviesForeign, "|- Иностранные мультфильмы");
            caps.Categories.AddCategoryMapping(484, NewznabStandardCategory.MoviesForeign, "|- Иностранные короткометражные мультфильмы");
            caps.Categories.AddCategoryMapping(822, NewznabStandardCategory.Movies, "|- Сборники мультфильмов");
            caps.Categories.AddCategoryMapping(921, NewznabStandardCategory.TV, "Мультсериалы");
            caps.Categories.AddCategoryMapping(815, NewznabStandardCategory.TVSD, "|- Мультсериалы (SD Video)");
            caps.Categories.AddCategoryMapping(816, NewznabStandardCategory.TVHD, "|- Мультсериалы (DVD Video)");
            caps.Categories.AddCategoryMapping(1460, NewznabStandardCategory.TVHD, "|- Мультсериалы (HD Video)");
            caps.Categories.AddCategoryMapping(33, NewznabStandardCategory.TVAnime, "Аниме");
            caps.Categories.AddCategoryMapping(2484, NewznabStandardCategory.TVAnime, "|- Артбуки и журналы (Аниме)");
            caps.Categories.AddCategoryMapping(1386, NewznabStandardCategory.TVAnime, "|- Обоисканы, аватары, арт");
            caps.Categories.AddCategoryMapping(1387, NewznabStandardCategory.TVAnime, "|- AMV и другие ролики");
            caps.Categories.AddCategoryMapping(599, NewznabStandardCategory.TVAnime, "|- Аниме (DVD)");
            caps.Categories.AddCategoryMapping(1105, NewznabStandardCategory.TVAnime, "|- Аниме (HD Video)");
            caps.Categories.AddCategoryMapping(1389, NewznabStandardCategory.TVAnime, "|- Аниме (основной подраздел)");
            caps.Categories.AddCategoryMapping(1391, NewznabStandardCategory.TVAnime, "|- Аниме (плеерный подраздел)");
            caps.Categories.AddCategoryMapping(2491, NewznabStandardCategory.TVAnime, "|- Аниме (QC подраздел)");
            caps.Categories.AddCategoryMapping(404, NewznabStandardCategory.TVAnime, "|- Покемоны");
            caps.Categories.AddCategoryMapping(1390, NewznabStandardCategory.TVAnime, "|- Наруто");
            caps.Categories.AddCategoryMapping(1642, NewznabStandardCategory.TVAnime, "|- Гандам");
            caps.Categories.AddCategoryMapping(893, NewznabStandardCategory.TVAnime, "|- Японские мультфильмы");
            caps.Categories.AddCategoryMapping(809, NewznabStandardCategory.Audio, "|- Звуковые дорожки (Аниме)");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.TV, "Русские сериалы");
            caps.Categories.AddCategoryMapping(81, NewznabStandardCategory.TVHD, "|- Русские сериалы (HD Video)");
            caps.Categories.AddCategoryMapping(80, NewznabStandardCategory.TV, "|- Возвращение Мухтара");
            caps.Categories.AddCategoryMapping(1535, NewznabStandardCategory.TV, "|- Воронины");
            caps.Categories.AddCategoryMapping(188, NewznabStandardCategory.TV, "|- Чернобыль: Зона отчуждения");
            caps.Categories.AddCategoryMapping(91, NewznabStandardCategory.TV, "|- Кухня / Отель Элеон");
            caps.Categories.AddCategoryMapping(990, NewznabStandardCategory.TV, "|- Универ / Универ. Новая общага / СашаТаня");
            caps.Categories.AddCategoryMapping(1408, NewznabStandardCategory.TV, "|- Ольга / Физрук");
            caps.Categories.AddCategoryMapping(175, NewznabStandardCategory.TV, "|- След");
            caps.Categories.AddCategoryMapping(79, NewznabStandardCategory.TV, "|- Солдаты и пр.");
            caps.Categories.AddCategoryMapping(104, NewznabStandardCategory.TV, "|- Тайны следствия");
            caps.Categories.AddCategoryMapping(189, NewznabStandardCategory.TVForeign, "Зарубежные сериалы");
            caps.Categories.AddCategoryMapping(842, NewznabStandardCategory.TVForeign, "|- Новинки и сериалы в стадии показа");
            caps.Categories.AddCategoryMapping(235, NewznabStandardCategory.TVForeign, "|- Сериалы США и Канады");
            caps.Categories.AddCategoryMapping(242, NewznabStandardCategory.TVForeign, "|- Сериалы Великобритании и Ирландии");
            caps.Categories.AddCategoryMapping(819, NewznabStandardCategory.TVForeign, "|- Скандинавские сериалы");
            caps.Categories.AddCategoryMapping(1531, NewznabStandardCategory.TVForeign, "|- Испанские сериалы");
            caps.Categories.AddCategoryMapping(721, NewznabStandardCategory.TVForeign, "|- Итальянские сериалы");
            caps.Categories.AddCategoryMapping(1102, NewznabStandardCategory.TVForeign, "|- Европейские сериалы");
            caps.Categories.AddCategoryMapping(1120, NewznabStandardCategory.TVForeign, "|- Сериалы стран АфрикиБлижнего и Среднего Востока");
            caps.Categories.AddCategoryMapping(1214, NewznabStandardCategory.TVForeign, "|- Сериалы Австралии и Новой Зеландии");
            caps.Categories.AddCategoryMapping(489, NewznabStandardCategory.TVForeign, "|- Сериалы Ближнего Зарубежья");
            caps.Categories.AddCategoryMapping(387, NewznabStandardCategory.TVForeign, "|- Сериалы совместного производства нескольких стран");
            caps.Categories.AddCategoryMapping(1359, NewznabStandardCategory.TVForeign, "|- Веб-сериалыВебизоды к сериалам и Пилотные серии сериалов");
            caps.Categories.AddCategoryMapping(184, NewznabStandardCategory.TVForeign, "|- Бесстыжие / Shameless (US)");
            caps.Categories.AddCategoryMapping(1171, NewznabStandardCategory.TVForeign, "|- Викинги / Vikings");
            caps.Categories.AddCategoryMapping(1417, NewznabStandardCategory.TVForeign, "|- Во все тяжкие / Breaking Bad");
            caps.Categories.AddCategoryMapping(625, NewznabStandardCategory.TVForeign, "|- Доктор Хаус / House M.D.");
            caps.Categories.AddCategoryMapping(1449, NewznabStandardCategory.TVForeign, "|- Игра престолов / Game of Thrones");
            caps.Categories.AddCategoryMapping(273, NewznabStandardCategory.TVForeign, "|- Карточный Домик / House of Cards");
            caps.Categories.AddCategoryMapping(504, NewznabStandardCategory.TVForeign, "|- Клан Сопрано / The Sopranos");
            caps.Categories.AddCategoryMapping(372, NewznabStandardCategory.TVForeign, "|- Сверхъестественное / Supernatural");
            caps.Categories.AddCategoryMapping(110, NewznabStandardCategory.TVForeign, "|- Секретные материалы / The X-Files");
            caps.Categories.AddCategoryMapping(121, NewznabStandardCategory.TVForeign, "|- Твин пикс / Twin Peaks");
            caps.Categories.AddCategoryMapping(507, NewznabStandardCategory.TVForeign, "|- Теория большого взрыва + Детство Шелдона");
            caps.Categories.AddCategoryMapping(536, NewznabStandardCategory.TVForeign, "|- Форс-мажоры / Костюмы в законе / Suits");
            caps.Categories.AddCategoryMapping(1144, NewznabStandardCategory.TVForeign, "|- Ходячие мертвецы + Бойтесь ходячих мертвецов");
            caps.Categories.AddCategoryMapping(173, NewznabStandardCategory.TVForeign, "|- Черное зеркало / Black Mirror");
            caps.Categories.AddCategoryMapping(195, NewznabStandardCategory.TVForeign, "|- Для некондиционных раздач");
            caps.Categories.AddCategoryMapping(2366, NewznabStandardCategory.TVHD, "Зарубежные сериалы (HD Video)");
            caps.Categories.AddCategoryMapping(119, NewznabStandardCategory.TVForeign, "|- Зарубежные сериалы (UHD Video)");
            caps.Categories.AddCategoryMapping(1803, NewznabStandardCategory.TVHD, "|- Новинки и сериалы в стадии показа (HD Video)");
            caps.Categories.AddCategoryMapping(266, NewznabStandardCategory.TVHD, "|- Сериалы США и Канады (HD Video)");
            caps.Categories.AddCategoryMapping(193, NewznabStandardCategory.TVHD, "|- Сериалы Великобритании и Ирландии (HD Video)");
            caps.Categories.AddCategoryMapping(1690, NewznabStandardCategory.TVHD, "|- Скандинавские сериалы (HD Video)");
            caps.Categories.AddCategoryMapping(1459, NewznabStandardCategory.TVHD, "|- Европейские сериалы (HD Video)");
            caps.Categories.AddCategoryMapping(1463, NewznabStandardCategory.TVHD, "|- Сериалы стран АфрикиБлижнего и Среднего Востока (HD Video)");
            caps.Categories.AddCategoryMapping(825, NewznabStandardCategory.TVHD, "|- Сериалы Австралии и Новой Зеландии (HD Video)");
            caps.Categories.AddCategoryMapping(1248, NewznabStandardCategory.TVHD, "|- Сериалы Ближнего Зарубежья (HD Video)");
            caps.Categories.AddCategoryMapping(1288, NewznabStandardCategory.TVHD, "|- Сериалы совместного производства нескольких стран (HD Video)");
            caps.Categories.AddCategoryMapping(1669, NewznabStandardCategory.TVHD, "|- Викинги / Vikings (HD Video)");
            caps.Categories.AddCategoryMapping(2393, NewznabStandardCategory.TVHD, "|- Доктор Хаус / House M.D. (HD Video)");
            caps.Categories.AddCategoryMapping(265, NewznabStandardCategory.TVHD, "|- Игра престолов / Game of Thrones (HD Video)");
            caps.Categories.AddCategoryMapping(2406, NewznabStandardCategory.TVHD, "|- Карточный домик (HD Video)");
            caps.Categories.AddCategoryMapping(2404, NewznabStandardCategory.TVHD, "|- Сверхъестественное / Supernatural (HD Video)");
            caps.Categories.AddCategoryMapping(2405, NewznabStandardCategory.TVHD, "|- Секретные материалы / The X-Files (HD Video)");
            caps.Categories.AddCategoryMapping(2370, NewznabStandardCategory.TVHD, "|- Твин пикс / Twin Peaks (HD Video)");
            caps.Categories.AddCategoryMapping(2396, NewznabStandardCategory.TVHD, "|- Теория Большого Взрыва / The Big Bang Theory (HD Video)");
            caps.Categories.AddCategoryMapping(2398, NewznabStandardCategory.TVHD, "|- Ходячие мертвецы + Бойтесь ходячих мертвецов (HD Video)");
            caps.Categories.AddCategoryMapping(1949, NewznabStandardCategory.TVHD, "|- Черное зеркало / Black Mirror (HD Video)");
            caps.Categories.AddCategoryMapping(1498, NewznabStandardCategory.TVHD, "|- Для некондиционных раздач (HD Video)");
            caps.Categories.AddCategoryMapping(911, NewznabStandardCategory.TVForeign, "Сериалы Латинской АмерикиТурции и Индии");
            caps.Categories.AddCategoryMapping(1493, NewznabStandardCategory.TVForeign, "|- Актёры и актрисы латиноамериканских сериалов");
            caps.Categories.AddCategoryMapping(325, NewznabStandardCategory.TVForeign, "|- Сериалы Аргентины");
            caps.Categories.AddCategoryMapping(534, NewznabStandardCategory.TVForeign, "|- Сериалы Бразилии");
            caps.Categories.AddCategoryMapping(594, NewznabStandardCategory.TVForeign, "|- Сериалы Венесуэлы");
            caps.Categories.AddCategoryMapping(1301, NewznabStandardCategory.TVForeign, "|- Сериалы Индии");
            caps.Categories.AddCategoryMapping(607, NewznabStandardCategory.TVForeign, "|- Сериалы Колумбии");
            caps.Categories.AddCategoryMapping(1574, NewznabStandardCategory.TVForeign, "|- Сериалы Латинской Америки с озвучкой (раздачи папками)");
            caps.Categories.AddCategoryMapping(1539, NewznabStandardCategory.TVForeign, "|- Сериалы Латинской Америки с субтитрами");
            caps.Categories.AddCategoryMapping(1940, NewznabStandardCategory.TVForeign, "|- Официальные краткие версии сериалов Латинской Америки");
            caps.Categories.AddCategoryMapping(694, NewznabStandardCategory.TVForeign, "|- Сериалы Мексики");
            caps.Categories.AddCategoryMapping(775, NewznabStandardCategory.TVForeign, "|- Сериалы ПеруСальвадора, Чили и других стран");
            caps.Categories.AddCategoryMapping(781, NewznabStandardCategory.TVForeign, "|- Сериалы совместного производства");
            caps.Categories.AddCategoryMapping(718, NewznabStandardCategory.TVForeign, "|- Сериалы США (латиноамериканские)");
            caps.Categories.AddCategoryMapping(704, NewznabStandardCategory.TVForeign, "|- Сериалы Турции");
            caps.Categories.AddCategoryMapping(1537, NewznabStandardCategory.TVForeign, "|- Для некондиционных раздач");
            caps.Categories.AddCategoryMapping(2100, NewznabStandardCategory.TVForeign, "Азиатские сериалы");
            caps.Categories.AddCategoryMapping(717, NewznabStandardCategory.TVForeign, "|- Китайские сериалы с субтитрами");
            caps.Categories.AddCategoryMapping(915, NewznabStandardCategory.TVForeign, "|- Корейские сериалы с озвучкой");
            caps.Categories.AddCategoryMapping(1242, NewznabStandardCategory.TVForeign, "|- Корейские сериалы с субтитрами");
            caps.Categories.AddCategoryMapping(2412, NewznabStandardCategory.TVForeign, "|- Прочие азиатские сериалы с озвучкой");
            caps.Categories.AddCategoryMapping(1938, NewznabStandardCategory.TVForeign, "|- Тайваньские сериалы с субтитрами");
            caps.Categories.AddCategoryMapping(2104, NewznabStandardCategory.TVForeign, "|- Японские сериалы с субтитрами");
            caps.Categories.AddCategoryMapping(1939, NewznabStandardCategory.TVForeign, "|- Японские сериалы с озвучкой");
            caps.Categories.AddCategoryMapping(2102, NewznabStandardCategory.TVForeign, "|- VMV и др. ролики");
            caps.Categories.AddCategoryMapping(670, NewznabStandardCategory.TVDocumentary, "Вера и религия");
            caps.Categories.AddCategoryMapping(1475, NewznabStandardCategory.TVDocumentary, "|- [Видео Религия] Христианство");
            caps.Categories.AddCategoryMapping(2107, NewznabStandardCategory.TVDocumentary, "|- [Видео Религия] Ислам");
            caps.Categories.AddCategoryMapping(294, NewznabStandardCategory.TVDocumentary, "|- [Видео Религия] Религии ИндииТибета и Восточной Азии");
            caps.Categories.AddCategoryMapping(1453, NewznabStandardCategory.TVDocumentary, "|- [Видео Религия] Культы и новые религиозные движения");
            caps.Categories.AddCategoryMapping(46, NewznabStandardCategory.TVDocumentary, "Документальные фильмы и телепередачи");
            caps.Categories.AddCategoryMapping(103, NewznabStandardCategory.TVDocumentary, "|- Документальные (DVD)");
            caps.Categories.AddCategoryMapping(671, NewznabStandardCategory.TVDocumentary, "|- [Док] Биографии. Личности и кумиры");
            caps.Categories.AddCategoryMapping(2177, NewznabStandardCategory.TVDocumentary, "|- [Док] Кинематограф и мультипликация");
            caps.Categories.AddCategoryMapping(656, NewznabStandardCategory.TVDocumentary, "|- [Док] Мастера искусств Театра и Кино");
            caps.Categories.AddCategoryMapping(2538, NewznabStandardCategory.TVDocumentary, "|- [Док] Искусствоистория искусств");
            caps.Categories.AddCategoryMapping(2159, NewznabStandardCategory.TVDocumentary, "|- [Док] Музыка");
            caps.Categories.AddCategoryMapping(251, NewznabStandardCategory.TVDocumentary, "|- [Док] Криминальная документалистика");
            caps.Categories.AddCategoryMapping(98, NewznabStandardCategory.TVDocumentary, "|- [Док] Тайны века / Спецслужбы / Теории Заговоров");
            caps.Categories.AddCategoryMapping(97, NewznabStandardCategory.TVDocumentary, "|- [Док] Военное дело");
            caps.Categories.AddCategoryMapping(851, NewznabStandardCategory.TVDocumentary, "|- [Док] Вторая мировая война");
            caps.Categories.AddCategoryMapping(2178, NewznabStandardCategory.TVDocumentary, "|- [Док] Аварии / Катастрофы / Катаклизмы");
            caps.Categories.AddCategoryMapping(821, NewznabStandardCategory.TVDocumentary, "|- [Док] Авиация");
            caps.Categories.AddCategoryMapping(2076, NewznabStandardCategory.TVDocumentary, "|- [Док] Космос");
            caps.Categories.AddCategoryMapping(56, NewznabStandardCategory.TVDocumentary, "|- [Док] Научно-популярные фильмы");
            caps.Categories.AddCategoryMapping(2123, NewznabStandardCategory.TVDocumentary, "|- [Док] Флора и фауна");
            caps.Categories.AddCategoryMapping(876, NewznabStandardCategory.TVDocumentary, "|- [Док] Путешествия и туризм");
            caps.Categories.AddCategoryMapping(2139, NewznabStandardCategory.TVDocumentary, "|- [Док] Медицина");
            caps.Categories.AddCategoryMapping(2380, NewznabStandardCategory.TVDocumentary, "|- [Док] Социальные ток-шоу");
            caps.Categories.AddCategoryMapping(1467, NewznabStandardCategory.TVDocumentary, "|- [Док] Информационно-аналитические и общественно-политические перед..");
            caps.Categories.AddCategoryMapping(1469, NewznabStandardCategory.TVDocumentary, "|- [Док] Архитектура и строительство");
            caps.Categories.AddCategoryMapping(672, NewznabStandardCategory.TVDocumentary, "|- [Док] Всё о домебыте и дизайне");
            caps.Categories.AddCategoryMapping(249, NewznabStandardCategory.TVDocumentary, "|- [Док] BBC");
            caps.Categories.AddCategoryMapping(552, NewznabStandardCategory.TVDocumentary, "|- [Док] Discovery");
            caps.Categories.AddCategoryMapping(500, NewznabStandardCategory.TVDocumentary, "|- [Док] National Geographic");
            caps.Categories.AddCategoryMapping(2112, NewznabStandardCategory.TVDocumentary, "|- [Док] История: Древний мир / Античность / Средневековье");
            caps.Categories.AddCategoryMapping(1327, NewznabStandardCategory.TVDocumentary, "|- [Док] История: Новое и Новейшее время");
            caps.Categories.AddCategoryMapping(1468, NewznabStandardCategory.TVDocumentary, "|- [Док] Эпоха СССР");
            caps.Categories.AddCategoryMapping(1280, NewznabStandardCategory.TVDocumentary, "|- [Док] Битва экстрасенсов / Теория невероятности / Искатели / Галил..");
            caps.Categories.AddCategoryMapping(752, NewznabStandardCategory.TVDocumentary, "|- [Док] Русские сенсации / Программа Максимум / Профессия репортёр");
            caps.Categories.AddCategoryMapping(1114, NewznabStandardCategory.TVDocumentary, "|- [Док] Паранормальные явления");
            caps.Categories.AddCategoryMapping(2168, NewznabStandardCategory.TVDocumentary, "|- [Док] Альтернативная история и наука");
            caps.Categories.AddCategoryMapping(2160, NewznabStandardCategory.TVDocumentary, "|- [Док] Внежанровая документалистика");
            caps.Categories.AddCategoryMapping(2176, NewznabStandardCategory.TVDocumentary, "|- [Док] Разное / некондиция");
            caps.Categories.AddCategoryMapping(314, NewznabStandardCategory.TVDocumentary, "Документальные (HD Video)");
            caps.Categories.AddCategoryMapping(2323, NewznabStandardCategory.TVDocumentary, "|- Информационно-аналитические и общественно-политические (HD Video)");
            caps.Categories.AddCategoryMapping(1278, NewznabStandardCategory.TVDocumentary, "|- Биографии. Личности и кумиры (HD Video)");
            caps.Categories.AddCategoryMapping(1281, NewznabStandardCategory.TVDocumentary, "|- Военное дело (HD Video)");
            caps.Categories.AddCategoryMapping(2110, NewznabStandardCategory.TVDocumentary, "|- Естествознаниенаука и техника (HD Video)");
            caps.Categories.AddCategoryMapping(979, NewznabStandardCategory.TVDocumentary, "|- Путешествия и туризм (HD Video)");
            caps.Categories.AddCategoryMapping(2169, NewznabStandardCategory.TVDocumentary, "|- Флора и фауна (HD Video)");
            caps.Categories.AddCategoryMapping(2166, NewznabStandardCategory.TVDocumentary, "|- История (HD Video)");
            caps.Categories.AddCategoryMapping(2164, NewznabStandardCategory.TVDocumentary, "|- BBCDiscovery, National Geographic (HD Video)");
            caps.Categories.AddCategoryMapping(2163, NewznabStandardCategory.TVDocumentary, "|- Криминальная документалистика (HD Video)");
            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.TVDocumentary, "Развлекательные телепередачи и шоуприколы и юмор");
            caps.Categories.AddCategoryMapping(1959, NewznabStandardCategory.TVOther, "|- [Видео Юмор] Интеллектуальные игры и викторины");
            caps.Categories.AddCategoryMapping(939, NewznabStandardCategory.TVOther, "|- [Видео Юмор] Реалити и ток-шоу / номинации / показы");
            caps.Categories.AddCategoryMapping(1481, NewznabStandardCategory.TVOther, "|- [Видео Юмор] Детские телешоу");
            caps.Categories.AddCategoryMapping(113, NewznabStandardCategory.TVOther, "|- [Видео Юмор] КВН");
            caps.Categories.AddCategoryMapping(115, NewznabStandardCategory.TVOther, "|- [Видео Юмор] Пост КВН");
            caps.Categories.AddCategoryMapping(882, NewznabStandardCategory.TVOther, "|- [Видео Юмор] Кривое Зеркало / Городок / В Городке");
            caps.Categories.AddCategoryMapping(1482, NewznabStandardCategory.TVOther, "|- [Видео Юмор] Ледовые шоу");
            caps.Categories.AddCategoryMapping(393, NewznabStandardCategory.TVOther, "|- [Видео Юмор] Музыкальные шоу");
            caps.Categories.AddCategoryMapping(1569, NewznabStandardCategory.TVOther, "|- [Видео Юмор] Званый ужин");
            caps.Categories.AddCategoryMapping(373, NewznabStandardCategory.TVOther, "|- [Видео Юмор] Хорошие Шутки");
            caps.Categories.AddCategoryMapping(1186, NewznabStandardCategory.TVOther, "|- [Видео Юмор] Вечерний Квартал");
            caps.Categories.AddCategoryMapping(137, NewznabStandardCategory.TVOther, "|- [Видео Юмор] Фильмы со смешным переводом (пародии)");
            caps.Categories.AddCategoryMapping(2537, NewznabStandardCategory.TVOther, "|- [Видео Юмор] Stand-up comedy");
            caps.Categories.AddCategoryMapping(532, NewznabStandardCategory.TVOther, "|- [Видео Юмор] Украинские Шоу");
            caps.Categories.AddCategoryMapping(827, NewznabStandardCategory.TVOther, "|- [Видео Юмор] Танцевальные шоуконцерты, выступления");
            caps.Categories.AddCategoryMapping(1484, NewznabStandardCategory.TVOther, "|- [Видео Юмор] Цирк");
            caps.Categories.AddCategoryMapping(1485, NewznabStandardCategory.TVOther, "|- [Видео Юмор] Школа злословия");
            caps.Categories.AddCategoryMapping(114, NewznabStandardCategory.TVOther, "|- [Видео Юмор] Сатирики и юмористы");
            caps.Categories.AddCategoryMapping(1332, NewznabStandardCategory.TVOther, "|- Юмористические аудиопередачи");
            caps.Categories.AddCategoryMapping(1495, NewznabStandardCategory.TVOther, "|- Аудио и видео ролики (Приколы и юмор)");
            caps.Categories.AddCategoryMapping(1315, NewznabStandardCategory.TVSport, "Зимние Олимпийские игры 2018");
            caps.Categories.AddCategoryMapping(1336, NewznabStandardCategory.TVSport, "|- Биатлон");
            caps.Categories.AddCategoryMapping(2171, NewznabStandardCategory.TVSport, "|- Лыжные гонки");
            caps.Categories.AddCategoryMapping(1339, NewznabStandardCategory.TVSport, "|- Прыжки на лыжах с трамплина / Лыжное двоеборье");
            caps.Categories.AddCategoryMapping(2455, NewznabStandardCategory.TVSport, "|- Горные лыжи / Сноубординг / Фристайл");
            caps.Categories.AddCategoryMapping(1434, NewznabStandardCategory.TVSport, "|- Бобслей / Санный спорт / Скелетон");
            caps.Categories.AddCategoryMapping(2350, NewznabStandardCategory.TVSport, "|- Конькобежный спорт / Шорт-трек");
            caps.Categories.AddCategoryMapping(1472, NewznabStandardCategory.TVSport, "|- Фигурное катание");
            caps.Categories.AddCategoryMapping(2068, NewznabStandardCategory.TVSport, "|- Хоккей");
            caps.Categories.AddCategoryMapping(2016, NewznabStandardCategory.TVSport, "|- Керлинг");
            caps.Categories.AddCategoryMapping(1311, NewznabStandardCategory.TVSport, "|- Обзорные и аналитические программы");
            caps.Categories.AddCategoryMapping(255, NewznabStandardCategory.TVSport, "Спортивные турнирыфильмы и передачи");
            caps.Categories.AddCategoryMapping(256, NewznabStandardCategory.TVSport, "|- Автоспорт");
            caps.Categories.AddCategoryMapping(1986, NewznabStandardCategory.TVSport, "|- Мотоспорт");
            caps.Categories.AddCategoryMapping(660, NewznabStandardCategory.TVSport, "|- Формула-1 (2020)");
            caps.Categories.AddCategoryMapping(1551, NewznabStandardCategory.TVSport, "|- Формула-1 (2012-2019)");
            caps.Categories.AddCategoryMapping(626, NewznabStandardCategory.TVSport, "|- Формула 1 (до 2011 вкл.)");
            caps.Categories.AddCategoryMapping(262, NewznabStandardCategory.TVSport, "|- Велоспорт");
            caps.Categories.AddCategoryMapping(1326, NewznabStandardCategory.TVSport, "|- Волейбол/Гандбол");
            caps.Categories.AddCategoryMapping(978, NewznabStandardCategory.TVSport, "|- Бильярд");
            caps.Categories.AddCategoryMapping(1287, NewznabStandardCategory.TVSport, "|- Покер");
            caps.Categories.AddCategoryMapping(1188, NewznabStandardCategory.TVSport, "|- Бодибилдинг/Силовые виды спорта");
            caps.Categories.AddCategoryMapping(1667, NewznabStandardCategory.TVSport, "|- Бокс");
            caps.Categories.AddCategoryMapping(1675, NewznabStandardCategory.TVSport, "|- Классические единоборства");
            caps.Categories.AddCategoryMapping(257, NewznabStandardCategory.TVSport, "|- Смешанные единоборства и K-1");
            caps.Categories.AddCategoryMapping(875, NewznabStandardCategory.TVSport, "|- Американский футбол");
            caps.Categories.AddCategoryMapping(263, NewznabStandardCategory.TVSport, "|- Регби");
            caps.Categories.AddCategoryMapping(2073, NewznabStandardCategory.TVSport, "|- Бейсбол");
            caps.Categories.AddCategoryMapping(550, NewznabStandardCategory.TVSport, "|- Теннис");
            caps.Categories.AddCategoryMapping(2124, NewznabStandardCategory.TVSport, "|- Бадминтон/Настольный теннис");
            caps.Categories.AddCategoryMapping(1470, NewznabStandardCategory.TVSport, "|- Гимнастика/Соревнования по танцам");
            caps.Categories.AddCategoryMapping(528, NewznabStandardCategory.TVSport, "|- Лёгкая атлетика/Водные виды спорта");
            caps.Categories.AddCategoryMapping(486, NewznabStandardCategory.TVSport, "|- Зимние виды спорта");
            caps.Categories.AddCategoryMapping(854, NewznabStandardCategory.TVSport, "|- Фигурное катание");
            caps.Categories.AddCategoryMapping(2079, NewznabStandardCategory.TVSport, "|- Биатлон");
            caps.Categories.AddCategoryMapping(260, NewznabStandardCategory.TVSport, "|- Экстрим");
            caps.Categories.AddCategoryMapping(1319, NewznabStandardCategory.TVSport, "|- Спорт (видео)");
            caps.Categories.AddCategoryMapping(1608, NewznabStandardCategory.TVSport, "⚽ Футбол");
            caps.Categories.AddCategoryMapping(2294, NewznabStandardCategory.TVSport, "|- UHDTV. Футбол в формате высокой четкости");
            caps.Categories.AddCategoryMapping(136, NewznabStandardCategory.TVSport, "|- Чемпионат Европы 2020 (квалификация)");
            caps.Categories.AddCategoryMapping(592, NewznabStandardCategory.TVSport, "|- Лига Наций");
            caps.Categories.AddCategoryMapping(1693, NewznabStandardCategory.TVSport, "|- Чемпионат Мира 2022 (отбор)");
            caps.Categories.AddCategoryMapping(2533, NewznabStandardCategory.TVSport, "|- Чемпионат Мира 2018 (игры)");
            caps.Categories.AddCategoryMapping(1952, NewznabStandardCategory.TVSport, "|- Чемпионат Мира 2018 (обзорные передачидокументалистика)");
            caps.Categories.AddCategoryMapping(1621, NewznabStandardCategory.TVSport, "|- Чемпионаты Мира");
            caps.Categories.AddCategoryMapping(2075, NewznabStandardCategory.TVSport, "|- Россия 2018-2019");
            caps.Categories.AddCategoryMapping(1668, NewznabStandardCategory.TVSport, "|- Россия 2019-2020");
            caps.Categories.AddCategoryMapping(1613, NewznabStandardCategory.TVSport, "|- Россия/СССР");
            caps.Categories.AddCategoryMapping(1614, NewznabStandardCategory.TVSport, "|- Англия");
            caps.Categories.AddCategoryMapping(1623, NewznabStandardCategory.TVSport, "|- Испания");
            caps.Categories.AddCategoryMapping(1615, NewznabStandardCategory.TVSport, "|- Италия");
            caps.Categories.AddCategoryMapping(1630, NewznabStandardCategory.TVSport, "|- Германия");
            caps.Categories.AddCategoryMapping(2425, NewznabStandardCategory.TVSport, "|- Франция");
            caps.Categories.AddCategoryMapping(2514, NewznabStandardCategory.TVSport, "|- Украина");
            caps.Categories.AddCategoryMapping(1616, NewznabStandardCategory.TVSport, "|- Другие национальные чемпионаты и кубки");
            caps.Categories.AddCategoryMapping(2014, NewznabStandardCategory.TVSport, "|- Международные турниры");
            caps.Categories.AddCategoryMapping(1442, NewznabStandardCategory.TVSport, "|- Еврокубки 2020-2021");
            caps.Categories.AddCategoryMapping(1491, NewznabStandardCategory.TVSport, "|- Еврокубки 2019-2020");
            caps.Categories.AddCategoryMapping(1987, NewznabStandardCategory.TVSport, "|- Еврокубки 2011-2018");
            caps.Categories.AddCategoryMapping(1617, NewznabStandardCategory.TVSport, "|- Еврокубки");
            caps.Categories.AddCategoryMapping(1620, NewznabStandardCategory.TVSport, "|- Чемпионаты Европы");
            caps.Categories.AddCategoryMapping(1998, NewznabStandardCategory.TVSport, "|- Товарищеские турниры и матчи");
            caps.Categories.AddCategoryMapping(1343, NewznabStandardCategory.TVSport, "|- Обзорные и аналитические передачи 2018-2020");
            caps.Categories.AddCategoryMapping(751, NewznabStandardCategory.TVSport, "|- Обзорные и аналитические передачи");
            caps.Categories.AddCategoryMapping(497, NewznabStandardCategory.TVSport, "|- Документальные фильмы (футбол)");
            caps.Categories.AddCategoryMapping(1697, NewznabStandardCategory.TVSport, "|- Мини-футбол/Пляжный футбол");
            caps.Categories.AddCategoryMapping(2004, NewznabStandardCategory.TVSport, "🏀 Баскетбол");
            caps.Categories.AddCategoryMapping(2001, NewznabStandardCategory.TVSport, "|- Международные соревнования");
            caps.Categories.AddCategoryMapping(2002, NewznabStandardCategory.TVSport, "|- NBA / NCAA (до 2000 г.)");
            caps.Categories.AddCategoryMapping(283, NewznabStandardCategory.TVSport, "|- NBA / NCAA (2000-2010 гг.)");
            caps.Categories.AddCategoryMapping(1997, NewznabStandardCategory.TVSport, "|- NBA / NCAA (2010-2020 гг.)");
            caps.Categories.AddCategoryMapping(2003, NewznabStandardCategory.TVSport, "|- Европейский клубный баскетбол");
            caps.Categories.AddCategoryMapping(2009, NewznabStandardCategory.TVSport, "🏒 Хоккей");
            caps.Categories.AddCategoryMapping(2010, NewznabStandardCategory.TVSport, "|- Хоккей с мячом / Бенди");
            caps.Categories.AddCategoryMapping(1229, NewznabStandardCategory.TVSport, "|- Чемпионат Мира по хоккею 2019");
            caps.Categories.AddCategoryMapping(2006, NewznabStandardCategory.TVSport, "|- Международные турниры");
            caps.Categories.AddCategoryMapping(2007, NewznabStandardCategory.TVSport, "|- КХЛ");
            caps.Categories.AddCategoryMapping(2005, NewznabStandardCategory.TVSport, "|- НХЛ (до 2011/12)");
            caps.Categories.AddCategoryMapping(259, NewznabStandardCategory.TVSport, "|- НХЛ (с 2013)");
            caps.Categories.AddCategoryMapping(2008, NewznabStandardCategory.TVSport, "|- СССР - Канада");
            caps.Categories.AddCategoryMapping(126, NewznabStandardCategory.TVSport, "|- Документальные фильмы и аналитика");
            caps.Categories.AddCategoryMapping(845, NewznabStandardCategory.TVSport, "Рестлинг");
            caps.Categories.AddCategoryMapping(343, NewznabStandardCategory.TVSport, "|- Professional Wrestling");
            caps.Categories.AddCategoryMapping(2111, NewznabStandardCategory.TVSport, "|- Independent Wrestling");
            caps.Categories.AddCategoryMapping(1527, NewznabStandardCategory.TVSport, "|- International Wrestling");
            caps.Categories.AddCategoryMapping(2069, NewznabStandardCategory.TVSport, "|- Oldschool Wrestling");
            caps.Categories.AddCategoryMapping(1323, NewznabStandardCategory.TVSport, "|- Documentary Wrestling");
            caps.Categories.AddCategoryMapping(1411, NewznabStandardCategory.TVSport, "|- Сканированиеобработка сканов");
            caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.Books, "Книги и журналы (общий раздел)");
            caps.Categories.AddCategoryMapping(2157, NewznabStandardCategory.Books, "|- Кинотеатр, ТВ, мультипликация, цирк");
            caps.Categories.AddCategoryMapping(765, NewznabStandardCategory.Books, "|- Рисунокграфический дизайн");
            caps.Categories.AddCategoryMapping(2019, NewznabStandardCategory.Books, "|- Фото и видеосъемка");
            caps.Categories.AddCategoryMapping(31, NewznabStandardCategory.BooksMags, "|- Журналы и газеты (общий раздел)");
            caps.Categories.AddCategoryMapping(1427, NewznabStandardCategory.Books, "|- Эзотерикагадания, магия, фен-шуй");
            caps.Categories.AddCategoryMapping(2422, NewznabStandardCategory.Books, "|- Астрология");
            caps.Categories.AddCategoryMapping(2195, NewznabStandardCategory.Books, "|- Красота. Уход. Домоводство");
            caps.Categories.AddCategoryMapping(2521, NewznabStandardCategory.Books, "|- Мода. Стиль. Этикет");
            caps.Categories.AddCategoryMapping(2223, NewznabStandardCategory.Books, "|- Путешествия и туризм");
            caps.Categories.AddCategoryMapping(2447, NewznabStandardCategory.Books, "|- Знаменитости и кумиры");
            caps.Categories.AddCategoryMapping(39, NewznabStandardCategory.Books, "|- Разное (книги)");
            caps.Categories.AddCategoryMapping(2086, NewznabStandardCategory.Books, "- Самиздатстатьи из журналов, фрагменты книг");
            caps.Categories.AddCategoryMapping(1101, NewznabStandardCategory.Books, "Для детейродителей и учителей");
            caps.Categories.AddCategoryMapping(745, NewznabStandardCategory.Books, "|- Учебная литература для детского сада и начальной школы (до 4 класс..");
            caps.Categories.AddCategoryMapping(1689, NewznabStandardCategory.Books, "|- Учебная литература для старших классов (5-11 класс)");
            caps.Categories.AddCategoryMapping(2336, NewznabStandardCategory.Books, "|- Учителям и педагогам");
            caps.Categories.AddCategoryMapping(2337, NewznabStandardCategory.Books, "|- Научно-популярная и познавательная литература (для детей)");
            caps.Categories.AddCategoryMapping(1353, NewznabStandardCategory.Books, "|- Досуг и творчество");
            caps.Categories.AddCategoryMapping(1400, NewznabStandardCategory.Books, "|- Воспитание и развитие");
            caps.Categories.AddCategoryMapping(1415, NewznabStandardCategory.Books, "|- Худ. лит-ра для дошкольников и младших классов");
            caps.Categories.AddCategoryMapping(2046, NewznabStandardCategory.Books, "|- Худ. лит-ра для средних и старших классов");
            caps.Categories.AddCategoryMapping(1802, NewznabStandardCategory.Books, "Спортфизическая культура, боевые искусства");
            caps.Categories.AddCategoryMapping(2189, NewznabStandardCategory.Books, "|- Футбол (книги и журналы)");
            caps.Categories.AddCategoryMapping(2190, NewznabStandardCategory.Books, "|- Хоккей (книги и журналы)");
            caps.Categories.AddCategoryMapping(2443, NewznabStandardCategory.Books, "|- Игровые виды спорта");
            caps.Categories.AddCategoryMapping(1477, NewznabStandardCategory.Books, "|- Легкая атлетика. Плавание. Гимнастика. Тяжелая атлетика. Гребля");
            caps.Categories.AddCategoryMapping(669, NewznabStandardCategory.Books, "|- Автоспорт. Мотоспорт. Велоспорт");
            caps.Categories.AddCategoryMapping(2196, NewznabStandardCategory.Books, "|- Шахматы. Шашки");
            caps.Categories.AddCategoryMapping(2056, NewznabStandardCategory.Books, "|- Боевые искусстваединоборства");
            caps.Categories.AddCategoryMapping(1436, NewznabStandardCategory.Books, "|- Экстрим (книги)");
            caps.Categories.AddCategoryMapping(2191, NewznabStandardCategory.Books, "|- Физкультурафитнес, бодибилдинг");
            caps.Categories.AddCategoryMapping(2477, NewznabStandardCategory.Books, "|- Спортивная пресса");
            caps.Categories.AddCategoryMapping(1680, NewznabStandardCategory.Books, "Гуманитарные науки");
            caps.Categories.AddCategoryMapping(1684, NewznabStandardCategory.Books, "|- Искусствоведение. Культурология");
            caps.Categories.AddCategoryMapping(2446, NewznabStandardCategory.Books, "|- Фольклор. Эпос. Мифология");
            caps.Categories.AddCategoryMapping(2524, NewznabStandardCategory.Books, "|- Литературоведение");
            caps.Categories.AddCategoryMapping(2525, NewznabStandardCategory.Books, "|- Лингвистика");
            caps.Categories.AddCategoryMapping(995, NewznabStandardCategory.Books, "|- Философия");
            caps.Categories.AddCategoryMapping(2022, NewznabStandardCategory.Books, "|- Политология");
            caps.Categories.AddCategoryMapping(2471, NewznabStandardCategory.Books, "|- Социология");
            caps.Categories.AddCategoryMapping(2375, NewznabStandardCategory.Books, "|- Публицистикажурналистика");
            caps.Categories.AddCategoryMapping(764, NewznabStandardCategory.Books, "|- Бизнесменеджмент");
            caps.Categories.AddCategoryMapping(1685, NewznabStandardCategory.Books, "|- Маркетинг");
            caps.Categories.AddCategoryMapping(1688, NewznabStandardCategory.Books, "|- Экономика");
            caps.Categories.AddCategoryMapping(2472, NewznabStandardCategory.Books, "|- Финансы");
            caps.Categories.AddCategoryMapping(1687, NewznabStandardCategory.Books, "|- Юридические науки. Право. Криминалистика");
            caps.Categories.AddCategoryMapping(2020, NewznabStandardCategory.Books, "Исторические науки");
            caps.Categories.AddCategoryMapping(1349, NewznabStandardCategory.Books, "|- Методология и философия исторической науки");
            caps.Categories.AddCategoryMapping(1967, NewznabStandardCategory.Books, "|- Исторические источники (книгипериодика)");
            caps.Categories.AddCategoryMapping(1341, NewznabStandardCategory.Books, "|- Исторические источники (документы)");
            caps.Categories.AddCategoryMapping(2049, NewznabStandardCategory.Books, "|- Исторические персоны");
            caps.Categories.AddCategoryMapping(1681, NewznabStandardCategory.Books, "|- Альтернативные исторические теории");
            caps.Categories.AddCategoryMapping(2319, NewznabStandardCategory.Books, "|- Археология");
            caps.Categories.AddCategoryMapping(2434, NewznabStandardCategory.Books, "|- Древний мир. Античность");
            caps.Categories.AddCategoryMapping(1683, NewznabStandardCategory.Books, "|- Средние века");
            caps.Categories.AddCategoryMapping(2444, NewznabStandardCategory.Books, "|- История Нового и Новейшего времени");
            caps.Categories.AddCategoryMapping(2427, NewznabStandardCategory.Books, "|- История Европы");
            caps.Categories.AddCategoryMapping(2452, NewznabStandardCategory.Books, "|- История Азии и Африки");
            caps.Categories.AddCategoryMapping(2445, NewznabStandardCategory.Books, "|- История АмерикиАвстралии, Океании");
            caps.Categories.AddCategoryMapping(2435, NewznabStandardCategory.Books, "|- История России");
            caps.Categories.AddCategoryMapping(667, NewznabStandardCategory.Books, "|- История России до 1917 года");
            caps.Categories.AddCategoryMapping(2436, NewznabStandardCategory.Books, "|- Эпоха СССР");
            caps.Categories.AddCategoryMapping(1335, NewznabStandardCategory.Books, "|- История России после 1991 года");
            caps.Categories.AddCategoryMapping(2453, NewznabStandardCategory.Books, "|- История стран бывшего СССР");
            caps.Categories.AddCategoryMapping(2320, NewznabStandardCategory.Books, "|- Этнографияантропология");
            caps.Categories.AddCategoryMapping(1801, NewznabStandardCategory.Books, "|- Международные отношения. Дипломатия");
            caps.Categories.AddCategoryMapping(2023, NewznabStandardCategory.BooksTechnical, "Точныеестественные и инженерные науки");
            caps.Categories.AddCategoryMapping(2024, NewznabStandardCategory.BooksTechnical, "|- Авиация / Космонавтика");
            caps.Categories.AddCategoryMapping(2026, NewznabStandardCategory.BooksTechnical, "|- Физика");
            caps.Categories.AddCategoryMapping(2192, NewznabStandardCategory.BooksTechnical, "|- Астрономия");
            caps.Categories.AddCategoryMapping(2027, NewznabStandardCategory.BooksTechnical, "|- Биология / Экология");
            caps.Categories.AddCategoryMapping(295, NewznabStandardCategory.BooksTechnical, "|- Химия / Биохимия");
            caps.Categories.AddCategoryMapping(2028, NewznabStandardCategory.BooksTechnical, "|- Математика");
            caps.Categories.AddCategoryMapping(2029, NewznabStandardCategory.BooksTechnical, "|- География / Геология / Геодезия");
            caps.Categories.AddCategoryMapping(1325, NewznabStandardCategory.BooksTechnical, "|- Электроника / Радио");
            caps.Categories.AddCategoryMapping(2386, NewznabStandardCategory.BooksTechnical, "|- Схемы и сервис-мануалы (оригинальная документация)");
            caps.Categories.AddCategoryMapping(2031, NewznabStandardCategory.BooksTechnical, "|- Архитектура / Строительство / Инженерные сети / Ландшафтный дизайн");
            caps.Categories.AddCategoryMapping(2030, NewznabStandardCategory.BooksTechnical, "|- Машиностроение");
            caps.Categories.AddCategoryMapping(2526, NewznabStandardCategory.BooksTechnical, "|- Сварка / Пайка / Неразрушающий контроль");
            caps.Categories.AddCategoryMapping(2527, NewznabStandardCategory.BooksTechnical, "|- Автоматизация / Робототехника");
            caps.Categories.AddCategoryMapping(2254, NewznabStandardCategory.BooksTechnical, "|- Металлургия / Материаловедение");
            caps.Categories.AddCategoryMapping(2376, NewznabStandardCategory.BooksTechnical, "|- Механикасопротивление материалов");
            caps.Categories.AddCategoryMapping(2054, NewznabStandardCategory.BooksTechnical, "|- Энергетика / электротехника");
            caps.Categories.AddCategoryMapping(770, NewznabStandardCategory.BooksTechnical, "|- Нефтянаягазовая и химическая промышленность");
            caps.Categories.AddCategoryMapping(2476, NewznabStandardCategory.BooksTechnical, "|- Сельское хозяйство и пищевая промышленность");
            caps.Categories.AddCategoryMapping(2494, NewznabStandardCategory.BooksTechnical, "|- Железнодорожное дело");
            caps.Categories.AddCategoryMapping(1528, NewznabStandardCategory.BooksTechnical, "|- Нормативная документация");
            caps.Categories.AddCategoryMapping(2032, NewznabStandardCategory.BooksTechnical, "|- Журналы: научныенаучно-популярные, радио и др.");
            caps.Categories.AddCategoryMapping(919, NewznabStandardCategory.Books, "Ноты и Музыкальная литература");
            caps.Categories.AddCategoryMapping(944, NewznabStandardCategory.Books, "|- Академическая музыка (Ноты и Media CD)");
            caps.Categories.AddCategoryMapping(980, NewznabStandardCategory.Books, "|- Другие направления (Нотытабулатуры)");
            caps.Categories.AddCategoryMapping(946, NewznabStandardCategory.Books, "|- Самоучители и Школы");
            caps.Categories.AddCategoryMapping(977, NewznabStandardCategory.Books, "|- Песенники (Songbooks)");
            caps.Categories.AddCategoryMapping(2074, NewznabStandardCategory.Books, "|- Музыкальная литература и Теория");
            caps.Categories.AddCategoryMapping(2349, NewznabStandardCategory.Books, "|- Музыкальные журналы");
            caps.Categories.AddCategoryMapping(768, NewznabStandardCategory.Books, "Военное дело");
            caps.Categories.AddCategoryMapping(2099, NewznabStandardCategory.Books, "|- Милитария");
            caps.Categories.AddCategoryMapping(2021, NewznabStandardCategory.Books, "|- Военная история");
            caps.Categories.AddCategoryMapping(2437, NewznabStandardCategory.Books, "|- История Второй мировой войны");
            caps.Categories.AddCategoryMapping(1337, NewznabStandardCategory.Books, "|- Биографии и мемуары военных деятелей");
            caps.Categories.AddCategoryMapping(1447, NewznabStandardCategory.Books, "|- Военная техника");
            caps.Categories.AddCategoryMapping(2468, NewznabStandardCategory.Books, "|- Стрелковое оружие");
            caps.Categories.AddCategoryMapping(2469, NewznabStandardCategory.Books, "|- Учебно-методическая литература");
            caps.Categories.AddCategoryMapping(2470, NewznabStandardCategory.Books, "|- Спецслужбы мира");
            caps.Categories.AddCategoryMapping(1686, NewznabStandardCategory.Books, "Вера и религия");
            caps.Categories.AddCategoryMapping(2215, NewznabStandardCategory.Books, "|- Христианство");
            caps.Categories.AddCategoryMapping(2216, NewznabStandardCategory.Books, "|- Ислам");
            caps.Categories.AddCategoryMapping(2217, NewznabStandardCategory.Books, "|- Религии ИндииТибета и Восточной Азии / Иудаизм");
            caps.Categories.AddCategoryMapping(2218, NewznabStandardCategory.Books, "|- Нетрадиционные религиозныедуховные и мистические учения");
            caps.Categories.AddCategoryMapping(2252, NewznabStandardCategory.Books, "|- Религиоведение. История Религии");
            caps.Categories.AddCategoryMapping(2543, NewznabStandardCategory.Books, "|- Атеизм. Научный атеизм");
            caps.Categories.AddCategoryMapping(767, NewznabStandardCategory.Books, "Психология");
            caps.Categories.AddCategoryMapping(2515, NewznabStandardCategory.Books, "|- Общая и прикладная психология");
            caps.Categories.AddCategoryMapping(2516, NewznabStandardCategory.Books, "|- Психотерапия и консультирование");
            caps.Categories.AddCategoryMapping(2517, NewznabStandardCategory.Books, "|- Психодиагностика и психокоррекция");
            caps.Categories.AddCategoryMapping(2518, NewznabStandardCategory.Books, "|- Социальная психология и психология отношений");
            caps.Categories.AddCategoryMapping(2519, NewznabStandardCategory.Books, "|- Тренинг и коучинг");
            caps.Categories.AddCategoryMapping(2520, NewznabStandardCategory.Books, "|- Саморазвитие и самосовершенствование");
            caps.Categories.AddCategoryMapping(1696, NewznabStandardCategory.Books, "|- Популярная психология");
            caps.Categories.AddCategoryMapping(2253, NewznabStandardCategory.Books, "|- Сексология. Взаимоотношения полов (18+)");
            caps.Categories.AddCategoryMapping(2033, NewznabStandardCategory.Books, "Коллекционированиеувлечения и хобби");
            caps.Categories.AddCategoryMapping(1412, NewznabStandardCategory.Books, "|- Коллекционирование и вспомогательные ист. дисциплины");
            caps.Categories.AddCategoryMapping(1446, NewznabStandardCategory.Books, "|- Вышивание");
            caps.Categories.AddCategoryMapping(753, NewznabStandardCategory.Books, "|- Вязание");
            caps.Categories.AddCategoryMapping(2037, NewznabStandardCategory.Books, "|- Шитьепэчворк");
            caps.Categories.AddCategoryMapping(2224, NewznabStandardCategory.Books, "|- Кружевоплетение");
            caps.Categories.AddCategoryMapping(2194, NewznabStandardCategory.Books, "|- Бисероплетение. Ювелирика. Украшения из проволоки.");
            caps.Categories.AddCategoryMapping(2418, NewznabStandardCategory.Books, "|- Бумажный арт");
            caps.Categories.AddCategoryMapping(1410, NewznabStandardCategory.Books, "|- Другие виды декоративно-прикладного искусства");
            caps.Categories.AddCategoryMapping(2034, NewznabStandardCategory.Books, "|- Домашние питомцы и аквариумистика");
            caps.Categories.AddCategoryMapping(2433, NewznabStandardCategory.Books, "|- Охота и рыбалка");
            caps.Categories.AddCategoryMapping(1961, NewznabStandardCategory.Books, "|- Кулинария (книги)");
            caps.Categories.AddCategoryMapping(2432, NewznabStandardCategory.Books, "|- Кулинария (газеты и журналы)");
            caps.Categories.AddCategoryMapping(565, NewznabStandardCategory.Books, "|- Моделизм");
            caps.Categories.AddCategoryMapping(1523, NewznabStandardCategory.Books, "|- Приусадебное хозяйство / Цветоводство");
            caps.Categories.AddCategoryMapping(1575, NewznabStandardCategory.Books, "|- Ремонтчастное строительство, дизайн интерьеров");
            caps.Categories.AddCategoryMapping(1520, NewznabStandardCategory.Books, "|- Деревообработка");
            caps.Categories.AddCategoryMapping(2424, NewznabStandardCategory.Books, "|- Настольные игры");
            caps.Categories.AddCategoryMapping(769, NewznabStandardCategory.Books, "|- Прочие хобби и игры");
            caps.Categories.AddCategoryMapping(2038, NewznabStandardCategory.Books, "Художественная литература");
            caps.Categories.AddCategoryMapping(2043, NewznabStandardCategory.Books, "|- Русская литература");
            caps.Categories.AddCategoryMapping(2042, NewznabStandardCategory.Books, "|- Зарубежная литература (до 1900 г.)");
            caps.Categories.AddCategoryMapping(2041, NewznabStandardCategory.Books, "|- Зарубежная литература (XX и XXI век)");
            caps.Categories.AddCategoryMapping(2044, NewznabStandardCategory.Books, "|- Детективбоевик");
            caps.Categories.AddCategoryMapping(2039, NewznabStandardCategory.Books, "|- Женский роман");
            caps.Categories.AddCategoryMapping(2045, NewznabStandardCategory.Books, "|- Отечественная фантастика / фэнтези / мистика");
            caps.Categories.AddCategoryMapping(2080, NewznabStandardCategory.Books, "|- Зарубежная фантастика / фэнтези / мистика");
            caps.Categories.AddCategoryMapping(2047, NewznabStandardCategory.Books, "|- Приключения");
            caps.Categories.AddCategoryMapping(2193, NewznabStandardCategory.Books, "|- Литературные журналы");
            caps.Categories.AddCategoryMapping(1037, NewznabStandardCategory.Books, "|- Самиздат и книгиизданные за счет авторов");
            caps.Categories.AddCategoryMapping(1418, NewznabStandardCategory.BooksTechnical, "Компьютерная литература");
            caps.Categories.AddCategoryMapping(1422, NewznabStandardCategory.BooksTechnical, "|- Программы от Microsoft");
            caps.Categories.AddCategoryMapping(1423, NewznabStandardCategory.BooksTechnical, "|- Другие программы");
            caps.Categories.AddCategoryMapping(1424, NewznabStandardCategory.BooksTechnical, "|- Mac OS; LinuxFreeBSD и прочие *NIX");
            caps.Categories.AddCategoryMapping(1445, NewznabStandardCategory.BooksTechnical, "|- СУБД");
            caps.Categories.AddCategoryMapping(1425, NewznabStandardCategory.BooksTechnical, "|- Веб-дизайн и программирование");
            caps.Categories.AddCategoryMapping(1426, NewznabStandardCategory.BooksTechnical, "|- Программирование (книги)");
            caps.Categories.AddCategoryMapping(1428, NewznabStandardCategory.BooksTechnical, "|- Графикаобработка видео");
            caps.Categories.AddCategoryMapping(1429, NewznabStandardCategory.BooksTechnical, "|- Сети / VoIP");
            caps.Categories.AddCategoryMapping(1430, NewznabStandardCategory.BooksTechnical, "|- Хакинг и безопасность");
            caps.Categories.AddCategoryMapping(1431, NewznabStandardCategory.BooksTechnical, "|- Железо (книги о ПК)");
            caps.Categories.AddCategoryMapping(1433, NewznabStandardCategory.BooksTechnical, "|- Инженерные и научные программы (книги)");
            caps.Categories.AddCategoryMapping(1432, NewznabStandardCategory.BooksTechnical, "|- Компьютерные журналы и приложения к ним");
            caps.Categories.AddCategoryMapping(2202, NewznabStandardCategory.BooksTechnical, "|- Дисковые приложения к игровым журналам");
            caps.Categories.AddCategoryMapping(862, NewznabStandardCategory.BooksComics, "Комиксыманга, ранобэ");
            caps.Categories.AddCategoryMapping(2461, NewznabStandardCategory.BooksComics, "|- Комиксы на русском языке");
            caps.Categories.AddCategoryMapping(2462, NewznabStandardCategory.BooksComics, "|- Комиксы издательства Marvel");
            caps.Categories.AddCategoryMapping(2463, NewznabStandardCategory.BooksComics, "|- Комиксы издательства DC");
            caps.Categories.AddCategoryMapping(2464, NewznabStandardCategory.BooksComics, "|- Комиксы других издательств");
            caps.Categories.AddCategoryMapping(2473, NewznabStandardCategory.BooksComics, "|- Комиксы на других языках");
            caps.Categories.AddCategoryMapping(281, NewznabStandardCategory.BooksComics, "|- Манга (на русском языке)");
            caps.Categories.AddCategoryMapping(2465, NewznabStandardCategory.BooksComics, "|- Манга (на иностранных языках)");
            caps.Categories.AddCategoryMapping(2458, NewznabStandardCategory.BooksComics, "|- Ранобэ");
            caps.Categories.AddCategoryMapping(2048, NewznabStandardCategory.BooksOther, "Коллекции книг и библиотеки");
            caps.Categories.AddCategoryMapping(1238, NewznabStandardCategory.BooksOther, "|- Библиотеки (зеркала сетевых библиотек/коллекций)");
            caps.Categories.AddCategoryMapping(2055, NewznabStandardCategory.BooksOther, "|- Тематические коллекции (подборки)");
            caps.Categories.AddCategoryMapping(754, NewznabStandardCategory.BooksOther, "|- Многопредметные коллекции (подборки)");
            caps.Categories.AddCategoryMapping(2114, NewznabStandardCategory.BooksEBook, "Мультимедийные и интерактивные издания");
            caps.Categories.AddCategoryMapping(2438, NewznabStandardCategory.BooksEBook, "|- Мультимедийные энциклопедии");
            caps.Categories.AddCategoryMapping(2439, NewznabStandardCategory.BooksEBook, "|- Интерактивные обучающие и развивающие материалы");
            caps.Categories.AddCategoryMapping(2440, NewznabStandardCategory.BooksEBook, "|- Обучающие издания для детей");
            caps.Categories.AddCategoryMapping(2441, NewznabStandardCategory.BooksEBook, "|- Кулинария. Цветоводство. Домоводство");
            caps.Categories.AddCategoryMapping(2442, NewznabStandardCategory.BooksEBook, "|- Культура. Искусство. История");
            caps.Categories.AddCategoryMapping(2125, NewznabStandardCategory.Books, "Медицина и здоровье");
            caps.Categories.AddCategoryMapping(2133, NewznabStandardCategory.Books, "|- Клиническая медицина до 1980 г.");
            caps.Categories.AddCategoryMapping(2130, NewznabStandardCategory.Books, "|- Клиническая медицина с 1980 по 2000 г.");
            caps.Categories.AddCategoryMapping(2313, NewznabStandardCategory.Books, "|- Клиническая медицина после 2000 г.");
            caps.Categories.AddCategoryMapping(2528, NewznabStandardCategory.Books, "|- Научная медицинская периодика (газеты и журналы)");
            caps.Categories.AddCategoryMapping(2129, NewznabStandardCategory.Books, "|- Медико-биологические науки");
            caps.Categories.AddCategoryMapping(2141, NewznabStandardCategory.Books, "|- Фармация и фармакология");
            caps.Categories.AddCategoryMapping(2314, NewznabStandardCategory.Books, "|- Популярная медицинская периодика (газеты и журналы)");
            caps.Categories.AddCategoryMapping(2132, NewznabStandardCategory.Books, "|- Нетрадиционнаянародная медицина и популярные книги о здоровье");
            caps.Categories.AddCategoryMapping(2131, NewznabStandardCategory.Books, "|- Ветеринарияразное");
            caps.Categories.AddCategoryMapping(2315, NewznabStandardCategory.Books, "|- Тематические коллекции книг");
            caps.Categories.AddCategoryMapping(2362, NewznabStandardCategory.BooksEBook, "Иностранные языки для взрослых");
            caps.Categories.AddCategoryMapping(1265, NewznabStandardCategory.BooksEBook, "|- Английский язык (для взрослых)");
            caps.Categories.AddCategoryMapping(1266, NewznabStandardCategory.BooksEBook, "|- Немецкий язык");
            caps.Categories.AddCategoryMapping(1267, NewznabStandardCategory.BooksEBook, "|- Французский язык");
            caps.Categories.AddCategoryMapping(1358, NewznabStandardCategory.BooksEBook, "|- Испанский язык");
            caps.Categories.AddCategoryMapping(2363, NewznabStandardCategory.BooksEBook, "|- Итальянский язык");
            caps.Categories.AddCategoryMapping(734, NewznabStandardCategory.BooksEBook, "|- Финский язык");
            caps.Categories.AddCategoryMapping(1268, NewznabStandardCategory.BooksEBook, "|- Другие европейские языки");
            caps.Categories.AddCategoryMapping(1673, NewznabStandardCategory.BooksEBook, "|- Арабский язык");
            caps.Categories.AddCategoryMapping(1269, NewznabStandardCategory.BooksEBook, "|- Китайский язык");
            caps.Categories.AddCategoryMapping(1270, NewznabStandardCategory.BooksEBook, "|- Японский язык");
            caps.Categories.AddCategoryMapping(1275, NewznabStandardCategory.BooksEBook, "|- Другие восточные языки");
            caps.Categories.AddCategoryMapping(2364, NewznabStandardCategory.BooksEBook, "|- Русский язык как иностранный");
            caps.Categories.AddCategoryMapping(1276, NewznabStandardCategory.BooksEBook, "|- Мультиязычные сборники и курсы");
            caps.Categories.AddCategoryMapping(2094, NewznabStandardCategory.BooksEBook, "|- LIM-курсы");
            caps.Categories.AddCategoryMapping(1274, NewznabStandardCategory.BooksEBook, "|- Разное (иностранные языки)");
            caps.Categories.AddCategoryMapping(1264, NewznabStandardCategory.BooksEBook, "Иностранные языки для детей");
            caps.Categories.AddCategoryMapping(2358, NewznabStandardCategory.BooksEBook, "|- Английский язык (для детей)");
            caps.Categories.AddCategoryMapping(2359, NewznabStandardCategory.BooksEBook, "|- Другие европейские языки (для детей)");
            caps.Categories.AddCategoryMapping(2360, NewznabStandardCategory.BooksEBook, "|- Восточные языки (для детей)");
            caps.Categories.AddCategoryMapping(2361, NewznabStandardCategory.BooksEBook, "|- Школьные учебникиЕГЭ");
            caps.Categories.AddCategoryMapping(2057, NewznabStandardCategory.BooksEBook, "Художественная литература (ин.языки)");
            caps.Categories.AddCategoryMapping(2355, NewznabStandardCategory.BooksEBook, "|- Художественная литература на английском языке");
            caps.Categories.AddCategoryMapping(2474, NewznabStandardCategory.BooksEBook, "|- Художественная литература на французском языке");
            caps.Categories.AddCategoryMapping(2356, NewznabStandardCategory.BooksEBook, "|- Художественная литература на других европейских языках");
            caps.Categories.AddCategoryMapping(2357, NewznabStandardCategory.BooksEBook, "|- Художественная литература на восточных языках");
            caps.Categories.AddCategoryMapping(2413, NewznabStandardCategory.AudioAudiobook, "Аудиокниги на иностранных языках");
            caps.Categories.AddCategoryMapping(1501, NewznabStandardCategory.AudioAudiobook, "|- Аудиокниги на английском языке");
            caps.Categories.AddCategoryMapping(1580, NewznabStandardCategory.AudioAudiobook, "|- Аудиокниги на немецком языке");
            caps.Categories.AddCategoryMapping(525, NewznabStandardCategory.AudioAudiobook, "|- Аудиокниги на других иностранных языках");
            caps.Categories.AddCategoryMapping(610, NewznabStandardCategory.BooksOther, "Видеоуроки и обучающие интерактивные DVD");
            caps.Categories.AddCategoryMapping(1568, NewznabStandardCategory.BooksOther, "|- Кулинария");
            caps.Categories.AddCategoryMapping(1542, NewznabStandardCategory.BooksOther, "|- Спорт");
            caps.Categories.AddCategoryMapping(2335, NewznabStandardCategory.BooksOther, "|- Фитнес - Кардио-Силовые Тренировки");
            caps.Categories.AddCategoryMapping(1544, NewznabStandardCategory.BooksOther, "|- Фитнес - Разум и Тело");
            caps.Categories.AddCategoryMapping(1546, NewznabStandardCategory.BooksOther, "|- Бодибилдинг");
            caps.Categories.AddCategoryMapping(1549, NewznabStandardCategory.BooksOther, "|- Оздоровительные практики");
            caps.Categories.AddCategoryMapping(1597, NewznabStandardCategory.BooksOther, "|- Йога");
            caps.Categories.AddCategoryMapping(1552, NewznabStandardCategory.BooksOther, "|- Видео- и фотосъёмка");
            caps.Categories.AddCategoryMapping(1550, NewznabStandardCategory.BooksOther, "|- Уход за собой");
            caps.Categories.AddCategoryMapping(1553, NewznabStandardCategory.BooksOther, "|- Рисование");
            caps.Categories.AddCategoryMapping(1554, NewznabStandardCategory.BooksOther, "|- Игра на гитаре");
            caps.Categories.AddCategoryMapping(617, NewznabStandardCategory.BooksOther, "|- Ударные инструменты");
            caps.Categories.AddCategoryMapping(1555, NewznabStandardCategory.BooksOther, "|- Другие музыкальные инструменты");
            caps.Categories.AddCategoryMapping(2017, NewznabStandardCategory.BooksOther, "|- Игра на бас-гитаре");
            caps.Categories.AddCategoryMapping(1257, NewznabStandardCategory.BooksOther, "|- Бальные танцы");
            caps.Categories.AddCategoryMapping(1258, NewznabStandardCategory.BooksOther, "|- Танец живота");
            caps.Categories.AddCategoryMapping(2208, NewznabStandardCategory.BooksOther, "|- Уличные и клубные танцы");
            caps.Categories.AddCategoryMapping(677, NewznabStandardCategory.BooksOther, "|- Танцыразное");
            caps.Categories.AddCategoryMapping(1255, NewznabStandardCategory.BooksOther, "|- Охота");
            caps.Categories.AddCategoryMapping(1479, NewznabStandardCategory.BooksOther, "|- Рыболовство и подводная охота");
            caps.Categories.AddCategoryMapping(1261, NewznabStandardCategory.BooksOther, "|- Фокусы и трюки");
            caps.Categories.AddCategoryMapping(614, NewznabStandardCategory.BooksOther, "|- Образование");
            caps.Categories.AddCategoryMapping(1583, NewznabStandardCategory.BooksOther, "|- Финансы");
            caps.Categories.AddCategoryMapping(1259, NewznabStandardCategory.BooksOther, "|- Продажибизнес");
            caps.Categories.AddCategoryMapping(2065, NewznabStandardCategory.BooksOther, "|- Беременностьроды, материнство");
            caps.Categories.AddCategoryMapping(1254, NewznabStandardCategory.BooksOther, "|- Учебные видео для детей");
            caps.Categories.AddCategoryMapping(1260, NewznabStandardCategory.BooksOther, "|- Психология");
            caps.Categories.AddCategoryMapping(2209, NewznabStandardCategory.BooksOther, "|- Эзотерикасаморазвитие");
            caps.Categories.AddCategoryMapping(2210, NewznabStandardCategory.BooksOther, "|- Пикапзнакомства");
            caps.Categories.AddCategoryMapping(1547, NewznabStandardCategory.BooksOther, "|- Строительстворемонт и дизайн");
            caps.Categories.AddCategoryMapping(1548, NewznabStandardCategory.BooksOther, "|- Дерево- и металлообработка");
            caps.Categories.AddCategoryMapping(2211, NewznabStandardCategory.BooksOther, "|- Растения и животные");
            caps.Categories.AddCategoryMapping(1596, NewznabStandardCategory.BooksOther, "|- Хобби и рукоделие");
            caps.Categories.AddCategoryMapping(2135, NewznabStandardCategory.BooksOther, "|- Медицина и стоматология");
            caps.Categories.AddCategoryMapping(2140, NewznabStandardCategory.BooksOther, "|- Психотерапия и клиническая психология");
            caps.Categories.AddCategoryMapping(2136, NewznabStandardCategory.BooksOther, "|- Массаж");
            caps.Categories.AddCategoryMapping(2138, NewznabStandardCategory.BooksOther, "|- Здоровье");
            caps.Categories.AddCategoryMapping(615, NewznabStandardCategory.BooksOther, "|- Разное");
            caps.Categories.AddCategoryMapping(1581, NewznabStandardCategory.BooksOther, "Боевые искусства (Видеоуроки)");
            caps.Categories.AddCategoryMapping(1590, NewznabStandardCategory.BooksOther, "|- Айкидо и айки-дзюцу");
            caps.Categories.AddCategoryMapping(1587, NewznabStandardCategory.BooksOther, "|- Вин чун");
            caps.Categories.AddCategoryMapping(1594, NewznabStandardCategory.BooksOther, "|- Джиу-джитсу");
            caps.Categories.AddCategoryMapping(1591, NewznabStandardCategory.BooksOther, "|- Дзюдо и самбо");
            caps.Categories.AddCategoryMapping(1588, NewznabStandardCategory.BooksOther, "|- Каратэ");
            caps.Categories.AddCategoryMapping(1585, NewznabStandardCategory.BooksOther, "|- Работа с оружием");
            caps.Categories.AddCategoryMapping(1586, NewznabStandardCategory.BooksOther, "|- Русский стиль");
            caps.Categories.AddCategoryMapping(2078, NewznabStandardCategory.BooksOther, "|- Рукопашный бой");
            caps.Categories.AddCategoryMapping(1929, NewznabStandardCategory.BooksOther, "|- Смешанные стили");
            caps.Categories.AddCategoryMapping(1593, NewznabStandardCategory.BooksOther, "|- Ударные стили");
            caps.Categories.AddCategoryMapping(1592, NewznabStandardCategory.BooksOther, "|- Ушу");
            caps.Categories.AddCategoryMapping(1595, NewznabStandardCategory.BooksOther, "|- Разное");
            caps.Categories.AddCategoryMapping(1556, NewznabStandardCategory.BooksTechnical, "Компьютерные видеоуроки и обучающие интерактивные DVD");
            caps.Categories.AddCategoryMapping(1560, NewznabStandardCategory.BooksTechnical, "|- Компьютерные сети и безопасность");
            caps.Categories.AddCategoryMapping(1991, NewznabStandardCategory.BooksTechnical, "|- Devops");
            caps.Categories.AddCategoryMapping(1561, NewznabStandardCategory.BooksTechnical, "|- ОС и серверные программы Microsoft");
            caps.Categories.AddCategoryMapping(1653, NewznabStandardCategory.BooksTechnical, "|- Офисные программы Microsoft");
            caps.Categories.AddCategoryMapping(1570, NewznabStandardCategory.BooksTechnical, "|- ОС и программы семейства UNIX");
            caps.Categories.AddCategoryMapping(1654, NewznabStandardCategory.BooksTechnical, "|- Adobe Photoshop");
            caps.Categories.AddCategoryMapping(1655, NewznabStandardCategory.BooksTechnical, "|- Autodesk Maya");
            caps.Categories.AddCategoryMapping(1656, NewznabStandardCategory.BooksTechnical, "|- Autodesk 3ds Max");
            caps.Categories.AddCategoryMapping(1930, NewznabStandardCategory.BooksTechnical, "|- Autodesk Softimage (XSI)");
            caps.Categories.AddCategoryMapping(1931, NewznabStandardCategory.BooksTechnical, "|- ZBrush");
            caps.Categories.AddCategoryMapping(1932, NewznabStandardCategory.BooksTechnical, "|- FlashFlex и ActionScript");
            caps.Categories.AddCategoryMapping(1562, NewznabStandardCategory.BooksTechnical, "|- 2D-графика");
            caps.Categories.AddCategoryMapping(1563, NewznabStandardCategory.BooksTechnical, "|- 3D-графика");
            caps.Categories.AddCategoryMapping(1626, NewznabStandardCategory.BooksTechnical, "|- Инженерные и научные программы (видеоуроки)");
            caps.Categories.AddCategoryMapping(1564, NewznabStandardCategory.BooksTechnical, "|- Web-дизайн");
            caps.Categories.AddCategoryMapping(1545, NewznabStandardCategory.BooksTechnical, "|- WEBSMM, SEO, интернет-маркетинг");
            caps.Categories.AddCategoryMapping(1565, NewznabStandardCategory.BooksTechnical, "|- Программирование (видеоуроки)");
            caps.Categories.AddCategoryMapping(1559, NewznabStandardCategory.BooksTechnical, "|- Программы для Mac OS");
            caps.Categories.AddCategoryMapping(1566, NewznabStandardCategory.BooksTechnical, "|- Работа с видео");
            caps.Categories.AddCategoryMapping(1573, NewznabStandardCategory.BooksTechnical, "|- Работа со звуком");
            caps.Categories.AddCategoryMapping(1567, NewznabStandardCategory.BooksTechnical, "|- Разное (Компьютерные видеоуроки)");
            caps.Categories.AddCategoryMapping(2326, NewznabStandardCategory.AudioAudiobook, "Радиоспектаклиистория, мемуары");
            caps.Categories.AddCategoryMapping(574, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Радиоспектакли и литературные чтения");
            caps.Categories.AddCategoryMapping(1036, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Жизнь замечательных людей");
            caps.Categories.AddCategoryMapping(400, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Историякультурология, философия");
            caps.Categories.AddCategoryMapping(2389, NewznabStandardCategory.AudioAudiobook, "Фантастикафэнтези, мистика, ужасы, фанфики");
            caps.Categories.AddCategoryMapping(2388, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Зарубежная фантастикафэнтези, мистика, ужасы, фанфики");
            caps.Categories.AddCategoryMapping(2387, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Российская фантастикафэнтези, мистика, ужасы, фанфики");
            caps.Categories.AddCategoryMapping(661, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Любовно-фантастический роман");
            caps.Categories.AddCategoryMapping(2348, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Сборники/разное Фантастикафэнтези, мистика, ужасы, фанфи..");
            caps.Categories.AddCategoryMapping(2327, NewznabStandardCategory.AudioAudiobook, "Художественная литература");
            caps.Categories.AddCategoryMapping(695, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Поэзия");
            caps.Categories.AddCategoryMapping(399, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Зарубежная литература");
            caps.Categories.AddCategoryMapping(402, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Русская литература");
            caps.Categories.AddCategoryMapping(467, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Современные любовные романы");
            caps.Categories.AddCategoryMapping(490, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Детская литература");
            caps.Categories.AddCategoryMapping(499, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Зарубежные детективыприключения, триллеры, боевики");
            caps.Categories.AddCategoryMapping(2137, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Российские детективыприключения, триллеры, боевики");
            caps.Categories.AddCategoryMapping(2127, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Азиатская подростковая литератураранобэ, веб-новеллы");
            caps.Categories.AddCategoryMapping(2324, NewznabStandardCategory.AudioAudiobook, "Религии");
            caps.Categories.AddCategoryMapping(2325, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Православие");
            caps.Categories.AddCategoryMapping(2342, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Ислам");
            caps.Categories.AddCategoryMapping(530, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Другие традиционные религии");
            caps.Categories.AddCategoryMapping(2152, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Нетрадиционные религиозно-философские учения");
            caps.Categories.AddCategoryMapping(2328, NewznabStandardCategory.AudioAudiobook, "Прочая литература");
            caps.Categories.AddCategoryMapping(1350, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Книги по медицине");
            caps.Categories.AddCategoryMapping(403, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Учебная и научно-популярная литература");
            caps.Categories.AddCategoryMapping(1279, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] lossless-аудиокниги");
            caps.Categories.AddCategoryMapping(716, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Бизнес");
            caps.Categories.AddCategoryMapping(2165, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Разное");
            caps.Categories.AddCategoryMapping(401, NewznabStandardCategory.AudioAudiobook, "|- [Аудио] Некондиционные раздачи");
            caps.Categories.AddCategoryMapping(1964, NewznabStandardCategory.Books, "Ремонт и эксплуатация транспортных средств");
            caps.Categories.AddCategoryMapping(1973, NewznabStandardCategory.Books, "|- Оригинальные каталоги по подбору запчастей");
            caps.Categories.AddCategoryMapping(1974, NewznabStandardCategory.Books, "|- Неоригинальные каталоги по подбору запчастей");
            caps.Categories.AddCategoryMapping(1975, NewznabStandardCategory.Books, "|- Программы по диагностике и ремонту");
            caps.Categories.AddCategoryMapping(1976, NewznabStandardCategory.Books, "|- Тюнингчиптюнинг, настройка");
            caps.Categories.AddCategoryMapping(1977, NewznabStandardCategory.Books, "|- Книги по ремонту/обслуживанию/эксплуатации ТС");
            caps.Categories.AddCategoryMapping(1203, NewznabStandardCategory.Books, "|- Мультимедийки по ремонту/обслуживанию/эксплуатации ТС");
            caps.Categories.AddCategoryMapping(1978, NewznabStandardCategory.Books, "|- Учетутилиты и прочее");
            caps.Categories.AddCategoryMapping(1979, NewznabStandardCategory.Books, "|- Виртуальная автошкола");
            caps.Categories.AddCategoryMapping(1980, NewznabStandardCategory.Books, "|- Видеоуроки по вождению транспортных средств");
            caps.Categories.AddCategoryMapping(1981, NewznabStandardCategory.Books, "|- Видеоуроки по ремонту транспортных средств");
            caps.Categories.AddCategoryMapping(1970, NewznabStandardCategory.Books, "|- Журналы по авто/мото");
            caps.Categories.AddCategoryMapping(334, NewznabStandardCategory.Books, "|- Водный транспорт");
            caps.Categories.AddCategoryMapping(1202, NewznabStandardCategory.TVDocumentary, "Фильмы и передачи по авто/мото");
            caps.Categories.AddCategoryMapping(1985, NewznabStandardCategory.TVDocumentary, "|- Документальные/познавательные фильмы");
            caps.Categories.AddCategoryMapping(1982, NewznabStandardCategory.TVOther, "|- Развлекательные передачи");
            caps.Categories.AddCategoryMapping(2151, NewznabStandardCategory.TVDocumentary, "|- Top Gear/Топ Гир");
            caps.Categories.AddCategoryMapping(1983, NewznabStandardCategory.TVDocumentary, "|- Тест драйв/Обзоры/Автосалоны");
            caps.Categories.AddCategoryMapping(1984, NewznabStandardCategory.TVDocumentary, "|- Тюнинг/форсаж");
            caps.Categories.AddCategoryMapping(409, NewznabStandardCategory.Audio, "Классическая и современная академическая музыка");
            caps.Categories.AddCategoryMapping(560, NewznabStandardCategory.AudioLossless, "|- Полные собрания сочинений и многодисковые издания (lossless)");
            caps.Categories.AddCategoryMapping(794, NewznabStandardCategory.AudioLossless, "|- Опера (lossless)");
            caps.Categories.AddCategoryMapping(556, NewznabStandardCategory.AudioLossless, "|- Вокальная музыка (lossless)");
            caps.Categories.AddCategoryMapping(2307, NewznabStandardCategory.AudioLossless, "|- Хоровая музыка (lossless)");
            caps.Categories.AddCategoryMapping(557, NewznabStandardCategory.AudioLossless, "|- Оркестровая музыка (lossless)");
            caps.Categories.AddCategoryMapping(2308, NewznabStandardCategory.AudioLossless, "|- Концерт для инструмента с оркестром (lossless)");
            caps.Categories.AddCategoryMapping(558, NewznabStandardCategory.AudioLossless, "|- Камерная инструментальная музыка (lossless)");
            caps.Categories.AddCategoryMapping(793, NewznabStandardCategory.AudioLossless, "|- Сольная инструментальная музыка (lossless)");
            caps.Categories.AddCategoryMapping(1395, NewznabStandardCategory.AudioLossless, "|- Духовные песнопения и музыка (lossless)");
            caps.Categories.AddCategoryMapping(1396, NewznabStandardCategory.AudioMP3, "|- Духовные песнопения и музыка (lossy)");
            caps.Categories.AddCategoryMapping(436, NewznabStandardCategory.AudioMP3, "|- Полные собрания сочинений и многодисковые издания (lossy)");
            caps.Categories.AddCategoryMapping(2309, NewznabStandardCategory.AudioMP3, "|- Вокальная и хоровая музыка (lossy)");
            caps.Categories.AddCategoryMapping(2310, NewznabStandardCategory.AudioMP3, "|- Оркестровая музыка (lossy)");
            caps.Categories.AddCategoryMapping(2311, NewznabStandardCategory.AudioMP3, "|- Камерная и сольная инструментальная музыка (lossy)");
            caps.Categories.AddCategoryMapping(969, NewznabStandardCategory.Audio, "|- Классика в современной обработкеClassical Crossover (lossy и los..");
            caps.Categories.AddCategoryMapping(1125, NewznabStandardCategory.Audio, "ФольклорНародная и Этническая музыка");
            caps.Categories.AddCategoryMapping(1130, NewznabStandardCategory.AudioMP3, "|- Восточноевропейский фолк (lossy)");
            caps.Categories.AddCategoryMapping(1131, NewznabStandardCategory.AudioLossless, "|- Восточноевропейский фолк (lossless)");
            caps.Categories.AddCategoryMapping(1132, NewznabStandardCategory.AudioMP3, "|- Западноевропейский фолк (lossy)");
            caps.Categories.AddCategoryMapping(1133, NewznabStandardCategory.AudioLossless, "|- Западноевропейский фолк (lossless)");
            caps.Categories.AddCategoryMapping(2084, NewznabStandardCategory.Audio, "|- Klezmer и Еврейский фольклор (lossy и lossless)");
            caps.Categories.AddCategoryMapping(1128, NewznabStandardCategory.AudioMP3, "|- Этническая музыка СибириСредней и Восточной Азии (lossy)");
            caps.Categories.AddCategoryMapping(1129, NewznabStandardCategory.AudioLossless, "|- Этническая музыка СибириСредней и Восточной Азии (lossless)");
            caps.Categories.AddCategoryMapping(1856, NewznabStandardCategory.AudioMP3, "|- Этническая музыка Индии (lossy)");
            caps.Categories.AddCategoryMapping(2430, NewznabStandardCategory.AudioLossless, "|- Этническая музыка Индии (lossless)");
            caps.Categories.AddCategoryMapping(1283, NewznabStandardCategory.AudioMP3, "|- Этническая музыка Африки и Ближнего Востока (lossy)");
            caps.Categories.AddCategoryMapping(2085, NewznabStandardCategory.AudioLossless, "|- Этническая музыка Африки и Ближнего Востока (lossless)");
            caps.Categories.AddCategoryMapping(1282, NewznabStandardCategory.Audio, "|- ФольклорнаяНародная, Эстрадная музыка Кавказа и Закавказья (loss..");
            caps.Categories.AddCategoryMapping(1284, NewznabStandardCategory.AudioMP3, "|- Этническая музыка Северной и Южной Америки (lossy)");
            caps.Categories.AddCategoryMapping(1285, NewznabStandardCategory.AudioLossless, "|- Этническая музыка Северной и Южной Америки (lossless)");
            caps.Categories.AddCategoryMapping(1138, NewznabStandardCategory.Audio, "|- Этническая музыка АвстралииТихого и Индийского океанов (lossy и ..");
            caps.Categories.AddCategoryMapping(1136, NewznabStandardCategory.AudioMP3, "|- CountryBluegrass (lossy)");
            caps.Categories.AddCategoryMapping(1137, NewznabStandardCategory.AudioLossless, "|- CountryBluegrass (lossless)");
            caps.Categories.AddCategoryMapping(1849, NewznabStandardCategory.Audio, "New AgeRelax, Meditative & Flamenco");
            caps.Categories.AddCategoryMapping(1126, NewznabStandardCategory.AudioMP3, "|- New Age & Meditative (lossy)");
            caps.Categories.AddCategoryMapping(1127, NewznabStandardCategory.AudioLossless, "|- New Age & Meditative (lossless)");
            caps.Categories.AddCategoryMapping(1134, NewznabStandardCategory.AudioMP3, "|- Фламенко и акустическая гитара (lossy)");
            caps.Categories.AddCategoryMapping(1135, NewznabStandardCategory.AudioLossless, "|- Фламенко и акустическая гитара (lossless)");
            caps.Categories.AddCategoryMapping(2018, NewznabStandardCategory.Audio, "|- Музыка для бальных танцев (lossy и lossless)");
            caps.Categories.AddCategoryMapping(855, NewznabStandardCategory.Audio, "|- Звуки природы");
            caps.Categories.AddCategoryMapping(408, NewznabStandardCategory.Audio, "РэпХип-Хоп, R'n'B");
            caps.Categories.AddCategoryMapping(441, NewznabStandardCategory.AudioMP3, "|- Отечественный РэпХип-Хоп (lossy)");
            caps.Categories.AddCategoryMapping(1173, NewznabStandardCategory.AudioMP3, "|- Отечественный R'n'B (lossy)");
            caps.Categories.AddCategoryMapping(1486, NewznabStandardCategory.AudioLossless, "|- Отечественный РэпХип-Хоп, R'n'B (lossless)");
            caps.Categories.AddCategoryMapping(1172, NewznabStandardCategory.AudioMP3, "|- Зарубежный R'n'B (lossy)");
            caps.Categories.AddCategoryMapping(446, NewznabStandardCategory.AudioMP3, "|- Зарубежный РэпХип-Хоп (lossy)");
            caps.Categories.AddCategoryMapping(909, NewznabStandardCategory.AudioLossless, "|- Зарубежный РэпХип-Хоп (lossless)");
            caps.Categories.AddCategoryMapping(1665, NewznabStandardCategory.AudioLossless, "|- Зарубежный R'n'B (lossless)");
            caps.Categories.AddCategoryMapping(1760, NewznabStandardCategory.Audio, "ReggaeSka, Dub");
            caps.Categories.AddCategoryMapping(1764, NewznabStandardCategory.Audio, "|- RocksteadyEarly Reggae, Ska-Jazz, Trad.Ska (lossy и lossless)");
            caps.Categories.AddCategoryMapping(1767, NewznabStandardCategory.AudioMP3, "|- 3rd Wave Ska (lossy)");
            caps.Categories.AddCategoryMapping(1769, NewznabStandardCategory.AudioMP3, "|- Ska-PunkSka-Core (lossy)");
            caps.Categories.AddCategoryMapping(1765, NewznabStandardCategory.AudioMP3, "|- Reggae (lossy)");
            caps.Categories.AddCategoryMapping(1771, NewznabStandardCategory.AudioMP3, "|- Dub (lossy)");
            caps.Categories.AddCategoryMapping(1770, NewznabStandardCategory.AudioMP3, "|- DancehallRaggamuffin (lossy)");
            caps.Categories.AddCategoryMapping(1768, NewznabStandardCategory.AudioLossless, "|- ReggaeDancehall, Dub (lossless)");
            caps.Categories.AddCategoryMapping(1774, NewznabStandardCategory.AudioLossless, "|- SkaSka-Punk, Ska-Jazz (lossless)");
            caps.Categories.AddCategoryMapping(1772, NewznabStandardCategory.Audio, "|- Отечественный ReggaeDub (lossy и lossless)");
            caps.Categories.AddCategoryMapping(1773, NewznabStandardCategory.Audio, "|- Отечественная Ska музыка (lossy и lossless)");
            caps.Categories.AddCategoryMapping(2233, NewznabStandardCategory.Audio, "|- ReggaeSka, Dub (компиляции) (lossy и lossless)");
            caps.Categories.AddCategoryMapping(416, NewznabStandardCategory.Audio, "Саундтрекикараоке и мюзиклы");
            caps.Categories.AddCategoryMapping(2377, NewznabStandardCategory.AudioVideo, "|- Караоке (видео)");
            caps.Categories.AddCategoryMapping(468, NewznabStandardCategory.Audio, "|- Минусовки (lossy и lossless)");
            caps.Categories.AddCategoryMapping(691, NewznabStandardCategory.AudioLossless, "|- Саундтреки к отечественным фильмам (lossless)");
            caps.Categories.AddCategoryMapping(469, NewznabStandardCategory.AudioMP3, "|- Саундтреки к отечественным фильмам (lossy)");
            caps.Categories.AddCategoryMapping(786, NewznabStandardCategory.AudioLossless, "|- Саундтреки к зарубежным фильмам (lossless)");
            caps.Categories.AddCategoryMapping(785, NewznabStandardCategory.AudioMP3, "|- Саундтреки к зарубежным фильмам (lossy)");
            caps.Categories.AddCategoryMapping(1631, NewznabStandardCategory.AudioLossless, "|- Саундтреки к сериалам (lossless)");
            caps.Categories.AddCategoryMapping(1499, NewznabStandardCategory.AudioMP3, "|- Саундтреки к сериалам (lossy)");
            caps.Categories.AddCategoryMapping(715, NewznabStandardCategory.Audio, "|- Саундтреки к мультфильмам (lossy и lossless)");
            caps.Categories.AddCategoryMapping(1388, NewznabStandardCategory.AudioLossless, "|- Саундтреки к аниме (lossless)");
            caps.Categories.AddCategoryMapping(282, NewznabStandardCategory.AudioMP3, "|- Саундтреки к аниме (lossy)");
            caps.Categories.AddCategoryMapping(796, NewznabStandardCategory.AudioMP3, "|- Неофициальные саундтреки к фильмам и сериалам (lossy)");
            caps.Categories.AddCategoryMapping(784, NewznabStandardCategory.AudioLossless, "|- Саундтреки к играм (lossless)");
            caps.Categories.AddCategoryMapping(783, NewznabStandardCategory.AudioMP3, "|- Саундтреки к играм (lossy)");
            caps.Categories.AddCategoryMapping(2331, NewznabStandardCategory.AudioMP3, "|- Неофициальные саундтреки к играм (lossy)");
            caps.Categories.AddCategoryMapping(2431, NewznabStandardCategory.Audio, "|- Аранжировки музыки из игр (lossy и lossless)");
            caps.Categories.AddCategoryMapping(880, NewznabStandardCategory.Audio, "|- Мюзикл (lossy и lossless)");
            caps.Categories.AddCategoryMapping(1215, NewznabStandardCategory.Audio, "ШансонАвторская и Военная песня");
            caps.Categories.AddCategoryMapping(1220, NewznabStandardCategory.AudioLossless, "|- Отечественный шансон (lossless)");
            caps.Categories.AddCategoryMapping(1221, NewznabStandardCategory.AudioMP3, "|- Отечественный шансон (lossy)");
            caps.Categories.AddCategoryMapping(1334, NewznabStandardCategory.AudioMP3, "|- Сборники отечественного шансона (lossy)");
            caps.Categories.AddCategoryMapping(1216, NewznabStandardCategory.AudioLossless, "|- Военная песнямарши (lossless)");
            caps.Categories.AddCategoryMapping(1223, NewznabStandardCategory.AudioMP3, "|- Военная песнямарши (lossy)");
            caps.Categories.AddCategoryMapping(1224, NewznabStandardCategory.AudioLossless, "|- Авторская песня (lossless)");
            caps.Categories.AddCategoryMapping(1225, NewznabStandardCategory.AudioMP3, "|- Авторская песня (lossy)");
            caps.Categories.AddCategoryMapping(1226, NewznabStandardCategory.Audio, "|- Менестрели и ролевики (lossy и lossless)");
            caps.Categories.AddCategoryMapping(1842, NewznabStandardCategory.AudioLossless, "Label Packs (lossless)");
            caps.Categories.AddCategoryMapping(1648, NewznabStandardCategory.AudioMP3, "Label packsScene packs (lossy)");
            caps.Categories.AddCategoryMapping(2495, NewznabStandardCategory.Audio, "Отечественная поп-музыка");
            caps.Categories.AddCategoryMapping(424, NewznabStandardCategory.AudioMP3, "|- Отечественная поп-музыка (lossy)");
            caps.Categories.AddCategoryMapping(1361, NewznabStandardCategory.AudioMP3, "|- Отечественная поп-музыка (сборники) (lossy)");
            caps.Categories.AddCategoryMapping(425, NewznabStandardCategory.AudioLossless, "|- Отечественная поп-музыка (lossless)");
            caps.Categories.AddCategoryMapping(1635, NewznabStandardCategory.AudioMP3, "|- Советская эстрадаретро, романсы (lossy)");
            caps.Categories.AddCategoryMapping(1634, NewznabStandardCategory.AudioLossless, "|- Советская эстрадаретро, романсы (lossless)");
            caps.Categories.AddCategoryMapping(2497, NewznabStandardCategory.Audio, "Зарубежная поп-музыка");
            caps.Categories.AddCategoryMapping(428, NewznabStandardCategory.AudioMP3, "|- Зарубежная поп-музыка (lossy)");
            caps.Categories.AddCategoryMapping(1362, NewznabStandardCategory.AudioMP3, "|- Зарубежная поп-музыка (сборники) (lossy)");
            caps.Categories.AddCategoryMapping(429, NewznabStandardCategory.AudioLossless, "|- Зарубежная поп-музыка (lossless)");
            caps.Categories.AddCategoryMapping(735, NewznabStandardCategory.AudioMP3, "|- Итальянская поп-музыка (lossy)");
            caps.Categories.AddCategoryMapping(1753, NewznabStandardCategory.AudioLossless, "|- Итальянская поп-музыка (lossless)");
            caps.Categories.AddCategoryMapping(2232, NewznabStandardCategory.AudioMP3, "|- Латиноамериканская поп-музыка (lossy)");
            caps.Categories.AddCategoryMapping(714, NewznabStandardCategory.AudioLossless, "|- Латиноамериканская поп-музыка (lossless)");
            caps.Categories.AddCategoryMapping(1331, NewznabStandardCategory.AudioMP3, "|- Восточноазиатская поп-музыка (lossy)");
            caps.Categories.AddCategoryMapping(1330, NewznabStandardCategory.AudioLossless, "|- Восточноазиатская поп-музыка (lossless)");
            caps.Categories.AddCategoryMapping(1219, NewznabStandardCategory.AudioMP3, "|- Зарубежный шансон (lossy)");
            caps.Categories.AddCategoryMapping(1452, NewznabStandardCategory.AudioLossless, "|- Зарубежный шансон (lossless)");
            caps.Categories.AddCategoryMapping(2275, NewznabStandardCategory.AudioMP3, "|- Easy ListeningInstrumental Pop (lossy)");
            caps.Categories.AddCategoryMapping(2270, NewznabStandardCategory.AudioLossless, "|- Easy ListeningInstrumental Pop (lossless)");
            caps.Categories.AddCategoryMapping(1351, NewznabStandardCategory.Audio, "|- Сборники песен для детей (lossy и lossless)");
            caps.Categories.AddCategoryMapping(2499, NewznabStandardCategory.Audio, "EurodanceDisco, Hi-NRG");
            caps.Categories.AddCategoryMapping(2503, NewznabStandardCategory.AudioMP3, "|- EurodanceEuro-House, Technopop (lossy)");
            caps.Categories.AddCategoryMapping(2504, NewznabStandardCategory.AudioMP3, "|- EurodanceEuro-House, Technopop (сборники) (lossy)");
            caps.Categories.AddCategoryMapping(2502, NewznabStandardCategory.AudioLossless, "|- EurodanceEuro-House, Technopop (lossless)");
            caps.Categories.AddCategoryMapping(2501, NewznabStandardCategory.AudioMP3, "|- DiscoItalo-Disco, Euro-Disco, Hi-NRG (lossy)");
            caps.Categories.AddCategoryMapping(2505, NewznabStandardCategory.AudioMP3, "|- DiscoItalo-Disco, Euro-Disco, Hi-NRG (сборники) (lossy)");
            caps.Categories.AddCategoryMapping(2500, NewznabStandardCategory.AudioLossless, "|- DiscoItalo-Disco, Euro-Disco, Hi-NRG (lossless)");
            caps.Categories.AddCategoryMapping(2267, NewznabStandardCategory.Audio, "Зарубежный джаз");
            caps.Categories.AddCategoryMapping(2277, NewznabStandardCategory.AudioLossless, "|- Early JazzSwing, Gypsy (lossless)");
            caps.Categories.AddCategoryMapping(2278, NewznabStandardCategory.AudioLossless, "|- Bop (lossless)");
            caps.Categories.AddCategoryMapping(2279, NewznabStandardCategory.AudioLossless, "|- Mainstream JazzCool (lossless)");
            caps.Categories.AddCategoryMapping(2280, NewznabStandardCategory.AudioLossless, "|- Jazz Fusion (lossless)");
            caps.Categories.AddCategoryMapping(2281, NewznabStandardCategory.AudioLossless, "|- World FusionEthnic Jazz (lossless)");
            caps.Categories.AddCategoryMapping(2282, NewznabStandardCategory.AudioLossless, "|- Avant-Garde JazzFree Improvisation (lossless)");
            caps.Categories.AddCategoryMapping(2353, NewznabStandardCategory.AudioLossless, "|- Modern CreativeThird Stream (lossless)");
            caps.Categories.AddCategoryMapping(2284, NewznabStandardCategory.AudioLossless, "|- SmoothJazz-Pop (lossless)");
            caps.Categories.AddCategoryMapping(2285, NewznabStandardCategory.AudioLossless, "|- Vocal Jazz (lossless)");
            caps.Categories.AddCategoryMapping(2283, NewznabStandardCategory.AudioLossless, "|- FunkSoul, R&B (lossless)");
            caps.Categories.AddCategoryMapping(2286, NewznabStandardCategory.AudioLossless, "|- Сборники зарубежного джаза (lossless)");
            caps.Categories.AddCategoryMapping(2287, NewznabStandardCategory.AudioMP3, "|- Зарубежный джаз (lossy)");
            caps.Categories.AddCategoryMapping(2268, NewznabStandardCategory.Audio, "Зарубежный блюз");
            caps.Categories.AddCategoryMapping(2293, NewznabStandardCategory.AudioLossless, "|- Blues (TexasChicago, Modern and Others) (lossless)");
            caps.Categories.AddCategoryMapping(2292, NewznabStandardCategory.AudioLossless, "|- Blues-rock (lossless)");
            caps.Categories.AddCategoryMapping(2290, NewznabStandardCategory.AudioLossless, "|- RootsPre-War Blues, Early R&B, Gospel (lossless)");
            caps.Categories.AddCategoryMapping(2289, NewznabStandardCategory.AudioLossless, "|- Зарубежный блюз (сборники; Tribute VA) (lossless)");
            caps.Categories.AddCategoryMapping(2288, NewznabStandardCategory.AudioMP3, "|- Зарубежный блюз (lossy)");
            caps.Categories.AddCategoryMapping(2269, NewznabStandardCategory.Audio, "Отечественный джаз и блюз");
            caps.Categories.AddCategoryMapping(2297, NewznabStandardCategory.AudioLossless, "|- Отечественный джаз (lossless)");
            caps.Categories.AddCategoryMapping(2295, NewznabStandardCategory.AudioMP3, "|- Отечественный джаз (lossy)");
            caps.Categories.AddCategoryMapping(2296, NewznabStandardCategory.AudioLossless, "|- Отечественный блюз (lossless)");
            caps.Categories.AddCategoryMapping(2298, NewznabStandardCategory.AudioMP3, "|- Отечественный блюз (lossy)");
            caps.Categories.AddCategoryMapping(1698, NewznabStandardCategory.Audio, "Зарубежный Rock");
            caps.Categories.AddCategoryMapping(1702, NewznabStandardCategory.AudioLossless, "|- Classic Rock & Hard Rock (lossless)");
            caps.Categories.AddCategoryMapping(1703, NewznabStandardCategory.AudioMP3, "|- Classic Rock & Hard Rock (lossy)");
            caps.Categories.AddCategoryMapping(1704, NewznabStandardCategory.AudioLossless, "|- Progressive & Art-Rock (lossless)");
            caps.Categories.AddCategoryMapping(1705, NewznabStandardCategory.AudioMP3, "|- Progressive & Art-Rock (lossy)");
            caps.Categories.AddCategoryMapping(1706, NewznabStandardCategory.AudioLossless, "|- Folk-Rock (lossless)");
            caps.Categories.AddCategoryMapping(1707, NewznabStandardCategory.AudioMP3, "|- Folk-Rock (lossy)");
            caps.Categories.AddCategoryMapping(2329, NewznabStandardCategory.AudioLossless, "|- AOR (Melodic Hard RockArena rock) (lossless)");
            caps.Categories.AddCategoryMapping(2330, NewznabStandardCategory.AudioMP3, "|- AOR (Melodic Hard RockArena rock) (lossy)");
            caps.Categories.AddCategoryMapping(1708, NewznabStandardCategory.AudioLossless, "|- Pop-Rock & Soft Rock (lossless)");
            caps.Categories.AddCategoryMapping(1709, NewznabStandardCategory.AudioMP3, "|- Pop-Rock & Soft Rock (lossy)");
            caps.Categories.AddCategoryMapping(1710, NewznabStandardCategory.AudioLossless, "|- Instrumental Guitar Rock (lossless)");
            caps.Categories.AddCategoryMapping(1711, NewznabStandardCategory.AudioMP3, "|- Instrumental Guitar Rock (lossy)");
            caps.Categories.AddCategoryMapping(1712, NewznabStandardCategory.AudioLossless, "|- RockabillyPsychobilly, Rock'n'Roll (lossless)");
            caps.Categories.AddCategoryMapping(1713, NewznabStandardCategory.AudioMP3, "|- RockabillyPsychobilly, Rock'n'Roll (lossy)");
            caps.Categories.AddCategoryMapping(731, NewznabStandardCategory.AudioLossless, "|- Сборники зарубежного рока (lossless)");
            caps.Categories.AddCategoryMapping(1799, NewznabStandardCategory.AudioMP3, "|- Сборники зарубежного рока (lossy)");
            caps.Categories.AddCategoryMapping(1714, NewznabStandardCategory.AudioLossless, "|- Восточноазиатский рок (lossless)");
            caps.Categories.AddCategoryMapping(1715, NewznabStandardCategory.AudioMP3, "|- Восточноазиатский рок (lossy)");
            caps.Categories.AddCategoryMapping(1716, NewznabStandardCategory.Audio, "Зарубежный Metal");
            caps.Categories.AddCategoryMapping(1796, NewznabStandardCategory.AudioLossless, "|- Avant-gardeExperimental Metal (lossless)");
            caps.Categories.AddCategoryMapping(1797, NewznabStandardCategory.AudioMP3, "|- Avant-gardeExperimental Metal (lossy)");
            caps.Categories.AddCategoryMapping(1719, NewznabStandardCategory.AudioLossless, "|- Black (lossless)");
            caps.Categories.AddCategoryMapping(1778, NewznabStandardCategory.AudioMP3, "|- Black (lossy)");
            caps.Categories.AddCategoryMapping(1779, NewznabStandardCategory.AudioLossless, "|- DeathDoom (lossless)");
            caps.Categories.AddCategoryMapping(1780, NewznabStandardCategory.AudioMP3, "|- DeathDoom (lossy)");
            caps.Categories.AddCategoryMapping(1720, NewznabStandardCategory.AudioLossless, "|- FolkPagan, Viking (lossless)");
            caps.Categories.AddCategoryMapping(798, NewznabStandardCategory.AudioMP3, "|- FolkPagan, Viking (lossy)");
            caps.Categories.AddCategoryMapping(1724, NewznabStandardCategory.AudioLossless, "|- Gothic Metal (lossless)");
            caps.Categories.AddCategoryMapping(1725, NewznabStandardCategory.AudioMP3, "|- Gothic Metal (lossy)");
            caps.Categories.AddCategoryMapping(1730, NewznabStandardCategory.AudioLossless, "|- GrindBrutal Death (lossless)");
            caps.Categories.AddCategoryMapping(1731, NewznabStandardCategory.AudioMP3, "|- GrindBrutal Death (lossy)");
            caps.Categories.AddCategoryMapping(1726, NewznabStandardCategory.AudioLossless, "|- HeavyPower, Progressive (lossless)");
            caps.Categories.AddCategoryMapping(1727, NewznabStandardCategory.AudioMP3, "|- HeavyPower, Progressive (lossy)");
            caps.Categories.AddCategoryMapping(1815, NewznabStandardCategory.AudioLossless, "|- SludgeStoner, Post-Metal (lossless)");
            caps.Categories.AddCategoryMapping(1816, NewznabStandardCategory.AudioMP3, "|- SludgeStoner, Post-Metal (lossy)");
            caps.Categories.AddCategoryMapping(1728, NewznabStandardCategory.AudioLossless, "|- ThrashSpeed (lossless)");
            caps.Categories.AddCategoryMapping(1729, NewznabStandardCategory.AudioMP3, "|- ThrashSpeed (lossy)");
            caps.Categories.AddCategoryMapping(2230, NewznabStandardCategory.AudioLossless, "|- Сборники (lossless)");
            caps.Categories.AddCategoryMapping(2231, NewznabStandardCategory.AudioMP3, "|- Сборники (lossy)");
            caps.Categories.AddCategoryMapping(1732, NewznabStandardCategory.Audio, "Зарубежные AlternativePunk, Independent");
            caps.Categories.AddCategoryMapping(1736, NewznabStandardCategory.AudioLossless, "|- Alternative & Nu-metal (lossless)");
            caps.Categories.AddCategoryMapping(1737, NewznabStandardCategory.AudioMP3, "|- Alternative & Nu-metal (lossy)");
            caps.Categories.AddCategoryMapping(1738, NewznabStandardCategory.AudioLossless, "|- Punk (lossless)");
            caps.Categories.AddCategoryMapping(1739, NewznabStandardCategory.AudioMP3, "|- Punk (lossy)");
            caps.Categories.AddCategoryMapping(1740, NewznabStandardCategory.AudioLossless, "|- Hardcore (lossless)");
            caps.Categories.AddCategoryMapping(1741, NewznabStandardCategory.AudioMP3, "|- Hardcore (lossy)");
            caps.Categories.AddCategoryMapping(1742, NewznabStandardCategory.AudioLossless, "|- IndiePost-Rock & Post-Punk (lossless)");
            caps.Categories.AddCategoryMapping(1743, NewznabStandardCategory.AudioMP3, "|- IndiePost-Rock & Post-Punk (lossy)");
            caps.Categories.AddCategoryMapping(1744, NewznabStandardCategory.AudioLossless, "|- Industrial & Post-industrial (lossless)");
            caps.Categories.AddCategoryMapping(1745, NewznabStandardCategory.AudioMP3, "|- Industrial & Post-industrial (lossy)");
            caps.Categories.AddCategoryMapping(1746, NewznabStandardCategory.AudioLossless, "|- EmocorePost-hardcore, Metalcore (lossless)");
            caps.Categories.AddCategoryMapping(1747, NewznabStandardCategory.AudioMP3, "|- EmocorePost-hardcore, Metalcore (lossy)");
            caps.Categories.AddCategoryMapping(1748, NewznabStandardCategory.AudioLossless, "|- Gothic Rock & Dark Folk (lossless)");
            caps.Categories.AddCategoryMapping(1749, NewznabStandardCategory.AudioMP3, "|- Gothic Rock & Dark Folk (lossy)");
            caps.Categories.AddCategoryMapping(2175, NewznabStandardCategory.AudioLossless, "|- Avant-gardeExperimental Rock (lossless)");
            caps.Categories.AddCategoryMapping(2174, NewznabStandardCategory.AudioMP3, "|- Avant-gardeExperimental Rock (lossy)");
            caps.Categories.AddCategoryMapping(722, NewznabStandardCategory.Audio, "Отечественный RockMetal");
            caps.Categories.AddCategoryMapping(737, NewznabStandardCategory.AudioLossless, "|- Rock (lossless)");
            caps.Categories.AddCategoryMapping(738, NewznabStandardCategory.AudioMP3, "|- Rock (lossy)");
            caps.Categories.AddCategoryMapping(464, NewznabStandardCategory.AudioLossless, "|- AlternativePunk, Independent (lossless)");
            caps.Categories.AddCategoryMapping(463, NewznabStandardCategory.AudioMP3, "|- AlternativePunk, Independent (lossy)");
            caps.Categories.AddCategoryMapping(739, NewznabStandardCategory.AudioLossless, "|- Metal (lossless)");
            caps.Categories.AddCategoryMapping(740, NewznabStandardCategory.AudioMP3, "|- Metal (lossy)");
            caps.Categories.AddCategoryMapping(951, NewznabStandardCategory.AudioLossless, "|- Rock на языках народов xUSSR (lossless)");
            caps.Categories.AddCategoryMapping(952, NewznabStandardCategory.AudioMP3, "|- Rock на языках народов xUSSR (lossy)");
            caps.Categories.AddCategoryMapping(1821, NewznabStandardCategory.Audio, "TranceGoa Trance, Psy-Trance, PsyChill, Ambient, Dub");
            caps.Categories.AddCategoryMapping(1844, NewznabStandardCategory.AudioLossless, "|- Goa TrancePsy-Trance (lossless)");
            caps.Categories.AddCategoryMapping(1822, NewznabStandardCategory.AudioMP3, "|- Goa TrancePsy-Trance (lossy)");
            caps.Categories.AddCategoryMapping(1894, NewznabStandardCategory.AudioLossless, "|- PsyChillAmbient, Dub (lossless)");
            caps.Categories.AddCategoryMapping(1895, NewznabStandardCategory.AudioMP3, "|- PsyChillAmbient, Dub (lossy)");
            caps.Categories.AddCategoryMapping(460, NewznabStandardCategory.AudioMP3, "|- Goa TrancePsy-Trance, PsyChill, Ambient, Dub (Live Sets, Mixes) ..");
            caps.Categories.AddCategoryMapping(1818, NewznabStandardCategory.AudioLossless, "|- Trance (lossless)");
            caps.Categories.AddCategoryMapping(1819, NewznabStandardCategory.AudioMP3, "|- Trance (lossy)");
            caps.Categories.AddCategoryMapping(1847, NewznabStandardCategory.AudioMP3, "|- Trance (SinglesEPs) (lossy)");
            caps.Categories.AddCategoryMapping(1824, NewznabStandardCategory.AudioMP3, "|- Trance (RadioshowsPodcasts, Live Sets, Mixes) (lossy)");
            caps.Categories.AddCategoryMapping(1807, NewznabStandardCategory.Audio, "HouseTechno, Hardcore, Hardstyle, Jumpstyle");
            caps.Categories.AddCategoryMapping(1829, NewznabStandardCategory.AudioLossless, "|- HardcoreHardstyle, Jumpstyle (lossless)");
            caps.Categories.AddCategoryMapping(1830, NewznabStandardCategory.AudioMP3, "|- HardcoreHardstyle, Jumpstyle (lossy)");
            caps.Categories.AddCategoryMapping(1831, NewznabStandardCategory.AudioMP3, "|- HardcoreHardstyle, Jumpstyle (vinyl, web)");
            caps.Categories.AddCategoryMapping(1857, NewznabStandardCategory.AudioLossless, "|- House (lossless)");
            caps.Categories.AddCategoryMapping(1859, NewznabStandardCategory.AudioMP3, "|- House (RadioshowPodcast, Liveset, Mixes)");
            caps.Categories.AddCategoryMapping(1858, NewznabStandardCategory.AudioMP3, "|- House (lossy)");
            caps.Categories.AddCategoryMapping(840, NewznabStandardCategory.AudioMP3, "|- House (Проморелизысборники) (lossy)");
            caps.Categories.AddCategoryMapping(1860, NewznabStandardCategory.AudioMP3, "|- House (SinglesEPs) (lossy)");
            caps.Categories.AddCategoryMapping(1825, NewznabStandardCategory.AudioLossless, "|- Techno (lossless)");
            caps.Categories.AddCategoryMapping(1826, NewznabStandardCategory.AudioMP3, "|- Techno (lossy)");
            caps.Categories.AddCategoryMapping(1827, NewznabStandardCategory.AudioMP3, "|- Techno (RadioshowsPodcasts, Livesets, Mixes)");
            caps.Categories.AddCategoryMapping(1828, NewznabStandardCategory.AudioMP3, "|- Techno (SinglesEPs) (lossy)");
            caps.Categories.AddCategoryMapping(1808, NewznabStandardCategory.Audio, "Drum & BassJungle, Breakbeat, Dubstep, IDM, Electro");
            caps.Categories.AddCategoryMapping(797, NewznabStandardCategory.AudioLossless, "|- ElectroElectro-Freestyle, Nu Electro (lossless)");
            caps.Categories.AddCategoryMapping(1805, NewznabStandardCategory.AudioMP3, "|- ElectroElectro-Freestyle, Nu Electro (lossy)");
            caps.Categories.AddCategoryMapping(1832, NewznabStandardCategory.AudioLossless, "|- Drum & BassJungle (lossless)");
            caps.Categories.AddCategoryMapping(1833, NewznabStandardCategory.AudioMP3, "|- Drum & BassJungle (lossy)");
            caps.Categories.AddCategoryMapping(1834, NewznabStandardCategory.AudioMP3, "|- Drum & BassJungle (Radioshows, Podcasts, Livesets, Mixes)");
            caps.Categories.AddCategoryMapping(1836, NewznabStandardCategory.AudioLossless, "|- Breakbeat (lossless)");
            caps.Categories.AddCategoryMapping(1837, NewznabStandardCategory.AudioMP3, "|- Breakbeat (lossy)");
            caps.Categories.AddCategoryMapping(1839, NewznabStandardCategory.AudioLossless, "|- Dubstep (lossless)");
            caps.Categories.AddCategoryMapping(454, NewznabStandardCategory.AudioMP3, "|- Dubstep (lossy)");
            caps.Categories.AddCategoryMapping(1838, NewznabStandardCategory.AudioMP3, "|- BreakbeatDubstep (Radioshows, Podcasts, Livesets, Mixes)");
            caps.Categories.AddCategoryMapping(1840, NewznabStandardCategory.AudioLossless, "|- IDM (lossless)");
            caps.Categories.AddCategoryMapping(1841, NewznabStandardCategory.AudioMP3, "|- IDM (lossy)");
            caps.Categories.AddCategoryMapping(2229, NewznabStandardCategory.AudioMP3, "|- IDM Discography & Collections (lossy)");
            caps.Categories.AddCategoryMapping(1809, NewznabStandardCategory.Audio, "ChilloutLounge, Downtempo, Trip-Hop");
            caps.Categories.AddCategoryMapping(1861, NewznabStandardCategory.AudioLossless, "|- ChilloutLounge, Downtempo (lossless)");
            caps.Categories.AddCategoryMapping(1862, NewznabStandardCategory.AudioMP3, "|- ChilloutLounge, Downtempo (lossy)");
            caps.Categories.AddCategoryMapping(1947, NewznabStandardCategory.AudioLossless, "|- Nu JazzAcid Jazz, Future Jazz (lossless)");
            caps.Categories.AddCategoryMapping(1946, NewznabStandardCategory.AudioMP3, "|- Nu JazzAcid Jazz, Future Jazz (lossy)");
            caps.Categories.AddCategoryMapping(1945, NewznabStandardCategory.AudioLossless, "|- Trip HopAbstract Hip-Hop (lossless)");
            caps.Categories.AddCategoryMapping(1944, NewznabStandardCategory.AudioMP3, "|- Trip HopAbstract Hip-Hop (lossy)");
            caps.Categories.AddCategoryMapping(1810, NewznabStandardCategory.Audio, "Traditional ElectronicAmbient, Modern Classical, Electroacoustic, Ex..");
            caps.Categories.AddCategoryMapping(1864, NewznabStandardCategory.AudioLossless, "|- Traditional ElectronicAmbient (lossless)");
            caps.Categories.AddCategoryMapping(1865, NewznabStandardCategory.AudioMP3, "|- Traditional ElectronicAmbient (lossy)");
            caps.Categories.AddCategoryMapping(1871, NewznabStandardCategory.AudioLossless, "|- Modern ClassicalElectroacoustic (lossless)");
            caps.Categories.AddCategoryMapping(1867, NewznabStandardCategory.AudioMP3, "|- Modern ClassicalElectroacoustic (lossy)");
            caps.Categories.AddCategoryMapping(1869, NewznabStandardCategory.AudioLossless, "|- Experimental (lossless)");
            caps.Categories.AddCategoryMapping(1873, NewznabStandardCategory.AudioMP3, "|- Experimental (lossy)");
            caps.Categories.AddCategoryMapping(1907, NewznabStandardCategory.Audio, "|- 8-bitChiptune (lossy & lossless)");
            caps.Categories.AddCategoryMapping(1811, NewznabStandardCategory.Audio, "IndustrialNoise, EBM, Dark Electro, Aggrotech, Synthpop, New Wave");
            caps.Categories.AddCategoryMapping(1868, NewznabStandardCategory.AudioLossless, "|- EBMDark Electro, Aggrotech (lossless)");
            caps.Categories.AddCategoryMapping(1875, NewznabStandardCategory.AudioMP3, "|- EBMDark Electro, Aggrotech (lossy)");
            caps.Categories.AddCategoryMapping(1877, NewznabStandardCategory.AudioLossless, "|- IndustrialNoise (lossless)");
            caps.Categories.AddCategoryMapping(1878, NewznabStandardCategory.AudioMP3, "|- IndustrialNoise (lossy)");
            caps.Categories.AddCategoryMapping(1880, NewznabStandardCategory.AudioLossless, "|- SynthpopFuturepop, New Wave, Electropop (lossless)");
            caps.Categories.AddCategoryMapping(1881, NewznabStandardCategory.AudioMP3, "|- SynthpopFuturepop, New Wave, Electropop (lossy)");
            caps.Categories.AddCategoryMapping(466, NewznabStandardCategory.AudioLossless, "|- SynthwaveSpacesynth, Dreamwave, Retrowave, Outrun (lossless)");
            caps.Categories.AddCategoryMapping(465, NewznabStandardCategory.AudioMP3, "|- SynthwaveSpacesynth, Dreamwave, Retrowave, Outrun (lossy)");
            caps.Categories.AddCategoryMapping(1866, NewznabStandardCategory.AudioLossless, "|- DarkwaveNeoclassical, Ethereal, Dungeon Synth (lossless)");
            caps.Categories.AddCategoryMapping(406, NewznabStandardCategory.AudioMP3, "|- DarkwaveNeoclassical, Ethereal, Dungeon Synth (lossy)");
            caps.Categories.AddCategoryMapping(1299, NewznabStandardCategory.Audio, "Hi-Res stereo и многоканальная музыка");
            caps.Categories.AddCategoryMapping(1884, NewznabStandardCategory.Audio, "|- Классика и классика в современной обработке (Hi-Res stereo)");
            caps.Categories.AddCategoryMapping(1164, NewznabStandardCategory.Audio, "|- Классика и классика в современной обработке (многоканальная музыка..");
            caps.Categories.AddCategoryMapping(2513, NewznabStandardCategory.Audio, "|- New AgeRelax, Meditative & Flamenco (Hi-Res stereo и многоканаль..");
            caps.Categories.AddCategoryMapping(1397, NewznabStandardCategory.Audio, "|- Саундтреки (Hi-Res stereo и многоканальная музыка)");
            caps.Categories.AddCategoryMapping(2512, NewznabStandardCategory.Audio, "|- Музыка разных жанров (Hi-Res stereo и многоканальная музыка)");
            caps.Categories.AddCategoryMapping(1885, NewznabStandardCategory.Audio, "|- Поп-музыка (Hi-Res stereo)");
            caps.Categories.AddCategoryMapping(1163, NewznabStandardCategory.Audio, "|- Поп-музыка (многоканальная музыка)");
            caps.Categories.AddCategoryMapping(2302, NewznabStandardCategory.Audio, "|- Джаз и Блюз (Hi-Res stereo)");
            caps.Categories.AddCategoryMapping(2303, NewznabStandardCategory.Audio, "|- Джаз и Блюз (многоканальная музыка)");
            caps.Categories.AddCategoryMapping(1755, NewznabStandardCategory.Audio, "|- Рок-музыка (Hi-Res stereo)");
            caps.Categories.AddCategoryMapping(1757, NewznabStandardCategory.Audio, "|- Рок-музыка (многоканальная музыка)");
            caps.Categories.AddCategoryMapping(1893, NewznabStandardCategory.Audio, "|- Электронная музыка (Hi-Res stereo)");
            caps.Categories.AddCategoryMapping(1890, NewznabStandardCategory.Audio, "|- Электронная музыка (многоканальная музыка)");
            caps.Categories.AddCategoryMapping(2219, NewznabStandardCategory.Audio, "Оцифровки с аналоговых носителей");
            caps.Categories.AddCategoryMapping(1660, NewznabStandardCategory.Audio, "|- Классика и классика в современной обработке (оцифровки)");
            caps.Categories.AddCategoryMapping(506, NewznabStandardCategory.Audio, "|- Фольклорнародная и этническая музыка (оцифровки)");
            caps.Categories.AddCategoryMapping(1835, NewznabStandardCategory.Audio, "|- RapHip-Hop, R'n'B, Reggae, Ska, Dub (оцифровки)");
            caps.Categories.AddCategoryMapping(1625, NewznabStandardCategory.Audio, "|- Саундтреки и мюзиклы (оцифровки)");
            caps.Categories.AddCategoryMapping(1217, NewznabStandardCategory.Audio, "|- Шансонавторские, военные песни и марши (оцифровки)");
            caps.Categories.AddCategoryMapping(974, NewznabStandardCategory.Audio, "|- Музыка других жанров (оцифровки)");
            caps.Categories.AddCategoryMapping(1444, NewznabStandardCategory.Audio, "|- Зарубежная поп-музыка (оцифровки)");
            caps.Categories.AddCategoryMapping(2401, NewznabStandardCategory.Audio, "|- Советская эстрадаретро, романсы (оцифровки)");
            caps.Categories.AddCategoryMapping(239, NewznabStandardCategory.Audio, "|- Отечественная поп-музыка (оцифровки)");
            caps.Categories.AddCategoryMapping(450, NewznabStandardCategory.Audio, "|- Инструментальная поп-музыка (оцифровки)");
            caps.Categories.AddCategoryMapping(2301, NewznabStandardCategory.Audio, "|- Джаз и блюз (оцифровки)");
            caps.Categories.AddCategoryMapping(1756, NewznabStandardCategory.Audio, "|- Зарубежная рок-музыка (оцифровки)");
            caps.Categories.AddCategoryMapping(1758, NewznabStandardCategory.Audio, "|- Отечественная рок-музыка (оцифровки)");
            caps.Categories.AddCategoryMapping(1766, NewznabStandardCategory.Audio, "|- Зарубежный Metal (оцифровки)");
            caps.Categories.AddCategoryMapping(1754, NewznabStandardCategory.Audio, "|- Электронная музыка (оцифровки)");
            caps.Categories.AddCategoryMapping(860, NewznabStandardCategory.Audio, "Неофициальные конверсии цифровых форматов");
            caps.Categories.AddCategoryMapping(453, NewznabStandardCategory.Audio, "|- Конверсии Quadraphonic");
            caps.Categories.AddCategoryMapping(1170, NewznabStandardCategory.Audio, "|- Конверсии SACD");
            caps.Categories.AddCategoryMapping(1759, NewznabStandardCategory.Audio, "|- Конверсии Blu-RayADVD и DVD-Audio");
            caps.Categories.AddCategoryMapping(1852, NewznabStandardCategory.Audio, "|- Апмиксы-Upmixes/Даунмиксы-Downmix");
            caps.Categories.AddCategoryMapping(413, NewznabStandardCategory.AudioVideo, "Музыкальное SD видео");
            caps.Categories.AddCategoryMapping(445, NewznabStandardCategory.AudioVideo, "|- Классическая и современная академическая музыка (Видео)");
            caps.Categories.AddCategoryMapping(702, NewznabStandardCategory.AudioVideo, "|- ОпераОперетта и Мюзикл (Видео) ");
            caps.Categories.AddCategoryMapping(1990, NewznabStandardCategory.AudioVideo, "|- Балет и современная хореография (Видео)");
            caps.Categories.AddCategoryMapping(1793, NewznabStandardCategory.AudioVideo, "|- Классика в современной обработкеical Crossover (Видео)");
            caps.Categories.AddCategoryMapping(1141, NewznabStandardCategory.AudioVideo, "|- ФольклорНародная и Этническая музыка и фламенко (Видео)");
            caps.Categories.AddCategoryMapping(1775, NewznabStandardCategory.AudioVideo, "|- New AgeRelax, Meditative, Рэп, Хип-Хоп, R'n'B, Reggae, Ska, Dub .. ");
            caps.Categories.AddCategoryMapping(1227, NewznabStandardCategory.AudioVideo, "|- Зарубежный и Отечественный ШансонАвторская и Военная песня (Виде..");
            caps.Categories.AddCategoryMapping(475, NewznabStandardCategory.AudioVideo, "|- Музыка других жанровСоветская эстрада, ретро, романсы (Видео)");
            caps.Categories.AddCategoryMapping(1121, NewznabStandardCategory.AudioVideo, "|- Отечественная поп-музыка (Видео)");
            caps.Categories.AddCategoryMapping(431, NewznabStandardCategory.AudioVideo, "|- Зарубежная поп-музыка (Видео)");
            caps.Categories.AddCategoryMapping(2378, NewznabStandardCategory.AudioVideo, "|- Восточноазиатская поп-музыка (Видео)");
            caps.Categories.AddCategoryMapping(2383, NewznabStandardCategory.AudioVideo, "|- Зарубежный шансон (Видео)");
            caps.Categories.AddCategoryMapping(2305, NewznabStandardCategory.AudioVideo, "|- Джаз и Блюз (Видео)");
            caps.Categories.AddCategoryMapping(1782, NewznabStandardCategory.AudioVideo, "|- Rock (Видео)");
            caps.Categories.AddCategoryMapping(1787, NewznabStandardCategory.AudioVideo, "|- Metal (Видео)");
            caps.Categories.AddCategoryMapping(1789, NewznabStandardCategory.AudioVideo, "|- AlternativePunk, Independent (Видео)");
            caps.Categories.AddCategoryMapping(1791, NewznabStandardCategory.AudioVideo, "|- Отечественный РокПанк, Альтернатива (Видео)");
            caps.Categories.AddCategoryMapping(1912, NewznabStandardCategory.AudioVideo, "|- Электронная музыка (Видео)");
            caps.Categories.AddCategoryMapping(1189, NewznabStandardCategory.AudioVideo, "|- Документальные фильмы о музыке и музыкантах (Видео)");
            caps.Categories.AddCategoryMapping(2403, NewznabStandardCategory.AudioVideo, "Музыкальное DVD видео");
            caps.Categories.AddCategoryMapping(984, NewznabStandardCategory.AudioVideo, "|- Классическая и современная академическая музыка (DVD Video)");
            caps.Categories.AddCategoryMapping(983, NewznabStandardCategory.AudioVideo, "|- ОпераОперетта и Мюзикл (DVD видео)");
            caps.Categories.AddCategoryMapping(2352, NewznabStandardCategory.AudioVideo, "|- Балет и современная хореография (DVD Video)");
            caps.Categories.AddCategoryMapping(2384, NewznabStandardCategory.AudioVideo, "|- Классика в современной обработкеical Crossover (DVD Video)");
            caps.Categories.AddCategoryMapping(1142, NewznabStandardCategory.AudioVideo, "|- ФольклорНародная и Этническая музыка и Flamenco (DVD Video)");
            caps.Categories.AddCategoryMapping(1107, NewznabStandardCategory.AudioVideo, "|- New AgeRelax, Meditative, Рэп, Хип-Хоп, R &#039;n &#039;B, Reggae, Ska, Dub ..");
            caps.Categories.AddCategoryMapping(1228, NewznabStandardCategory.AudioVideo, "|- Зарубежный и Отечественный ШансонАвторская и Военная песня (DVD ..");
            caps.Categories.AddCategoryMapping(988, NewznabStandardCategory.AudioVideo, "|- Музыка других жанровСоветская эстрада, ретро, романсы (DVD Video..");
            caps.Categories.AddCategoryMapping(1122, NewznabStandardCategory.AudioVideo, "|- Отечественная поп-музыка (DVD Video)");
            caps.Categories.AddCategoryMapping(986, NewznabStandardCategory.AudioVideo, "|- Зарубежная Поп-музыкаEurodance, Disco (DVD Video)");
            caps.Categories.AddCategoryMapping(2379, NewznabStandardCategory.AudioVideo, "|- Восточноазиатская поп-музыка (DVD Video)");
            caps.Categories.AddCategoryMapping(2088, NewznabStandardCategory.AudioVideo, "|- Разножанровые сборные концерты и сборники видеоклипов (DVD Video)");
            caps.Categories.AddCategoryMapping(2304, NewznabStandardCategory.AudioVideo, "|- Джаз и Блюз (DVD Видео)");
            caps.Categories.AddCategoryMapping(1783, NewznabStandardCategory.AudioVideo, "|- Зарубежный Rock (DVD Video)");
            caps.Categories.AddCategoryMapping(1788, NewznabStandardCategory.AudioVideo, "|- Зарубежный Metal (DVD Video)");
            caps.Categories.AddCategoryMapping(1790, NewznabStandardCategory.AudioVideo, "|- Зарубежный AlternativePunk, Independent (DVD Video)");
            caps.Categories.AddCategoryMapping(1792, NewznabStandardCategory.AudioVideo, "|- Отечественный РокМетал, Панк, Альтернатива (DVD Video)");
            caps.Categories.AddCategoryMapping(1886, NewznabStandardCategory.AudioVideo, "|- Электронная музыка (DVD Video)");
            caps.Categories.AddCategoryMapping(2509, NewznabStandardCategory.AudioVideo, "|- Документальные фильмы о музыке и музыкантах (DVD Video)");
            caps.Categories.AddCategoryMapping(2507, NewznabStandardCategory.AudioVideo, "Неофициальные DVD видео ");
            caps.Categories.AddCategoryMapping(2263, NewznabStandardCategory.AudioVideo, "Классическая музыкаОпера, Балет, Мюзикл (Неофициальные DVD Video)");
            caps.Categories.AddCategoryMapping(2511, NewznabStandardCategory.AudioVideo, "ШансонАвторская песня, Сборные концерты, МДЖ (Неофициальные DVD Video)");
            caps.Categories.AddCategoryMapping(2264, NewznabStandardCategory.AudioVideo, "|- Зарубежная и Отечественная Поп-музыка (Неофициальные DVD Video)");
            caps.Categories.AddCategoryMapping(2262, NewznabStandardCategory.AudioVideo, "|- Джаз и Блюз (Неофициальные DVD Video)");
            caps.Categories.AddCategoryMapping(2261, NewznabStandardCategory.AudioVideo, "|- Зарубежная и Отечественная Рок-музыка (Неофициальные DVD Video)");
            caps.Categories.AddCategoryMapping(1887, NewznabStandardCategory.AudioVideo, "|- Электронная музыка (Неофициальныелюбительские DVD Video)");
            caps.Categories.AddCategoryMapping(2531, NewznabStandardCategory.AudioVideo, "|- Прочие жанры (Неофициальные DVD видео)");
            caps.Categories.AddCategoryMapping(2400, NewznabStandardCategory.AudioVideo, "Музыкальное HD видео");
            caps.Categories.AddCategoryMapping(1812, NewznabStandardCategory.AudioVideo, "|- Классическая и современная академическая музыка (HD Video)");
            caps.Categories.AddCategoryMapping(655, NewznabStandardCategory.AudioVideo, "|- ОпераОперетта и Мюзикл (HD Видео)");
            caps.Categories.AddCategoryMapping(1777, NewznabStandardCategory.AudioVideo, "|- Балет и современная хореография (HD Video)");
            caps.Categories.AddCategoryMapping(2530, NewznabStandardCategory.AudioVideo, "|- ФольклорНародная, Этническая музыка и Flamenco (HD Видео)");
            caps.Categories.AddCategoryMapping(2529, NewznabStandardCategory.AudioVideo, "|- New AgeRelax, Meditative, Рэп, Хип-Хоп, R'n'B, Reggae, Ska, Dub ..");
            caps.Categories.AddCategoryMapping(1781, NewznabStandardCategory.AudioVideo, "|- Музыка других жанровРазножанровые сборные концерты (HD видео)");
            caps.Categories.AddCategoryMapping(2508, NewznabStandardCategory.AudioVideo, "|- Зарубежная поп-музыка (HD Video)");
            caps.Categories.AddCategoryMapping(2426, NewznabStandardCategory.AudioVideo, "|- Отечественная поп-музыка (HD видео)");
            caps.Categories.AddCategoryMapping(2351, NewznabStandardCategory.AudioVideo, "|- Восточноазиатская Поп-музыка (HD Video)");
            caps.Categories.AddCategoryMapping(2306, NewznabStandardCategory.AudioVideo, "|- Джаз и Блюз (HD Video)");
            caps.Categories.AddCategoryMapping(1795, NewznabStandardCategory.AudioVideo, "|- Зарубежный рок (HD Video)");
            caps.Categories.AddCategoryMapping(2271, NewznabStandardCategory.AudioVideo, "|- Отечественный рок (HD видео)");
            caps.Categories.AddCategoryMapping(1913, NewznabStandardCategory.AudioVideo, "|- Электронная музыка (HD Video)");
            caps.Categories.AddCategoryMapping(1784, NewznabStandardCategory.AudioVideo, "|- UHD музыкальное видео");
            caps.Categories.AddCategoryMapping(1892, NewznabStandardCategory.AudioVideo, "|- Документальные фильмы о музыке и музыкантах (HD Video)");
            caps.Categories.AddCategoryMapping(518, NewznabStandardCategory.AudioVideo, "Некондиционное музыкальное видео (ВидеоDVD видео, HD видео)");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.PCGames, "Игры для Windows");
            caps.Categories.AddCategoryMapping(635, NewznabStandardCategory.PCGames, "|- Горячие Новинки");
            caps.Categories.AddCategoryMapping(127, NewznabStandardCategory.PCGames, "|- Аркады");
            caps.Categories.AddCategoryMapping(2203, NewznabStandardCategory.PCGames, "|- Файтинги");
            caps.Categories.AddCategoryMapping(647, NewznabStandardCategory.PCGames, "|- Экшены от первого лица");
            caps.Categories.AddCategoryMapping(646, NewznabStandardCategory.PCGames, "|- Экшены от третьего лица");
            caps.Categories.AddCategoryMapping(50, NewznabStandardCategory.PCGames, "|- Хорроры");
            caps.Categories.AddCategoryMapping(53, NewznabStandardCategory.PCGames, "|- Приключения и квесты");
            caps.Categories.AddCategoryMapping(1008, NewznabStandardCategory.PCGames, "|- Квесты в стиле \"Поиск предметов\"");
            caps.Categories.AddCategoryMapping(900, NewznabStandardCategory.PCGames, "|- Визуальные новеллы");
            caps.Categories.AddCategoryMapping(128, NewznabStandardCategory.PCGames, "|- Для самых маленьких");
            caps.Categories.AddCategoryMapping(2204, NewznabStandardCategory.PCGames, "|- Логические игры");
            caps.Categories.AddCategoryMapping(278, NewznabStandardCategory.PCGames, "|- Шахматы");
            caps.Categories.AddCategoryMapping(2118, NewznabStandardCategory.PCGames, "|- Многопользовательские игры");
            caps.Categories.AddCategoryMapping(52, NewznabStandardCategory.PCGames, "|- Ролевые игры");
            caps.Categories.AddCategoryMapping(54, NewznabStandardCategory.PCGames, "|- Симуляторы");
            caps.Categories.AddCategoryMapping(51, NewznabStandardCategory.PCGames, "|- Стратегии в реальном времени");
            caps.Categories.AddCategoryMapping(2226, NewznabStandardCategory.PCGames, "|- Пошаговые стратегии");
            caps.Categories.AddCategoryMapping(2228, NewznabStandardCategory.PCGames, "|- IBM-PC-несовместимые компьютеры");
            caps.Categories.AddCategoryMapping(139, NewznabStandardCategory.PCGames, "Прочее для Windows-игр");
            caps.Categories.AddCategoryMapping(2478, NewznabStandardCategory.PCGames, "|- Официальные патчимоды, плагины, дополнения");
            caps.Categories.AddCategoryMapping(2480, NewznabStandardCategory.PCGames, "|- Неофициальные модификацииплагины, дополнения");
            caps.Categories.AddCategoryMapping(2481, NewznabStandardCategory.PCGames, "|- Русификаторы");
            caps.Categories.AddCategoryMapping(2142, NewznabStandardCategory.PCGames, "Прочее для Microsoft Flight SimulatorPrepar3D, X-Plane");
            caps.Categories.AddCategoryMapping(2060, NewznabStandardCategory.PCGames, "|- Сценариимеши и аэропорты для FS2004, FSX, P3D");
            caps.Categories.AddCategoryMapping(2145, NewznabStandardCategory.PCGames, "|- Самолёты и вертолёты для FS2004FSX, P3D");
            caps.Categories.AddCategoryMapping(2146, NewznabStandardCategory.PCGames, "|- Миссиитрафик, звуки, паки и утилиты для FS2004, FSX, P3D");
            caps.Categories.AddCategoryMapping(2143, NewznabStandardCategory.PCGames, "|- Сценариимиссии, трафик, звуки, паки и утилиты для X-Plane");
            caps.Categories.AddCategoryMapping(2012, NewznabStandardCategory.PCGames, "|- Самолёты и вертолёты для X-Plane");
            caps.Categories.AddCategoryMapping(960, NewznabStandardCategory.PCMac, "Игры для Apple Macintosh");
            caps.Categories.AddCategoryMapping(537, NewznabStandardCategory.PCMac, "|- Нативные игры для Mac");
            caps.Categories.AddCategoryMapping(637, NewznabStandardCategory.PCMac, "|- Портированные игры для Mac");
            caps.Categories.AddCategoryMapping(899, NewznabStandardCategory.PCGames, "Игры для Linux");
            caps.Categories.AddCategoryMapping(1992, NewznabStandardCategory.PCGames, "|- Нативные игры для Linux");
            caps.Categories.AddCategoryMapping(2059, NewznabStandardCategory.PCGames, "|- Портированные игры для Linux");
            caps.Categories.AddCategoryMapping(548, NewznabStandardCategory.Console, "Игры для консолей");
            caps.Categories.AddCategoryMapping(908, NewznabStandardCategory.Console, "|- PS");
            caps.Categories.AddCategoryMapping(357, NewznabStandardCategory.ConsoleOther, "|- PS2");
            caps.Categories.AddCategoryMapping(886, NewznabStandardCategory.ConsolePS3, "|- PS3");
            caps.Categories.AddCategoryMapping(546, NewznabStandardCategory.Console, "|- Игры PS1PS2 и PSP для PS3");
            caps.Categories.AddCategoryMapping(973, NewznabStandardCategory.ConsolePS4, "|- PS4");
            caps.Categories.AddCategoryMapping(1352, NewznabStandardCategory.ConsolePSP, "|- PSP");
            caps.Categories.AddCategoryMapping(1116, NewznabStandardCategory.ConsolePSP, "|- Игры PS1 для PSP");
            caps.Categories.AddCategoryMapping(595, NewznabStandardCategory.ConsolePSVita, "|- PS Vita");
            caps.Categories.AddCategoryMapping(887, NewznabStandardCategory.ConsoleXBox, "|- Original Xbox");
            caps.Categories.AddCategoryMapping(510, NewznabStandardCategory.ConsoleXBox360, "|- Xbox 360");
            caps.Categories.AddCategoryMapping(773, NewznabStandardCategory.ConsoleWii, "|- Wii/WiiU");
            caps.Categories.AddCategoryMapping(774, NewznabStandardCategory.ConsoleNDS, "|- NDS/3DS");
            caps.Categories.AddCategoryMapping(1605, NewznabStandardCategory.Console, "|- Switch");
            caps.Categories.AddCategoryMapping(968, NewznabStandardCategory.Console, "|- Dreamcast");
            caps.Categories.AddCategoryMapping(129, NewznabStandardCategory.Console, "|- Остальные платформы");
            caps.Categories.AddCategoryMapping(2185, NewznabStandardCategory.ConsoleOther, "Видео для консолей");
            caps.Categories.AddCategoryMapping(2487, NewznabStandardCategory.ConsoleOther, "|- Видео для PS Vita");
            caps.Categories.AddCategoryMapping(2182, NewznabStandardCategory.ConsoleOther, "|- Фильмы для PSP");
            caps.Categories.AddCategoryMapping(2181, NewznabStandardCategory.ConsoleOther, "|- Сериалы для PSP");
            caps.Categories.AddCategoryMapping(2180, NewznabStandardCategory.ConsoleOther, "|- Мультфильмы для PSP");
            caps.Categories.AddCategoryMapping(2179, NewznabStandardCategory.ConsoleOther, "|- Дорамы для PSP");
            caps.Categories.AddCategoryMapping(2186, NewznabStandardCategory.ConsoleOther, "|- Аниме для PSP");
            caps.Categories.AddCategoryMapping(700, NewznabStandardCategory.ConsoleOther, "|- Видео для PSP");
            caps.Categories.AddCategoryMapping(1926, NewznabStandardCategory.ConsoleOther, "|- Видео для PS3 и других консолей");
            caps.Categories.AddCategoryMapping(650, NewznabStandardCategory.PCMobileOther, "Игры для мобильных устройств");
            caps.Categories.AddCategoryMapping(2149, NewznabStandardCategory.PCMobileAndroid, "|- Игры для Android");
            caps.Categories.AddCategoryMapping(1001, NewznabStandardCategory.PCMobileOther, "|- Игры для Java");
            caps.Categories.AddCategoryMapping(1004, NewznabStandardCategory.PCMobileOther, "|- Игры для Symbian");
            caps.Categories.AddCategoryMapping(1002, NewznabStandardCategory.PCMobileOther, "|- Игры для Windows Mobile");
            caps.Categories.AddCategoryMapping(2420, NewznabStandardCategory.PCMobileOther, "|- Игры для Windows Phone");
            caps.Categories.AddCategoryMapping(240, NewznabStandardCategory.OtherMisc, "Игровое видео");
            caps.Categories.AddCategoryMapping(2415, NewznabStandardCategory.OtherMisc, "|- Видеопрохождения игр");
            caps.Categories.AddCategoryMapping(1012, NewznabStandardCategory.PC, "Операционные системы от Microsoft");
            caps.Categories.AddCategoryMapping(2523, NewznabStandardCategory.PC, "|- Настольные ОС от Microsoft - Windows 8 и далее");
            caps.Categories.AddCategoryMapping(2153, NewznabStandardCategory.PC, "|- Настольные ОС от Microsoft - Windows XP - Windows 7");
            caps.Categories.AddCategoryMapping(1019, NewznabStandardCategory.PC, "|- Настольные ОС от Microsoft (выпущенные до Windows XP)");
            caps.Categories.AddCategoryMapping(1021, NewznabStandardCategory.PC, "|- Серверные ОС от Microsoft");
            caps.Categories.AddCategoryMapping(1025, NewznabStandardCategory.PC, "|- Разное (Операционные системы от Microsoft)");
            caps.Categories.AddCategoryMapping(1376, NewznabStandardCategory.PC, "LinuxUnix и другие ОС");
            caps.Categories.AddCategoryMapping(1379, NewznabStandardCategory.PC, "|- Операционные системы (LinuxUnix)");
            caps.Categories.AddCategoryMapping(1381, NewznabStandardCategory.PC, "|- Программное обеспечение (LinuxUnix)");
            caps.Categories.AddCategoryMapping(1473, NewznabStandardCategory.PC, "|- Другие ОС и ПО под них");
            caps.Categories.AddCategoryMapping(1195, NewznabStandardCategory.PC, "Тестовые диски для настройки аудио/видео аппаратуры");
            caps.Categories.AddCategoryMapping(1013, NewznabStandardCategory.PC, "Системные программы");
            caps.Categories.AddCategoryMapping(1028, NewznabStandardCategory.PC, "|- Работа с жёстким диском");
            caps.Categories.AddCategoryMapping(1029, NewznabStandardCategory.PC, "|- Резервное копирование");
            caps.Categories.AddCategoryMapping(1030, NewznabStandardCategory.PC, "|- Архиваторы и файловые менеджеры");
            caps.Categories.AddCategoryMapping(1031, NewznabStandardCategory.PC, "|- Программы для настройки и оптимизации ОС");
            caps.Categories.AddCategoryMapping(1032, NewznabStandardCategory.PC, "|- Сервисное обслуживание компьютера");
            caps.Categories.AddCategoryMapping(1033, NewznabStandardCategory.PC, "|- Работа с носителями информации");
            caps.Categories.AddCategoryMapping(1034, NewznabStandardCategory.PC, "|- Информация и диагностика");
            caps.Categories.AddCategoryMapping(1066, NewznabStandardCategory.PC, "|- Программы для интернет и сетей");
            caps.Categories.AddCategoryMapping(1035, NewznabStandardCategory.PC, "|- ПО для защиты компьютера (Антивирусное ПОФаерволлы)");
            caps.Categories.AddCategoryMapping(1038, NewznabStandardCategory.PC, "|- Анти-шпионы и анти-трояны");
            caps.Categories.AddCategoryMapping(1039, NewznabStandardCategory.PC, "|- Программы для защиты информации");
            caps.Categories.AddCategoryMapping(1536, NewznabStandardCategory.PC, "|- Драйверы и прошивки");
            caps.Categories.AddCategoryMapping(1051, NewznabStandardCategory.PC, "|- Оригинальные диски к компьютерам и комплектующим");
            caps.Categories.AddCategoryMapping(1040, NewznabStandardCategory.PC, "|- Серверное ПО для Windows");
            caps.Categories.AddCategoryMapping(1041, NewznabStandardCategory.PC, "|- Изменение интерфейса ОС Windows");
            caps.Categories.AddCategoryMapping(1636, NewznabStandardCategory.PC, "|- Скринсейверы");
            caps.Categories.AddCategoryMapping(1042, NewznabStandardCategory.PC, "|- Разное (Системные программы под Windows)");
            caps.Categories.AddCategoryMapping(1014, NewznabStandardCategory.PC, "Системы для бизнесаофиса, научной и проектной работы");
            caps.Categories.AddCategoryMapping(2134, NewznabStandardCategory.PC, "|- Медицина - интерактивный софт");
            caps.Categories.AddCategoryMapping(1060, NewznabStandardCategory.PC, "|- Всё для дома: кройкашитьё, кулинария");
            caps.Categories.AddCategoryMapping(1061, NewznabStandardCategory.PC, "|- Офисные системы");
            caps.Categories.AddCategoryMapping(1062, NewznabStandardCategory.PC, "|- Системы для бизнеса");
            caps.Categories.AddCategoryMapping(1067, NewznabStandardCategory.PC, "|- Распознавание текстазвука и синтез речи");
            caps.Categories.AddCategoryMapping(1086, NewznabStandardCategory.PC, "|- Работа с PDF и DjVu");
            caps.Categories.AddCategoryMapping(1068, NewznabStandardCategory.PC, "|- Словарипереводчики");
            caps.Categories.AddCategoryMapping(1063, NewznabStandardCategory.PC, "|- Системы для научной работы");
            caps.Categories.AddCategoryMapping(1087, NewznabStandardCategory.PC, "|- САПР (общие и машиностроительные)");
            caps.Categories.AddCategoryMapping(1192, NewznabStandardCategory.PC, "|- САПР (электроникаавтоматика, ГАП)");
            caps.Categories.AddCategoryMapping(1088, NewznabStandardCategory.PC, "|- Программы для архитекторов и строителей");
            caps.Categories.AddCategoryMapping(1193, NewznabStandardCategory.PC, "|- Библиотеки и проекты для архитекторов и дизайнеров интерьеров");
            caps.Categories.AddCategoryMapping(1071, NewznabStandardCategory.PC, "|- Прочие справочные системы");
            caps.Categories.AddCategoryMapping(1073, NewznabStandardCategory.PC, "|- Разное (Системы для бизнесаофиса, научной и проектной работы)");
            caps.Categories.AddCategoryMapping(1052, NewznabStandardCategory.PC, "Веб-разработка и Программирование");
            caps.Categories.AddCategoryMapping(1053, NewznabStandardCategory.PC, "|- WYSIWYG Редакторы для веб-диза");
            caps.Categories.AddCategoryMapping(1054, NewznabStandardCategory.PC, "|- Текстовые редакторы с подсветкой");
            caps.Categories.AddCategoryMapping(1055, NewznabStandardCategory.PC, "|- Среды программированиякомпиляторы и вспомогательные программы");
            caps.Categories.AddCategoryMapping(1056, NewznabStandardCategory.PC, "|- Компоненты для сред программирования");
            caps.Categories.AddCategoryMapping(2077, NewznabStandardCategory.PC, "|- Системы управления базами данных");
            caps.Categories.AddCategoryMapping(1057, NewznabStandardCategory.PC, "|- Скрипты и движки сайтовCMS а также расширения к ним");
            caps.Categories.AddCategoryMapping(1018, NewznabStandardCategory.PC, "|- Шаблоны для сайтов и CMS");
            caps.Categories.AddCategoryMapping(1058, NewznabStandardCategory.PC, "|- Разное (Веб-разработка и программирование)");
            caps.Categories.AddCategoryMapping(1016, NewznabStandardCategory.PC, "Программы для работы с мультимедиа и 3D");
            caps.Categories.AddCategoryMapping(1079, NewznabStandardCategory.PC, "|- Программные комплекты");
            caps.Categories.AddCategoryMapping(1080, NewznabStandardCategory.PC, "|- Плагины для программ компании Adobe");
            caps.Categories.AddCategoryMapping(1081, NewznabStandardCategory.PC, "|- Графические редакторы");
            caps.Categories.AddCategoryMapping(1082, NewznabStandardCategory.PC, "|- Программы для версткипечати и работы со шрифтами");
            caps.Categories.AddCategoryMapping(1083, NewznabStandardCategory.PC, "|- 3D моделированиерендеринг и плагины для них");
            caps.Categories.AddCategoryMapping(1084, NewznabStandardCategory.PC, "|- Анимация");
            caps.Categories.AddCategoryMapping(1085, NewznabStandardCategory.PC, "|- Создание BD/HD/DVD-видео");
            caps.Categories.AddCategoryMapping(1089, NewznabStandardCategory.PC, "|- Редакторы видео");
            caps.Categories.AddCategoryMapping(1090, NewznabStandardCategory.PC, "|- Видео- Аудио- конверторы");
            caps.Categories.AddCategoryMapping(1065, NewznabStandardCategory.PC, "|- Аудио- и видео-CD- проигрыватели и каталогизаторы");
            caps.Categories.AddCategoryMapping(1064, NewznabStandardCategory.PC, "|- Каталогизаторы и просмотрщики графики");
            caps.Categories.AddCategoryMapping(1092, NewznabStandardCategory.PC, "|- Разное (Программы для работы с мультимедиа и 3D)");
            caps.Categories.AddCategoryMapping(1204, NewznabStandardCategory.PC, "|- Виртуальные студиисеквенсоры и аудиоредакторы");
            caps.Categories.AddCategoryMapping(1027, NewznabStandardCategory.PC, "|- Виртуальные инструменты и синтезаторы");
            caps.Categories.AddCategoryMapping(1199, NewznabStandardCategory.PC, "|- Плагины для обработки звука");
            caps.Categories.AddCategoryMapping(1091, NewznabStandardCategory.PC, "|- Разное (Программы для работы со звуком)");
            caps.Categories.AddCategoryMapping(838, NewznabStandardCategory.OtherMisc, "|- Ищу/Предлагаю (Материалы для мультимедиа и дизайна)");
            caps.Categories.AddCategoryMapping(1357, NewznabStandardCategory.OtherMisc, "|- Авторские работы");
            caps.Categories.AddCategoryMapping(890, NewznabStandardCategory.OtherMisc, "|- Официальные сборники векторных клипартов");
            caps.Categories.AddCategoryMapping(830, NewznabStandardCategory.OtherMisc, "|- Прочие векторные клипарты");
            caps.Categories.AddCategoryMapping(1290, NewznabStandardCategory.OtherMisc, "|- Photostoсks");
            caps.Categories.AddCategoryMapping(1962, NewznabStandardCategory.OtherMisc, "|- Дополнения для программ компоузинга и постобработки");
            caps.Categories.AddCategoryMapping(831, NewznabStandardCategory.OtherMisc, "|- Рамкишаблоны, текстуры и фоны");
            caps.Categories.AddCategoryMapping(829, NewznabStandardCategory.OtherMisc, "|- Прочие растровые клипарты");
            caps.Categories.AddCategoryMapping(633, NewznabStandardCategory.OtherMisc, "|- 3D моделисцены и материалы");
            caps.Categories.AddCategoryMapping(1009, NewznabStandardCategory.OtherMisc, "|- Футажи");
            caps.Categories.AddCategoryMapping(1963, NewznabStandardCategory.OtherMisc, "|- Прочие сборники футажей");
            caps.Categories.AddCategoryMapping(1954, NewznabStandardCategory.OtherMisc, "|- Музыкальные библиотеки");
            caps.Categories.AddCategoryMapping(1010, NewznabStandardCategory.OtherMisc, "|- Звуковые эффекты");
            caps.Categories.AddCategoryMapping(1674, NewznabStandardCategory.OtherMisc, "|- Библиотеки сэмплов");
            caps.Categories.AddCategoryMapping(2421, NewznabStandardCategory.OtherMisc, "|- Библиотеки и саундбанки для сэмплеровпресеты для синтезаторов");
            caps.Categories.AddCategoryMapping(2492, NewznabStandardCategory.OtherMisc, "|- Multitracks");
            caps.Categories.AddCategoryMapping(839, NewznabStandardCategory.OtherMisc, "|- Материалы для создания меню и обложек DVD");
            caps.Categories.AddCategoryMapping(1679, NewznabStandardCategory.OtherMisc, "|- Дополнениястили, кисти, формы, узоры для программ Adobe");
            caps.Categories.AddCategoryMapping(1011, NewznabStandardCategory.OtherMisc, "|- Шрифты");
            caps.Categories.AddCategoryMapping(835, NewznabStandardCategory.OtherMisc, "|- Разное (Материалы для мультимедиа и дизайна)");
            caps.Categories.AddCategoryMapping(1503, NewznabStandardCategory.OtherMisc, "ГИСсистемы навигации и карты");
            caps.Categories.AddCategoryMapping(1507, NewznabStandardCategory.OtherMisc, "|- ГИС (Геоинформационные системы)");
            caps.Categories.AddCategoryMapping(1526, NewznabStandardCategory.OtherMisc, "|- Картыснабженные программной оболочкой");
            caps.Categories.AddCategoryMapping(1508, NewznabStandardCategory.OtherMisc, "|- Атласы и карты современные (после 1950 г.)");
            caps.Categories.AddCategoryMapping(1509, NewznabStandardCategory.OtherMisc, "|- Атласы и карты старинные (до 1950 г.)");
            caps.Categories.AddCategoryMapping(1510, NewznabStandardCategory.OtherMisc, "|- Карты прочие (астрономическиеисторические, тематические)");
            caps.Categories.AddCategoryMapping(1511, NewznabStandardCategory.OtherMisc, "|- Встроенная автомобильная навигация");
            caps.Categories.AddCategoryMapping(1512, NewznabStandardCategory.OtherMisc, "|- Garmin");
            caps.Categories.AddCategoryMapping(1513, NewznabStandardCategory.OtherMisc, "|- Ozi");
            caps.Categories.AddCategoryMapping(1514, NewznabStandardCategory.OtherMisc, "|- TomTom");
            caps.Categories.AddCategoryMapping(1515, NewznabStandardCategory.OtherMisc, "|- Navigon / Navitel");
            caps.Categories.AddCategoryMapping(1516, NewznabStandardCategory.OtherMisc, "|- Igo");
            caps.Categories.AddCategoryMapping(1517, NewznabStandardCategory.OtherMisc, "|- Разное - системы навигации и карты");
            caps.Categories.AddCategoryMapping(285, NewznabStandardCategory.PCMobileOther, "Приложения для мобильных устройств");
            caps.Categories.AddCategoryMapping(2154, NewznabStandardCategory.PCMobileAndroid, "|- Приложения для Android");
            caps.Categories.AddCategoryMapping(1005, NewznabStandardCategory.PCMobileOther, "|- Приложения для Java");
            caps.Categories.AddCategoryMapping(289, NewznabStandardCategory.PCMobileOther, "|- Приложения для Symbian");
            caps.Categories.AddCategoryMapping(290, NewznabStandardCategory.PCMobileOther, "|- Приложения для Windows Mobile");
            caps.Categories.AddCategoryMapping(2419, NewznabStandardCategory.PCMobileOther, "|- Приложения для Windows Phone");
            caps.Categories.AddCategoryMapping(288, NewznabStandardCategory.PCMobileOther, "|- Софт для работы с телефоном");
            caps.Categories.AddCategoryMapping(292, NewznabStandardCategory.PCMobileOther, "|- Прошивки для телефонов");
            caps.Categories.AddCategoryMapping(291, NewznabStandardCategory.PCMobileOther, "|- Обои и темы");
            caps.Categories.AddCategoryMapping(957, NewznabStandardCategory.PCMobileOther, "Видео для мобильных устройств");
            caps.Categories.AddCategoryMapping(287, NewznabStandardCategory.PCMobileOther, "|- Видео для смартфонов и КПК");
            caps.Categories.AddCategoryMapping(286, NewznabStandardCategory.PCMobileOther, "|- Видео в формате 3GP для мобильных");
            caps.Categories.AddCategoryMapping(1366, NewznabStandardCategory.PCMac, "Apple Macintosh");
            caps.Categories.AddCategoryMapping(1368, NewznabStandardCategory.PCMac, "|- Mac OS (для Macintosh)");
            caps.Categories.AddCategoryMapping(1383, NewznabStandardCategory.PCMac, "|- Mac OS (для РС-Хакинтош)");
            caps.Categories.AddCategoryMapping(1394, NewznabStandardCategory.PCMac, "|- Программы для просмотра и обработки видео (Mac OS)");
            caps.Categories.AddCategoryMapping(1370, NewznabStandardCategory.PCMac, "|- Программы для создания и обработки графики (Mac OS)");
            caps.Categories.AddCategoryMapping(2237, NewznabStandardCategory.PCMac, "|- Плагины для программ компании Adobe (Mac OS)");
            caps.Categories.AddCategoryMapping(1372, NewznabStandardCategory.PCMac, "|- Аудио редакторы и конвертеры (Mac OS)");
            caps.Categories.AddCategoryMapping(1373, NewznabStandardCategory.PCMac, "|- Системные программы (Mac OS)");
            caps.Categories.AddCategoryMapping(1375, NewznabStandardCategory.PCMac, "|- Офисные программы (Mac OS)");
            caps.Categories.AddCategoryMapping(1371, NewznabStandardCategory.PCMac, "|- Программы для интернета и сетей (Mac OS)");
            caps.Categories.AddCategoryMapping(1374, NewznabStandardCategory.PCMac, "|- Другие программы (Mac OS)");
            caps.Categories.AddCategoryMapping(1933, NewznabStandardCategory.PCMobileiOS, "iOS");
            caps.Categories.AddCategoryMapping(1935, NewznabStandardCategory.PCMobileiOS, "|- Программы для iOS");
            caps.Categories.AddCategoryMapping(1003, NewznabStandardCategory.PCMobileiOS, "|- Игры для iOS");
            caps.Categories.AddCategoryMapping(1937, NewznabStandardCategory.PCMobileiOS, "|- Разное для iOS");
            caps.Categories.AddCategoryMapping(2235, NewznabStandardCategory.PCMobileiOS, "Видео");
            caps.Categories.AddCategoryMapping(1908, NewznabStandardCategory.PCMobileiOS, "|- Фильмы для iPodiPhone, iPad");
            caps.Categories.AddCategoryMapping(864, NewznabStandardCategory.PCMobileiOS, "|- Сериалы для iPodiPhone, iPad");
            caps.Categories.AddCategoryMapping(863, NewznabStandardCategory.PCMobileiOS, "|- Мультфильмы для iPodiPhone, iPad");
            caps.Categories.AddCategoryMapping(2535, NewznabStandardCategory.PCMobileiOS, "|- Аниме для iPodiPhone, iPad");
            caps.Categories.AddCategoryMapping(2534, NewznabStandardCategory.PCMobileiOS, "|- Музыкальное видео для iPodiPhone, iPad");
            caps.Categories.AddCategoryMapping(2238, NewznabStandardCategory.PCMac, "Видео HD");
            caps.Categories.AddCategoryMapping(1936, NewznabStandardCategory.PCMac, "|- Фильмы HD для Apple TV");
            caps.Categories.AddCategoryMapping(315, NewznabStandardCategory.PCMac, "|- Сериалы HD для Apple TV");
            caps.Categories.AddCategoryMapping(1363, NewznabStandardCategory.PCMac, "|- Мультфильмы HD для Apple TV");
            caps.Categories.AddCategoryMapping(2082, NewznabStandardCategory.PCMac, "|- Документальное видео HD для Apple TV");
            caps.Categories.AddCategoryMapping(2241, NewznabStandardCategory.PCMac, "|- Музыкальное видео HD для Apple TV");
            caps.Categories.AddCategoryMapping(2236, NewznabStandardCategory.Audio, "Аудио");
            caps.Categories.AddCategoryMapping(1909, NewznabStandardCategory.AudioAudiobook, "|- Аудиокниги (AACALAC)");
            caps.Categories.AddCategoryMapping(1927, NewznabStandardCategory.AudioLossless, "|- Музыка lossless (ALAC)");
            caps.Categories.AddCategoryMapping(2240, NewznabStandardCategory.Audio, "|- Музыка Lossy (AAC-iTunes)");
            caps.Categories.AddCategoryMapping(2248, NewznabStandardCategory.Audio, "|- Музыка Lossy (AAC)");
            caps.Categories.AddCategoryMapping(2244, NewznabStandardCategory.Audio, "|- Музыка Lossy (AAC) (SinglesEPs)");
            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.OtherMisc, "Разное (раздачи)");
            caps.Categories.AddCategoryMapping(865, NewznabStandardCategory.OtherMisc, "|- Психоактивные аудиопрограммы");
            caps.Categories.AddCategoryMapping(1100, NewznabStandardCategory.OtherMisc, "|- АватарыИконки, Смайлы");
            caps.Categories.AddCategoryMapping(1643, NewznabStandardCategory.OtherMisc, "|- ЖивописьГрафика, Скульптура, Digital Art");
            caps.Categories.AddCategoryMapping(848, NewznabStandardCategory.OtherMisc, "|- Картинки");
            caps.Categories.AddCategoryMapping(808, NewznabStandardCategory.OtherMisc, "|- Любительские фотографии");
            caps.Categories.AddCategoryMapping(630, NewznabStandardCategory.OtherMisc, "|- Обои");
            caps.Categories.AddCategoryMapping(1664, NewznabStandardCategory.OtherMisc, "|- Фото знаменитостей");
            caps.Categories.AddCategoryMapping(148, NewznabStandardCategory.Audio, "|- Аудио");
            caps.Categories.AddCategoryMapping(965, NewznabStandardCategory.AudioMP3, "|- Музыка (lossy)");
            caps.Categories.AddCategoryMapping(134, NewznabStandardCategory.AudioLossless, "|- Музыка (lossless)");
            caps.Categories.AddCategoryMapping(807, NewznabStandardCategory.TVOther, "|- Видео");
            caps.Categories.AddCategoryMapping(147, NewznabStandardCategory.Books, "|- Публикации и учебные материалы (тексты)");
            caps.Categories.AddCategoryMapping(847, NewznabStandardCategory.MoviesOther, "|- Трейлеры и дополнительные материалы к фильмам");
            caps.Categories.AddCategoryMapping(1167, NewznabStandardCategory.TVOther, "|- Любительские видеоклипы");

            return caps;
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            if (action == "getUrls")
            {
                var links = IndexerUrls;

                return new
                {
                    options = links.Select(d => new { Value = d, Name = d })
                };
            }

            return null;
        }
    }

    public class RuTrackerRequestGenerator : IIndexerRequestGenerator
    {
        public RuTrackerSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public RuTrackerRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, int season = 0)
        {
            var searchUrl = string.Format("{0}/forum/tracker.php", Settings.BaseUrl.TrimEnd('/'));

            var queryCollection = new NameValueCollection();

            var searchString = term;

            // if the search string is empty use the getnew view
            if (string.IsNullOrWhiteSpace(searchString))
            {
                queryCollection.Add("nm", searchString);
            }
            else
            {
                // use the normal search
                searchString = searchString.Replace("-", " ");
                if (season != 0)
                {
                    searchString += " Сезон: " + season;
                }

                queryCollection.Add("nm", searchString);
            }

            searchUrl = searchUrl + "?" + queryCollection.GetQueryString();

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            if (searchCriteria.Season == null)
            {
                searchCriteria.Season = 0;
            }

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class RuTrackerParser : IParseIndexerResponse
    {
        private readonly RuTrackerSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public RuTrackerParser(RuTrackerSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            var doc = parser.ParseDocument(indexerResponse.Content);
            var rows = doc.QuerySelectorAll("table#tor-tbl > tbody > tr");

            foreach (var row in rows)
            {
                var release = ParseReleaseRow(row);
                if (release != null)
                {
                    torrentInfos.Add(release);
                }
            }

            return torrentInfos.ToArray();
        }

        private TorrentInfo ParseReleaseRow(IElement row)
        {
            var qDownloadLink = row.QuerySelector("td.tor-size > a.tr-dl");

            // Expects moderation
            if (qDownloadLink == null)
            {
                return null;
            }

            var link = _settings.BaseUrl + "forum/" + qDownloadLink.GetAttribute("href");

            var qDetailsLink = row.QuerySelector("td.t-title-col > div.t-title > a.tLink");
            var details = _settings.BaseUrl + "forum/" + qDetailsLink.GetAttribute("href");

            var category = GetCategoryOfRelease(row);

            var size = GetSizeOfRelease(row);

            var seeders = GetSeedersOfRelease(row);
            var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(8)").TextContent);

            var grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(9)").TextContent);

            var publishDate = GetPublishDateOfRelease(row);

            var release = new TorrentInfo
            {
                MinimumRatio = 1,
                MinimumSeedTime = 0,
                Title = qDetailsLink.TextContent,
                InfoUrl = details,
                DownloadUrl = link,
                Guid = details,
                Size = size,
                Seeders = seeders,
                Peers = leechers + seeders,
                Grabs = grabs,
                PublishDate = publishDate,
                Categories = category,
                DownloadVolumeFactor = 1,
                UploadVolumeFactor = 1
            };

            // TODO finish extracting release variables to simplify release initialization
            if (IsAnyTvCategory(release.Categories))
            {
                // extract season and episodes
                // should also handle multi-season releases listed as Сезон: 1-8 and Сезоны: 1-8
                var regex = new Regex(@".+\/\s([^а-яА-я\/]+)\s\/.+Сезон.\s*[:]*\s+(\d*\-?\d*).+(?:Серии|Эпизод)+\s*[:]*\s+(\d+-?\d*).+(\[.*\])[\s]?(.*)");

                var title = regex.Replace(release.Title, "$1 - S$2E$3 - rus $4 $5");
                title = Regex.Replace(title, "-Rip", "Rip", RegexOptions.IgnoreCase);
                title = Regex.Replace(title, "WEB-DLRip", "WEBDL", RegexOptions.IgnoreCase);
                title = Regex.Replace(title, "WEB-DL", "WEBDL", RegexOptions.IgnoreCase);
                title = Regex.Replace(title, "HDTVRip", "HDTV", RegexOptions.IgnoreCase);
                title = Regex.Replace(title, "Кураж-Бамбей", "kurazh", RegexOptions.IgnoreCase);

                release.Title = title;
            }
            else if (IsAnyMovieCategory(release.Categories))
            {
                // Bluray quality fix: radarr parse Blu-ray Disc as Bluray-1080p but should be BR-DISK
                release.Title = Regex.Replace(release.Title, "Blu-ray Disc", "BR-DISK", RegexOptions.IgnoreCase);
            }

            if (IsAnyTvCategory(release.Categories) | IsAnyMovieCategory(release.Categories))
            {
                                // remove director's name from title
                // rutracker movies titles look like: russian name / english name (russian director / english director) other stuff
                // Ирландец / The Irishman (Мартин Скорсезе / Martin Scorsese) [2019, США, криминал, драма, биография, WEB-DL 1080p] Dub (Пифагор) + MVO (Jaskier) + AVO (Юрий Сербин) + Sub Rus, Eng + Original Eng
                // this part should be removed: (Мартин Скорсезе / Martin Scorsese)
                //var director = new Regex(@"(\([А-Яа-яЁё\W]+)\s/\s(.+?)\)");
                var director = new Regex(@"(\([А-Яа-яЁё\W].+?\))");
                release.Title = director.Replace(release.Title, "");

                // Remove VO, MVO and DVO from titles
                var vo = new Regex(@".VO\s\(.+?\)");
                release.Title = vo.Replace(release.Title, "");

                // Remove R5 and (R5) from release names
                var r5 = new Regex(@"(.*)(.R5.)(.*)");
                release.Title = r5.Replace(release.Title, "$1");

                // Remove Sub languages from release names
                var sub = new Regex(@"(Sub.*\+)|(Sub.*$)");
                release.Title = sub.Replace(release.Title, "");

                // language fix: all rutracker releases contains russian track
                if (release.Title.IndexOf("rus", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    release.Title += " rus";
                }

                // remove russian letters
                if (_settings.RussianLetters == true)
                {
                    //Strip russian letters
                    var rusRegex = new Regex(@"(\([А-Яа-яЁё\W]+\))|(^[А-Яа-яЁё\W\d]+\/ )|([а-яА-ЯЁё \-]+,+)|([а-яА-ЯЁё]+)");

                    release.Title = rusRegex.Replace(release.Title, "");

                    // Replace everything after first forward slash with a year (to avoid filtering away releases with an fwdslash after title+year, like: Title Year [stuff / stuff])
                    var fwdslashRegex = new Regex(@"(\/\s.+?\[)");
                    release.Title = fwdslashRegex.Replace(release.Title, "[");
                }
            }

            return release;
        }

        private int GetSeedersOfRelease(in IElement row)
        {
            var seeders = 0;
            var qSeeders = row.QuerySelector("td:nth-child(7)");
            if (qSeeders != null && !qSeeders.TextContent.Contains("дн"))
            {
                var seedersString = qSeeders.QuerySelector("b").TextContent;
                if (!string.IsNullOrWhiteSpace(seedersString))
                {
                    seeders = ParseUtil.CoerceInt(seedersString);
                }
            }

            return seeders;
        }

        private ICollection<IndexerCategory> GetCategoryOfRelease(in IElement row)
        {
            var forum = row.QuerySelector("td.f-name-col > div.f-name > a");
            var forumid = forum.GetAttribute("href").Split('=')[1];
            return _categories.MapTrackerCatToNewznab(forumid);
        }

        private long GetSizeOfRelease(in IElement row)
        {
            var qSize = row.QuerySelector("td.tor-size");
            var size = ReleaseInfo.GetBytes(qSize.GetAttribute("data-ts_text"));
            return size;
        }

        private DateTime GetPublishDateOfRelease(in IElement row)
        {
            var timestr = row.QuerySelector("td:nth-child(10)").GetAttribute("data-ts_text");
            var publishDate = DateTimeUtil.UnixTimestampToDateTime(long.Parse(timestr));
            return publishDate;
        }

        private bool IsAnyTvCategory(ICollection<IndexerCategory> category)
        {
            return category.Contains(NewznabStandardCategory.TV)
                || NewznabStandardCategory.TV.SubCategories.Any(subCat => category.Contains(subCat));
        }

        private bool IsAnyMovieCategory(ICollection<IndexerCategory> category)
        {
            return category.Contains(NewznabStandardCategory.Movies)
                || NewznabStandardCategory.Movies.SubCategories.Any(subCat => category.Contains(subCat));
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class RuTrackerSettingsValidator : AbstractValidator<RuTrackerSettings>
    {
        public RuTrackerSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    public class RuTrackerSettings : IIndexerSettings
    {
        private static readonly RuTrackerSettingsValidator Validator = new RuTrackerSettingsValidator();

        public RuTrackerSettings()
        {
            Username = "";
            Password = "";
            RussianLetters = false;
        }

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Username", Advanced = false, HelpText = "Site Username")]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Password", Type = FieldType.Password, Privacy = PrivacyLevel.Password, HelpText = "Site Password")]
        public string Password { get; set; }

        [FieldDefinition(4, Label = "Strip Russian letters", Type = FieldType.Checkbox, SelectOptionsProviderAction = "stripRussian", HelpText = "Removes russian letters")]
        public bool RussianLetters { get; set; }

        [FieldDefinition(5)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
