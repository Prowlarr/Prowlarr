using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.History;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests
{
    public class IndexerLimitServiceFixture : CoreTest<IndexerLimitService>
    {
        private IndexerDefinition CreateIndexerWithLimitsUnit(IndexerLimitsUnit unit, int id = 1)
        {
            return new IndexerDefinition
            {
                Id = id,
                Settings = new TestIndexerSettings
                {
                    BaseSettings = new IndexerBaseSettings
                    {
                        LimitsUnit = (int)unit
                    }
                }
            };
        }

        [Test]
        public void should_return_1440_for_day_unit()
        {
            var indexer = CreateIndexerWithLimitsUnit(IndexerLimitsUnit.Day);

            Subject.CalculateIntervalLimitMinutes(indexer).Should().Be(1440);
        }

        [Test]
        public void should_return_60_for_hour_unit()
        {
            var indexer = CreateIndexerWithLimitsUnit(IndexerLimitsUnit.Hour);

            Subject.CalculateIntervalLimitMinutes(indexer).Should().Be(60);
        }

        [Test]
        public void should_return_1_for_minute_unit()
        {
            var indexer = CreateIndexerWithLimitsUnit(IndexerLimitsUnit.Minute);

            Subject.CalculateIntervalLimitMinutes(indexer).Should().Be(1);
        }

        [Test]
        public void should_return_1440_for_default_when_id_is_zero()
        {
            var indexer = CreateIndexerWithLimitsUnit(IndexerLimitsUnit.Hour, id: 0);

            Subject.CalculateIntervalLimitMinutes(indexer).Should().Be(1440);
        }

        [Test]
        public void should_format_day_interval()
        {
            IndexerLimitService.FormatIntervalLimit(1440).Should().Be("1 day");
        }

        [Test]
        public void should_format_hour_interval()
        {
            IndexerLimitService.FormatIntervalLimit(60).Should().Be("1 hour");
        }

        [Test]
        public void should_format_minute_interval()
        {
            IndexerLimitService.FormatIntervalLimit(1).Should().Be("1 minute");
        }

        [Test]
        public void should_format_minutes_interval()
        {
            IndexerLimitService.FormatIntervalLimit(5).Should().Be("5 minute(s)");
        }

        [Test]
        public void should_return_true_when_at_query_limit()
        {
            var indexer = CreateIndexerWithLimitsUnit(IndexerLimitsUnit.Minute);
            ((IIndexerSettings)indexer.Settings).BaseSettings.QueryLimit = 10;

            Mocker.GetMock<IHistoryService>()
                .Setup(s => s.CountSince(indexer.Id, It.IsAny<DateTime>(), It.Is<List<HistoryEventType>>(l => l.Contains(HistoryEventType.IndexerQuery))))
                .Returns(10);

            Subject.AtQueryLimit(indexer).Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_under_query_limit()
        {
            var indexer = CreateIndexerWithLimitsUnit(IndexerLimitsUnit.Minute);
            ((IIndexerSettings)indexer.Settings).BaseSettings.QueryLimit = 10;

            Mocker.GetMock<IHistoryService>()
                .Setup(s => s.CountSince(indexer.Id, It.IsAny<DateTime>(), It.Is<List<HistoryEventType>>(l => l.Contains(HistoryEventType.IndexerQuery))))
                .Returns(9);

            Subject.AtQueryLimit(indexer).Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_at_download_limit()
        {
            var indexer = CreateIndexerWithLimitsUnit(IndexerLimitsUnit.Hour);
            ((IIndexerSettings)indexer.Settings).BaseSettings.GrabLimit = 5;

            Mocker.GetMock<IHistoryService>()
                .Setup(s => s.CountSince(indexer.Id, It.IsAny<DateTime>(), It.Is<List<HistoryEventType>>(l => l.Contains(HistoryEventType.ReleaseGrabbed))))
                .Returns(5);

            Subject.AtDownloadLimit(indexer).Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_under_download_limit()
        {
            var indexer = CreateIndexerWithLimitsUnit(IndexerLimitsUnit.Hour);
            ((IIndexerSettings)indexer.Settings).BaseSettings.GrabLimit = 5;

            Mocker.GetMock<IHistoryService>()
                .Setup(s => s.CountSince(indexer.Id, It.IsAny<DateTime>(), It.Is<List<HistoryEventType>>(l => l.Contains(HistoryEventType.ReleaseGrabbed))))
                .Returns(4);

            Subject.AtDownloadLimit(indexer).Should().BeFalse();
        }

        [Test]
        public void should_use_correct_time_window_for_query_limit_minutes()
        {
            var indexer = CreateIndexerWithLimitsUnit(IndexerLimitsUnit.Minute);
            ((IIndexerSettings)indexer.Settings).BaseSettings.QueryLimit = 10;

            Subject.AtQueryLimit(indexer);

            Mocker.GetMock<IHistoryService>()
                .Verify(v => v.CountSince(indexer.Id, It.Is<DateTime>(d => d > DateTime.Now.AddMinutes(-1).AddSeconds(-5) && d < DateTime.Now.AddMinutes(-1).AddSeconds(5)), It.IsAny<List<HistoryEventType>>()), Times.Once);
        }
    }
}
