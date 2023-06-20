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

        public override bool IsRssSearch =>
            SearchTerm.IsNullOrWhiteSpace() &&
            !IsIdSearch;

        public override bool IsIdSearch =>
            Album.IsNotNullOrWhiteSpace() ||
            Artist.IsNotNullOrWhiteSpace() ||
            Label.IsNotNullOrWhiteSpace() ||
            Genre.IsNotNullOrWhiteSpace() ||
            Track.IsNotNullOrWhiteSpace() ||
            Year.HasValue;
    }
}
