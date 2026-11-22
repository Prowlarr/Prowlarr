using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.HealthCheck.Checks;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.Test.Framework;
using HealthCheckModel = NzbDrone.Core.HealthCheck.HealthCheck;

namespace NzbDrone.Core.Test.NotificationTests
{
    [TestFixture]
    public class NotificationServiceFixture : CoreTest<NotificationService>
    {
        private List<INotification> _notifications = new List<INotification>();

        [SetUp]
        public void SetUp()
        {
            _notifications = new List<INotification>();

            Mocker.GetMock<INotificationFactory>()
                  .Setup(v => v.OnHealthIssueEnabled(It.IsAny<bool>()))
                  .Returns(_notifications);

            Mocker.GetMock<INotificationFactory>()
                  .Setup(v => v.OnHealthRestoredEnabled(It.IsAny<bool>()))
                  .Returns(_notifications);
        }

        private Mock<INotification> GivenNotification(params int[] tags)
        {
            var mockNotification = new Mock<INotification>();
            mockNotification.SetupGet(s => s.Definition).Returns(new NotificationDefinition
            {
                Id = _notifications.Count + 1,
                Name = "Notification " + (_notifications.Count + 1),
                IncludeHealthWarnings = true,
                Tags = new HashSet<int>(tags)
            });

            _notifications.Add(mockNotification.Object);

            return mockNotification;
        }

        /// <summary>
        /// A failing check for one affected indexer per element of <paramref name="tagsPerIndexer"/>,
        /// each carrying the given tags (an empty array is an indexer without tags).
        /// </summary>
        private static HealthCheckModel GivenIndexerHealthCheck(HealthCheckResult type, params int[][] tagsPerIndexer)
        {
            var indexers = new List<IndexerDefinition>();

            foreach (var tags in tagsPerIndexer)
            {
                indexers.Add(new IndexerDefinition { Tags = new HashSet<int>(tags) });
            }

            return new HealthCheckModel(typeof(IndexerStatusCheck), type, HealthCheckReason.IndexerStatusUnavailable, "Test health check")
                .WithIndexers(indexers);
        }

        private static HealthCheckModel GivenIndexerHealthCheck(params int[][] tagsPerIndexer)
        {
            return GivenIndexerHealthCheck(HealthCheckResult.Error, tagsPerIndexer);
        }

        private static HealthCheckModel GivenSystemHealthCheck()
        {
            return new HealthCheckModel(typeof(SystemTimeCheck), HealthCheckResult.Error, HealthCheckReason.SystemTime, "Test health check");
        }

        private static int[] Tags(params int[] tags)
        {
            return tags;
        }

        private static void VerifyIssue(Mock<INotification> notification, Times times)
        {
            notification.Verify(v => v.OnHealthIssue(It.IsAny<HealthCheckModel>()), times);
        }

        private static void VerifyRestored(Mock<INotification> notification, Times times)
        {
            notification.Verify(v => v.OnHealthRestored(It.IsAny<HealthCheckModel>()), times);
        }

        [Test]
        public void should_send_health_issue_when_notification_has_no_tags()
        {
            var notification = GivenNotification();

            Subject.Handle(new HealthCheckFailedEvent(GivenIndexerHealthCheck(Tags(1)), false));

            VerifyIssue(notification, Times.Once());
        }

        [Test]
        public void should_send_health_issue_when_an_affected_indexer_has_a_matching_tag()
        {
            var notification = GivenNotification(1);

            Subject.Handle(new HealthCheckFailedEvent(GivenIndexerHealthCheck(Tags(2), Tags(1, 3)), false));

            VerifyIssue(notification, Times.Once());
        }

        [Test]
        public void should_not_send_health_issue_when_no_affected_indexer_has_a_matching_tag()
        {
            var notification = GivenNotification(1);

            Subject.Handle(new HealthCheckFailedEvent(GivenIndexerHealthCheck(Tags(2), Tags()), false));

            VerifyIssue(notification, Times.Never());
        }

        [Test]
        public void should_send_health_issue_to_tagged_notification_when_check_is_not_tied_to_indexers()
        {
            var notification = GivenNotification(1);

            Subject.Handle(new HealthCheckFailedEvent(GivenSystemHealthCheck(), false));

            VerifyIssue(notification, Times.Once());
        }

        [Test]
        public void should_send_health_restored_when_an_affected_indexer_has_a_matching_tag()
        {
            var notification = GivenNotification(1);

            Subject.Handle(new HealthCheckRestoredEvent(GivenIndexerHealthCheck(Tags(1)), false));

            VerifyRestored(notification, Times.Once());
        }

        [Test]
        public void should_not_send_health_restored_when_no_affected_indexer_has_a_matching_tag()
        {
            var notification = GivenNotification(1);

            Subject.Handle(new HealthCheckRestoredEvent(GivenIndexerHealthCheck(Tags(2)), false));

            VerifyRestored(notification, Times.Never());
        }

        [Test]
        public void should_send_health_issue_when_update_adds_first_indexer_with_matching_tag()
        {
            var notification = GivenNotification(1);
            var current = GivenIndexerHealthCheck(Tags(2), Tags(1));

            Subject.Handle(new HealthCheckUpdatedEvent(GivenIndexerHealthCheck(Tags(2)), current, false));

            notification.Verify(v => v.OnHealthIssue(current), Times.Once());
            VerifyRestored(notification, Times.Never());
        }

        [Test]
        public void should_not_send_health_issue_again_when_update_adds_another_matching_indexer()
        {
            var notification = GivenNotification(1);

            Subject.Handle(new HealthCheckUpdatedEvent(GivenIndexerHealthCheck(Tags(1)), GivenIndexerHealthCheck(Tags(1), Tags(1, 2)), false));

            VerifyIssue(notification, Times.Never());
            VerifyRestored(notification, Times.Never());
        }

        [Test]
        public void should_not_send_anything_on_update_when_no_indexer_has_a_matching_tag()
        {
            var notification = GivenNotification(1);

            Subject.Handle(new HealthCheckUpdatedEvent(GivenIndexerHealthCheck(Tags(2)), GivenIndexerHealthCheck(Tags(2), Tags(3)), false));

            VerifyIssue(notification, Times.Never());
            VerifyRestored(notification, Times.Never());
        }

        [Test]
        public void should_send_health_restored_when_update_removes_last_indexer_with_matching_tag()
        {
            var notification = GivenNotification(1);
            var previous = GivenIndexerHealthCheck(Tags(2), Tags(1));

            Subject.Handle(new HealthCheckUpdatedEvent(previous, GivenIndexerHealthCheck(Tags(2)), false));

            notification.Verify(v => v.OnHealthRestored(previous), Times.Once());
            VerifyIssue(notification, Times.Never());
        }

        [Test]
        public void should_not_send_anything_on_update_when_notification_has_no_tags()
        {
            var notification = GivenNotification();

            Subject.Handle(new HealthCheckUpdatedEvent(GivenIndexerHealthCheck(Tags(1)), GivenIndexerHealthCheck(Tags(2)), false));

            VerifyIssue(notification, Times.Never());
            VerifyRestored(notification, Times.Never());
        }

        [Test]
        public void should_not_send_anything_on_update_during_startup_grace_period()
        {
            var notification = GivenNotification(1);

            Subject.Handle(new HealthCheckUpdatedEvent(GivenIndexerHealthCheck(Tags(2)), GivenIndexerHealthCheck(Tags(1)), true));

            VerifyIssue(notification, Times.Never());
            VerifyRestored(notification, Times.Never());
        }

        [Test]
        public void should_not_send_health_issue_on_update_for_excluded_warning()
        {
            var notification = GivenNotification(1);
            ((NotificationDefinition)notification.Object.Definition).IncludeHealthWarnings = false;

            Subject.Handle(new HealthCheckUpdatedEvent(GivenIndexerHealthCheck(Tags(2)), GivenIndexerHealthCheck(HealthCheckResult.Warning, Tags(2), Tags(1)), false));

            VerifyIssue(notification, Times.Never());
        }

        [Test]
        public void should_not_send_health_restored_on_update_for_excluded_warning()
        {
            var notification = GivenNotification(1);
            ((NotificationDefinition)notification.Object.Definition).IncludeHealthWarnings = false;

            Subject.Handle(new HealthCheckUpdatedEvent(GivenIndexerHealthCheck(HealthCheckResult.Warning, Tags(2), Tags(1)), GivenIndexerHealthCheck(Tags(2)), false));

            VerifyRestored(notification, Times.Never());
        }
    }
}
