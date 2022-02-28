using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Applications;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedApplicationFixture : DbTest<CleanupOrphanedApplicationStatus, ApplicationStatus>
    {
        private ApplicationDefinition _application;

        [SetUp]
        public void Setup()
        {
            _application = Builder<ApplicationDefinition>.CreateNew()
                                                         .BuildNew();
        }

        private void GivenApplication()
        {
            Db.Insert(_application);
        }

        [Test]
        public void should_delete_orphaned_applicationstatus()
        {
            var status = Builder<ApplicationStatus>.CreateNew()
                                                   .With(h => h.ProviderId = _application.Id)
                                                   .BuildNew();
            Db.Insert(status);

            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_unorphaned_applicationstatus()
        {
            GivenApplication();

            var status = Builder<ApplicationStatus>.CreateNew()
                                                   .With(h => h.ProviderId = _application.Id)
                                                   .BuildNew();
            Db.Insert(status);

            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
            AllStoredModels.Should().Contain(h => h.ProviderId == _application.Id);
        }
    }
}
