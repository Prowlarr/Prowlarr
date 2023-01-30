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
using NzbDrone.Core.Indexers.Definitions.FileList;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.FileListTests
{
    [TestFixture]
    public class FileListFixture : CoreTest<FileList>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new IndexerDefinition
            {
                Name = "FileList",
                Settings = new FileListSettings
                {
                    BaseUrl = "https://filelist.io/",
                    Username = "someuser",
                    Passkey = "somepass"
                }
            };
        }

        [Test]
        public async Task should_parse_recent_feed_from_FileList()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/FileList/recentfeed.json");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.Is<HttpRequest>(v => v.Method == HttpMethod.Get), Subject.Definition))
                .Returns<HttpRequest, IndexerDefinition>((r, d) => Task.FromResult(new HttpResponse(r, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), recentFeed)));

            var releases = (await Subject.Fetch(new MovieSearchCriteria { Categories = new[] { 2000 } })).Releases;

            releases.Should().HaveCount(4);
            releases.First().Should().BeOfType<TorrentInfo>();

            var torrentInfo = releases.First() as TorrentInfo;

            torrentInfo.Title.Should().Be("Storming.Juno.2010.1080p.BluRay.x264-GUACAMOLE");
            torrentInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            torrentInfo.DownloadUrl.Should().Be("https://filelist.io/download.php?id=665873&passkey=somepass");
            torrentInfo.InfoUrl.Should().Be("https://filelist.io/details.php?id=665873");
            torrentInfo.CommentUrl.Should().BeNullOrEmpty();
            torrentInfo.Indexer.Should().Be(Subject.Definition.Name);
            torrentInfo.PublishDate.Should().Be(DateTime.Parse("2020-01-25 20:20:19"));
            torrentInfo.Size.Should().Be(8300512414);
            torrentInfo.InfoHash.Should().Be(null);
            torrentInfo.MagnetUrl.Should().Be(null);
            torrentInfo.Peers.Should().Be(2 + 12);
            torrentInfo.Seeders.Should().Be(12);

            releases.Any(t => t.IndexerFlags.Contains(IndexerFlag.Internal)).Should().Be(true);
        }
    }
}
