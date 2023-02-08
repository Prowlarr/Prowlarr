using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class MusicSearchCriteria : SearchCriteriaBase
    {
        public string Album { get; set; }
        public string Artist { get; set; }
        public string Label { get; set; }
        public string Genre { get; set; }
        public string Track { get; set; }
        public int? Year { get; set; }

        public override bool RssSearch => SearchTerm.IsNullOrWhiteSpace() && Album.IsNullOrWhiteSpace() && Artist.IsNullOrWhiteSpace() && Label.IsNullOrWhiteSpace();
    }
}
