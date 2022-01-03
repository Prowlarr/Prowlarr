using System;
using System.Collections.Generic;
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
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class PornoLab : TorrentIndexerBase<PornoLabSettings>
    {
        public override string Name => "PornoLab";
        public override string[] IndexerUrls => new string[] { "https://pornolab.net/" };
        private string LoginUrl => Settings.BaseUrl + "forum/login.php";
        public override string Description => "PornoLab is a Semi-Private Russian site for Adult content";
        public override string Language => "ru-ru";
        public override Encoding Encoding => Encoding.GetEncoding("windows-1251");
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public PornoLab(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new PornoLabRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new PornoLabParser(Settings, Capabilities.Categories, _logger);
        }

        protected override async Task DoLogin()
        {
            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true,
                AllowAutoRedirect = true,
                Method = HttpMethod.Post
            };

            var authLoginRequest = requestBuilder
                .AddFormParameter("login_username", Settings.Username)
                .AddFormParameter("login_password", Settings.Password)
                .AddFormParameter("login", "Login")
                .SetHeader("Content-Type", "multipart/form-data")
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            if (CheckIfLoginNeeded(response))
            {
                var errorMessage = "Unknown error message, please report";
                var loginResultParser = new HtmlParser();
                var loginResultDocument = loginResultParser.ParseDocument(response.Content);
                var errormsg = loginResultDocument.QuerySelector("h4[class=\"warnColor1 tCenter mrg_16\"]");
                if (errormsg != null)
                {
                    errorMessage = errormsg.TextContent;
                }

                throw new IndexerAuthException(errorMessage);
            }

            UpdateCookies(response.GetCookies(), DateTime.Now + TimeSpan.FromDays(30));

            _logger.Debug("PornoLab authentication succeeded");
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            if (!httpResponse.Content.Contains("Вы зашли как:"))
            {
                return true;
            }

            return false;
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
            };

            caps.Categories.AddCategoryMapping(1768, NewznabStandardCategory.XXX, "Эротические фильмы / Erotic Movies");
            caps.Categories.AddCategoryMapping(60, NewznabStandardCategory.XXX, "Документальные фильмы / Documentary & Reality");
            caps.Categories.AddCategoryMapping(1644, NewznabStandardCategory.XXX, "Нудизм-Натуризм / Nudity");

            caps.Categories.AddCategoryMapping(1111, NewznabStandardCategory.XXXPack, "Паки полных фильмов / Full Length Movies Packs");
            caps.Categories.AddCategoryMapping(508, NewznabStandardCategory.XXX, "Классические фильмы / Classic");
            caps.Categories.AddCategoryMapping(555, NewznabStandardCategory.XXX, "Фильмы с сюжетом / Feature & Vignettes");
            caps.Categories.AddCategoryMapping(1673, NewznabStandardCategory.XXX, "Гонзо-фильмы 2011-2021 / Gonzo 2011-2021");
            caps.Categories.AddCategoryMapping(1112, NewznabStandardCategory.XXX, "Фильмы без сюжета 1991-2010 / All Sex & Amateur 1991-2010");
            caps.Categories.AddCategoryMapping(1718, NewznabStandardCategory.XXX, "Фильмы без сюжета 2011-2021 / All Sex & Amateur 2011-2021");
            caps.Categories.AddCategoryMapping(553, NewznabStandardCategory.XXX, "Лесбо-фильмы / All Girl & Solo");
            caps.Categories.AddCategoryMapping(1143, NewznabStandardCategory.XXX, "Этнические фильмы / Ethnic-Themed");
            caps.Categories.AddCategoryMapping(1646, NewznabStandardCategory.XXX, "Видео для телефонов и КПК / Pocket РС & Phone Video");

            caps.Categories.AddCategoryMapping(1712, NewznabStandardCategory.XXX, "Эротические и Документальные фильмы (DVD и HD) / EroticDocumentary & Reality (DVD & HD)");
            caps.Categories.AddCategoryMapping(1713, NewznabStandardCategory.XXXDVD, "Фильмы с сюжетомКлассические (DVD) / Feature & Vignettes, Classic (DVD)");
            caps.Categories.AddCategoryMapping(512, NewznabStandardCategory.XXXDVD, "ГонзоЛесбо и Фильмы без сюжета (DVD) / Gonzo, All Girl & Solo, All Sex (DVD)");
            caps.Categories.AddCategoryMapping(1775, NewznabStandardCategory.XXX, "Фильмы с сюжетом (HD Video) / Feature & Vignettes (HD Video)");
            caps.Categories.AddCategoryMapping(1450, NewznabStandardCategory.XXX, "ГонзоЛесбо и Фильмы без сюжета (HD Video) / Gonzo, All Girl & Solo, All Sex (HD Video)");

            caps.Categories.AddCategoryMapping(902, NewznabStandardCategory.XXX, "Русские порнофильмы / Russian Full Length Movies");
            caps.Categories.AddCategoryMapping(1675, NewznabStandardCategory.XXXPack, "Паки русских порнороликов / Russian Clips Packs");
            caps.Categories.AddCategoryMapping(36, NewznabStandardCategory.XXX, "Сайтрипы с русскими актрисами 1991-2015 / Russian SiteRip's 1991-2015");
            caps.Categories.AddCategoryMapping(1830, NewznabStandardCategory.XXX, "Сайтрипы с русскими актрисами 1991-2015 (HD Video) / Russian SiteRip's 1991-2015 (HD Video)");
            caps.Categories.AddCategoryMapping(1803, NewznabStandardCategory.XXX, "Сайтрипы с русскими актрисами 2016-2021 / Russian SiteRip's 2016-2021");
            caps.Categories.AddCategoryMapping(1831, NewznabStandardCategory.XXX, "Сайтрипы с русскими актрисами 2016-2021 (HD Video) / Russian SiteRip's 2016-2021 (HD Video)");
            caps.Categories.AddCategoryMapping(1741, NewznabStandardCategory.XXX, "Русские Порноролики Разное / Russian Clips (various)");
            caps.Categories.AddCategoryMapping(1676, NewznabStandardCategory.XXX, "Русское любительское видео / Russian Amateur Video");

            caps.Categories.AddCategoryMapping(1780, NewznabStandardCategory.XXXPack, "Паки сайтрипов (HD Video) / SiteRip's Packs (HD Video)");
            caps.Categories.AddCategoryMapping(1110, NewznabStandardCategory.XXXPack, "Паки сайтрипов / SiteRip's Packs");
            caps.Categories.AddCategoryMapping(1678, NewznabStandardCategory.XXXPack, "Паки порнороликов по актрисам / Actresses Clips Packs");
            caps.Categories.AddCategoryMapping(1124, NewznabStandardCategory.XXX, "Сайтрипы 1991-2010 (HD Video) / SiteRip's 1991-2010 (HD Video)");
            caps.Categories.AddCategoryMapping(1784, NewznabStandardCategory.XXX, "Сайтрипы 2011-2012 (HD Video) / SiteRip's 2011-2012 (HD Video)");
            caps.Categories.AddCategoryMapping(1769, NewznabStandardCategory.XXX, "Сайтрипы 2013 (HD Video) / SiteRip's 2013 (HD Video)");
            caps.Categories.AddCategoryMapping(1793, NewznabStandardCategory.XXX, "Сайтрипы 2014 (HD Video) / SiteRip's 2014 (HD Video)");
            caps.Categories.AddCategoryMapping(1797, NewznabStandardCategory.XXX, "Сайтрипы 2015 (HD Video) / SiteRip's 2015 (HD Video)");
            caps.Categories.AddCategoryMapping(1804, NewznabStandardCategory.XXX, "Сайтрипы 2016 (HD Video) / SiteRip's 2016 (HD Video)");
            caps.Categories.AddCategoryMapping(1819, NewznabStandardCategory.XXX, "Сайтрипы 2017 (HD Video) / SiteRip's 2017 (HD Video)");
            caps.Categories.AddCategoryMapping(1825, NewznabStandardCategory.XXX, "Сайтрипы 2018 (HD Video) / SiteRip's 2018 (HD Video)");
            caps.Categories.AddCategoryMapping(1836, NewznabStandardCategory.XXX, "Сайтрипы 2019 (HD Video) / SiteRip's 2019 (HD Video)");
            caps.Categories.AddCategoryMapping(1842, NewznabStandardCategory.XXX, "Сайтрипы 2020 (HD Video) / SiteRip's 2020 (HD Video)");
            caps.Categories.AddCategoryMapping(1846, NewznabStandardCategory.XXX, "Сайтрипы 2021 (HD Video) / SiteRip's 2021 (HD Video)");
            caps.Categories.AddCategoryMapping(1857, NewznabStandardCategory.XXX, "Сайтрипы 2022 (HD Video) / SiteRip's 2022 (HD Video)");

            caps.Categories.AddCategoryMapping(1451, NewznabStandardCategory.XXX, "Сайтрипы 1991-2010 / SiteRip's 1991-2010");
            caps.Categories.AddCategoryMapping(1788, NewznabStandardCategory.XXX, "Сайтрипы 2011-2012 / SiteRip's 2011-2012");
            caps.Categories.AddCategoryMapping(1789, NewznabStandardCategory.XXX, "Сайтрипы 2013 / SiteRip's 2013");
            caps.Categories.AddCategoryMapping(1792, NewznabStandardCategory.XXX, "Сайтрипы 2014 / SiteRip's 2014");
            caps.Categories.AddCategoryMapping(1798, NewznabStandardCategory.XXX, "Сайтрипы 2015 / SiteRip's 2015");
            caps.Categories.AddCategoryMapping(1805, NewznabStandardCategory.XXX, "Сайтрипы 2016 / SiteRip's 2016");
            caps.Categories.AddCategoryMapping(1820, NewznabStandardCategory.XXX, "Сайтрипы 2017 / SiteRip's 2017");
            caps.Categories.AddCategoryMapping(1826, NewznabStandardCategory.XXX, "Сайтрипы 2018 / SiteRip's 2018");
            caps.Categories.AddCategoryMapping(1837, NewznabStandardCategory.XXX, "Сайтрипы 2019 / SiteRip's 2019");
            caps.Categories.AddCategoryMapping(1843, NewznabStandardCategory.XXX, "Сайтрипы 2020 / SiteRip's 2020");
            caps.Categories.AddCategoryMapping(1847, NewznabStandardCategory.XXX, "Сайтрипы 2021 / SiteRip's 2021");
            caps.Categories.AddCategoryMapping(1856, NewznabStandardCategory.XXX, "Сайтрипы 2022 / SiteRip's 2022");

            caps.Categories.AddCategoryMapping(1707, NewznabStandardCategory.XXX, "Сцены из фильмов / Movie Scenes");
            caps.Categories.AddCategoryMapping(284, NewznabStandardCategory.XXX, "Порноролики Разное / Clips (various)");
            caps.Categories.AddCategoryMapping(1823, NewznabStandardCategory.XXX, "Порноролики в 3D и Virtual Reality (VR) / 3D & Virtual Reality Videos");

            caps.Categories.AddCategoryMapping(1801, NewznabStandardCategory.XXXPack, "Паки японских фильмов и сайтрипов / Full Length Japanese Movies Packs & SiteRip's Packs");
            caps.Categories.AddCategoryMapping(1719, NewznabStandardCategory.XXX, "Японские фильмы и сайтрипы (DVD и HD Video) / Japanese Movies & SiteRip's (DVD & HD Video)");
            caps.Categories.AddCategoryMapping(997, NewznabStandardCategory.XXX, "Японские фильмы и сайтрипы 1991-2014 / Japanese Movies & SiteRip's 1991-2014");
            caps.Categories.AddCategoryMapping(1818, NewznabStandardCategory.XXX, "Японские фильмы и сайтрипы 2015-2019 / Japanese Movies & SiteRip's 2015-2019");

            caps.Categories.AddCategoryMapping(1671, NewznabStandardCategory.XXX, "Эротические студии (видео) / Erotic Video Library");
            caps.Categories.AddCategoryMapping(1726, NewznabStandardCategory.XXX, "Met-Art & MetModels");
            caps.Categories.AddCategoryMapping(883, NewznabStandardCategory.XXXImageSet, "Эротические студии Разное / Erotic Picture Gallery (various)");
            caps.Categories.AddCategoryMapping(1759, NewznabStandardCategory.XXXImageSet, "Паки сайтрипов эротических студий / Erotic Picture SiteRip's Packs");
            caps.Categories.AddCategoryMapping(1728, NewznabStandardCategory.XXXImageSet, "Любительское фото / Amateur Picture Gallery");
            caps.Categories.AddCategoryMapping(1729, NewznabStandardCategory.XXXPack, "Подборки по актрисам / Actresses Picture Packs");
            caps.Categories.AddCategoryMapping(38, NewznabStandardCategory.XXXImageSet, "Подборки сайтрипов / SiteRip's Picture Packs");
            caps.Categories.AddCategoryMapping(1757, NewznabStandardCategory.XXXImageSet, "Подборки сетов / Picture Sets Packs");
            caps.Categories.AddCategoryMapping(1735, NewznabStandardCategory.XXXImageSet, "Тематическое и нетрадиционное фото / Misc & Special Interest Picture Packs");
            caps.Categories.AddCategoryMapping(1731, NewznabStandardCategory.XXXImageSet, "Журналы / Magazines");

            caps.Categories.AddCategoryMapping(1679, NewznabStandardCategory.XXX, "Хентай: основной подраздел / Hentai: main subsection");
            caps.Categories.AddCategoryMapping(1740, NewznabStandardCategory.XXX, "Хентай в высоком качестве (DVD и HD) / Hentai DVD & HD");
            caps.Categories.AddCategoryMapping(1834, NewznabStandardCategory.XXX, "Хентай: ролики 2D / Hentai: 2D video");
            caps.Categories.AddCategoryMapping(1752, NewznabStandardCategory.XXX, "Хентай: ролики 3D / Hentai: 3D video");
            caps.Categories.AddCategoryMapping(1760, NewznabStandardCategory.XXX, "Хентай: Манга / Hentai: Manga");
            caps.Categories.AddCategoryMapping(1781, NewznabStandardCategory.XXX, "Хентай: Арт и HCG / Hentai: Artwork & HCG");
            caps.Categories.AddCategoryMapping(1711, NewznabStandardCategory.XXX, "Мультфильмы / Cartoons");
            caps.Categories.AddCategoryMapping(1296, NewznabStandardCategory.XXX, "Комиксы и рисунки / Comics & Artwork");

            caps.Categories.AddCategoryMapping(1750, NewznabStandardCategory.XXX, "Игры: основной подраздел / Games: main subsection");
            caps.Categories.AddCategoryMapping(1756, NewznabStandardCategory.XXX, "Игры: визуальные новеллы / Games: Visual Novels");
            caps.Categories.AddCategoryMapping(1785, NewznabStandardCategory.XXX, "Игры: ролевые / Games: role-playing (RPG Maker and WOLF RPG Editor)");
            caps.Categories.AddCategoryMapping(1790, NewznabStandardCategory.XXX, "Игры и Софт: Анимация / Software: Animation");
            caps.Categories.AddCategoryMapping(1827, NewznabStandardCategory.XXX, "Игры: В разработке и Демо (основной подраздел) / Games: In Progress and Demo (main subsection)");
            caps.Categories.AddCategoryMapping(1828, NewznabStandardCategory.XXX, "Игры: В разработке и Демо (ролевые) / Games: In Progress and Demo (role-playing - RPG Maker and WOLF RPG Editor)");

            caps.Categories.AddCategoryMapping(1715, NewznabStandardCategory.XXX, "Транссексуалы (DVD и HD) / Transsexual (DVD & HD)");
            caps.Categories.AddCategoryMapping(1680, NewznabStandardCategory.XXX, "Транссексуалы / Transsexual");

            caps.Categories.AddCategoryMapping(1758, NewznabStandardCategory.XXX, "Бисексуалы / Bisexual");
            caps.Categories.AddCategoryMapping(1682, NewznabStandardCategory.XXX, "БДСМ / BDSM");
            caps.Categories.AddCategoryMapping(1733, NewznabStandardCategory.XXX, "Женское доминирование и страпон / Femdom & Strapon");
            caps.Categories.AddCategoryMapping(1754, NewznabStandardCategory.XXX, "Подглядывание / Voyeur");
            caps.Categories.AddCategoryMapping(1734, NewznabStandardCategory.XXX, "Фистинг и дилдо / Fisting & Dildo");
            caps.Categories.AddCategoryMapping(1791, NewznabStandardCategory.XXX, "Беременные / Pregnant");
            caps.Categories.AddCategoryMapping(509, NewznabStandardCategory.XXX, "Буккаке / Bukkake");
            caps.Categories.AddCategoryMapping(1685, NewznabStandardCategory.XXX, "Мочеиспускание / Peeing");
            caps.Categories.AddCategoryMapping(1762, NewznabStandardCategory.XXX, "Фетиш / Fetish");

            caps.Categories.AddCategoryMapping(903, NewznabStandardCategory.XXX, "Полнометражные гей-фильмы / Full Length Movies (Gay)");
            caps.Categories.AddCategoryMapping(1765, NewznabStandardCategory.XXX, "Полнометражные азиатские гей-фильмы / Full-length Asian Films (Gay)");
            caps.Categories.AddCategoryMapping(1767, NewznabStandardCategory.XXX, "Классические гей-фильмы (до 1990 года) / Classic Gay Films (Pre-1990's)");
            caps.Categories.AddCategoryMapping(1755, NewznabStandardCategory.XXX, "Гей-фильмы в высоком качестве (DVD и HD) / High-Quality Full Length Movies (Gay DVD & HD)");
            caps.Categories.AddCategoryMapping(1787, NewznabStandardCategory.XXX, "Азиатские гей-фильмы в высоком качестве (DVD и HD) / High-Quality Full Length Asian Movies (Gay DVD & HD)");
            caps.Categories.AddCategoryMapping(1763, NewznabStandardCategory.XXXPack, "ПАКи гей-роликов и сайтрипов / Clip's & SiteRip's Packs (Gay)");
            caps.Categories.AddCategoryMapping(1777, NewznabStandardCategory.XXX, "Гей-ролики в высоком качестве (HD Video) / Gay Clips (HD Video)");
            caps.Categories.AddCategoryMapping(1691, NewznabStandardCategory.XXX, "РоликиSiteRip'ы и сцены из гей-фильмов / Clips & Movie Scenes (Gay)");
            caps.Categories.AddCategoryMapping(1692, NewznabStandardCategory.XXXImageSet, "Гей-журналыфото, разное / Magazines, Photo, Rest (Gay)");

            caps.Categories.AddCategoryMapping(1817, NewznabStandardCategory.XXX, "Обход блокировки");
            caps.Categories.AddCategoryMapping(1670, NewznabStandardCategory.XXX, "Эротическое видео / Erotic&Softcore");
            caps.Categories.AddCategoryMapping(1672, NewznabStandardCategory.XXX, "Зарубежные порнофильмы / Full Length Movies");
            caps.Categories.AddCategoryMapping(1717, NewznabStandardCategory.XXX, "Зарубежные фильмы в высоком качестве (DVD&HD) / Full Length ..");
            caps.Categories.AddCategoryMapping(1674, NewznabStandardCategory.XXX, "Русское порно / Russian Video");
            caps.Categories.AddCategoryMapping(1677, NewznabStandardCategory.XXX, "Зарубежные порноролики / Clips");
            caps.Categories.AddCategoryMapping(1800, NewznabStandardCategory.XXX, "Японское порно / Japanese Adult Video (JAV)");
            caps.Categories.AddCategoryMapping(1815, NewznabStandardCategory.XXX, "Архив (Японское порно)");
            caps.Categories.AddCategoryMapping(1723, NewznabStandardCategory.XXX, "Эротические студиифото и журналы / Erotic Picture Gallery ..");
            caps.Categories.AddCategoryMapping(1802, NewznabStandardCategory.XXX, "Архив (Фото)");
            caps.Categories.AddCategoryMapping(1745, NewznabStandardCategory.XXX, "Хентай и МангаМультфильмы и Комиксы, Рисунки / Hentai&Ma..");
            caps.Categories.AddCategoryMapping(1838, NewznabStandardCategory.XXX, "Игры / Games");
            caps.Categories.AddCategoryMapping(1829, NewznabStandardCategory.XXX, "Обсуждение игр / Games Discussion");
            caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.XXX, "Нетрадиционное порно / Special Interest Movies&Clips");
            caps.Categories.AddCategoryMapping(1681, NewznabStandardCategory.XXX, "Дефекация / Scat");
            caps.Categories.AddCategoryMapping(1683, NewznabStandardCategory.XXX, "Архив (общий)");
            caps.Categories.AddCategoryMapping(1688, NewznabStandardCategory.XXX, "Гей-порно / Gay Forum");
            caps.Categories.AddCategoryMapping(1720, NewznabStandardCategory.XXX, "Архив (Гей-порно)");

            return caps;
        }
    }

    public class PornoLabRequestGenerator : IIndexerRequestGenerator
    {
        public PornoLabSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public PornoLabRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
        {
            var searchUrl = string.Format("{0}/forum/tracker.php", Settings.BaseUrl.TrimEnd('/'));

            var searchString = term;

            // NameValueCollection don't support cat[]=19&cat[]=6
            var qc = new List<KeyValuePair<string, string>>
            {
                { "o", "1" },
                { "s", "2" }
            };

            // if the search string is empty use the getnew view
            if (string.IsNullOrWhiteSpace(searchString))
            {
                qc.Add("nm", searchString);
            }
            else
            {
                // use the normal search
                searchString = searchString.Replace("-", " ");
                qc.Add("nm", searchString);
            }

            foreach (var cat in Capabilities.Categories.MapTorznabCapsToTrackers(categories))
            {
                qc.Add("f[]", cat);
            }

            searchUrl = searchUrl + "?" + qc.GetQueryString();

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

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories));

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

    public class PornoLabParser : IParseIndexerResponse
    {
        private readonly PornoLabSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        private readonly Logger _logger;
        private static readonly Regex StripRussianRegex = new Regex(@"(\([А-Яа-яЁё\W]+\))|(^[А-Яа-яЁё\W\d]+\/ )|([а-яА-ЯЁё \-]+,+)|([а-яА-ЯЁё]+)");

        public PornoLabParser(PornoLabSettings settings, IndexerCapabilitiesCategories categories, Logger logger)
        {
            _settings = settings;
            _categories = categories;
            _logger = logger;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var rowsSelector = "table#tor-tbl > tbody > tr";

            var searchResultParser = new HtmlParser();
            var searchResultDocument = searchResultParser.ParseDocument(indexerResponse.Content);
            var rows = searchResultDocument.QuerySelectorAll(rowsSelector);
            foreach (var row in rows)
            {
                try
                {
                    var qDownloadLink = row.QuerySelector("a.tr-dl");

                    // Expects moderation
                    if (qDownloadLink == null)
                    {
                        continue;
                    }

                    var qForumLink = row.QuerySelector("a.f");
                    var qDetailsLink = row.QuerySelector("a.tLink");
                    var qSize = row.QuerySelector("td:nth-child(6) u");
                    var link = new Uri(_settings.BaseUrl + "forum/" + qDetailsLink.GetAttribute("href"));
                    var seederString = row.QuerySelector("td:nth-child(7) b").TextContent;
                    var seeders = string.IsNullOrWhiteSpace(seederString) ? 0 : ParseUtil.CoerceInt(seederString);

                    var timestr = row.QuerySelector("td:nth-child(11) u").TextContent;
                    var forum = qForumLink;
                    var forumid = forum.GetAttribute("href").Split('=')[1];
                    var title = _settings.StripRussianLetters
                        ? StripRussianRegex.Replace(qDetailsLink.TextContent, "")
                        : qDetailsLink.TextContent;
                    var size = ParseUtil.GetBytes(qSize.TextContent);
                    var leechers = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(8)").TextContent);
                    var grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(9)").TextContent);
                    var publishDate = DateTimeUtil.UnixTimestampToDateTime(long.Parse(timestr));
                    var release = new TorrentInfo
                    {
                        MinimumRatio = 1,
                        MinimumSeedTime = 0,
                        Title = title,
                        InfoUrl = link.AbsoluteUri,
                        Description = qForumLink.TextContent,
                        DownloadUrl = link.AbsoluteUri,
                        Guid = link.AbsoluteUri,
                        Size = size,
                        Seeders = seeders,
                        Peers = leechers + seeders,
                        Grabs = grabs,
                        PublishDate = publishDate,
                        Categories = _categories.MapTrackerCatToNewznab(forumid),
                        DownloadVolumeFactor = 1,
                        UploadVolumeFactor = 1
                    };

                    torrentInfos.Add(release);
                }
                catch (Exception ex)
                {
                    _logger.Error(string.Format("Pornolab: Error while parsing row '{0}':\n\n{1}", row.OuterHtml, ex));
                }
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class PornoLabSettingsValidator : AbstractValidator<PornoLabSettings>
    {
        public PornoLabSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Password).NotEmpty();
        }
    }

    public class PornoLabSettings : IIndexerSettings
    {
        private static readonly PornoLabSettingsValidator Validator = new PornoLabSettingsValidator();

        public PornoLabSettings()
        {
            Username = "";
            Password = "";
        }

        [FieldDefinition(1, Label = "Base Url", HelpText = "Select which baseurl Prowlarr will use for requests to the site", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Username", HelpText = "Site Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Password", HelpText = "Site Password", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Password { get; set; }

        [FieldDefinition(4, Label = "Strip Russian Letters", HelpLink = "Strip Cyrillic letters from release names", Type = FieldType.Checkbox)]
        public bool StripRussianLetters { get; set; }

        [FieldDefinition(5)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
