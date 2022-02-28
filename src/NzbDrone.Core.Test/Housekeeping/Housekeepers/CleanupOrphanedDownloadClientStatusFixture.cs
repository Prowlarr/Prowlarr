using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Clients.Flood;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedDownloadClientStatusFixture : DbTest<CleanupOrphanedDownloadClientStatus, DownloadClientStatus>
    {
        private DownloadClientDefinition _downloadClient;

        [SetUp]
        public void Setup()
        {
            _downloadClient = Builder<DownloadClientDefinition>.CreateNew()
                                                               .With(c => c.Settings = new FloodSettings())
                                                               .BuildNew();
        }

        private void GivenClient()
        {
            Db.Insert(_downloadClient);
        }

        [Test]
        public void should_delete_orphaned_downloadclientstatus()
        {
            var status = Builder<DownloadClientStatus>.CreateNew()
                                                      .With(h => h.ProviderId = _downloadClient.Id)
                                                      .BuildNew();
            Db.Insert(status);

            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_unorphaned_downloadclientstatus()
        {
            GivenClient();

            var status = Builder<DownloadClientStatus>.CreateNew()
                                                      .With(h => h.ProviderId = _downloadClient.Id)
                                                      .BuildNew();
            Db.Insert(status);

            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
            AllStoredModels.Should().Contain(h => h.ProviderId == _downloadClient.Id);
        }
    }
}
