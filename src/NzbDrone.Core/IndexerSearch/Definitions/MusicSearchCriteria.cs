using System.Text.RegularExpressions;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class MusicSearchCriteria : SearchCriteriaBase
    {
        private static readonly Regex VariousArtists = new (@"\bVarious Artists\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex BeginningThe = new (@"^the\s", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public string Album { get; set; }
        public string Artist { get; set; }
        public string Label { get; set; }
        public string Genre { get; set; }
        public string Track { get; set; }
        public int? Year { get; set; }

        public string SanitizedAlbum => GetSanitizedTerm(Album);
        public string SanitizedArtist => GetSanitizedTerm(Artist);
        public string SanitizedLabel => GetSanitizedTerm(Label);
        public string SanitizedGenre => GetSanitizedTerm(Genre);
        public string SanitizedTrack => GetSanitizedTerm(Track);

        public override bool RssSearch => SearchTerm.IsNullOrWhiteSpace() && Album.IsNullOrWhiteSpace() && Artist.IsNullOrWhiteSpace() && Label.IsNullOrWhiteSpace();

        protected override string GetSanitizedTerm(string term)
        {
            term ??= "";

            // Most VA albums are listed as VA, not Various Artists
            term = VariousArtists.Replace(term, "VA");

            term = BeginningThe.Replace(term, string.Empty);

            return base.GetSanitizedTerm(term);
        }
    }
}
