using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Applications;
using NzbDrone.Core.Applications.Radarr;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Datastore.Converters;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Test.Applications
{
    [TestFixture]
    public class RadarrFixture : CoreTest<Radarr>
    {
        private const int RemoteIndexerId = 5;

        private RadarrSettings _settings;
        private IndexerDefinition _indexer;
        private List<RadarrIndexer> _schema;
        private RadarrIndexer _added;
        private RadarrIndexer _updated;

        private class TestTorrentSettings : ITorrentIndexerSettings
        {
            public string BaseUrl { get; set; }
            public IndexerBaseSettings BaseSettings { get; set; } = new();
            public IndexerTorrentBaseSettings TorrentBaseSettings { get; set; } = new();

            public NzbDroneValidationResult Validate() => new(new FluentValidation.Results.ValidationResult());
        }

        private class SettingsSerializer : ProviderSettingConverter
        {
            public JsonSerializerOptions Options => SerializerSettings;
        }

        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<ICacheManager>(new CacheManager());
            Mocker.GetMock<IConfigFileProvider>().SetupGet(c => c.ApiKey).Returns("prowlarr-key");

            _settings = new RadarrSettings { ProwlarrUrl = "http://localhost:9696", BaseUrl = "http://localhost:7878", ApiKey = "radarr-key" };
            _schema = BuildSchema(true);

            var capabilities = new IndexerCapabilities();
            capabilities.MovieSearchParams.Add(MovieSearchParam.Q);
            capabilities.Categories.AddCategoryMapping(1, NewznabStandardCategory.Movies);

            _indexer = new IndexerDefinition
            {
                Id = 1,
                Name = "Test",
                Enable = true,
                Protocol = DownloadProtocol.Torrent,
                Settings = new TestTorrentSettings(),
                Capabilities = capabilities,
                AppProfile = new LazyLoaded<AppSyncProfile>(new AppSyncProfile { EnableRss = true, EnableAutomaticSearch = true, EnableInteractiveSearch = true, MinimumSeeders = 1 })
            };

            Mocker.GetMock<IRadarrV3Proxy>()
                .Setup(p => p.GetIndexerSchema(It.IsAny<RadarrSettings>()))
                .Returns(() => _schema);

            Mocker.GetMock<IRadarrV3Proxy>()
                .Setup(p => p.AddIndexer(It.IsAny<RadarrIndexer>(), It.IsAny<RadarrSettings>()))
                .Callback<RadarrIndexer, RadarrSettings>((i, _) => _added = i)
                .Returns(() => new RadarrIndexer { Id = RemoteIndexerId });

            Mocker.GetMock<IRadarrV3Proxy>()
                .Setup(p => p.UpdateIndexer(It.IsAny<RadarrIndexer>(), It.IsAny<RadarrSettings>()))
                .Callback<RadarrIndexer, RadarrSettings>((i, _) => _updated = i)
                .Returns<RadarrIndexer, RadarrSettings>((i, _) => i);

            Mocker.GetMock<IAppIndexerMapService>()
                .Setup(m => m.GetMappingsForApp(It.IsAny<int>()))
                .Returns(new List<AppIndexerMap> { new() { Id = 1, AppId = 1, IndexerId = _indexer.Id, RemoteIndexerId = RemoteIndexerId } });

            Mocker.GetMock<IIndexerFactory>()
                .Setup(f => f.GetInstance(It.IsAny<IndexerDefinition>()))
                .Returns(Mock.Of<IIndexer>(i => i.GetCapabilities() == capabilities));
        }

        private void GivenSettings(params int[] failDownloads)
        {
            _settings.SyncFailDownloads = failDownloads;

            Subject.Definition = new ApplicationDefinition { Id = 1, Settings = _settings };
        }

        private static List<RadarrIndexer> BuildSchema(bool withFailDownloads)
        {
            RadarrIndexer Build(string implementation)
            {
                var fields = new List<RadarrField>
                {
                    new() { Name = "baseUrl" },
                    new() { Name = "apiPath" },
                    new() { Name = "apiKey" },
                    new() { Name = "categories" },
                    new() { Name = "minimumSeeders" },
                    new() { Name = "seedCriteria.seedRatio" },
                    new() { Name = "seedCriteria.seedTime" },
                    new() { Name = "additionalParameters" }
                };

                if (withFailDownloads)
                {
                    fields.Add(new RadarrField { Name = "failDownloads", Value = new JArray() });
                }

                return new RadarrIndexer { Implementation = implementation, ConfigContract = $"{implementation}Settings", Fields = fields };
            }

            return new List<RadarrIndexer> { Build("Newznab"), Build("Torznab") };
        }

        private static int[] FailDownloadsOf(RadarrIndexer indexer)
        {
            return JArray.FromObject(indexer.Fields.Single(f => f.Name == "failDownloads").Value).Select(x => (int)x).ToArray();
        }

        private void GivenRemoteIndexer(params int[] remoteFailDownloads)
        {
            Subject.AddIndexer(_indexer);

            var remote = _added;
            remote.Id = RemoteIndexerId;
            remote.Fields = remote.Fields.Where(f => f.Name != "failDownloads").Select(f => f.Clone()).ToList();
            remote.Fields.Add(new RadarrField { Name = "failDownloads", Value = JArray.FromObject(remoteFailDownloads) });

            _updated = null;

            Mocker.GetMock<IRadarrV3Proxy>()
                .Setup(p => p.GetIndexer(RemoteIndexerId, It.IsAny<RadarrSettings>()))
                .Returns(remote);
        }

        [Test]
        public void should_not_send_fail_downloads_when_setting_is_empty()
        {
            GivenSettings();

            Subject.AddIndexer(_indexer);

            _added.Should().NotBeNull();
            _added.Fields.Should().NotContain(f => f.Name == "failDownloads");
        }

        [Test]
        public void should_send_fail_downloads_when_setting_is_set()
        {
            GivenSettings(0, 1);

            Subject.AddIndexer(_indexer);

            FailDownloadsOf(_added).Should().BeEquivalentTo(new[] { 0, 1 });
        }

        [Test]
        public void should_not_send_fail_downloads_when_app_schema_does_not_expose_it()
        {
            _schema = BuildSchema(false);
            GivenSettings(0, 1);

            Subject.AddIndexer(_indexer);

            _added.Should().NotBeNull();
            _added.Fields.Should().NotContain(f => f.Name == "failDownloads");
        }

        [Test]
        public void should_not_send_fail_downloads_when_stored_setting_is_null()
        {
            GivenSettings();
            _settings.SyncFailDownloads = null;

            Subject.AddIndexer(_indexer);

            _added.Should().NotBeNull();
            _added.Fields.Should().NotContain(f => f.Name == "failDownloads");
        }

        [Test]
        public void should_retain_remote_fail_downloads_on_update_when_setting_is_empty()
        {
            GivenSettings();
            GivenRemoteIndexer(0, 1);

            Subject.UpdateIndexer(_indexer, true);

            _updated.Should().NotBeNull();
            FailDownloadsOf(_updated).Should().BeEquivalentTo(new[] { 0, 1 });
        }

        [Test]
        public void should_not_update_when_only_remote_fail_downloads_differs_and_setting_is_empty()
        {
            GivenSettings();
            GivenRemoteIndexer(0, 1);

            Subject.UpdateIndexer(_indexer);

            _updated.Should().BeNull();
        }

        [Test]
        public void should_update_remote_fail_downloads_when_setting_differs_from_remote()
        {
            GivenSettings(0, 1);
            GivenRemoteIndexer(0);

            Subject.UpdateIndexer(_indexer);

            _updated.Should().NotBeNull();
            FailDownloadsOf(_updated).Should().BeEquivalentTo(new[] { 0, 1 });
        }

        [Test]
        public void should_deserialize_settings_without_fail_downloads_as_empty()
        {
            var options = new SettingsSerializer().Options;
            var json = "{\"prowlarrUrl\":\"http://localhost:9696\",\"baseUrl\":\"http://localhost:7878\",\"apiKey\":\"key\",\"syncCategories\":[2000]}";

            var settings = (RadarrSettings)JsonSerializer.Deserialize(json, typeof(RadarrSettings), options);

            settings.SyncFailDownloads.Should().NotBeNull();
            settings.SyncFailDownloads.Should().BeEmpty();
        }
    }
}
