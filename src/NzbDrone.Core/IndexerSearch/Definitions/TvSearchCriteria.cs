using System;
using System.Globalization;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class TvSearchCriteria : SearchCriteriaBase
    {
        public int? Season { get; set; }
        public string Episode { get; set; }

        public string ImdbId { get; set; }
        public int? TvdbId { get; set; }
        public int? RId { get; set; }
        public int? TvMazeId { get; set; }
        public int? TraktId { get; set; }

        public string SanitizedTvSearchString => (SanitizedSearchTerm + " " + EpisodeSearchString).Trim();
        public string EpisodeSearchString => GetEpisodeSearchString();

        public string FullImdbId => ParseUtil.GetFullImdbId(ImdbId);

        public override bool RssSearch
        {
            get
            {
                if (SearchTerm.IsNullOrWhiteSpace() && ImdbId.IsNullOrWhiteSpace() && !TvdbId.HasValue && !RId.HasValue && !TraktId.HasValue && !TvMazeId.HasValue)
                {
                    return true;
                }

                return false;
            }
        }

        private string GetEpisodeSearchString()
        {
            if (Season == null || Season == 0)
            {
                return string.Empty;
            }

            string episodeString;
            if (DateTime.TryParseExact(string.Format("{0} {1}", Season, Episode), "yyyy MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var showDate))
            {
                episodeString = showDate.ToString("yyyy.MM.dd");
            }
            else if (Episode.IsNullOrWhiteSpace())
            {
                episodeString = string.Format("S{0:00}", Season);
            }
            else
            {
                try
                {
                    episodeString = string.Format("S{0:00}E{1:00}", Season, ParseUtil.CoerceInt(Episode));
                }
                catch (FormatException)
                {
                    episodeString = string.Format("S{0:00}E{1}", Season, Episode);
                }
            }

            return episodeString;
        }
    }
}
