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
        private List<IndexerDefinition> _indexers = new List<IndexerDefinition>();

        [SetUp]
        public void SetUp()
        {
            _notifications = new List<INotification>();
            _indexers = new List<IndexerDefinition>();

            Mocker.GetMock<INotificationFactory>()
                  .Setup(v => v.OnHealthIssueEnabled(It.IsAny<bool>()))
                  .Returns(_notifications);

            Mocker.GetMock<INotificationFactory>()
                  .Setup(v => v.OnHealthRestoredEnabled(It.IsAny<bool>()))
                  .Returns(_notifications);

            Mocker.GetMock<IIndexerFactory>()
                  .Setup(v => v.All())
                  .Returns(_indexers);
        }

        private Mock<INotification> GivenNotification(params int[] tags)
        {
            var mockNotification = new Mock<INotification>();
            mockNotification.SetupGet(s => s.Definition).Returns(new NotificationDefinition
            {
                Id = _notifications.Count + 1,
                IncludeHealthWarnings = true,
                Tags = new HashSet<int>(tags)
            });

            _notifications.Add(mockNotification.Object);

            return mockNotification;
        }

        private void GivenIndexer(int id, params int[] tags)
        {
            _indexers.Add(new IndexerDefinition
            {
                Id = id,
                Tags = new HashSet<int>(tags)
            });
        }

        private HealthCheckModel GivenHealthCheck(params int[] relatedProviders)
        {
            return new HealthCheckModel(typeof(IndexerStatusCheck), HealthCheckResult.Error, "Test health check")
            {
                RelatedProviders = relatedProviders
            };
        }

        [Test]
        public void should_send_health_issue_when_notification_has_no_tags()
        {
            var notification = GivenNotification();
            GivenIndexer(1, 1);

            Subject.Handle(new HealthCheckFailedEvent(GivenHealthCheck(1), false));

            notification.Verify(v => v.OnHealthIssue(It.IsAny<HealthCheckModel>()), Times.Once());
        }

        [Test]
        public void should_send_health_issue_when_a_related_indexer_has_a_matching_tag()
        {
            var notification = GivenNotification(1);
            GivenIndexer(5, 1);

            Subject.Handle(new HealthCheckFailedEvent(GivenHealthCheck(5), false));

            notification.Verify(v => v.OnHealthIssue(It.IsAny<HealthCheckModel>()), Times.Once());
        }

        [Test]
        public void should_not_send_health_issue_when_no_related_indexer_has_a_matching_tag()
        {
            var notification = GivenNotification(1);
            GivenIndexer(5, 2);

            Subject.Handle(new HealthCheckFailedEvent(GivenHealthCheck(5), false));

            notification.Verify(v => v.OnHealthIssue(It.IsAny<HealthCheckModel>()), Times.Never());
        }

        [Test]
        public void should_send_health_issue_when_check_has_no_related_indexers()
        {
            var notification = GivenNotification(1);

            Subject.Handle(new HealthCheckFailedEvent(GivenHealthCheck(), false));

            notification.Verify(v => v.OnHealthIssue(It.IsAny<HealthCheckModel>()), Times.Once());
        }

        [Test]
        public void should_not_send_health_restored_when_no_related_indexer_has_a_matching_tag()
        {
            var notification = GivenNotification(1);
            GivenIndexer(5, 2);

            Subject.Handle(new HealthCheckRestoredEvent(GivenHealthCheck(5), false));

            notification.Verify(v => v.OnHealthRestored(It.IsAny<HealthCheckModel>()), Times.Never());
        }
    }
}
