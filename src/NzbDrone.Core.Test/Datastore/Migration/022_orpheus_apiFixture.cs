using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class orpheus_apiFixture : MigrationTest<orpheus_api>
    {
        [Test]
        public void should_convert_and_disable_orpheus_instance()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Indexers").Row(new
                {
                    Enable = true,
                    Name = "Orpheus",
                    Priority = 25,
                    Added = DateTime.UtcNow,
                    Implementation = "Orpheus",
                    Settings = new GazelleIndexerSettings021
                    {
                        Username = "some name",
                        Password = "some pass"
                    }.ToJson(),
                    ConfigContract = "GazelleSettings"
                });
            });

            var items = db.Query<IndexerDefinition022>("SELECT \"Id\", \"Enable\", \"ConfigContract\", \"Settings\" FROM \"Indexers\"");

            items.Should().HaveCount(1);
            items.First().ConfigContract.Should().Be("OrpheusSettings");
            items.First().Enable.Should().Be(false);
            items.First().Settings.Should().NotContain("username");
            items.First().Settings.Should().NotContain("password");
        }
    }

    public class IndexerDefinition022
    {
        public int Id { get; set; }
        public bool Enable { get; set; }
        public string ConfigContract { get; set; }
        public string Settings { get; set; }
    }

    public class GazelleIndexerSettings021
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
