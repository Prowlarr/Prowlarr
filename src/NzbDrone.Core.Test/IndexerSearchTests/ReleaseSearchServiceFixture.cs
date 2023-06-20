using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    public class ReleaseSearchServiceFixture : CoreTest<ReleaseSearchService>
    {
        private Mock<IIndexer> _mockIndexer;

        [SetUp]
        public void SetUp()
        {
            _mockIndexer = Mocker.GetMock<IIndexer>();
            _mockIndexer.SetupGet(s => s.Definition).Returns(new IndexerDefinition { Id = 1 });
            _mockIndexer.SetupGet(s => s.SupportsSearch).Returns(true);

            Mocker.GetMock<IIndexerFactory>()
                .Setup(s => s.Enabled(It.IsAny<bool>()))
                .Returns(new List<IIndexer> { _mockIndexer.Object });
        }

        private List<SearchCriteriaBase> WatchForSearchCriteria()
        {
            var result = new List<SearchCriteriaBase>();

            _mockIndexer.Setup(v => v.Fetch(It.IsAny<MovieSearchCriteria>()))
                .Callback<MovieSearchCriteria>(s => result.Add(s))
                .Returns(Task.FromResult(new IndexerPageableQueryResult()));

            _mockIndexer.Setup(v => v.Fetch(It.IsAny<TvSearchCriteria>()))
                .Callback<TvSearchCriteria>(s => result.Add(s))
                .Returns(Task.FromResult(new IndexerPageableQueryResult()));

            return result;
        }

        [TestCase("tt0183790", "0183790")]
        [TestCase("0183790", "0183790")]
        [TestCase("183790", "0183790")]
        [TestCase("tt10001870", "10001870")]
        [TestCase("10001870", "10001870")]
        public void should_normalize_imdbid_movie_search_criteria(string input, string expected)
        {
            var allCriteria = WatchForSearchCriteria();

            var request = new NewznabRequest
            {
                t = "movie",
                imdbid = input
            };

            Subject.Search(request, new List<int> { 1 }, false);

            var criteria = allCriteria.OfType<MovieSearchCriteria>().ToList();

            criteria.Count.Should().Be(1);
            criteria[0].ImdbId.Should().Be(expected);
        }

        [TestCase("tt0183790", "0183790")]
        [TestCase("0183790", "0183790")]
        [TestCase("183790", "0183790")]
        [TestCase("tt10001870", "10001870")]
        [TestCase("10001870", "10001870")]
        public void should_normalize_imdbid_tv_search_criteria(string input, string expected)
        {
            var allCriteria = WatchForSearchCriteria();

            var request = new NewznabRequest
            {
                t = "tvsearch",
                imdbid = input
            };

            Subject.Search(request, new List<int> { 1 }, false);

            var criteria = allCriteria.OfType<TvSearchCriteria>().ToList();

            criteria.Count.Should().Be(1);
            criteria[0].ImdbId.Should().Be(expected);
        }
    }
}
