using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class indexer_user_agentFixture : MigrationTest<indexer_user_agent>
    {
        private static object ExistingIndexer(int id, string name) => new
        {
            Id = id,
            Name = name,
            Implementation = "Newznab",
            Settings = "{\"baseUrl\":\"https://example.com\",\"apiKey\":\"testapikey\"}",
            ConfigContract = "NewznabSettings",
            Enable = true,
            Priority = 25,
            Added = DateTime.UtcNow,
            Redirect = false,
            AppProfileId = 1,
            Tags = "[]",
            DownloadClientId = 0
        };

        // Selects "UserAgent" explicitly rather than "*": the test mapper only populates properties for
        // columns the result set actually has, so "SELECT *" would leave UserAgent null and pass even if
        // the migration never ran. Naming the column makes a missing one a SQL error instead.
        [Test]
        public void should_add_user_agent_column_leaving_existing_indexers_unset()
        {
            var db = WithMigrationTestDb(c => c.Insert.IntoTable("Indexers").Row(ExistingIndexer(1, "Test")));

            var items = db.Query<IndexerDefinition045>("SELECT \"Id\", \"UserAgent\" FROM \"Indexers\"");

            items.Should().HaveCount(1);
            items.First().UserAgent.Should().BeNull();
        }

        [Test]
        public void should_not_disturb_other_indexer_columns()
        {
            var db = WithMigrationTestDb(c => c.Insert.IntoTable("Indexers").Row(ExistingIndexer(1, "Test")));

            var item = db.Query<IndexerDefinition045>("SELECT * FROM \"Indexers\"").First();

            item.Name.Should().Be("Test");
            item.Implementation.Should().Be("Newznab");
            item.Priority.Should().Be(25);
            item.Enable.Should().BeTrue();
        }

        [Test]
        public void should_migrate_multiple_indexers()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Indexers").Row(ExistingIndexer(1, "Test 1"));
                c.Insert.IntoTable("Indexers").Row(ExistingIndexer(2, "Test 2"));
            });

            var items = db.Query<IndexerDefinition045>("SELECT \"Id\", \"UserAgent\" FROM \"Indexers\"");

            items.Should().HaveCount(2);
            items.Should().OnlyContain(i => i.UserAgent == null);
        }
    }

    public class IndexerDefinition045
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
        public string UserAgent { get; set; }
    }
}
