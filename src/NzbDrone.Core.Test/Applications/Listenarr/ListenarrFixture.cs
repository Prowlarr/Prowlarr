using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
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
    public class ListenarrFixture : CoreTest<Core.Applications.Listenarr.Listenarr>
    {
        [SetUp]
        public void Setup()
        {
            Subject.Definition = new ApplicationDefinition
            {
                Settings = new ListenarrSettings
                {
                    ProwlarrUrl = "http://localhost:9696",
                    BaseUrl = "http://localhost:4545",
                    ApiKey = "abc",
                    SyncCategories = new List<int> { NewznabStandardCategory.AudioAudiobook.Id }
                }
            };

            Mocker.GetMock<IConfigFileProvider>().SetupGet(c => c.ApiKey).Returns("abc");

            var cached = new Mock<ICached<List<ListenarrIndexer>>>();
            cached.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<Func<List<ListenarrIndexer>>>(), It.IsAny<TimeSpan>()))
                  .Returns<string, Func<List<ListenarrIndexer>>, TimeSpan>((k, f, t) => f());

            Mocker.GetMock<ICacheManager>().Setup(m => m.GetCache<List<ListenarrIndexer>>(It.IsAny<Type>())).Returns(cached.Object);
        }

        [Test]
        public void GetIndexerMappings_should_return_mappings_when_baseUrl_matches_prowlarr()
        {
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

            var mappings = Subject.GetIndexerMappings();

            mappings.Should().HaveCount(1);
            mappings[0].IndexerId.Should().Be(45);
            mappings[0].RemoteIndexerId.Should().Be(99);
        }

        [Test]
        public void GetIndexerMappings_should_skip_non_matching_api_key_and_baseurl()
        {
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

            var mappings = Subject.GetIndexerMappings();

            mappings.Should().BeEmpty();
        }

        [Test]
        public void Test_should_call_testconnection_and_return_success_when_valid()
        {
            var schema = new List<ListenarrIndexer>
            {
                new ListenarrIndexer
                {
                    Implementation = "Newznab",
                    Fields = new List<ListenarrField>
                    {
                        new ListenarrField { Name = "baseUrl", Value = "" },
                        new ListenarrField { Name = "apiPath", Value = "" },
                        new ListenarrField { Name = "apiKey", Value = "" },
                        new ListenarrField { Name = "categories", Value = new List<int>() }
                    }
                }
            };

            Mocker.GetMock<IListenarrV1Proxy>().Setup(c => c.GetIndexerSchema(It.IsAny<ListenarrSettings>())).Returns(schema);
            Mocker.GetMock<IListenarrV1Proxy>().Setup(c => c.TestConnection(It.IsAny<ListenarrIndexer>(), It.IsAny<ListenarrSettings>())).Returns((FluentValidation.Results.ValidationFailure)null).Verifiable();

            var cachedForTest = new Mock<ICached<List<ListenarrIndexer>>>();
            cachedForTest.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<Func<List<ListenarrIndexer>>>(), It.IsAny<TimeSpan>()))
                         .Returns<string, Func<List<ListenarrIndexer>>, TimeSpan>((k, f, t) => f());
            typeof(Core.Applications.Listenarr.Listenarr).GetField("_schemaCache", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(Subject, cachedForTest.Object);

            var result = Subject.Test();

            result.IsValid.Should().BeTrue();
            Mocker.GetMock<IListenarrV1Proxy>().Verify(m => m.TestConnection(It.IsAny<ListenarrIndexer>(), It.IsAny<ListenarrSettings>()), Times.Once);
        }

        [Test]
        public void Test_should_retry_and_use_fresh_schema_when_cached_schema_is_incomplete()
        {
            var cachedSchema = new List<ListenarrIndexer>
            {
                new ListenarrIndexer
                {
                    Implementation = "Newznab",
                    Fields = new List<ListenarrField>
                    {
                        new ListenarrField { Name = "apiKey", Value = "" }
                    }
                }
            };

            var freshSchema = new List<ListenarrIndexer>
            {
                new ListenarrIndexer
                {
                    Implementation = "Newznab",
                    Fields = new List<ListenarrField>
                    {
                        new ListenarrField { Name = "baseUrl", Value = "" },
                        new ListenarrField { Name = "apiPath", Value = "" },
                        new ListenarrField { Name = "apiKey", Value = "" },
                        new ListenarrField { Name = "categories", Value = new List<int>() }
                    }
                }
            };

            var cachedForTest = new Mock<ICached<List<ListenarrIndexer>>>();
            cachedForTest.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<Func<List<ListenarrIndexer>>>(), It.IsAny<TimeSpan>()))
                         .Returns<string, Func<List<ListenarrIndexer>>, TimeSpan>((k, f, t) => cachedSchema);

            typeof(Core.Applications.Listenarr.Listenarr).GetField("_schemaCache", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(Subject, cachedForTest.Object);

            Mocker.GetMock<IListenarrV1Proxy>().Setup(c => c.GetIndexerSchema(It.IsAny<ListenarrSettings>())).Returns(freshSchema);
            Mocker.GetMock<IListenarrV1Proxy>().Setup(c => c.TestConnection(It.IsAny<ListenarrIndexer>(), It.IsAny<ListenarrSettings>())).Returns((FluentValidation.Results.ValidationFailure)null);

            var result = Subject.Test();

            result.IsValid.Should().BeTrue();
            Mocker.GetMock<IListenarrV1Proxy>().Verify(m => m.GetIndexerSchema(It.IsAny<ListenarrSettings>()), Times.AtLeastOnce);
        }

        [Test]
        public void Test_should_handle_exception_from_testconnection()
        {
            var schema = new List<ListenarrIndexer>
            {
                new ListenarrIndexer
                {
                    Implementation = "Newznab",
                    Fields = new List<ListenarrField>
                    {
                        new ListenarrField { Name = "baseUrl", Value = "" },
                        new ListenarrField { Name = "apiPath", Value = "" },
                        new ListenarrField { Name = "apiKey", Value = "" },
                        new ListenarrField { Name = "categories", Value = new List<int>() }
                    }
                }
            };

            Mocker.GetMock<IListenarrV1Proxy>().Setup(c => c.GetIndexerSchema(It.IsAny<ListenarrSettings>())).Returns(schema);
            Mocker.GetMock<IListenarrV1Proxy>().Setup(c => c.TestConnection(It.IsAny<ListenarrIndexer>(), It.IsAny<ListenarrSettings>())).Throws(new Exception("boom"));

            var cachedForTest = new Mock<ICached<List<ListenarrIndexer>>>();
            cachedForTest.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<Func<List<ListenarrIndexer>>>(), It.IsAny<TimeSpan>()))
                         .Returns<string, Func<List<ListenarrIndexer>>, TimeSpan>((k, f, t) => f());
            typeof(Core.Applications.Listenarr.Listenarr).GetField("_schemaCache", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(Subject, cachedForTest.Object);

            var result = Subject.Test();

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("Unable to complete application test"));
            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void Test_should_fail_when_schema_missing()
        {
            Mocker.GetMock<IListenarrV1Proxy>().Setup(c => c.GetIndexerSchema(It.IsAny<ListenarrSettings>())).Returns(new List<ListenarrIndexer>());

            var method = typeof(Core.Applications.Listenarr.Listenarr).GetMethod("BuildListenarrIndexer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var indexerDef = new IndexerDefinition { Id = 1, Name = "Test", Protocol = DownloadProtocol.Usenet, Capabilities = new IndexerCapabilities() };

            var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() => method.Invoke(Subject, new object[] { indexerDef, indexerDef.Capabilities, DownloadProtocol.Usenet, 0 }));
            Assert.IsInstanceOf<ApplicationException>(ex.InnerException);
            Assert.That(ex.InnerException.Message, Does.Contain("indexer schemas"));
            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void Test_should_fail_when_schema_missing_required_fields()
        {
            var schema = new List<ListenarrIndexer>
            {
                new ListenarrIndexer
                {
                    Implementation = "Newznab",
                    Fields = new List<ListenarrField>
                    {
                        new ListenarrField { Name = "apiKey", Value = "" }
                    }
                }
            };

            Mocker.GetMock<IListenarrV1Proxy>().Setup(c => c.GetIndexerSchema(It.IsAny<ListenarrSettings>())).Returns(schema);

            var cachedForTest = new Mock<ICached<List<ListenarrIndexer>>>();
            cachedForTest.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<Func<List<ListenarrIndexer>>>(), It.IsAny<TimeSpan>()))
                         .Returns<string, Func<List<ListenarrIndexer>>, TimeSpan>((k, f, t) => f());
            typeof(Core.Applications.Listenarr.Listenarr).GetField("_schemaCache", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(Subject, cachedForTest.Object);

            try
            {
                var result = Subject.Test();
                result.IsValid.Should().BeFalse();
                result.Errors.Should().ContainSingle(e => e.ErrorMessage.Contains("missing required fields"));
            }
            finally
            {
                ExceptionVerification.IgnoreWarns();
            }
        }

        [Test]
        public void AddIndexer_should_insert_app_indexer_mapping_on_success()
        {
            var indexerDefinition = new IndexerDefinition
            {
                Id = 12,
                Name = "TestIndexer",
                Protocol = DownloadProtocol.Usenet,
                Capabilities = new IndexerCapabilities(),
                Enable = true,
                AppProfile = new LazyLoaded<AppSyncProfile>(new AppSyncProfile { EnableRss = true, EnableAutomaticSearch = true, EnableInteractiveSearch = true })
            };

            indexerDefinition.Capabilities.Categories.AddCategoryMapping(1, NewznabStandardCategory.AudioAudiobook);

            var mockIndexer = new Mock<IIndexer>();
            mockIndexer.Setup(i => i.GetCapabilities()).Returns(indexerDefinition.Capabilities);

            Mocker.GetMock<IIndexerFactory>().Setup(m => m.GetInstance(It.IsAny<IndexerDefinition>())).Returns(mockIndexer.Object);

            var schema = new List<ListenarrIndexer>
            {
                new ListenarrIndexer
                {
                    Implementation = "Newznab",
                    Fields = new List<ListenarrField>
                    {
                        new ListenarrField { Name = "baseUrl", Value = "" },
                        new ListenarrField { Name = "apiPath", Value = "" },
                        new ListenarrField { Name = "apiKey", Value = "" },
                        new ListenarrField { Name = "categories", Value = new List<int>() }
                    }
                }
            };

            Mocker.GetMock<IListenarrV1Proxy>().Setup(c => c.GetIndexerSchema(It.IsAny<ListenarrSettings>())).Returns(schema);
            Mocker.GetMock<IListenarrV1Proxy>().Setup(c => c.AddIndexer(It.IsAny<ListenarrIndexer>(), It.IsAny<ListenarrSettings>())).Returns(new ListenarrIndexer { Id = 501 });

            var cachedForTest = new Mock<ICached<List<ListenarrIndexer>>>();
            cachedForTest.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<Func<List<ListenarrIndexer>>>(), It.IsAny<TimeSpan>()))
                         .Returns<string, Func<List<ListenarrIndexer>>, TimeSpan>((k, f, t) => f());
            typeof(Core.Applications.Listenarr.Listenarr).GetField("_schemaCache", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(Subject, cachedForTest.Object);

            indexerDefinition.Capabilities.Categories.SupportedCategories(((ListenarrSettings)Subject.Definition.Settings).SyncCategories.ToArray()).Should().NotBeEmpty();

            Subject.AddIndexer(indexerDefinition);

            Mocker.GetMock<IListenarrV1Proxy>().Verify(m => m.AddIndexer(It.IsAny<ListenarrIndexer>(), It.IsAny<ListenarrSettings>()), Times.Once());
            Mocker.GetMock<IAppIndexerMapService>().Verify(m => m.Insert(It.Is<AppIndexerMap>(a => a.IndexerId == 12 && a.RemoteIndexerId == 501)), Times.Once());
        }

        [Test]
        public void AddIndexer_should_use_existing_remote_indexer_if_baseUrl_matches()
        {
            var indexerDefinition = new IndexerDefinition
            {
                Id = 12,
                Name = "TestIndexer",
                Protocol = DownloadProtocol.Usenet,
                Capabilities = new IndexerCapabilities(),
                Enable = true,
                AppProfile = new LazyLoaded<AppSyncProfile>(new AppSyncProfile { EnableRss = true, EnableAutomaticSearch = true, EnableInteractiveSearch = true })
            };

            indexerDefinition.Capabilities.Categories.AddCategoryMapping(1, NewznabStandardCategory.AudioAudiobook);

            var mockIndexer = new Mock<IIndexer>();
            mockIndexer.Setup(i => i.GetCapabilities()).Returns(indexerDefinition.Capabilities);

            Mocker.GetMock<IIndexerFactory>().Setup(m => m.GetInstance(It.IsAny<IndexerDefinition>())).Returns(mockIndexer.Object);

            var schema = new List<ListenarrIndexer>
            {
                new ListenarrIndexer
                {
                    Implementation = "Newznab",
                    Fields = new List<ListenarrField>
                    {
                        new ListenarrField { Name = "baseUrl", Value = "" },
                        new ListenarrField { Name = "apiPath", Value = "" },
                        new ListenarrField { Name = "apiKey", Value = "" },
                        new ListenarrField { Name = "categories", Value = new List<int>() }
                    }
                }
            };

            var existing = new ListenarrIndexer
            {
                Id = 501,
                Implementation = "Newznab",
                Fields = new List<ListenarrField>
                {
                    new ListenarrField { Name = "baseUrl", Value = $"{((ListenarrSettings)Subject.Definition.Settings).ProwlarrUrl.TrimEnd('/')}/12/" },
                    new ListenarrField { Name = "apiPath", Value = "/api" },
                    new ListenarrField { Name = "apiKey", Value = "abc" },
                    new ListenarrField { Name = "categories", Value = new List<int>() }
                }
            };

            Mocker.GetMock<IListenarrV1Proxy>().Setup(c => c.GetIndexerSchema(It.IsAny<ListenarrSettings>())).Returns(schema);
            Mocker.GetMock<IListenarrV1Proxy>().Setup(c => c.GetIndexers(It.IsAny<ListenarrSettings>())).Returns(new List<ListenarrIndexer> { existing });

            var cachedForTest = new Mock<ICached<List<ListenarrIndexer>>>();
            cachedForTest.Setup(c => c.Get(It.IsAny<string>(), It.IsAny<Func<List<ListenarrIndexer>>>(), It.IsAny<TimeSpan>()))
                         .Returns<string, Func<List<ListenarrIndexer>>, TimeSpan>((k, f, t) => f());
            typeof(Core.Applications.Listenarr.Listenarr).GetField("_schemaCache", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(Subject, cachedForTest.Object);

            indexerDefinition.Capabilities.Categories.SupportedCategories(((ListenarrSettings)Subject.Definition.Settings).SyncCategories.ToArray()).Should().NotBeEmpty();

            Subject.AddIndexer(indexerDefinition);

            Mocker.GetMock<IListenarrV1Proxy>().Verify(m => m.AddIndexer(It.IsAny<ListenarrIndexer>(), It.IsAny<ListenarrSettings>()), Times.Never());
            Mocker.GetMock<IAppIndexerMapService>().Verify(m => m.Insert(It.Is<AppIndexerMap>(a => a.IndexerId == 12 && a.RemoteIndexerId == 501)), Times.Once());
        }
    }
}
