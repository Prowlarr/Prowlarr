using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class MovieSearchCriteria : SearchCriteriaBase
    {
        public string ImdbId { get; set; }
        public int? TmdbId { get; set; }
        public int? TraktId { get; set; }

        public override bool RssSearch
        {
            get
            {
                if (SearchTerm.IsNullOrWhiteSpace() && ImdbId.IsNullOrWhiteSpace() && !TmdbId.HasValue && !TraktId.HasValue)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
