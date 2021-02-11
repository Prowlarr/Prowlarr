using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Applications;
using NzbDrone.Core.HealthCheck.Checks;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.HealthCheck.Checks
{
    [TestFixture]
    public class ApplicationStatusCheckFixture : CoreTest<ApplicationStatusCheck>
    {
        private List<IApplication> _applications = new List<IApplication>();
        private List<ApplicationStatus> _blockedApplications = new List<ApplicationStatus>();

        [SetUp]
        public void SetUp()
        {
            Mocker.GetMock<IApplicationFactory>()
                  .Setup(v => v.GetAvailableProviders())
                  .Returns(_applications);

            Mocker.GetMock<IApplicationStatusService>()
                   .Setup(v => v.GetBlockedProviders())
                   .Returns(_blockedApplications);

            Mocker.GetMock<ILocalizationService>()
                  .Setup(s => s.GetLocalizedString(It.IsAny<string>()))
                  .Returns("Some Warning Message");
        }

        private Mock<IApplication> GivenIndexer(int i, double backoffHours, double failureHours)
        {
            var id = i;

            var mockIndexer = new Mock<IApplication>();
            mockIndexer.SetupGet(s => s.Definition).Returns(new ApplicationDefinition { Id = id });

            _applications.Add(mockIndexer.Object);

            if (backoffHours != 0.0)
            {
                _blockedApplications.Add(new ApplicationStatus
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
            GivenIndexer(1, 2.0, 4.0);
            GivenIndexer(2, 0.0, 0.0);

            Subject.Check().ShouldBeWarning();
        }

        [Test]
        public void should_return_error_if_all_indexers_unavailable()
        {
            GivenIndexer(1, 2.0, 4.0);

            Subject.Check().ShouldBeError();
        }

        [Test]
        public void should_return_warning_if_few_indexers_unavailable()
        {
            GivenIndexer(1, 2.0, 4.0);
            GivenIndexer(2, 2.0, 4.0);
            GivenIndexer(3, 0.0, 0.0);

            Subject.Check().ShouldBeWarning();
        }
    }
}
