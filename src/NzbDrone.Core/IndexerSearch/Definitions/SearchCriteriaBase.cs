using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public abstract class SearchCriteriaBase
    {
        private static readonly Regex CleanDashesRegex = new (@"\p{Pd}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CleanSingleQuotesRegex = new (@"[\u0060\u00B4\u2018\u2019]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public virtual bool InteractiveSearch { get; set; }
        public List<int> IndexerIds { get; set; }
        public string SearchTerm { get; set; }
        public int[] Categories { get; set; }
        public string SearchType { get; set; }
        public int? Limit { get; set; }
        public int? Offset { get; set; }
        public string Source { get; set; }
        public string Host { get; set; }

        public override string ToString() => $"{SearchQuery}, Offset: {Offset ?? 0}, Limit: {Limit ?? 0}, Categories: [{string.Join(", ", Categories)}]";

        public virtual string SearchQuery => $"Term: [{SearchTerm}]";

        public virtual bool RssSearch => SearchTerm.IsNullOrWhiteSpace();

        public string SanitizedSearchTerm => GetSanitizedTerm(SearchTerm);

        protected virtual string GetSanitizedTerm(string term)
        {
            term ??= "";

            term = CleanDashesRegex.Replace(term, "-");
            term = CleanSingleQuotesRegex.Replace(term, "'");

            var safeTitle = term.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || c is '-' or '.' or '_' or '(' or ')' or '@' or '/' or '\'' or '[' or ']' or '+' or '%');

            return string.Concat(safeTitle);
        }
    }
}
