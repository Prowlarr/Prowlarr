namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class MovieSearchCriteria : SearchCriteriaBase
    {
        public string ImdbId { get; set; }
        public int? TmdbId { get; set; }
        public int? Year { get; set; }
        public int? TraktId { get; set; }
    }
}
