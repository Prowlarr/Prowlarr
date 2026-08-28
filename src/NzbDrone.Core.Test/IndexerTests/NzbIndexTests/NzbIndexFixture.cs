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
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.NzbIndexTests
{
    [TestFixture]
    public class NzbIndexFixture : CoreTest<NzbIndex>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition
            {
                Name = "NzbIndex",
                Settings = new NzbIndexSettings
                {
                    BaseUrl = "https://nzbindex.com/"
                }
            };
        }

        [Test]
        public async Task should_parse_search_results()
        {
            var responseJson = ReadAllText(@"Files/Indexers/NzbIndex/search.json");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get), Subject.Definition))
                .ReturnsAsync((HttpRequest r, IndexerDefinition _) => new HttpResponse(r, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), responseJson));

            var releases = (await Subject.Fetch(new BasicSearchCriteria { SearchTerm = "test" })).Releases;

            releases.Should().HaveCount(2);

            var first = releases.First();
            first.Title.Should().Be("Ubuntu Gubuntu 11.10 Unity Edition (64bit)");
            first.DownloadUrl.Should().Be("https://nzbindex.com/api/download/a1b2c3d4-e5f6-7890-abcd-ef1234567890.nzb");
            first.InfoUrl.Should().Be("https://nzbindex.com/collection/a1b2c3d4-e5f6-7890-abcd-ef1234567890");
            first.PublishDate.Should().Be(DateTime.Parse("2023-11-14T22:13:20Z").ToUniversalTime());
            first.Size.Should().Be(8589934592);
            first.Files.Should().Be(42);
        }

        [Test]
        public void should_build_search_url_without_api_key()
        {
            var searchCriteria = new BasicSearchCriteria { SearchTerm = "test", Limit = 25, Offset = 0 };
            var requests = Subject.GetRequestGenerator().GetSearchRequests(searchCriteria);
            var pages = requests.GetAllTiers().ToList();

            pages.Should().HaveCount(1);
            pages.First().First().Url.FullUri.Should().Contain("/api/search");
            pages.First().First().Url.FullUri.Should().NotContain("key=");
        }

        [Test]
        public void should_build_search_url_with_api_key()
        {
            Subject.Definition.Settings = new NzbIndexSettings
            {
                BaseUrl = "https://nzbindex.com/",
                ApiKey = "myapikey"
            };

            var searchCriteria = new BasicSearchCriteria { SearchTerm = "test", Limit = 25, Offset = 0 };
            var requests = Subject.GetRequestGenerator().GetSearchRequests(searchCriteria);
            var pages = requests.GetAllTiers().ToList();

            pages.First().First().Url.FullUri.Should().Contain("key=myapikey");
        }
    }
}
