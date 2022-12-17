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
            Mocker.GetMock<INewznabCapabilitiesProvider>()
                .Setup(v => v.GetCapabilities(It.IsAny<GenericNewznabSettings>(), It.IsAny<IndexerDefinition>()))
                .Returns(_caps);
        }

        [Test]
        public async Task should_parse_recent_feed_from_newznab_nzb_su()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/Newznab/newznab_nzb_su.xml");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) => Task.FromResult(new HttpResponse(r, new HttpHeader(), new CookieCollection(), recentFeed)));

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
        public void should_use_pagesize_reported_by_caps()
        {
            _caps.LimitsMax = 30;
            _caps.LimitsDefault = 25;

            Subject.PageSize.Should().Be(25);
        }
    }
}
