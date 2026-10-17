using System;
using System.Linq;
using System.Net;
using System.Xml;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.NewznabTests
{
    [TestFixture]
    public class NewznabCapabilitiesProviderFixture : CoreTest<NewznabCapabilitiesProvider>
    {
        private NewznabSettings _settings;
        private IndexerDefinition _definition;
        private string _caps;

        [SetUp]
        public void SetUp()
        {
            _settings = new NewznabSettings()
            {
                BaseUrl = "http://indxer.local"
            };

            _definition = new IndexerDefinition()
            {
                Id = 5,
                Name = "Newznab",
                Settings = new NewznabSettings()
                {
                    BaseUrl = "http://indexer.local/"
                }
            };

            _caps = ReadAllText("Files/Indexers/Newznab/newznab_caps.xml");
        }

        private void GivenCapsResponse(string caps)
        {
            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxied(It.IsAny<HttpRequest>(), It.IsAny<IndexerDefinition>()))
                .Returns<HttpRequest, IndexerDefinition>((r, d) => new HttpResponse(r, new HttpHeader(), new CookieCollection(), caps));
        }

        [Test]
        public void should_parse_curupira_capabilities()
        {
            GivenCapsResponse(ReadAllText("Files/Indexers/Newznab/newznab_curupira_caps.xml"));

            var caps = Subject.GetCapabilities(_settings, _definition);

            caps.LimitsDefault.Should().Be(50);
            caps.LimitsMax.Should().Be(100);
            caps.SearchParams.Should().BeEquivalentTo(new[] { SearchParam.Q });
            caps.MovieSearchParams.Should().BeEquivalentTo(new[] { MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId });
            caps.TvSearchParams.Should().BeEquivalentTo(new[] { TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.RId, TvSearchParam.TvdbId, TvSearchParam.TmdbId, TvSearchParam.ImdbId });
            caps.MusicSearchParams.Should().BeEmpty();
            caps.BookSearchParams.Should().BeEmpty();
            caps.Categories.GetTrackerCategories().Select(int.Parse).Should().BeEquivalentTo(new[]
            {
                1000, 1050, 1090, 1110, 1130, 1180,
                2000, 2010, 2020, 2030, 2040, 2045, 2050, 2060,
                3000, 3010, 3020, 3030, 3040,
                4000, 4030, 4040, 4050,
                5000, 5010, 5020, 5030, 5040, 5045, 5050, 5070, 5080,
                7000, 7010, 7020, 7030,
                8000, 8010
            });
            caps.Categories.MapTrackerCatToNewznab("2040").Should().Contain(c => c.Id == 2040);
            caps.Categories.MapTrackerCatToNewznab("4050").Should().Contain(c => c.Id == 4050);
            caps.Categories.MapTrackerCatToNewznab("1180").Should().Contain(c => c.Id == 1180);
            caps.Categories.MapTrackerCatToNewznab("5070").Should().Contain(c => c.Id == 5070);
            caps.Categories.MapTrackerCatToNewznab("7020").Should().Contain(c => c.Id == 7020);
        }

        [Test]
        public void should_not_request_same_caps_twice()
        {
            GivenCapsResponse(_caps);

            Subject.GetCapabilities(_settings, _definition);
            Subject.GetCapabilities(_settings, _definition);

            Mocker.GetMock<IIndexerHttpClient>()
                .Verify(o => o.ExecuteProxied(It.IsAny<HttpRequest>(), It.IsAny<IndexerDefinition>()), Times.Once());
        }

        [Test]
        public void should_report_pagesize()
        {
            GivenCapsResponse(_caps);

            var caps = Subject.GetCapabilities(_settings, _definition);

            caps.LimitsDefault.Value.Should().Be(25);
            caps.LimitsMax.Value.Should().Be(60);
        }

        [Test]
        public void should_map_different_categories()
        {
            GivenCapsResponse(_caps);

            var caps = Subject.GetCapabilities(_settings, _definition);

            var bookCats = caps.Categories.MapTorznabCapsToTrackers(new int[] { NewznabStandardCategory.Books.Id });

            bookCats.Count.Should().Be(2);
            bookCats.Should().Contain("8000");
        }

        [TestCase(5070)]
        [TestCase(2020)]
        public void should_find_sub_categories_as_main_categories(int category)
        {
            GivenCapsResponse(ReadAllText("Files/Indexers/Torznab/torznab_animetosho_caps.xml"));

            var caps = Subject.GetCapabilities(_settings, _definition);

            var indexerCategories = caps.Categories.MapTrackerCatToNewznab(category.ToString());

            indexerCategories.Count.Should().Be(2);
            indexerCategories.First().Id.Should().Be(category);
        }

        [Test]
        public void should_map_by_name_when_available()
        {
            GivenCapsResponse(_caps);

            var caps = Subject.GetCapabilities(_settings, _definition);

            var bookCats = caps.Categories.MapTrackerCatToNewznab("5999");

            bookCats.Count.Should().Be(2);
            bookCats.First().Id.Should().Be(5050);
        }

        [Test]
        public void should_use_default_pagesize_if_missing()
        {
            GivenCapsResponse(_caps.Replace("<limits", "<abclimits"));

            var caps = Subject.GetCapabilities(_settings, _definition);

            caps.LimitsDefault.Value.Should().Be(100);
            caps.LimitsMax.Value.Should().Be(100);
        }

        [Test]
        public void should_throw_if_failed_to_get()
        {
            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxied(It.IsAny<HttpRequest>(), It.IsAny<IndexerDefinition>()))
                .Throws<Exception>();

            Assert.Throws<Exception>(() => Subject.GetCapabilities(_settings, _definition));
        }

        [Test]
        public void should_throw_if_xml_invalid()
        {
            GivenCapsResponse(_caps.Replace("<limits", "<>"));

            Assert.Throws<XmlException>(() => Subject.GetCapabilities(_settings, _definition));
        }

        [Test]
        public void should_not_throw_on_xml_data_unexpected()
        {
            GivenCapsResponse(_caps.Replace("5030", "asdf"));

            var result = Subject.GetCapabilities(_settings, _definition);

            result.Should().NotBeNull();
        }
    }
}
