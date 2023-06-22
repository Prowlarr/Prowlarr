using System;
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
    public class history_fix_data_titlesFixture : MigrationTest<history_fix_data_titles>
    {
        [Test]
        public void should_update_data_for_book_search()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("History").Row(new
                {
                    IndexerId = 1,
                    Date = DateTime.UtcNow,
                    Data = new
                    {
                        Author = "Fake Author",
                        BookTitle = "Fake Book Title",
                        Publisher = "",
                        Year = "",
                        Genre = "",
                        Query = "",
                        QueryType = "book",
                        Source = "Prowlarr",
                        Host = "localhost"
                    }.ToJson(),
                    EventType = 2,
                    Successful = true
                });
            });

            var items = db.Query<HistoryDefinition34>("SELECT * FROM \"History\"");

            items.Should().HaveCount(1);

            items.First().Data.Should().NotContainKey("bookTitle");
            items.First().Data.Should().ContainKey("title");
            items.First().Data.GetValueOrDefault("title").Should().Be("Fake Book Title");
        }

        [Test]
        public void should_update_data_for_release_grabbed()
        {
            var db = WithMigrationTestDb(c =>
            {
                c.Insert.IntoTable("History").Row(new
                {
                    IndexerId = 1,
                    Date = DateTime.UtcNow,
                    Data = new
                    {
                        GrabMethod = "Proxy",
                        Title = "Fake Release Title",
                        Source = "Prowlarr",
                        Host = "localhost"
                    }.ToJson(),
                    EventType = 1,
                    Successful = true
                });
            });

            var items = db.Query<HistoryDefinition34>("SELECT * FROM \"History\"");

            items.Should().HaveCount(1);

            items.First().Data.Should().NotContainKey("title");
            items.First().Data.Should().ContainKey("grabTitle");
            items.First().Data.GetValueOrDefault("grabTitle").Should().Be("Fake Release Title");
        }
    }

    public class HistoryDefinition34
    {
        public int Id { get; set; }
        public int IndexerId { get; set; }
        public DateTime Date { get; set; }
        public Dictionary<string, string> Data { get; set; }
        public int EventType { get; set; }
        public string DownloadId { get; set; }
        public bool Successful { get; set; }
    }
}
