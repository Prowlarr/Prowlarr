using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class BookSearchCriteria : SearchCriteriaBase
    {
        public string Author { get; set; }
        public string Title { get; set; }
        public string Publisher { get; set; }
        public int? Year { get; set; }
        public string Genre { get; set; }

        public override bool IsRssSearch =>
            SearchTerm.IsNullOrWhiteSpace() &&
            !IsIdSearch;

        public override bool IsIdSearch =>
            Author.IsNotNullOrWhiteSpace() ||
            Title.IsNotNullOrWhiteSpace() ||
            Publisher.IsNotNullOrWhiteSpace() ||
            Genre.IsNotNullOrWhiteSpace() ||
            Year.HasValue;
    }
}
