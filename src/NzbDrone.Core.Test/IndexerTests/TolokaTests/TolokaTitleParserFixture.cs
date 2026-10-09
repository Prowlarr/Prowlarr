using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.IndexerTests.TolokaTests
{
    [TestFixture]
    public class TolokaTitleParserFixture : TestBase
    {
        private readonly TolokaTitleParser _parser = new();

        private static readonly ICollection<IndexerCategory> TvCategories = new List<IndexerCategory> { NewznabStandardCategory.TV, NewznabStandardCategory.TVHD };
        private static readonly ICollection<IndexerCategory> MovieCategories = new List<IndexerCategory> { NewznabStandardCategory.Movies, NewznabStandardCategory.MoviesHD };

        [TestCase("Спліт / Split (2016) UHD BDRemux 4K 2160p HDR H.265 Ukr/Eng | Sub Ukr",
                  "Split (2016) UHD BDRemux 4K 2160p HDR H.265 Ukr/Eng | Sub Ukr")]
        public void should_strip_cyrillic_title_from_movie_release(string title, string expected)
        {
            _parser.Parse(title, MovieCategories).Should().Be(expected);
        }

        [TestCase("Укриття (Сезон 3, Серії 1-9) / Silo (Season 3, Episodes 1-9) (2026) WEB-DL 1080p H.265 Ukr/Eng | sub Ukr/Eng",
                  "Silo (S03E01-09) (2026) WEB-DL 1080p H.265 Ukr/Eng | sub Ukr/Eng")]
        [TestCase("Укриття (Сезон 3, Серії 1-9) / Silo (S3, EP 1-9) (2026) WEB-DL 1080p Ukr/Eng | sub Ukr/Eng",
                  "Silo (S03E01-09) (2026) WEB-DL 1080p Ukr/Eng | sub Ukr/Eng")]
        [TestCase("Укриття (S3, C 1-9) / Silo (S3) (2026) WEB-DL 4K 2160p H.265 HDR DV P8 Ukr/Eng | Sub Ukr/Eng/Heb",
                  "Silo (S03E01-09) (2026) WEB-DL 4K 2160p H.265 HDR DV P8 Ukr/Eng | Sub Ukr/Eng/Heb")]
        [TestCase("Укриття (Сезон 3, Серія 10) / Silo (S3, E 10) (2026) WEB-DL 2160p Ukr/Eng",
                  "Silo (S03E10) (2026) WEB-DL 2160p Ukr/Eng")]
        public void should_normalize_partial_season_pack_to_zero_padded_episode_range(string title, string expected)
        {
            _parser.Parse(title, TvCategories).Should().Be(expected);
        }

        [TestCase("Укриття (Сезон 2) / Silo (S2) (2023) WEB-DL 2160p DV HDR10+ 3xUkr/Eng | sub Ukr/Eng",
                  "Silo (S02) (2023) WEB-DL 2160p DV HDR10+ 3xUkr/Eng | sub Ukr/Eng")]
        [TestCase("Укриття (Сезон 2) / Silo (Season 2) (2023) WEB-DL 1080p 3xUkr/Eng | Sub Ukr/Eng",
                  "Silo (S02) (2023) WEB-DL 1080p 3xUkr/Eng | Sub Ukr/Eng")]
        public void should_normalize_full_season_pack_to_zero_padded_season_and_drop_duplicate_tag(string title, string expected)
        {
            _parser.Parse(title, TvCategories).Should().Be(expected);
        }

        [TestCase("Дім (Сезон 1-8) / House (Seasons 1-8) (2004-2012) BDRip 1080p Ukr/Eng | Sub Eng",
                  "House (S1-8) (2004-2012) BDRip 1080p Ukr/Eng | Sub Eng (S1-8)")]
        public void should_leave_multi_season_range_untouched(string title, string expected)
        {
            _parser.Parse(title, TvCategories).Should().Be(expected);
        }

        [TestCase("Укриття (Сезон 3, Серії 1-9 з 10) / Silo (2026) WEB-DL 1080p Ukr/Eng",
                  "Silo (2026) WEB-DL 1080p Ukr/Eng (S03E01-09 of 10)")]
        public void should_zero_pad_episode_of_count_form(string title, string expected)
        {
            _parser.Parse(title, TvCategories).Should().Be(expected);
        }
    }
}
