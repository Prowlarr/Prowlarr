using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests
{
    [TestFixture]
    public class IndexerCapabilitiesCategoriesFixture : CoreTest<IndexerCapabilitiesCategories>
    {
        [Test]
        public void should_support_parent_if_child_mapping()
        {
            Subject.AddCategoryMapping(1, NewznabStandardCategory.MoviesSD, "Filme SD");

            var categories = new int[] { 2000 };

            var supported = Subject.SupportedCategories(categories);

            supported.Should().HaveCount(1);
        }

        [Test]
        public void should_support_category_if_mapped()
        {
            Subject.AddCategoryMapping(1, NewznabStandardCategory.MoviesSD, "Filme SD");

            var categories = new int[] { 2030 };

            var supported = Subject.SupportedCategories(categories);

            supported.Should().HaveCount(1);
        }

        [Test]
        public void should_not_support_category_if_not_mapped()
        {
            Subject.AddCategoryMapping(1, NewznabStandardCategory.MoviesSD, "Filme SD");

            var categories = new int[] { 2040 };

            var supported = Subject.SupportedCategories(categories);

            supported.Should().HaveCount(0);
        }

        [Test]
        public void should_get_tracker_category_list()
        {
            Subject.AddCategoryMapping(1, NewznabStandardCategory.MoviesSD, "Filme SD");
            Subject.AddCategoryMapping(2, NewznabStandardCategory.MoviesHD, "Filme HD");

            var supported = Subject.GetTrackerCategories();

            supported.Should().HaveCount(2);
            supported.First().Should().NotBeNull();
            supported.First().Should().Be("1");
        }

        [Test]
        public void should_get_category_by_tracker_id()
        {
            Subject.AddCategoryMapping(1, NewznabStandardCategory.MoviesSD, "Filme SD");
            Subject.AddCategoryMapping(2, NewznabStandardCategory.MoviesHD, "Filme HD");

            var supported = Subject.MapTrackerCatToNewznab(2.ToString());

            supported.Should().HaveCount(2);
            supported.First().Should().NotBeNull();
            supported.First().Id.Should().Be(NewznabStandardCategory.MoviesHD.Id);
        }

        [Test]
        public void should_get_category_by_tracker_desc()
        {
            Subject.AddCategoryMapping(1, NewznabStandardCategory.MoviesSD, "Filme SD");
            Subject.AddCategoryMapping(2, NewznabStandardCategory.MoviesHD, "Filme HD");

            var supported = Subject.MapTrackerCatDescToNewznab("Filme HD");

            supported.Should().HaveCount(2);
            supported.First().Should().NotBeNull();
            supported.First().Id.Should().Be(NewznabStandardCategory.MoviesHD.Id);
        }
    }
}
