using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers.FileList;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.FileListTests
{
    public class FileListRequestGeneratorFixture : CoreTest<FileListRequestGenerator>
    {
        private MovieSearchCriteria _movieSearchCriteria;

        [SetUp]
        public void Setup()
        {
            Subject.Settings = new FileListSettings()
            {
                BaseUrl = "http://127.0.0.1:1234/",
                Passkey = "abcd",
                Username = "somename"
            };

            _movieSearchCriteria = new MovieSearchCriteria
            {
                SearchTerm = "Star Wars",
                Categories = new int[] { 2000 }
            };
        }

        private void MovieWithoutIMDB()
        {
            _movieSearchCriteria.ImdbId = null;
        }

        [Test]
        public void should_use_categories_for_feed()
        {
            var results = Subject.GetSearchRequests(new MovieSearchCriteria { Categories = new int[] { 1, 2 } });

            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            page.Url.Query.Should().Contain("&category=1,2&");
        }

        [Test]
        public void should_not_search_by_imdbid_if_not_supported()
        {
            _movieSearchCriteria.ImdbId = "tt0076759";
            var results = Subject.GetSearchRequests(_movieSearchCriteria);

            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            page.Url.Query.Should().Contain("type=imdb");
            page.Url.Query.Should().Contain("query=tt0076759");
        }

        [Test]
        public void should_search_by_name_and_year_if_missing_imdbid()
        {
            MovieWithoutIMDB();

            var results = Subject.GetSearchRequests(_movieSearchCriteria);

            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            page.Url.Query.Should().Contain("type=name");
            page.Url.Query.Should().Contain("query=Star Wars");
        }
    }
}
