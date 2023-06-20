using System;
using System.Globalization;
using System.Text;
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
        public int? TmdbId { get; set; }
        public int? DoubanId { get; set; }
        public int? Year { get; set; }
        public string Genre { get; set; }

        public string SanitizedTvSearchString => $"{SanitizedSearchTerm} {EpisodeSearchString}".Trim();
        public string EpisodeSearchString => GetEpisodeSearchString();

        public string FullImdbId => ParseUtil.GetFullImdbId(ImdbId);

        public override bool IsRssSearch =>
            SearchTerm.IsNullOrWhiteSpace() &&
            !IsIdSearch;

        public override bool IsIdSearch =>
            Episode.IsNotNullOrWhiteSpace() ||
            ImdbId.IsNotNullOrWhiteSpace() ||
            Season.HasValue ||
            TvdbId.HasValue ||
            RId.HasValue ||
            TraktId.HasValue ||
            TvMazeId.HasValue ||
            TmdbId.HasValue ||
            DoubanId.HasValue;

        public override string SearchQuery
        {
            get
            {
                var searchQueryTerm = $"Term: []";
                var searchEpisodeTerm = $" for Season / Episode:[{EpisodeSearchString}]";
                if (SearchTerm.IsNotNullOrWhiteSpace())
                {
                    searchQueryTerm = $"Term: [{SearchTerm}]";
                }

                if (!ImdbId.IsNotNullOrWhiteSpace() && !TvdbId.HasValue && !RId.HasValue && !TraktId.HasValue)
                {
                    return $"{searchQueryTerm}{searchEpisodeTerm}";
                }

                var builder = new StringBuilder(searchQueryTerm);
                builder = builder.Append(" | ID(s):");

                if (ImdbId.IsNotNullOrWhiteSpace())
                {
                    builder.Append($" IMDbId:[{ImdbId}]");
                }

                if (TvdbId.HasValue)
                {
                    builder.Append($" TVDbId:[{TvdbId}]");
                }

                if (RId.HasValue)
                {
                    builder.Append($" TVRageId:[{RId}]");
                }

                if (TraktId.HasValue)
                {
                    builder.Append($" TraktId:[{TraktId}]");
                }

                if (TmdbId.HasValue)
                {
                    builder.Append($" TmdbId:[{TmdbId}]");
                }

                builder = builder.Append(searchEpisodeTerm);
                return builder.ToString().Trim();
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
