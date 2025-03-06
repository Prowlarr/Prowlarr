using System.Text;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class BasicSearchCriteria : SearchCriteriaBase
    {
        public int? Year { get; set; }

        public override string SearchQuery
        {
            get
            {
                var searchQueryTerm = $"Term: []";
                if (SearchTerm.IsNotNullOrWhiteSpace())
                {
                    searchQueryTerm = $"Term: [{SearchTerm}]";
                }

                var builder = new StringBuilder(searchQueryTerm);
                if (Year.HasValue)
                {
                    builder = builder.Append($" Year:[{Year}]");
                }

                return builder.ToString().Trim();
            }
        }
    }
}
