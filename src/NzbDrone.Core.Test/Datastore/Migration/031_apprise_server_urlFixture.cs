using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class apprise_server_urlFixture : MigrationTest<apprise_server_url>
    {
        [Test]
        public void should_rename_server_url_setting_for_apprise()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Notifications").Row(new
                {
                    Name = "Apprise",
                    Implementation = "Apprise",
                    Settings = new
                    {
                        BaseUrl = "http://localhost:8000",
                        NotificationType = 0
                    }.ToJson(),
                    ConfigContract = "AppriseSettings",
                    OnHealthIssue = true,
                    IncludeHealthWarnings = true,
                    OnApplicationUpdate = true,
                    OnGrab = true,
                    IncludeManualGrabs = true
                });
            });

            var items = db.Query<NotificationDefinition31>("SELECT * FROM \"Notifications\"");

            items.Should().HaveCount(1);

            items.First().Settings.Should().NotContainKey("baseUrl");
            items.First().Settings.Should().ContainKey("serverUrl");
            items.First().Settings.GetValueOrDefault("serverUrl").Should().Be("http://localhost:8000");
        }
    }

    public class NotificationDefinition31
    {
        public int Id { get; set; }
        public int Priority { get; set; }
        public string Name { get; set; }
        public string Implementation { get; set; }
        public Dictionary<string, string> Settings { get; set; }
        public string ConfigContract { get; set; }
        public bool OnHealthIssue { get; set; }
        public bool IncludeHealthWarnings { get; set; }
        public bool OnApplicationUpdate { get; set; }
        public bool OnGrab { get; set; }
        public bool IncludeManualGrabs { get; set; }
        public List<int> Tags { get; set; }
    }
}
