using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class newznab_indexers_enable_redirectFixture : MigrationTest<newznab_indexers_enable_redirect>
    {
        [Test]
        public void should_update_redirect_setting_to_true_if_false()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Indexers").Row(new
                {
                    Id = 1,
                    Name = "Test",
                    Implementation = "Newznab",
                    Settings = "{\"baseUrl\":\"https://example.com\",\"apiKey\":\"testapikey\"}",
                    ConfigContract = "NewznabSettings",
                    Enable = true,
                    Priority = 1,
                    Added = System.DateTime.UtcNow,
                    Redirect = false,
                    AppProfileId = 1,
                    Tags = "[]",
                    DownloadClientId = 0
                });
            });

            var items = db.Query<IndexerDefinition043>("SELECT * FROM \"Indexers\"");

            items.Should().HaveCount(1);
            items.First().Implementation.Should().Be("Newznab");
            items.First().Redirect.Should().BeTrue(); // Validate Redirect is updated
        }

        [Test]
        public void should_not_change_redirect_setting_if_already_true()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Indexers").Row(new
                {
                    Id = 1,
                    Name = "Test",
                    Implementation = "Newznab",
                    Settings = "{\"baseUrl\":\"https://example.com\",\"apiKey\":\"testapikey\"}",
                    ConfigContract = "NewznabSettings",
                    Enable = true,
                    Priority = 2,
                    Added = System.DateTime.UtcNow,
                    Redirect = true,
                    AppProfileId = 1,
                    Tags = "[]",
                    DownloadClientId = 0
                });
            });

            var items = db.Query<IndexerDefinition043>("SELECT * FROM \"Indexers\"");

            items.Should().HaveCount(1);
            items.First().Implementation.Should().Be("Newznab");
            items.First().Redirect.Should().BeTrue(); // Validate Redirect remains true
        }

        [Test]
        public void should_not_affect_non_newznab_indexers()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Indexers").Row(new
                {
                    Id = 1,
                    Name = "Test",
                    Implementation = "OtherIndexer",
                    Settings = "{\"baseUrl\":\"https://otherindexer.com\"}",
                    ConfigContract = "OtherIndexerSettings",
                    Enable = true,
                    Priority = 3,
                    Added = System.DateTime.UtcNow,
                    Redirect = false,
                    AppProfileId = 1,
                    Tags = "[]",
                    DownloadClientId = 0
                });
            });

            var items = db.Query<IndexerDefinition043>("SELECT * FROM \"Indexers\"");

            items.Should().HaveCount(1);
            items.First().Implementation.Should().Be("OtherIndexer");
            items.First().Redirect.Should().BeFalse(); // Validate Redirect is not changed
        }

        [Test]
        public void should_handle_multiple_indexers()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Indexers").Row(new
                {
                    Id = 1,
                    Name = "Test 1",
                    Implementation = "Newznab",
                    Settings = "{\"baseUrl\":\"https://example1.com\",\"apiKey\":\"testapikey1\"}",
                    ConfigContract = "NewznabSettings",
                    Enable = true,
                    Priority = 4,
                    Added = System.DateTime.UtcNow,
                    Redirect = false,
                    AppProfileId = 1,
                    Tags = "[]",
                    DownloadClientId = 0
                });
                c.Insert.IntoTable("Indexers").Row(new
                {
                    Id = 2,
                    Name = "Test 2",
                    Implementation = "Newznab",
                    Settings = "{\"baseUrl\":\"https://example2.com\",\"apiKey\":\"testapikey2\"}",
                    ConfigContract = "NewznabSettings",
                    Enable = true,
                    Priority = 5,
                    Added = System.DateTime.UtcNow,
                    Redirect = false,
                    AppProfileId = 1,
                    Tags = "[]",
                    DownloadClientId = 0
                });
                c.Insert.IntoTable("Indexers").Row(new
                {
                    Id = 3,
                    Name = "Test 3",
                    Implementation = "Newznab",
                    Settings = "{\"baseUrl\":\"https://example3.com\",\"apiKey\":\"testapikey3\"}",
                    ConfigContract = "NewznabSettings",
                    Enable = true,
                    Priority = 6,
                    Added = System.DateTime.UtcNow,
                    Redirect = true,
                    AppProfileId = 1,
                    Tags = "[]",
                    DownloadClientId = 0
                });
                c.Insert.IntoTable("Indexers").Row(new
                {
                    Id = 4,
                    Name = "Test 4",
                    Implementation = "OtherIndexer",
                    Settings = "{\"baseUrl\":\"https://otherindexer.com\"}",
                    ConfigContract = "OtherIndexerSettings",
                    Enable = true,
                    Priority = 7,
                    Added = System.DateTime.UtcNow,
                    Redirect = false,
                    AppProfileId = 1,
                    Tags = "[]",
                    DownloadClientId = 0
                });
            });

            var items = db.Query<IndexerDefinition043>("SELECT * FROM \"Indexers\"");

            items.Should().HaveCount(4);
            items.First(i => i.Id == 1).Redirect.Should().BeTrue(); // Validate Redirect is updated
            items.First(i => i.Id == 2).Redirect.Should().BeTrue(); // Validate Redirect is updated
            items.First(i => i.Id == 3).Redirect.Should().BeTrue(); // Validate Redirect remains true
            items.First(i => i.Id == 4).Redirect.Should().BeFalse(); // Validate Redirect is not changed
        }
    }

    public class IndexerDefinition043
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Implementation { get; set; }
        public string Settings { get; set; }
        public string ConfigContract { get; set; }
        public bool Enable { get; set; }
        public int Priority { get; set; }
        public DateTime Added { get; set; }
        public bool Redirect { get; set; }
        public int AppProfileId { get; set; }
        public string Tags { get; set; }
        public int DownloadClientId { get; set; }
    }
}
