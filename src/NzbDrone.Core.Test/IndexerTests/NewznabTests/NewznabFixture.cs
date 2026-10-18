using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.NewznabTests
{
    [TestFixture]
    public class NewznabFixture : CoreTest<Newznab>
    {
        private IndexerCapabilities _caps;

        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition()
            {
                Id = 5,
                Name = "Newznab",
                Settings = new NewznabSettings()
                {
                    BaseUrl = "http://indexer.local/"
                }
            };

            _caps = new IndexerCapabilities();

            _caps.Categories.AddCategoryMapping(2000, NewznabStandardCategory.Movies, "Movies");
            _caps.Categories.AddCategoryMapping(5000, NewznabStandardCategory.TV, "TV");

            Mocker.GetMock<INewznabCapabilitiesProvider>()
                .Setup(v => v.GetCapabilities(It.IsAny<NewznabSettings>(), It.IsAny<IndexerDefinition>()))
                .Returns(_caps);
        }

        [Test]
        public async Task should_parse_recent_feed_from_newznab_nzb_su()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/Newznab/newznab_nzb_su.xml");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get), Subject.Definition))
                .ReturnsAsync((HttpRequest r, IndexerDefinition _) => new HttpResponse(r, new HttpHeader(), new CookieCollection(), recentFeed));

            var releases = (await Subject.Fetch(new MovieSearchCriteria { Categories = new int[] { 2000 }, Limit = 100, Offset = 0 })).Releases;

            releases.Should().HaveCount(100);

            var releaseInfo = releases.First();

            releaseInfo.Title.Should().Be("White.Collar.S03E05.720p.HDTV.X264-DIMENSION");
            releaseInfo.DownloadProtocol.Should().Be(DownloadProtocol.Usenet);
            releaseInfo.DownloadUrl.Should().Be("http://nzb.su/getnzb/24967ef4c2e26296c65d3bbfa97aa8fe.nzb&i=37292&r=xxx");
            releaseInfo.InfoUrl.Should().Be("http://nzb.su/details/24967ef4c2e26296c65d3bbfa97aa8fe");
            releaseInfo.CommentUrl.Should().Be("http://nzb.su/details/24967ef4c2e26296c65d3bbfa97aa8fe#comments");
            releaseInfo.IndexerId.Should().Be(Subject.Definition.Id);
            releaseInfo.Indexer.Should().Be(Subject.Definition.Name);
            releaseInfo.PublishDate.Should().Be(DateTime.Parse("2012/02/27 16:09:39"));
            releaseInfo.Size.Should().Be(1183105773);
        }

        [Test]
        public void should_preserve_preset_metadata_when_building_the_catalog()
        {
            Subject.Definition.Settings = new NewznabSettings { BaseUrl = "https://curupira.cc" };
            var factory = Mocker.Resolve<IndexerFactory>();
            var definitions = Subject.DefaultDefinitions.Cast<IndexerDefinition>().ToList();

            foreach (var definition in definitions)
            {
                factory.SetProviderCharacteristics(Subject, definition);
            }

            var curupira = definitions.Single(d => d.Name == "Curupira");
            curupira.Language.Should().Be("pt-BR");
            curupira.Description.Should().Be("Curupira is a Brazilian private Usenet indexer");

            definitions.Where(d => d.Name != "Curupira").Should().OnlyContain(d =>
                d.Language == "en-US" && d.Description == "Newznab is an API search specification for Usenet");
        }

        [TestCase("https://curupira.cc", "/api", "pt-BR")]
        [TestCase("https://CURUPIRA.cc:443/", "/api/", "pt-BR")]
        [TestCase("https://curupira.cc", "/API", "en-US")]
        [TestCase("https://curupira.cc", "/other/api", "en-US")]
        [TestCase("https://curupira.cc:8443", "/api", "en-US")]
        [TestCase("https://curupira.cc/other", "/api", "en-US")]
        [TestCase("https://curupira.cc.example.test", "/api", "en-US")]
        [TestCase("https://example.test/curupira.cc", "/api", "en-US")]
        [TestCase("https://api.dognzb.cr", "/api", "en-US")]
        [TestCase("", "/api", "en-US")]
        [TestCase(null, "/api", "en-US")]
        public void should_resolve_metadata_from_the_configured_endpoint(string baseUrl, string apiPath, string language)
        {
            var definition = (IndexerDefinition)Subject.Definition;
            definition.Name = "My renamed indexer";
            definition.Settings = new NewznabSettings { BaseUrl = baseUrl, ApiPath = apiPath };

            Mocker.Resolve<IndexerFactory>().SetProviderCharacteristics(Subject, definition);

            definition.Language.Should().Be(language);
            definition.Description.Should().Be(language == "pt-BR"
                ? "Curupira is a Brazilian private Usenet indexer"
                : "Newznab is an API search specification for Usenet");
        }

        [Test]
        public void should_use_generic_metadata_without_a_definition()
        {
            Subject.Definition = null;

            Subject.Language.Should().Be("en-US");
            Subject.Description.Should().Be("Newznab is an API search specification for Usenet");
        }

        [Test]
        public void should_use_best_pagesize_reported_by_caps()
        {
            _caps.LimitsMax = 30;
            _caps.LimitsDefault = 25;

            Subject.PageSize.Should().Be(30);
        }

        [Test]
        public void should_not_use_pagesize_over_100_even_if_reported_in_caps()
        {
            _caps.LimitsMax = 250;
            _caps.LimitsDefault = 25;

            Subject.PageSize.Should().Be(100);
        }

        [Test]
        public void should_use_pagesize_over_100_if_reported_in_caps()
        {
            _caps.LimitsMax = 250;
            _caps.LimitsDefault = 200;

            Subject.PageSize.Should().Be(200);
        }
    }
}
