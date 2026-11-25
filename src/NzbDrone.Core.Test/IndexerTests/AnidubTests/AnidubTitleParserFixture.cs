using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions;

namespace NzbDrone.Core.Test.IndexerTests.AnidubTests
{
    [TestFixture]
    public class AnidubTitleParserFixture
    {
        private static readonly ICollection<IndexerCategory> AnimeTvCategories = new List<IndexerCategory> { NewznabStandardCategory.TVAnime };
        private static readonly ICollection<IndexerCategory> MovieCategories = new List<IndexerCategory> { NewznabStandardCategory.Movies };
        private static readonly ICollection<IndexerCategory> AudioCategories = new List<IndexerCategory> { NewznabStandardCategory.Audio };

        private readonly AnidubTitleParser _titleParser = new();

        [TestCase("Провожающая в последний путь Фрирен / Sousou no Frieren [28 из 28]", "Sousou no Frieren S1")]
        [TestCase("Провожающая в последний путь Фрирен ТВ-2 / Sousou no Frieren TV-2 [10 из 24]", "Sousou no Frieren S2E01-10 of 24")]
        [TestCase("Дандадан / Dandadan [12 из 12]", "Dandadan S1")]
        [TestCase("Дандадан TV-2 / Dandadan ТВ-2 [12 из 12]", "Dandadan S2")]
        [TestCase("Владыка ТВ-4 / Overlord TV-IV [13 из 13]", "Overlord S4")]
        [TestCase("Блич / Bleach [151-366 из 366]", "Bleach E151-366 of 366")]
        [TestCase("Ванпанчмен ТВ-1 / One-Punch Man TV-1 [12 из 12] + Specials [6 из 6]", "One-Punch Man S1")]
        [TestCase("Ми-ми-ми-мишка ТВ-2 / Kuma Kuma Kuma Bear Punch! [12 из 12]", "Kuma Kuma Kuma Bear Punch! S2")]
        [TestCase("Гинтама / Gintama ТВ-1 [81 из 201]", "Gintama S1E01-81 of 201")]
        [TestCase("Гинтама ТВ-4 / Gintama TV-4 [51 из 51 + SP]", "Gintama S4")]
        [TestCase("Истории чудовищ / Bakemonogatari [12 из 12 + 3 SP]", "Bakemonogatari S1")]
        [TestCase("Боруто: Новое околение / Boruto: Naruto Next Generations [293 из ххх]", "Boruto: Naruto Next Generations S1E01-293")]
        [TestCase("Волейбол!!  ТВ-4/ Haikyuu!! To the Top 2nd Cour [12 из 13]", "Haikyuu!! To the Top 2nd Cour S4E01-12 of 13")]
        [TestCase("Повесть о Стране Цветных Облаков TV-1 / Saiunkoku Monogatari TV-1 [01 из 39]", "Saiunkoku Monogatari S1E01 of 39")]
        [TestCase("Семья шпиона ТВ-1 Часть 2 / Spy x Family TV-1 Part 2 [13 из 13]", "Spy x Family Part 2 S1")]
        [TestCase("Блич: Тысячелетняя кровавая война - Конфликт ТВ-2 / Bleach: Sennen Kessen-hen - Soukoku-tan TV-2 [14 из 14]", "Bleach: Sennen Kessen-hen - Soukoku-tan S2")]
        [TestCase("Блич / Bleach", "Bleach")]
        [TestCase("Наруто (спэшлы) / Naruto Specials [02 из 02]", "Naruto Specials S1")]
        [TestCase("Хиган / Sweat Punch Series 4 Higan OVA [01 из 01]", "Sweat Punch Series 4 Higan OVA S1")]
        public void should_parse_anime_tv_titles_with_strip_cyrillic(string title, string expected)
        {
            _titleParser.Parse(title, AnimeTvCategories, stripCyrillicLetters: true).Should().Be(expected);
        }

        [TestCase("Провожающая в последний путь Фрирен / Sousou no Frieren [28 из 28]", "Провожающая в последний путь Фрирен / Sousou no Frieren S1")]
        [TestCase("Дандадан TV-2 / Dandadan ТВ-2 [12 из 12]", "Дандадан / Dandadan S2")]
        public void should_keep_cyrillic_title_part_when_strip_disabled(string title, string expected)
        {
            _titleParser.Parse(title, AnimeTvCategories, stripCyrillicLetters: false).Should().Be(expected);
        }

        [TestCase("Наруто: Кровавая тюрьма / Gekijouban Naruto: Blood Prison", "Gekijouban Naruto: Blood Prison")]
        [TestCase("Наруто: Ураганные Хроники - Узы / Gekijouban Naruto Shippuuden: Kizuna [Movie]", "Gekijouban Naruto Shippuuden: Kizuna")]
        public void should_parse_movie_titles_with_strip_cyrillic(string title, string expected)
        {
            _titleParser.Parse(title, MovieCategories, stripCyrillicLetters: true).Should().Be(expected);
        }

        [Test]
        public void should_add_russian_language_token_when_enabled()
        {
            _titleParser.Parse("Провожающая в последний путь Фрирен / Sousou no Frieren [28 из 28]", AnimeTvCategories, stripCyrillicLetters: true, addRussianToTitle: true)
                .Should().Be("Sousou no Frieren S1 RUS");
        }

        [Test]
        public void should_not_duplicate_russian_language_token()
        {
            _titleParser.Parse("Test Title RUS", AnimeTvCategories, stripCyrillicLetters: false, addRussianToTitle: true)
                .Should().Be("Test Title RUS");
        }

        [Test]
        public void should_not_touch_markers_for_non_tv_categories()
        {
            _titleParser.Parse("Волейбол!! / Haikyuu!! [OST]", AudioCategories, stripCyrillicLetters: true)
                .Should().Be("Haikyuu!! [OST]");
        }
    }
}
