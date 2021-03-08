using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class BookSearchCriteria : SearchCriteriaBase
    {
        public string Author { get; set; }
        public string Title { get; set; }

        public override bool RssSearch
        {
            get
            {
                if (SearchTerm.IsNullOrWhiteSpace() && Author.IsNullOrWhiteSpace() && Title.IsNullOrWhiteSpace())
                {
                    return true;
                }

                return false;
            }
        }
    }
}
