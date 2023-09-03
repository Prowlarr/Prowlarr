using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.History;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerStats;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerStatsTests
{
    public class IndexerStatisticsServiceFixture : CoreTest<IndexerStatisticsService>
    {
        private IndexerDefinition _indexer;

        [SetUp]
        public void Setup()
        {
            _indexer = Builder<IndexerDefinition>.CreateNew().With(x => x.Id = 5).Build();

            Mocker.GetMock<IIndexerFactory>()
                  .Setup(o => o.All())
                  .Returns(new List<IndexerDefinition> { _indexer });
        }

        [Test]
        public void should_pull_stats_if_all_events_are_failures()
        {
            var history = new List<History.History>
            {
                new History.History
                {
                    Date = DateTime.UtcNow.AddHours(-1),
                    EventType = HistoryEventType.IndexerRss,
                    Successful = false,
                    Id = 8,
                    IndexerId = 5,
                    Data = new Dictionary<string, string> { { "source", "prowlarr" } }
                }
            };

            Mocker.GetMock<IHistoryService>()
                .Setup(o => o.Between(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns<DateTime, DateTime>((s, f) => history);

            var statistics = Subject.IndexerStatistics(DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow, new List<int> { 5 });

            statistics.IndexerStatistics.Count.Should().Be(1);
            statistics.IndexerStatistics.First().AverageResponseTime.Should().Be(0);
        }
    }
}
