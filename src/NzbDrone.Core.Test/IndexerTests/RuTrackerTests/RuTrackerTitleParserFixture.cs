using System.Linq;
using System.Net;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.RuTrackerTests
{
    [TestFixture]
    public class RuTrackerTitleParserFixture : CoreTest<RuTrackerTitleParser>
    {
        [TestCase("Табакошка / Yani Neko / Chainsmoker Cat [TV] [12 из 12] [RUS(int), JAP+Sub] [2026, Сэйнэн, Комедия, WEB-DL] [1080p]", new int[] { 5070, 101105 }, "Табакошка / Yani Neko / Chainsmoker Cat [TV] [12 of 12] [RUS(int), JAP] [2026, Сэйнэн, Комедия, WEB-DL] [1080p]")]
        [TestCase("Мао / Mao [TV] [26 из 26] [RUS(int), JAP+Sub] [2026, Сёнэн, Сверхъестественное, WEB-DL] [1080p]", new int[] { 5070, 101105 }, "Мао / Mao [TV] [26 of 26] [RUS(int), JAP] [2026, Сёнэн, Сверхъестественное, WEB-DL] [1080p]")]
        [TestCase("История о перекуре за супермаркетом / Super no Ura de Yani Suu Futari / Behind the Supermarket, Smoking with You. / YaniSuu [TV] [12 из 12] [JAP+Sub, RUS(int)] & [1-11 из 12] [ENG] [2026, сэйнэн, романтика, WEB-DL] [1080p]", new int[] { 5070, 101105 }, "История о перекуре за супермаркетом / Super no Ura de Yani Suu Futari / Behind the Supermarket, Smoking with You. / YaniSuu [TV] [12 of 12] [JAP, RUS(int)] & [1-11 из 12] [ENG] [2026, сэйнэн, романтика, WEB-DL] [1080p]")]
        [TestCase("О моём перерождении в меч (ТВ-2) / Tensei shitara Ken deshita II / Reincarnated as a Sword Season 2 / I became the sword by transmigrating 2nd Season, TenKe 2 [TV] [1 из 12] [RUS(int), JAP+Sub] [2026, приключения, фэнтези, WEB-DL] [1080p]", new int[] { 5070, 102491 }, "О моём перерождении в меч (TV-2) / Tensei shitara Ken deshita II / Reincarnated as a Sword Season 2 / I became the sword by transmigrating 2nd Season, TenKe 2 [TV] [1 of 12] [RUS(int), JAP] [2026, приключения, фэнтези, WEB-DL] [1080p]")]
        [TestCase("Магическая битва: Смертельная миграция (ТВ-3, часть 1) / Jujutsu Kaisen: Shimetsu Kaiyuu - Zenpen / Sorcery Fight: The Culling Game / JJK 3rd Season [TV] [12 из 12] [RUS(int), ENG, JAP+Sub] [2026, приключение, сверхъестественное, фэнтези, сёнэн, BDRemux] [1080p]", new int[] { 5070, 101105 }, "Магическая битва: Смертельная миграция (TV-3, часть 1) / Jujutsu Kaisen: Shimetsu Kaiyuu - Zenpen / Sorcery Fight: The Culling Game / JJK 3rd Season [TV] [12 of 12] [RUS(int), ENG, JAP] [2026, приключение, сверхъестественное, фэнтези, сёнэн, BDRemux] [1080p]")]
        [TestCase("Стены изо льда (ТВ-2) / Koori no Jouheki 2nd Season / The Ramparts of Ice Season 2 / Ледяная стена 2 [TV] [1 из XX] [ENG, JAP+Sub] [2026, комедия, романтика, WEB-DL] [1080p]", new int[] { 5070, 101106 }, "Стены изо льда (TV-2) / Koori no Jouheki 2nd Season / The Ramparts of Ice Season 2 / Ледяная стена 2 [TV] [1 из XX] [ENG, JAP] [2026, комедия, романтика, WEB-DL] [1080p]")]
        [TestCase("Население приграничного владения начинается с нуля / Ryoumin 0-nin Start no Henkyou Ryoushu-sama / The Frontier Lord Begins with Zero Subjects / Лорд приграничья без подданных [TV] [12 из 12] [JAP+Sub] & [01-11 из 12] [RUS(int)] [2026, Фэнтези, WEB-DL] [1080p]", new int[] { 5070, 102491 }, "Население приграничного владения начинается с нуля / Ryoumin 0-nin Start no Henkyou Ryoushu-sama / The Frontier Lord Begins with Zero Subjects / Лорд приграничья без подданных [TV] [12 of 12] [JAP] & [01-11 из 12] [RUS(int)] [2026, Фэнтези, WEB-DL] [1080p]")]
        [TestCase("Большой Куш / One Piece / Ван-Пис [TV] [1156-1180 из 1000+] [RUS(ext), JAP+Sub] [1999, приключения, комедия, фэнтези, сёнэн, WEB-DL] [1080p]", new int[] { 5070, 102544 }, "Большой Куш / One Piece / Ван-Пис [TV] [1156-1180 из 1000+] [RUS(ext), JAP] [1999, приключения, комедия, фэнтези, сёнэн, WEB-DL] [1080p]")]
        [TestCase("Юный лорд — мастер побега (ТВ-2) / Nige Jouzu no Wakagimi / The Elusive Samurai / Беглый самурай / Неуловимый самурай [TV] [1-11 из 12] [RUS(ext), JAP+Sub] [2026, сёнэн, приключения, комедия, WEB-DL] [1080p]", new int[] { 5070, 101106 }, "Юный лорд - мастер побега (TV-2) / Nige Jouzu no Wakagimi / The Elusive Samurai / Беглый самурай / Неуловимый самурай [TV] [1-11 из 12] [RUS(ext), JAP] [2026, сёнэн, приключения, комедия, WEB-DL] [1080p]")]
        [TestCase("Переродившись в аристократа, я стану успешным благодаря навыку оценки (ТВ-3) / Tensei Kizoku, Kantei Skill de Nariagaru 3 / As a Reincarnated Aristocrat, I'll Use My Appraisal Skill to Rise in the World 3rd Season (Такао Като) [TV] [1 из ?] [JAP+Sub] [2026, приключения, фэнтези, WEB-DL] [1080p]", new int[] { 5070, 101106 }, "Переродившись в аристократа, я стану успешным благодаря навыку оценки (TV-3) / Tensei Kizoku, Kantei Skill de Nariagaru 3 / As a Reincarnated Aristocrat, I'll Use My Appraisal Skill to Rise in the World 3rd Season [TV] [1 из ?] [JAP] [2026, приключения, фэнтези, WEB-DL] [1080p]")]
        [TestCase("Сто девушек, которые очень-очень-очень-очень-очень сильно тебя любят (ТВ-2) / Kimi no Koto ga Dai Dai Dai Dai Daisuki (Daidaidaidaidaisuki) na 100-nin no Kanojo 2 / Hyakkano [TV+Special] [12+1 из 12+1] [JAP+Sub] & [12+0 из 12+1] [RUS(ext)] [2025, комедия, романтика, гарем, BDRemux] [1080p]", new int[] { 5070, 101105 }, "Сто девушек, которые очень-очень-очень-очень-очень сильно тебя любят (TV-2) / Kimi no Koto ga Dai Dai Dai Dai Daisuki (Daidaidaidaidaisuki) na 100-nin no Kanojo 2 / Hyakkano [TV+Special] [12+1 of 12+1] [JAP] & [12+0 of 12+1] [RUS(ext)] [2025, комедия, романтика, гарем, BDRemux] [1080p]")]
        [TestCase("Фумун / Fumoon / 24 Hour TV Specials [Special] [RUS(int), JAP+Sub] [1981, фантастика, BDRip] [1080p]", new int[] { 5070, 101105 }, "Фумун / Fumoon / 24 Hour TV Specials [Special] [RUS(int), JAP] [1981, фантастика, BDRip] [1080p]")]
        [TestCase("Дораэмон / Doraemon [TV+Special] [1-515+25 из 1787+25] [RUS(int)] [1979, приключения, комедия, фантастика, для детей, TVRip]", new int[] { 5070, 102491 }, "Дораэмон / Doraemon [TV+Special] [1-515+25 из 1787+25] [RUS(int)] [1979, приключения, комедия, фантастика, для детей, TVRip]")]
        [TestCase("Судья Тьмы / Yami no Shihoukan Judge [OVA] [1] [RUS(ext)x2, Sub] [1991, Ужасы, Сэйнэн, Сверхъестественное, DVDRip]", new int[] { 5070, 102491 }, "Судья Тьмы / Yami no Shihoukan Judge [OVA] [1] [RUS(ext)x2, ]")]
        [TestCase("Инопланетяне в школе № 9 / Alien Nine / Инопланетяне школы №9 / Чужой 9 [OVA] [4 из 4] [RUS(int), JAP+Sub] [2001, ужасы, фантастика, BDRip] [720p]", new int[] { 5070, 101105 }, "Инопланетяне в школе № 9 / Alien Nine / Инопланетяне школы №9 / Чужой 9 [OVA] [4 of 4] [RUS(int), JAP] [2001, ужасы, фантастика, BDRip] [720p]")]
        [TestCase("Сезон призм / Prism Season [OVA] [ZXX] [1989, детское, LDRip]", new int[] { 5070, 101389 }, "Сезон призм / Prism Season [OVA] [ZXX] [1989, детское, LDRip]")]
        [TestCase("Агент времени (ТВ-3, часть 1) / Shiguang Dailiren III / Link Click 3rd Season Part One [ONA] [1-9 из 12] [RUS(int), CHI+Sub] [2026, драма, фэнтези, WEB-DL] [1080p]", new int[] { 5070, 101106 }, "Агент времени (TV-3, часть 1) / Shiguang Dailiren III / Link Click 3rd Season Part One [ONA] [1-9 из 12] [RUS(int), CHI] [2026, драма, фэнтези, WEB-DL] [1080p]")]
        [TestCase("Кайдзю номер восемь: Рабочий день Наруми / Kaijuu 8-gou: Narumi no Heijitsu / Kaiju No. 8: Narumi's Week at Work [ONA] [4 из 4] [JAP+Sub] & [1-2 из 4] [RUS(int)] [2026, сёнэн, приключения, фантастика, WEB-DL] [1080p]", new int[] { 5070, 102491 }, "Кайдзю номер восемь: Рабочий день Наруми / Kaijuu 8-gou: Narumi no Heijitsu / Kaiju No. 8: Narumi's Week at Work [ONA] [4 of 4] [JAP] & [1-2 из 4] [RUS(int)] [2026, сёнэн, приключения, фантастика, WEB-DL] [1080p]")]
        [TestCase("Сервамп: Спецвыпуски / Servamp Specials / Sleepy Life of Servamp / Сонная жизнь Сервампа / Слуга-вампир / Слугвамп [Special] [4 из 4] [ENG, JAP+Sub] [2016, дзёсэй, экшен, BDRip] [1080p]", new int[] { 5070, 101105 }, "Сервамп: Спецвыпуски / Servamp Specials / Sleepy Life of Servamp / Сонная жизнь Сервампа / Слуга-вампир / Слугвамп [Special] [4 of 4] [ENG, JAP] [2016, дзёсэй, экшен, BDRip] [1080p]")]
        [TestCase("Повелитель: Священное королевство / Gekijouban Overlord: Sei Oukoku-hen / Overlord: The Sacred Kingdom / Overlord Movie 3: The Holy Kingdom / Владыка: Святое королевство / Оверлорд [Movie+Special] [1+1 из 1+1] [JAP+Sub] & [1+0 из 1+1] [RUS(int)] [2024, приключения, фэнтези, BDRip] [1080p]", new int[] { 5070, 101105 }, "Повелитель: Священное королевство / Gekijouban Overlord: Sei Oukoku-hen / Overlord: The Sacred Kingdom / Overlord Movie 3: The Holy Kingdom / Владыка: Святое королевство / Оверлорд [Movie+Special] [1+1 of 1+1] [JAP] & [1+0 of 1+1] [RUS(int)] [2024, приключения, фэнтези, BDRip] [1080p]")]
        [TestCase("Мобильный воин Гандам: Железнокровные сироты — Охота Урд. Путь маленького претендента / Kidou Senshi Gundam: Tekketsu no Orphans - Urdr Hunt: Chiisana Chousensha no Kiseki / Iron-Blooded Orphans – Urdr Hunt Special Edition: Path of the Little Challenger [Movie] [JAP+Sub] [2025, Меха, WEB-DL] [1080p]", new int[] { 5070, 101642 }, "Мобильный воин Гандам: Железнокровные сироты - Охота Урд. Путь маленького претендента / Kidou Senshi Gundam: Tekketsu no Orphans - Urdr Hunt: Chiisana Chousensha no Kiseki / Iron-Blooded Orphans - Urdr Hunt Special Edition: Path of the Little Challenger [Movie] [JAP] [2025, Меха, WEB-DL] [1080p]")]
        [TestCase("Этот глупый свин не понимает мечту девочки с рюкзаком (Фильм-3) / Seishun Buta Yarou wa Randoseru Girl no Yume o (wo) Minai / Rascal Does Not Dream of a Knapsack Kid [Movie] [RUS(int), JAP+Sub] [2023, драма, романтика, мистика, BDRip] [HWP]", new int[] { 5070, 101391 }, "Этот глупый свин не понимает мечту девочки с рюкзаком (Фильм-3) / Seishun Buta Yarou wa Randoseru Girl no Yume o (wo) Minai / Rascal Does Not Dream of a Knapsack Kid [Movie] [RUS(int), JAP] [2023, драма, романтика, мистика, BDRip] [HWP]")]
        [TestCase("Укрытие / Бункер / Silo / Сезон: 3 / Серии: 1-10 из 10 (Майкл Диннер, Арик Авелино) [2026, США, Фантастика, драма, триллер, WEB-DL 1080p] MVO (LostFilm) + Original + Sub (Rus, Eng)", new int[] { 5040, 100266 }, "Укрытие / Бункер / Silo / S3E1-10 of 10 [2026, США, Фантастика, драма, триллер, WEB-DL 1080p] + Original + ) RUS")]
        [TestCase("Укрытие / Silo / Сезон: 3 / Серии: 1-10 из 10 (Берт, Майкл Диннер, Арик Авелино, Олрик Райли) [2026, США, Фантастика, драма, триллер, HDR10, HDR10+, Dolby Vision, WEB-DL 2160p, 4k] 5 x MVO + VO (М. Яроцкий) + Original + Sub (Rus, Ukr, Eng, Heb)", new int[] { 5020, 101171 }, "Укрытие / Silo / S3E1-10 of 10 [2026, США, Фантастика, драма, триллер, HDR10, HDR10+, Dolby Vision, WEB-DL 2160p] 5 x MVO + VO + Original + ) RUS")]
        [TestCase("Укрытие / Бункер / Silo / Сезон: 1 / Серии: 1-10 из 10 (Мортен Тильдум) [2023, США, фантастика, драма, триллер, WEB-DL 1080p] MVO (LostFilm) + Original + Sub (Rus, Eng)", new int[] { 5040, 100266 }, "Укрытие / Бункер / Silo / S1E1-10 of 10 [2023, США, фантастика, драма, триллер, WEB-DL 1080p] + Original + ) RUS")]
        [TestCase("Матрица / The Matrix (Энди Вачовски, Ларри Вачовски / Andy Wachowski, Larry Wachowski) [1999, США, фантастика, WEB-DL 1080p] [Open Matte] Dub + 3х MVO + 3х DVO + 8х AVO + VO + Original (Eng) + Sub (Rus, Eng, Multi)", new int[] { 2040, 102198 }, "Матрица / The Matrix [1999, США, фантастика, WEB-DL 1080p] [Open Matte] Dub + 3х MVO + 3х DVO + 8х AVO + VO + Original (Eng) + ) RUS")]
        [TestCase("Матрица: Революция / The Matrix Revolutions (Энди Вачовски / Andy Wachowski, Ларри Вачовски / Larry Wachowski) [2003, США, Австралия, фантастика, боевик, WEB-DL 1080p] [Open Matte] Dub + 2x MVO + DVO + 2x AVO + Original (Eng) + Sub (Rus, Eng)", new int[] { 2040, 100313 }, "Матрица: Революция / The Matrix Revolutions [2003, США, Австралия, фантастика, боевик, WEB-DL 1080p] [Open Matte] Dub + 2x MVO + DVO + 2x AVO + Original (Eng) + ) RUS")]
        public void should_parse_sanitized_real_titles(string source, int[] categoryIds, string expected)
        {
            var categories = categoryIds.Select(category => new IndexerCategory { Id = category }).ToArray();

            Subject.Parse(source, categories, stripCyrillicLetters: false, addRussianToTitle: true)
                .Should().Be(expected);
        }

        [TestCase("[12 из 12]", "[12 of 12]")]
        [TestCase("[3 из 12]", "[3 of 12]")]
        [TestCase("[01 из 12]", "[01 of 12]")]
        [TestCase("[12+1 из 12+1]", "[12+1 of 12+1]")]
        [TestCase("[12+0 из 12+1]", "[12+0 of 12+1]")]
        [TestCase("[01-12 из 12]", "[01-12 из 12]")]
        [TestCase("[1-11 из 12]", "[1-11 из 12]")]
        [TestCase("[1 из XX]", "[1 из XX]")]
        [TestCase("[1 из ?]", "[1 из ?]")]
        public void should_preserve_counts_without_asserting_episode_or_season(string count, string expected)
        {
            var title = $"Example [TV] {count} [RUS(int)] [2026, WEB-DL] [1080p]";
            var result = Subject.Parse(title, new[] { NewznabStandardCategory.TVAnime }, stripCyrillicLetters: false);

            result.Should().Be($"Example [TV] {expected} [RUS(int)] [2026, WEB-DL] [1080p]");
        }

        [TestCase("[JAP+Sub]", "[JAP]")]
        [TestCase("[CHI+Sub]", "[CHI]")]
        [TestCase("[KOR+Sub]", "[KOR]")]
        [TestCase("[RUS(int), JPN+Sub]", "[RUS(int), JPN]")]
        public void should_preserve_explicit_anime_audio_without_inventing_a_language(string audio, string expected)
        {
            var result = Subject.Parse(
                $"Example [TV] [12 из 12] {audio} [2026, WEB-DL] [1080p]",
                new[] { NewznabStandardCategory.TVAnime },
                stripCyrillicLetters: false,
                addRussianToTitle: true);

            result.Should().Contain(expected).And.NotContain("Japanese").And.NotEndWith(" RUS");
        }

        [TestCase("ТВ-2")]
        [TestCase("TV-2")]
        public void should_not_turn_tracker_tv_ordinals_into_catalog_seasons(string ordinal)
        {
            Subject.Parse(
                $"Example ({ordinal}) [TV] [12 из 12] [JAP] [1080p]",
                new[] { NewznabStandardCategory.TVAnime },
                stripCyrillicLetters: false)
                .Should().Be("Example (TV-2) [TV] [12 of 12] [JAP] [1080p]");
        }

        [TestCase(false, false, false)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(true, true, false)]
        [TestCase(false, false, true)]
        [TestCase(true, false, true)]
        public void should_normalize_before_optional_stripping_and_tag_movement(bool strip, bool moveFirst, bool moveAll)
        {
            var result = Subject.Parse(
                "[Group] Example (ТВ-2) [TV] [12 из 12] [RUS(int), JAP+Sub] [2026, WEB-DL] [1080p]",
                new[] { NewznabStandardCategory.TVAnime },
                strip,
                moveFirst,
                moveAll);

            result.Should().Contain("TV-2").And.Contain("[12 of 12]").And.Contain("[RUS(int), JAP]");
            result.Should().NotContain("S2").And.NotContain("E12").And.NotEndWith(" RUS");
        }

        [TestCase(5040, "Example (S2) [TV] E12 of 12 [JAP]")]
        [TestCase(2040, "Example (ТВ-2) [TV] [12 из 12] [JAP]")]
        [TestCase(3000, "Example (ТВ-2) [TV] [12 из 12] [JAP]")]
        public void should_not_change_other_categories(int category, string expected)
        {
            Subject.Parse("Example (ТВ-2) [TV] [12 из 12] [JAP]", new[] { new IndexerCategory { Id = category } }, stripCyrillicLetters: false)
                .Should().Be(expected);
        }

        [TestCase(5040, "Example [JAP] RUS")]
        [TestCase(2040, "Example [JAP] RUS")]
        [TestCase(3000, "Example [JAP]")]
        public void should_preserve_non_anime_russian_hint_behavior(int category, string expected)
        {
            Subject.Parse("Example [JAP]", new[] { new IndexerCategory { Id = category } }, stripCyrillicLetters: false, addRussianToTitle: true)
                .Should().Be(expected);
        }

        [Test]
        public void should_preserve_source_description_and_release_metadata()
        {
            var settings = new RuTrackerSettings { BaseUrl = "https://rutracker.invalid/", AddRussianToTitle = true };
            var parser = new RuTrackerParser(settings, Mocker.Resolve<RuTracker>().Capabilities.Categories);
            var request = new IndexerRequest("https://rutracker.invalid/forum/tracker.php", HttpAccept.Html);
            var response = new HttpResponse(request.HttpRequest, new HttpHeader(), new CookieCollection(), ReadAllText("Files/Indexers/RuTracker/search.html"));
            var release = (TorrentInfo)parser.ParseResponse(new IndexerResponse(request, response)).Single();

            release.Title.Should().Be("Табакошка / Yani Neko / Chainsmoker Cat [TV] [12 of 12] [RUS(int), JAP] [2026, Сэйнэн, Комедия, WEB-DL] [1080p]");
            release.Description.Should().Be("Табакошка / Yani Neko / Chainsmoker Cat [TV] [12 из 12] [RUS(int), JAP+Sub] [2026, Сэйнэн, Комедия, WEB-DL] [1080p]");
            release.Categories.Should().Contain(category => category.Id == 5070);
            release.InfoUrl.Should().Be("https://rutracker.invalid/forum/viewtopic.php?t=1");
            release.DownloadUrl.Should().Be("https://rutracker.invalid/forum/dl.php?t=1");
            release.Size.Should().Be(11948050347L);
            release.Seeders.Should().Be(441);
            release.Peers.Should().Be(443);
            release.Languages.Should().BeEmpty();
        }
    }
}
