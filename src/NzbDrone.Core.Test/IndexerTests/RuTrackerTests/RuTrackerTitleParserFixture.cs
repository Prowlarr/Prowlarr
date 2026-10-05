using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using FluentAssertions;
using Newtonsoft.Json.Linq;
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
        public static IEnumerable TitleCases
        {
            get
            {
                var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "Indexers", "RuTracker", "titles.json");

                return JArray.Parse(File.ReadAllText(path))
                    .Select(row => new TestCaseData(
                            (string)row["source"],
                            row["categories"].Values<int>().ToArray(),
                            (string)row["expected"])
                        .SetName($"should_parse_{(string)row["id"]}"));
            }
        }

        [TestCaseSource(nameof(TitleCases))]
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
            var fixture = JArray.Parse(ReadAllText("Files/Indexers/RuTracker/titles.json")).Single(row => (string)row["id"] == "rt-6877836");
            var settings = new RuTrackerSettings { BaseUrl = "https://rutracker.invalid/", AddRussianToTitle = true };
            var parser = new RuTrackerParser(settings, Mocker.Resolve<RuTracker>().Capabilities.Categories);
            var request = new IndexerRequest("https://rutracker.invalid/forum/tracker.php", HttpAccept.Html);
            var response = new HttpResponse(request.HttpRequest, new HttpHeader(), new CookieCollection(), ReadAllText("Files/Indexers/RuTracker/search.html"));
            var release = (TorrentInfo)parser.ParseResponse(new IndexerResponse(request, response)).Single();

            release.Title.Should().Be((string)fixture["expected"]);
            release.Description.Should().Be((string)fixture["source"]);
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
