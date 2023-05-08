using System;
using FluentAssertions;
using FluentValidation.Results;
using NUnit.Framework;
using NzbDrone.Core.Notifications;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.NotificationTests
{
    [TestFixture]
    public class NotificationBaseFixture : TestBase
    {
        private class TestSetting : IProviderConfig
        {
            public NzbDroneValidationResult Validate()
            {
                return new NzbDroneValidationResult();
            }
        }

        private class TestNotificationWithApplicationUpdate : NotificationBase<TestSetting>
        {
            public override string Name => "TestNotification";
            public override string Link => "";

            public override ValidationResult Test()
            {
                throw new NotImplementedException();
            }

            public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
            {
                TestLogger.Info("OnApplicationUpdate was called");
            }
        }

        private class TestNotificationWithAllEvents : NotificationBase<TestSetting>
        {
            public override string Name => "TestNotification";
            public override string Link => "";

            public override ValidationResult Test()
            {
                throw new NotImplementedException();
            }

            public override void OnHealthIssue(NzbDrone.Core.HealthCheck.HealthCheck artist)
            {
                TestLogger.Info("OnHealthIssue was called");
            }

            public override void OnHealthRestored(Core.HealthCheck.HealthCheck healthCheck)
            {
                TestLogger.Info("OnHealthRestored was called");
            }

            public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
            {
                TestLogger.Info("OnApplicationUpdate was called");
            }

            public override void OnGrab(GrabMessage message)
            {
                TestLogger.Info("OnGrab was called");
            }
        }

        private class TestNotificationWithNoEvents : NotificationBase<TestSetting>
        {
            public override string Name => "TestNotification";
            public override string Link => "";

            public override ValidationResult Test()
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void should_support_all_if_implemented()
        {
            var notification = new TestNotificationWithAllEvents();

            notification.SupportsOnHealthIssue.Should().BeTrue();
            notification.SupportsOnHealthRestored.Should().BeTrue();
            notification.SupportsOnApplicationUpdate.Should().BeTrue();
            notification.SupportsOnGrab.Should().BeTrue();
        }

        [Test]
        public void should_support_none_if_none_are_implemented()
        {
            var notification = new TestNotificationWithNoEvents();

            notification.SupportsOnHealthIssue.Should().BeFalse();
            notification.SupportsOnHealthRestored.Should().BeFalse();
            notification.SupportsOnApplicationUpdate.Should().BeFalse();
            notification.SupportsOnGrab.Should().BeFalse();
        }
    }
}
