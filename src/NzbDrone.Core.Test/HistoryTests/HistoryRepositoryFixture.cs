using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.History;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.HistoryTests
{
    [TestFixture]
    public class HistoryRepositoryFixture : DbTest<HistoryRepository, History.History>
    {
        private DateTime _now;

        [SetUp]
        public void Setup()
        {
            _now = DateTime.UtcNow;
        }

        private void GivenHistory(int indexerId, HistoryEventType eventType, int hoursAgo, string downloadId = null)
        {
            Db.Insert(new History.History
            {
                IndexerId = indexerId,
                EventType = eventType,
                Date = _now.AddHours(-hoursAgo),
                DownloadId = downloadId
            });
        }

        [Test]
        public void get_by_indexer_id_should_filter_by_event_type_and_return_newest_first()
        {
            GivenHistory(1, HistoryEventType.IndexerQuery, 3);
            GivenHistory(1, HistoryEventType.IndexerQuery, 1);
            GivenHistory(1, HistoryEventType.IndexerQuery, 2);
            GivenHistory(1, HistoryEventType.ReleaseGrabbed, 0);
            GivenHistory(2, HistoryEventType.IndexerQuery, 0);

            var result = Subject.GetByIndexerId(1, HistoryEventType.IndexerQuery, 2);

            result.Should().HaveCount(2);
            result.Should().OnlyContain(h => h.IndexerId == 1 && h.EventType == HistoryEventType.IndexerQuery);
            result.Select(h => h.Date).Should().BeInDescendingOrder();
            result.First().Date.Should().BeCloseTo(_now.AddHours(-1), TimeSpan.FromSeconds(1));
        }

        [Test]
        public void get_by_indexer_id_should_return_all_without_limit()
        {
            GivenHistory(1, HistoryEventType.IndexerQuery, 2);
            GivenHistory(1, HistoryEventType.ReleaseGrabbed, 1);

            Subject.GetByIndexerId(1, null, null).Should().HaveCount(2);
        }

        [Test]
        public void between_should_only_return_requested_indexers_in_range()
        {
            GivenHistory(1, HistoryEventType.IndexerQuery, 1);
            GivenHistory(2, HistoryEventType.IndexerQuery, 1);
            GivenHistory(3, HistoryEventType.IndexerQuery, 1);
            GivenHistory(1, HistoryEventType.IndexerQuery, 48);

            var result = Subject.Between(_now.AddHours(-24), _now, new List<int> { 1, 2 });

            result.Should().HaveCount(2);
            result.Select(h => h.IndexerId).Should().BeEquivalentTo(new[] { 1, 2 });
        }

        [Test]
        public void between_should_return_nothing_for_empty_indexer_list()
        {
            GivenHistory(1, HistoryEventType.IndexerQuery, 1);

            Subject.Between(_now.AddHours(-24), _now, new List<int>()).Should().BeEmpty();
        }

        [Test]
        public void find_first_for_indexer_since_should_return_oldest_of_newest_events_within_limit()
        {
            GivenHistory(1, HistoryEventType.IndexerQuery, 4);
            GivenHistory(1, HistoryEventType.IndexerQuery, 3);
            GivenHistory(1, HistoryEventType.IndexerQuery, 2);
            GivenHistory(1, HistoryEventType.IndexerQuery, 1);
            GivenHistory(1, HistoryEventType.ReleaseGrabbed, 5);

            var result = Subject.FindFirstForIndexerSince(1, _now.AddHours(-24), new List<HistoryEventType> { HistoryEventType.IndexerQuery }, 2);

            result.Date.Should().BeCloseTo(_now.AddHours(-2), TimeSpan.FromSeconds(1));
        }

        [Test]
        public void most_recent_for_indexer_should_return_newest_event()
        {
            GivenHistory(1, HistoryEventType.IndexerQuery, 3);
            GivenHistory(1, HistoryEventType.IndexerQuery, 1);
            GivenHistory(2, HistoryEventType.IndexerQuery, 0);

            Subject.MostRecentForIndexer(1).Date.Should().BeCloseTo(_now.AddHours(-1), TimeSpan.FromSeconds(1));
        }

        [Test]
        public void most_recent_for_download_id_should_return_newest_event()
        {
            GivenHistory(1, HistoryEventType.ReleaseGrabbed, 3, "abc");
            GivenHistory(1, HistoryEventType.ReleaseGrabbed, 1, "abc");
            GivenHistory(1, HistoryEventType.ReleaseGrabbed, 0, "def");

            Subject.MostRecentForDownloadId("abc").Date.Should().BeCloseTo(_now.AddHours(-1), TimeSpan.FromSeconds(1));
        }
    }
}
