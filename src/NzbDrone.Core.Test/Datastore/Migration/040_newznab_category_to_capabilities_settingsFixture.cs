using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class newznab_category_to_capabilities_settingsFixture : MigrationTest<newznab_category_to_capabilities_settings>
    {
        [Test]
        public void should_migrate_categories_when_capabilities_is_not_defined()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Indexers").Row(new
                {
                    Name = "Usenet Indexer",
                    Redirect = false,
                    AppProfileId = 0,
                    DownloadClientId = 0,
                    Priority = 25,
                    Added = DateTime.UtcNow,
                    Implementation = "Newznab",
                    Settings = new
                    {
                        Categories = new[]
                        {
                            new { Id = 2000, Name = "Movies" },
                            new { Id = 5000, Name = "TV" }
                        }
                    }.ToJson(),
                    ConfigContract = "NewznabSettings"
                });
            });

            var items = db.Query<IndexerDefinition40>("SELECT \"Id\", \"Implementation\", \"ConfigContract\", \"Settings\" FROM \"Indexers\"");

            items.Should().HaveCount(1);
            items.First().Implementation.Should().Be("Newznab");
            items.First().ConfigContract.Should().Be("NewznabSettings");
            items.First().Settings.Should().ContainKey("capabilities");
            items.First().Settings.Should().NotContainKey("categories");

            var newznabSettings = items.First().Settings.ToObject<NewznabSettings40>();
            newznabSettings.Capabilities.Should().NotBeNull();
            newznabSettings.Capabilities.SupportsRawSearch.Should().Be(false);
            newznabSettings.Capabilities.Categories.Should().HaveCount(2);
            newznabSettings.Capabilities.Categories.Should().Contain(c => c.Id == 2000 && c.Name == "Movies");
            newznabSettings.Capabilities.Categories.Should().Contain(c => c.Id == 5000 && c.Name == "TV");
        }

        [Test]
        public void should_migrate_categories_when_capabilities_is_defined()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Indexers").Row(new
                {
                    Name = "Usenet Indexer",
                    Redirect = false,
                    AppProfileId = 0,
                    DownloadClientId = 0,
                    Priority = 25,
                    Added = DateTime.UtcNow,
                    Implementation = "Newznab",
                    Settings = new
                    {
                        Capabilities = new
                        {
                            SupportsRawSearch = true
                        },
                        Categories = new[]
                        {
                            new { Id = 2000, Name = "Movies" },
                            new { Id = 5000, Name = "TV" }
                        }
                    }.ToJson(),
                    ConfigContract = "NewznabSettings"
                });
            });

            var items = db.Query<IndexerDefinition40>("SELECT \"Id\", \"Implementation\", \"ConfigContract\", \"Settings\" FROM \"Indexers\"");

            items.Should().HaveCount(1);
            items.First().Implementation.Should().Be("Newznab");
            items.First().ConfigContract.Should().Be("NewznabSettings");
            items.First().Settings.Should().ContainKey("capabilities");
            items.First().Settings.Should().NotContainKey("categories");

            var newznabSettings = items.First().Settings.ToObject<NewznabSettings40>();
            newznabSettings.Capabilities.Should().NotBeNull();
            newznabSettings.Capabilities.SupportsRawSearch.Should().Be(true);
            newznabSettings.Capabilities.Categories.Should().HaveCount(2);
            newznabSettings.Capabilities.Categories.Should().Contain(c => c.Id == 2000 && c.Name == "Movies");
            newznabSettings.Capabilities.Categories.Should().Contain(c => c.Id == 5000 && c.Name == "TV");
        }

        [Test]
        public void should_use_defaults_when_categories_are_empty()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Indexers").Row(new
                {
                    Name = "Usenet Indexer",
                    Redirect = false,
                    AppProfileId = 0,
                    DownloadClientId = 0,
                    Priority = 25,
                    Added = DateTime.UtcNow,
                    Implementation = "Newznab",
                    Settings = new
                    {
                        Categories = Array.Empty<object>()
                    }.ToJson(),
                    ConfigContract = "NewznabSettings"
                });
            });

            var items = db.Query<IndexerDefinition40>("SELECT \"Id\", \"Implementation\", \"ConfigContract\", \"Settings\" FROM \"Indexers\"");

            items.Should().HaveCount(1);
            items.First().Implementation.Should().Be("Newznab");
            items.First().ConfigContract.Should().Be("NewznabSettings");
            items.First().Settings.Should().ContainKey("capabilities");
            items.First().Settings.Should().NotContainKey("categories");

            var newznabSettings = items.First().Settings.ToObject<NewznabSettings40>();
            newznabSettings.Capabilities.Should().NotBeNull();
            newznabSettings.Capabilities.SupportsRawSearch.Should().Be(false);
            newznabSettings.Capabilities.Categories.Should().NotBeNull();
            newznabSettings.Capabilities.Categories.Should().HaveCount(0);
        }

        [Test]
        public void should_use_defaults_when_settings_are_empty()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Indexers").Row(new
                {
                    Name = "Usenet Indexer",
                    Redirect = false,
                    AppProfileId = 0,
                    DownloadClientId = 0,
                    Priority = 25,
                    Added = DateTime.UtcNow,
                    Implementation = "Newznab",
                    Settings = new { }.ToJson(),
                    ConfigContract = "NewznabSettings"
                });
            });

            var items = db.Query<IndexerDefinition40>("SELECT \"Id\", \"Implementation\", \"ConfigContract\", \"Settings\" FROM \"Indexers\"");

            items.Should().HaveCount(1);
            items.First().Implementation.Should().Be("Newznab");
            items.First().ConfigContract.Should().Be("NewznabSettings");
            items.First().Settings.Should().NotContainKey("capabilities");
            items.First().Settings.Should().NotContainKey("categories");
            items.First().Settings.ToObject<NewznabSettings40>().Capabilities.Should().BeNull();
        }
    }

    public class IndexerDefinition40
    {
        public int Id { get; set; }
        public string Implementation { get; set; }
        public string ConfigContract { get; set; }
        public JObject Settings { get; set; }
    }

    public class NewznabSettings39
    {
        public object Categories { get; set; }
    }

    public class NewznabSettings40
    {
        public NewznabCapabilitiesSettings40 Capabilities { get; set; }
    }

    public class NewznabCapabilitiesSettings40
    {
        public bool SupportsRawSearch { get; set; }
        public List<IndexerCategory40> Categories { get; set; }
    }

    public class IndexerCategory40
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
