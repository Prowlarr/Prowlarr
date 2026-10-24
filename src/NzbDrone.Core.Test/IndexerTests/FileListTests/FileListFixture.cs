using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
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
                .ReturnsAsync((HttpRequest r, IndexerDefinition _) => new HttpResponse(r, new HttpHeader { { "Content-Type", "application/json" } }, new CookieCollection(), recentFeed));

            var releases = (await Subject.Fetch(new MovieSearchCriteria { Categories = [2000] })).Releases;

            releases.Should().HaveCount(4);
            releases.First().Should().BeOfType<TorrentInfo>();

            var torrentInfo = releases.First() as TorrentInfo;

            torrentInfo.Title.Should().Be("Storming.Juno.2010.1080p.BluRay.x264-GUACAMOLE");
            torrentInfo.DownloadProtocol.Should().Be(DownloadProtocol.Torrent);
            torrentInfo.DownloadUrl.Should().Be("https://filelist.io/download.php?id=665873&passkey=somepass");
            torrentInfo.InfoUrl.Should().Be("https://filelist.io/details.php?id=665873");
            torrentInfo.CommentUrl.Should().BeNullOrEmpty();
            torrentInfo.Indexer.Should().Be(Subject.Definition.Name);
            torrentInfo.PublishDate.Should().Be(DateTime.Parse("2020-01-25 19:20:19"));
            torrentInfo.Size.Should().Be(8300512414);
            torrentInfo.InfoHash.Should().Be(null);
            torrentInfo.MagnetUrl.Should().Be(null);
            torrentInfo.Peers.Should().Be(2 + 12);
            torrentInfo.Seeders.Should().Be(12);

            releases.Any(t => t.IndexerFlags.Contains(IndexerFlag.Internal)).Should().Be(true);
        }

        [Test]
        public async Task should_retry_on_httprequestexception_and_succeed()
        {
            var recentFeed = ReadAllText(@"Files/Indexers/FileList/recentfeed.json");
            var attempts = 0;

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.IsAny<HttpRequest>(), Subject.Definition))
                .Returns((HttpRequest r, IndexerDefinition _) =>
                {
                    attempts++;
                    if (attempts == 1)
                    {
                        throw new HttpRequestException("An error occurred while sending the request.",
                            new IOException("Unable to read data from the transport connection: Connection reset by peer.",
                                new SocketException((int)SocketError.ConnectionReset)));
                    }

                    return Task.FromResult(new HttpResponse(r,
                        new HttpHeader { { "Content-Type", "application/json" } },
                        new CookieCollection(),
                        recentFeed));
                });

            var releases = (await Subject.Fetch(new MovieSearchCriteria { Categories = [2000] })).Releases;

            attempts.Should().Be(2);
            releases.Should().HaveCount(4);
        }

        [Test]
        public async Task should_fail_after_max_retries_on_persistent_httprequestexception()
        {
            var attempts = 0;

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.IsAny<HttpRequest>(), Subject.Definition))
                .Returns(() =>
                {
                    attempts++;
                    throw new HttpRequestException("Connection reset by peer");
                });

            var releases = (await Subject.Fetch(new MovieSearchCriteria { Categories = [2000] })).Releases;

            // 1 initial attempt + 2 retries = 3 total attempts
            attempts.Should().Be(3);
            releases.Should().BeEmpty();
        }
    }
}
