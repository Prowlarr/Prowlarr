using System.Linq;
using System.Net;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.MyAnonamouseTests
{
    [TestFixture]
    public class MyAnonamouseParserFixture : CoreTest
    {
        [Test]
        public void should_parse_vip_flags_published_in_mixed_shapes()
        {
            var responseJson = ReadAllText("Files/Indexers/MyAnonamouse/mixed-vip-flags.json");

            var request = new HttpRequest("https://www.myanonamouse.net/tor/js/loadsearch2.php");
            var httpResponse = new HttpResponse(request, new HttpHeader { ContentType = HttpAccept.Json.Value }, new CookieCollection(), responseJson);

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxied(It.IsAny<HttpRequest>(), It.IsAny<IndexerDefinition>()))
                .Returns((HttpRequest r, IndexerDefinition _) => new HttpResponse(new HttpRequest("https://www.myanonamouse.net/jsonLoad.php"), new HttpHeader { ContentType = HttpAccept.Json.Value }, new CookieCollection(), "{\"classname\":\"User\"}"));

            var parser = new MyAnonamouseParser(
                new IndexerDefinition { Name = "MyAnonamouse" },
                new MyAnonamouseSettings { BaseUrl = "https://www.myanonamouse.net/" },
                new IndexerCapabilitiesCategories(),
                Mocker.GetMock<IIndexerHttpClient>().Object,
                Mocker.Resolve<NzbDrone.Common.Cache.ICacheManager>(),
                TestLogger)
            {
                CookiesUpdater = (_, _) => { }
            };

            var releases = parser.ParseResponse(new IndexerResponse(new IndexerRequest(request), httpResponse)).ToList();

            releases.Should().HaveCount(4);

            var stringFlags = releases[0] as TorrentInfo;
            stringFlags.Title.Should().Contain("Unexpected VIP flag value");
            stringFlags.DownloadVolumeFactor.Should().Be(1);

            var numericFlags = releases[1] as TorrentInfo;
            numericFlags.Title.Should().Contain("Numeric flags");
            numericFlags.DownloadVolumeFactor.Should().Be(0);

            var plainFlags = releases[2] as TorrentInfo;
            plainFlags.Title.Should().Contain("Plain flags").And.Contain("[VIP]");
            plainFlags.DownloadVolumeFactor.Should().Be(0);

            var emptyFlags = releases[3] as TorrentInfo;
            emptyFlags.Title.Should().Contain("Empty string flags").And.NotContain("[VIP]");
            emptyFlags.DownloadVolumeFactor.Should().Be(0);
        }
    }
}
