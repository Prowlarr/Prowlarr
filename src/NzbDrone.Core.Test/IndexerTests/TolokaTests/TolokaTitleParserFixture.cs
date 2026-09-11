using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions;

namespace NzbDrone.Core.Test.IndexerTests.TolokaTests
{
    [TestFixture]
    public class TolokaTitleParserFixture
    {
        private static readonly ICollection<IndexerCategory> TvCategories = new List<IndexerCategory> { NewznabStandardCategory.TVAnime };

        private readonly TolokaTitleParser _titleParser = new();

        // The Cyrillic title part can leave a stranded "<token> (S..) / " prefix after stripping when it
        // contains characters the strip regex keeps: an apostrophe ("Сім'я") or a digit ("Володар 2").
        [TestCase("Сім'я шпигуна (Сезон 1) / Spy x Family (2022) WEB-DL 1080p H.265 Ukr/Jap | sub Ukr", "Spy x Family (2022) WEB-DL 1080p H.265 Ukr/Jap | sub Ukr (S1)")]
        [TestCase("Володар 2 (Сезон 2) / Overlord II (2018) WEBRip 1080p H.265 Ukr/Jap | Sub Ukr", "Overlord II (2018) WEBRip 1080p H.265 Ukr/Jap | Sub Ukr (S2)")]
        [TestCase("Берсерк / Berserk (2016) WEB-DL 1080p Ukr/Jap", "Berserk (2016) WEB-DL 1080p Ukr/Jap")]
        public void should_relocate_stranded_season_token_after_stripping(string title, string expected)
        {
            _titleParser.Parse(title, TvCategories, stripCyrillicLetters: true)
                .Should().Be(expected);
        }
    }
}
