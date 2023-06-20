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

        [Test]
        public void should_find_sub_categories_as_main_categories()
        {
            GivenCapsResponse(ReadAllText("Files/Indexers/Torznab/torznab_animetosho_caps.xml"));

            var caps = Subject.GetCapabilities(_settings, _definition);

            var bookCats = caps.Categories.MapTrackerCatToNewznab("5070");

            bookCats.Count.Should().Be(2);
            bookCats.First().Id.Should().Be(5070);
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
