using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.HealthCheck.Checks;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.HealthCheck.Checks
{
    [TestFixture]
    public class IndexerLongTermStatusCheckFixture : CoreTest<IndexerLongTermStatusCheck>
    {
        private List<IIndexer> _indexers = new List<IIndexer>();
        private List<IndexerStatus> _blockedIndexers = new List<IndexerStatus>();

        [SetUp]
        public void SetUp()
        {
            _indexers = new List<IIndexer>();
            _blockedIndexers = new List<IndexerStatus>();

            Mocker.GetMock<IIndexerFactory>()
                  .Setup(v => v.GetAvailableProviders())
                  .Returns(_indexers);

            Mocker.GetMock<IIndexerStatusService>()
                   .Setup(v => v.GetBlockedProviders())
                   .Returns(_blockedIndexers);

            Mocker.GetMock<ILocalizationService>()
                   .Setup(v => v.GetLocalizedString(It.IsAny<string>()))
                   .Returns("Error");
        }

        private Mock<IIndexer> GivenIndexer(int id, double backoffHours, double failureHours, params int[] tags)
        {
            var mockIndexer = new Mock<IIndexer>();
            mockIndexer.SetupGet(s => s.Definition).Returns(new IndexerDefinition { Id = id, Tags = new HashSet<int>(tags) });
            mockIndexer.SetupGet(s => s.SupportsSearch).Returns(true);

            _indexers.Add(mockIndexer.Object);

            if (backoffHours != 0.0)
            {
                _blockedIndexers.Add(new IndexerStatus
                    {
                        ProviderId = id,
                        InitialFailure = DateTime.UtcNow.AddHours(-failureHours),
                        MostRecentFailure = DateTime.UtcNow.AddHours(-0.1),
                        EscalationLevel = 5,
                        DisabledTill = DateTime.UtcNow.AddHours(backoffHours)
                    });
            }

            return mockIndexer;
        }

        [Test]
        public void should_not_return_error_when_no_indexers()
        {
            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_return_warning_if_indexer_unavailable()
        {
            GivenIndexer(1, 10.0, 24.0);
            GivenIndexer(2, 0.0, 0.0);

            Subject.Check().ShouldBeWarning();
        }

        [Test]
        public void should_return_error_if_all_indexers_unavailable()
        {
            GivenIndexer(1, 10.0, 24.0);

            Subject.Check().ShouldBeError();
        }

        [Test]
        public void should_return_warning_if_few_indexers_unavailable()
        {
            GivenIndexer(1, 10.0, 24.0);
            GivenIndexer(2, 10.0, 24.0);
            GivenIndexer(3, 0.0, 0.0);

            Subject.Check().ShouldBeWarning();
        }

        [Test]
        public void should_capture_tags_of_unavailable_indexers_only()
        {
            GivenIndexer(1, 48.0, 48.0, 1);
            GivenIndexer(2, 0.0, 0.0, 2);

            Subject.Check().IndexerTags.Should().BeEquivalentTo(new[] { 1 });
        }
    }
}
