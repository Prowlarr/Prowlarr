using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.IPTorrentsTests
{
    [TestFixture]
    public class IPTorrentsFixture : CoreTest<IPTorrents>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition
            {
                Name = "IPTorrents",
                Settings = new IPTorrentsSettings
                {
                    Cookie = "uid=123; pass=abc",
                    UserAgent = "Mozilla/5.0"
                }
            };
        }

        [Test]
        public void should_advertise_imdb_search_by_default()
        {
            var caps = Subject.Capabilities;

            caps.MovieSearchParams.Should().Contain(MovieSearchParam.ImdbId);
            caps.TvSearchParams.Should().Contain(TvSearchParam.ImdbId);
        }

        [Test]
        public void should_not_advertise_imdb_search_when_disabled()
        {
            ((IPTorrentsSettings)Subject.Definition.Settings).DisableImdbSearch = true;

            var caps = Subject.Capabilities;

            caps.MovieSearchParams.Should().NotContain(MovieSearchParam.ImdbId);
            caps.TvSearchParams.Should().NotContain(TvSearchParam.ImdbId);
            caps.MovieSearchParams.Should().Contain(MovieSearchParam.Q);
            caps.TvSearchParams.Should().Contain(TvSearchParam.Q);
        }

        [Test]
        public void should_advertise_imdb_search_without_definition()
        {
            Subject.Definition = null;

            var caps = Subject.Capabilities;

            caps.MovieSearchParams.Should().Contain(MovieSearchParam.ImdbId);
            caps.TvSearchParams.Should().Contain(TvSearchParam.ImdbId);
        }
    }
}
