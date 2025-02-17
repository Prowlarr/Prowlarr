using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Definitions.HDBits;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.IndexerTests.HDBitsTests
{
    [TestFixture]
    public class HDBitsFixture : CoreTest<HDBits>
    {
        private MovieSearchCriteria _movieSearchCriteria;

        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition
            {
                Name = "HdBits",
                Settings = new HDBitsSettings { ApiKey = "fakekey" }
            };

            _movieSearchCriteria = new MovieSearchCriteria
            {
                Categories = new[] { 2000, 2010 },
                ImdbId = "0076759"
            };
        }

        [TestCase("Files/Indexers/HdBits/RecentFeedLongIDs.json")]
        [TestCase("Files/Indexers/HdBits/RecentFeedStringIDs.json")]
        public async Task should_parse_recent_feed_from_HDBits(string fileName)
        {
            var responseJson = ReadAllText(fileName);

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Post), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) => Task.FromResult(new HttpResponse(r, new HttpHeader(), new CookieCollection(), responseJson)));

            var torrents = (await Subject.Fetch(_movieSearchCriteria)).Releases;

            torrents.Should().HaveCount(2);
            torrents.First().Should().BeOfType<TorrentInfo>();

            var first = torrents.First() as TorrentInfo;

            first.Guid.Should().Be("HDBits-257142");
            first.Title.Should().Be("Supernatural.S10E17.1080p.WEB-DL.DD5.1.H.264-ECI");
            first.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            first.DownloadUrl.Should().Be("https://hdbits.org/download.php?id=257142&passkey=fakekey");
            first.InfoUrl.Should().Be("https://hdbits.org/details.php?id=257142");
            first.PublishDate.Should().Be(DateTime.Parse("2015-04-04T20:30:46+0000").ToUniversalTime());
            first.Size.Should().Be(1718009717);
            first.InfoHash.Should().Be("EABC50AEF9F53CEDED84ADF14144D3368E586F3A");
            first.MagnetUrl.Should().BeNullOrEmpty();
            first.Peers.Should().Be(47);
            first.Seeders.Should().Be(46);
        }

        [Test]
        public async Task should_warn_on_wrong_passkey()
        {
            var responseJson = new { status = 5, message = "Invalid authentication credentials" }.ToJson();

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.IsAny<HttpRequest>(), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) => Task.FromResult(new HttpResponse(r, new HttpHeader(), new CookieCollection(), Encoding.UTF8.GetBytes(responseJson))));

            var torrents = (await Subject.Fetch(_movieSearchCriteria)).Releases;

            torrents.Should().BeEmpty();

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}
