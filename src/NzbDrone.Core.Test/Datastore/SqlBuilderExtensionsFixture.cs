using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Test.Datastore
{
    [TestFixture]
    public class SqlBuilderExtensionsFixture
    {
        private static string Normalize(string sql)
        {
            return string.Join(" ", sql.Split(new[] { ' ', '\n' }, System.StringSplitOptions.RemoveEmptyEntries));
        }

        private static string GetSql(SqlBuilder builder)
        {
            return Normalize(builder.AddSelectTemplate(typeof(History.History)).RawSql);
        }

        [TestCase(DatabaseType.SQLite)]
        [TestCase(DatabaseType.PostgreSQL)]
        public void should_order_ascending(DatabaseType databaseType)
        {
            var builder = new SqlBuilder(databaseType)
                .OrderBy<History.History>(h => h.Date);

            GetSql(builder).Should().EndWith("ORDER BY \"History\".\"Date\" ASC");
        }

        [TestCase(DatabaseType.SQLite)]
        [TestCase(DatabaseType.PostgreSQL)]
        public void should_order_descending_with_limit_and_offset(DatabaseType databaseType)
        {
            var builder = new SqlBuilder(databaseType)
                .OrderBy<History.History>(h => h.Date, SortDirection.Descending)
                .Take(10)
                .Skip(20);

            GetSql(builder).Should().EndWith("ORDER BY \"History\".\"Date\" DESC LIMIT 10 OFFSET 20");
        }

        [Test]
        public void should_combine_multiple_order_columns()
        {
            var builder = new SqlBuilder(DatabaseType.SQLite)
                .OrderBy<History.History>(h => h.Date, SortDirection.Descending)
                .OrderBy<History.History>(h => h.Id);

            GetSql(builder).Should().EndWith("ORDER BY \"History\".\"Date\" DESC , \"History\".\"Id\" ASC");
        }

        [Test]
        public void should_not_add_limit_or_offset_by_default()
        {
            var builder = new SqlBuilder(DatabaseType.SQLite)
                .Where<History.History>(h => h.IndexerId == 1);

            GetSql(builder).Should().NotContain("LIMIT").And.NotContain("OFFSET");
        }
    }
}
