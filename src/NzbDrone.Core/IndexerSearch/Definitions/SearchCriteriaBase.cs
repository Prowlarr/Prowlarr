using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public abstract class SearchCriteriaBase
    {
        private static readonly Regex SpecialCharacter = new Regex(@"[`'.]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex NonWord = new Regex(@"[\W]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex BeginningThe = new Regex(@"^the\s", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public virtual bool InteractiveSearch { get; set; }
        public List<int> IndexerIds { get; set; }
        public string SearchTerm { get; set; }
        public int[] Categories { get; set; }
        public string SearchType { get; set; }
        public int? Limit { get; set; }
        public int? Offset { get; set; }
        public string Source { get; set; }
    }
}
