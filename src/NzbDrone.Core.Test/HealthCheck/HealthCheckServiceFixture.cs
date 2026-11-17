using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Messaging;
using NzbDrone.Common.TPL;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.HealthCheck
{
    public class HealthCheckServiceFixture : CoreTest<HealthCheckService>
    {
        private FakeHealthCheck _healthCheck;

        [SetUp]
        public void SetUp()
        {
            _healthCheck = new FakeHealthCheck();

            Mocker.SetConstant<IEnumerable<IProvideHealthCheck>>(new[] { _healthCheck });
            Mocker.SetConstant<ICacheManager>(Mocker.Resolve<CacheManager>());
            Mocker.SetConstant<IDebounceManager>(Mocker.Resolve<DebounceManager>());

            Mocker.GetMock<IDebounceManager>().Setup(s => s.CreateDebouncer(It.IsAny<Action>(), It.IsAny<TimeSpan>()))
                .Returns<Action, TimeSpan>((a, t) => new MockDebouncer(a, t));
        }

        [Test]
        public void should_not_execute_conditional()
        {
            Subject.HandleAsync(new FakeEvent());

            _healthCheck.Executed.Should().BeFalse();
        }

        [Test]
        public void should_execute_conditional()
        {
            Subject.HandleAsync(new FakeEvent() { ShouldExecute = true });

            _healthCheck.Executed.Should().BeTrue();
        }

        [Test]
        public void should_execute_unconditional()
        {
            Subject.HandleAsync(new FakeEvent2());

            _healthCheck.Executed.Should().BeTrue();
        }

        /// <summary>
        /// Makes the check fail for one indexer per element of <paramref name="tagsPerIndexer"/>,
        /// each carrying the given tags.
        /// </summary>
        private void GivenFailingCheck(params int[][] tagsPerIndexer)
        {
            _healthCheck.Result = new Core.HealthCheck.HealthCheck(typeof(FakeHealthCheck), HealthCheckResult.Warning, HealthCheckReason.IndexerStatusUnavailable, "Failing")
                .WithIndexers(tagsPerIndexer.Select(t => new IndexerDefinition { Tags = new HashSet<int>(t) }));
        }

        private void GivenFailingSystemCheck()
        {
            _healthCheck.Result = new Core.HealthCheck.HealthCheck(typeof(FakeHealthCheck), HealthCheckResult.Warning, HealthCheckReason.SystemTime, "Failing");
        }

        private void RunChecks()
        {
            Subject.Execute(new CheckHealthCommand { Trigger = CommandTrigger.Manual });
        }

        [Test]
        public void should_publish_failed_only_once_while_indexer_tags_are_unchanged()
        {
            GivenFailingCheck(new[] { 1 });
            RunChecks();

            GivenFailingCheck(new[] { 1 }, new[] { 1 });
            RunChecks();

            Mocker.GetMock<IEventAggregator>().Verify(v => v.PublishEvent(It.IsAny<HealthCheckFailedEvent>()), Times.Once());
            Mocker.GetMock<IEventAggregator>().Verify(v => v.PublishEvent(It.IsAny<HealthCheckUpdatedEvent>()), Times.Never());
        }

        [Test]
        public void should_not_publish_updated_for_repeated_check_not_tied_to_indexers()
        {
            GivenFailingSystemCheck();
            RunChecks();
            RunChecks();

            Mocker.GetMock<IEventAggregator>().Verify(v => v.PublishEvent(It.IsAny<HealthCheckFailedEvent>()), Times.Once());
            Mocker.GetMock<IEventAggregator>().Verify(v => v.PublishEvent(It.IsAny<HealthCheckUpdatedEvent>()), Times.Never());
        }

        [Test]
        public void should_publish_updated_when_indexer_tags_of_a_failing_check_change()
        {
            GivenFailingCheck(new[] { 1 });
            RunChecks();

            GivenFailingCheck(new[] { 1 }, new[] { 2 });
            RunChecks();

            Mocker.GetMock<IEventAggregator>().Verify(v => v.PublishEvent(It.IsAny<HealthCheckFailedEvent>()), Times.Once());
            Mocker.GetMock<IEventAggregator>().Verify(
                v => v.PublishEvent(It.Is<HealthCheckUpdatedEvent>(e =>
                    e.PreviousCheck.IndexerTags.SetEquals(new[] { 1 }) &&
                    e.HealthCheck.IndexerTags.SetEquals(new[] { 1, 2 }))),
                Times.Once());
        }

        [Test]
        public void should_publish_updated_when_a_failing_check_becomes_tied_to_indexers()
        {
            GivenFailingSystemCheck();
            RunChecks();

            GivenFailingCheck(Array.Empty<int>());
            RunChecks();

            Mocker.GetMock<IEventAggregator>().Verify(v => v.PublishEvent(It.IsAny<HealthCheckUpdatedEvent>()), Times.Once());
        }

        [Test]
        public void should_publish_restored_with_latest_result_after_an_update()
        {
            GivenFailingCheck(new[] { 1 });
            RunChecks();

            GivenFailingCheck(new[] { 1 }, new[] { 2 });
            RunChecks();

            _healthCheck.Result = null;
            RunChecks();

            Mocker.GetMock<IEventAggregator>().Verify(
                v => v.PublishEvent(It.Is<HealthCheckRestoredEvent>(e =>
                    e.PreviousCheck.IndexerTags.SetEquals(new[] { 1, 2 }))),
                Times.Once());
        }
    }

    public class FakeEvent : IEvent
    {
        public bool ShouldExecute { get; set; }
    }

    public class FakeEvent2 : IEvent
    {
        public bool ShouldExecute { get; set; }
    }

    [CheckOn(typeof(FakeEvent))]
    [CheckOn(typeof(FakeEvent2))]
    public class FakeHealthCheck : IProvideHealthCheck, ICheckOnCondition<FakeEvent>
    {
        public bool CheckOnStartup => false;
        public bool CheckOnSchedule => false;

        public bool Executed { get; set; }
        public bool Checked { get; set; }

        /// <summary>
        /// The result <see cref="Check"/> returns; null means the check passes.
        /// </summary>
        public Core.HealthCheck.HealthCheck Result { get; set; }

        public Core.HealthCheck.HealthCheck Check()
        {
            Executed = true;

            return Result ?? new Core.HealthCheck.HealthCheck(GetType());
        }

        public bool ShouldCheckOnEvent(FakeEvent message)
        {
            return message.ShouldExecute;
        }
    }
}
