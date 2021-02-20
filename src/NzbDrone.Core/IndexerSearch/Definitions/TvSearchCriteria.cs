namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class TvSearchCriteria : SearchCriteriaBase
    {
        public int? Season { get; set; }
        public int? Ep { get; set; }

        public string ImdbId { get; set; }
        public int? TvdbId { get; set; }
        public int? RId { get; set; }
        public int? TvMazeId { get; set; }
        public int? TraktId { get; set; }
    }
}
