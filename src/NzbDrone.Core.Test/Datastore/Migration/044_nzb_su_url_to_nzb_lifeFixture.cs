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
    public class nzb_su_url_to_nzb_lifeFixture : MigrationTest<nzb_su_url_to_nzb_life>
    {
        [TestCase("Newznab", "https://api.nzb.su")]
        [TestCase("Newznab", "http://api.nzb.su")]
        public void should_replace_old_url(string impl, string baseUrl)
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Indexers").Row(new
                {
                    Name = "Nzb.su",
                    Implementation = impl,
                    Settings = new NewznabSettings044
                    {
                        BaseUrl = baseUrl,
                        ApiPath = "/api"
                    }.ToJson(),
                    ConfigContract = impl + "Settings",
                    EnableInteractiveSearch = false
                });
            });

            var items = db.Query<IndexerDefinition044>("SELECT * FROM \"Indexers\"");

            items.Should().HaveCount(1);
            items.First().Settings.ToObject<NewznabSettings044>().BaseUrl.Should().Be(baseUrl.Replace("su", "life"));
        }

        [TestCase("Newznab", "https://api.indexer.com")]
        public void should_not_replace_different_url(string impl, string baseUrl)
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("Indexers").Row(new
                {
                    Name = "Indexer.com",
                    Implementation = impl,
                    Settings = new NewznabSettings044
                    {
                        BaseUrl = baseUrl,
                        ApiPath = "/api"
                    }.ToJson(),
                    ConfigContract = impl + "Settings",
                    EnableInteractiveSearch = false
                });
            });

            var items = db.Query<IndexerDefinition044>("SELECT * FROM \"Indexers\"");

            items.Should().HaveCount(1);
            items.First().Settings.ToObject<NewznabSettings044>().BaseUrl.Should().Be(baseUrl);
        }
    }

    internal class IndexerDefinition044
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public JObject Settings { get; set; }
        public int Priority { get; set; }
        public string Implementation { get; set; }
        public string ConfigContract { get; set; }
        public bool EnableRss { get; set; }
        public bool EnableAutomaticSearch { get; set; }
        public bool EnableInteractiveSearch { get; set; }
        public HashSet<int> Tags { get; set; }
        public int DownloadClientId { get; set; }
        public int SeasonSearchMaximumSingleEpisodeAge { get; set; }
    }

    internal class NewznabSettings044
    {
        public string BaseUrl { get; set; }
        public string ApiPath { get; set; }
    }
}
