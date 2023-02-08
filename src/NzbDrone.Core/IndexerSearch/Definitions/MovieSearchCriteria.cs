using System.Text;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class MovieSearchCriteria : SearchCriteriaBase
    {
        public string ImdbId { get; set; }
        public int? TmdbId { get; set; }
        public int? TraktId { get; set; }
        public int? DoubanId { get; set; }
        public int? Year { get; set; }
        public string Genre { get; set; }

        public override bool RssSearch => SearchTerm.IsNullOrWhiteSpace() && ImdbId.IsNullOrWhiteSpace() && !TmdbId.HasValue && !TraktId.HasValue;

        public string FullImdbId => ParseUtil.GetFullImdbId(ImdbId);

        public override string SearchQuery
        {
            get
            {
                var searchQueryTerm = $"Term: []";
                if (SearchTerm.IsNotNullOrWhiteSpace())
                {
                    searchQueryTerm = $"Term: [{SearchTerm}]";
                }

                if (!ImdbId.IsNotNullOrWhiteSpace() && !TmdbId.HasValue && !TraktId.HasValue)
                {
                    return searchQueryTerm;
                }

                var builder = new StringBuilder(searchQueryTerm);
                builder = builder.Append(" | ID(s):");

                if (ImdbId.IsNotNullOrWhiteSpace())
                {
                    builder = builder.Append($" IMDbId:[{ImdbId}]");
                }

                if (Year.HasValue)
                {
                    builder = builder.Append($" Year:[{Year}]");
                }

                if (TmdbId.HasValue)
                {
                    builder = builder.Append($" TMDbId:[{TmdbId}]");
                }

                if (TraktId.HasValue)
                {
                    builder = builder.Append($" TraktId:[{TraktId}]");
                }

                if (Genre.IsNotNullOrWhiteSpace())
                {
                    builder = builder.Append($" Genre:[{Genre}]");
                }

                return builder.ToString().Trim();
            }
        }
    }
}
