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
        public void should_include_curupira_preset()
        {
            var definition = Subject.DefaultDefinitions.Cast<IndexerDefinition>().Single(d => d.Name == "Curupira");
            var settings = (NewznabSettings)definition.Settings;

            settings.BaseUrl.Should().Be("https://curupira.cc");
            settings.ApiPath.Should().Be("/api");
            definition.Language.Should().Be("pt-BR");
            definition.Description.Should().Be("Curupira is a BRAZILIAN Private Usenet Indexer for MOVIES / TV / ANIME / GENERAL");
            definition.Capabilities.Categories.GetTorznabCategoryTree().Select(c => c.Id)
                .Should().BeEquivalentTo(new[] { 1000, 2000, 3000, 4000, 5000, 7000, 8000 });
            definition.Protocol.Should().Be(DownloadProtocol.Usenet);
            definition.Privacy.Should().Be(IndexerPrivacy.Private);
            definition.SupportsRedirect.Should().BeTrue();
            definition.Redirect.Should().BeTrue();
            definition.Implementation.Should().Be("Newznab");

            var unchanged = Subject.DefaultDefinitions.Cast<IndexerDefinition>().Single(d => d.Name == "DOGnzb");
            unchanged.Language.Should().BeNull();
            unchanged.Description.Should().BeNull();
        }

        [Test]
        public async Task should_parse_curupira_feed()
        {
            var feed = ReadAllText("Files/Indexers/Newznab/newznab_curupira.xml");
            _caps.Categories.AddCategoryMapping(5040, NewznabStandardCategory.TVHD, "TV/HD");
            _caps.Categories.AddCategoryMapping(2040, NewznabStandardCategory.MoviesHD, "Movies/HD");
            _caps.Categories.AddCategoryMapping(5070, NewznabStandardCategory.TVAnime, "TV/Anime");
            _caps.Categories.AddCategoryMapping(8010, NewznabStandardCategory.OtherMisc, "Other/Misc");
            _caps.Categories.AddCategoryMapping(7020, NewznabStandardCategory.BooksEBook, "Books/Ebook");

            Mocker.GetMock<IIndexerHttpClient>()
                .Setup(o => o.ExecuteProxiedAsync(It.IsAny<HttpRequest>(), Subject.Definition))
                .ReturnsAsync((HttpRequest r, IndexerDefinition _) => new HttpResponse(r, new HttpHeader(), new CookieCollection(), feed));

            var releases = (await Subject.Fetch(new BasicSearchCriteria { SearchType = "search", Limit = 100, Offset = 0 })).Releases;

            releases.Should().HaveCount(5);

            var release = releases.First();
            release.Title.Should().Be("Duna.Parte.Dois.2024.2160p.AMZN.WEB-DL.DDP5.1.HDR.H.265-GRP");
            release.DownloadProtocol.Should().Be(DownloadProtocol.Usenet);
            release.DownloadUrl.Should().Be("https://curupira.example.test/api?t=get&id=01960000-0000-7000-8000-000000000101&apikey=test-api-key-0000000000000000");
            release.InfoUrl.Should().Be("https://curupira.example.test/releases/01960000-0000-7000-8000-000000000101");
            release.Size.Should().Be(42949672960L);
            release.Grabs.Should().Be(128);
            release.Files.Should().Be(30);
            release.ImdbId.Should().Be(1375666);
            release.TmdbId.Should().Be(603);
            release.TvdbId.Should().Be(360893);
            release.PublishDate.Should().Be(new DateTime(2024, 1, 14, 8, 0, 0, DateTimeKind.Utc));
            releases.ElementAt(1).PublishDate.Should().Be(new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc));
            release.Languages.Should().Equal("Portuguese (Brazil)", "English");
            release.Categories.Should().Contain(c => c.Id == 5040);
            releases.ElementAt(1).Languages.Should().Equal("Portuguese");
            releases.ElementAt(2).Categories.Should().Contain(c => c.Id == 5070);
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
