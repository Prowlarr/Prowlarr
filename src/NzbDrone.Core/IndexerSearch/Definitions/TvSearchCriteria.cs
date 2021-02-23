using System;
using System.Globalization;
using NzbDrone.Core.Parser;

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

        public string EpisodeSearchString => GetEpisodeSearchString();

        private string GetEpisodeSearchString()
        {
            if (Season == 0)
            {
                return string.Empty;
            }

            string episodeString;
            if (DateTime.TryParseExact(string.Format("{0} {1}", Season, Ep), "yyyy MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var showDate))
            {
                episodeString = showDate.ToString("yyyy.MM.dd");
            }
            else if (!Ep.HasValue || Ep == 0)
            {
                episodeString = string.Format("S{0:00}", Season);
            }
            else
            {
                try
                {
                    episodeString = string.Format("S{0:00}E{1:00}", Season, Ep);
                }
                catch (FormatException)
                {
                    episodeString = string.Format("S{0:00}E{1}", Season, Ep);
                }
            }

            return episodeString;
        }
    }
}
