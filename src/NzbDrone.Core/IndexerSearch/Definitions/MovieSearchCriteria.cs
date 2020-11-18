namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class MovieSearchCriteria : SearchCriteriaBase
    {
        public string ImdbId { get; set; }
        public int TmdbId { get; set; }
        public override string ToString()
        {
            return string.Format("[{0}]", ImdbId);
        }
    }
}
