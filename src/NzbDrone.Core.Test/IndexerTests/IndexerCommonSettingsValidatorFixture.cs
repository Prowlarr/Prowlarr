using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests
{
    [TestFixture]
    public class IndexerCommonSettingsValidatorFixture : CoreTest
    {
        [TestCase(null)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(300)]
        public void should_accept_valid_query_timeout(int? seconds)
        {
            var settings = new NewznabSettings { BaseUrl = "http://indexer.local/" };
            settings.BaseSettings.QueryTimeout = seconds;

            settings.Validate().Errors.Should().NotContain(e => e.PropertyName.EndsWith("QueryTimeout"));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(1)]
        [TestCase(9)]
        [TestCase(301)]
        public void should_reject_out_of_range_query_timeout(int seconds)
        {
            var settings = new NewznabSettings { BaseUrl = "http://indexer.local/" };
            settings.BaseSettings.QueryTimeout = seconds;

            settings.Validate().Errors.Should().Contain(e => e.PropertyName.EndsWith("QueryTimeout"));
        }

        [Test]
        public void should_validate_query_timeout_without_query_limit()
        {
            var settings = new NewznabSettings { BaseUrl = "http://indexer.local/" };
            settings.BaseSettings.QueryLimit = null;
            settings.BaseSettings.QueryTimeout = 999;

            settings.Validate().Errors.Should().Contain(e => e.PropertyName.EndsWith("QueryTimeout"));
        }
    }
}
