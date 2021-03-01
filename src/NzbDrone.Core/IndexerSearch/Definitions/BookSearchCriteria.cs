namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class BookSearchCriteria : SearchCriteriaBase
    {
        public string Author { get; set; }
        public string Title { get; set; }
    }
}
