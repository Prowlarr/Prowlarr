using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Applications;
using NzbDrone.Core.Applications.Listenarr;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Applications.Listenarr
{
    [TestFixture]
    public class ListenarrFixture : CoreTest<NzbDrone.Core.Applications.Listenarr.Listenarr>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new ApplicationDefinition
            {
                Settings = new ListenarrSettings
                {
                    ProwlarrUrl = "http://localhost:9696",
                    BaseUrl = "http://localhost:5000",
                    ApiKey = "abc",
                    SyncCategories = new List<int> { NewznabStandardCategory.Movies.Id }
                }
            };

            Mocker.GetMock<IConfigFileProvider>().SetupGet(c => c.ApiKey).Returns("abc");
        }

        [Test]
        public void GetIndexerMappings_should_return_mappings_when_baseUrl_matches_prowlarr()
        {
            // Arrange
            var indexer = new ListenarrIndexer
            {
                Id = 99,
                Implementation = "Newznab",
                Fields = new List<ListenarrField>
                {
                    new ListenarrField { Name = "baseUrl", Value = "http://localhost:9696/45/api" },
                    new ListenarrField { Name = "apiKey", Value = "abc" }
                }
            };

            Mocker.GetMock<IListenarrV1Proxy>().Setup(c => c.GetIndexers(It.IsAny<ListenarrSettings>())).Returns(new List<ListenarrIndexer> { indexer });

            // Act
            var mappings = Subject.GetIndexerMappings();

            // Assert
            mappings.Should().HaveCount(1);
            mappings[0].IndexerId.Should().Be(45);
            mappings[0].RemoteIndexerId.Should().Be(99);
        }

        [Test]
        public void GetIndexerMappings_should_skip_non_matching_api_key_and_baseurl()
        {
            // Arrange
            var indexer = new ListenarrIndexer
            {
                Id = 100,
                Implementation = "Newznab",
                Fields = new List<ListenarrField>
                {
                    new ListenarrField { Name = "baseUrl", Value = "http://external/1/api" },
                    new ListenarrField { Name = "apiKey", Value = "wrong" }
                }
            };

            Mocker.GetMock<IListenarrV1Proxy>().Setup(c => c.GetIndexers(It.IsAny<ListenarrSettings>())).Returns(new List<ListenarrIndexer> { indexer });

            // Act
            var mappings = Subject.GetIndexerMappings();

            // Assert
            mappings.Should().BeEmpty();
        }

        [Test]
        public void Test_should_fail_when_status_null()
        {
            // Arrange
            Mocker.GetMock<IListenarrV1Proxy>().Setup(c => c.GetStatus(It.IsAny<ListenarrSettings>())).Returns((ListenarrStatus)null);

            // Act
            var result = Subject.Test();

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("Unable to connect to Listenarr"));
        }

        [Test]
        public void Test_should_fail_on_exception()
        {
            // Arrange
            Mocker.GetMock<IListenarrV1Proxy>().Setup(c => c.GetStatus(It.IsAny<ListenarrSettings>())).Throws(new Exception("boom"));

            // Act
            var result = Subject.Test();

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("Unable to send test message"));

            // expected error was logged
            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void AddIndexer_should_insert_app_indexer_mapping_on_success()
        {
            // Arrange
            var indexerDefinition = new IndexerDefinition
            {
                Id = 12,
                Name = "TestIndexer",
                Protocol = DownloadProtocol.Usenet,
                Capabilities = new IndexerCapabilities(),
                Enable = true,
                AppProfile = new LazyLoaded<AppSyncProfile>(new AppSyncProfile { EnableRss = true, EnableAutomaticSearch = true, EnableInteractiveSearch = true })
            };

            // Add a category that matches Settings.SyncCategories
            indexerDefinition.Capabilities.Categories.AddCategoryMapping(1, NewznabStandardCategory.Movies);

            var mockIndexer = new Mock<IIndexer>();
            mockIndexer.Setup(i => i.GetCapabilities()).Returns(indexerDefinition.Capabilities);

            Mocker.GetMock<IIndexerFactory>().Setup(m => m.GetInstance(It.IsAny<IndexerDefinition>())).Returns(mockIndexer.Object);

            Mocker.GetMock<IListenarrV1Proxy>().Setup(c => c.AddIndexer(It.IsAny<ListenarrIndexer>(), It.IsAny<ListenarrSettings>())).Returns(new ListenarrIndexer { Id = 501 });

            // pre-check
            indexerDefinition.Capabilities.Categories.SupportedCategories(((ListenarrSettings)Subject.Definition.Settings).SyncCategories.ToArray()).Should().NotBeEmpty();

            // Act
            Subject.AddIndexer(indexerDefinition);

            // Assert
            Mocker.GetMock<IListenarrV1Proxy>().Verify(m => m.AddIndexer(It.IsAny<ListenarrIndexer>(), It.IsAny<ListenarrSettings>()), Times.Once());
            Mocker.GetMock<IAppIndexerMapService>().Verify(m => m.Insert(It.Is<AppIndexerMap>(a => a.IndexerId == 12 && a.RemoteIndexerId == 501)), Times.Once());
        }
    }
}
