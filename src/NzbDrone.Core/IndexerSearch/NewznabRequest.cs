using System.Text.RegularExpressions;

namespace NzbDrone.Core.IndexerSearch
{
    public class NewznabRequest
    {
        private static readonly Regex TvRegex = new Regex(@"\{((?:imdbid\:)(?<imdbid>[^{]+)|(?:tvdbid\:)(?<tvdbid>[^{]+)|(?:tmdbid\:)(?<tmdbid>[^{]+)|(?:doubanid\:)(?<doubanid>[^{]+)|(?:season\:)(?<season>[^{]+)|(?:episode\:)(?<episode>[^{]+))\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex MovieRegex = new Regex(@"\{((?:imdbid\:)(?<imdbid>[^{]+)|(?:doubanid\:)(?<doubanid>[^{]+)|(?:tmdbid\:)(?<tmdbid>[^{]+))\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex MusicRegex = new Regex(@"\{((?:artist\:)(?<artist>[^{]+)|(?:album\:)(?<album>[^{]+)|(?:track\:)(?<track>[^{]+)|(?:label\:)(?<label>[^{]+))\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex BookRegex = new Regex(@"\{((?:author\:)(?<author>[^{]+)|(?:publisher\:)(?<publisher>[^{]+)|(?:title\:)(?<title>[^{]+))\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string t { get; set; }
        public string q { get; set; }
        public string cat { get; set; }
        public string imdbid { get; set; }
        public int? tmdbid { get; set; }
        public string extended { get; set; }
        public int? limit { get; set; }
        public int? offset { get; set; }
        public int? rid { get; set; }
        public int? tvmazeid { get; set; }
        public int? traktid { get; set; }
        public int? tvdbid { get; set; }
        public int? doubanid { get; set; }
        public int? season { get; set; }
        public string ep { get; set; }
        public string album { get; set; }
        public string artist { get; set; }
        public string label { get; set; }
        public string track { get; set; }
        public int? year { get; set; }
        public string genre { get; set; }
        public string author { get; set; }
        public string title { get; set; }
        public string publisher { get; set; }
        public string configured { get; set; }
        public string source { get; set; }
        public string host { get; set; }
        public string server { get; set; }

        public void QueryToParams()
        {
            if (t == "tvsearch")
            {
                var matches = TvRegex.Matches(q);

                foreach (Match match in matches)
                {
                    if (match.Groups["tvdbid"].Success)
                    {
                        tvdbid = int.TryParse(match.Groups["tvdbid"].Value, out var tvdb) ? tvdb : null;
                    }

                    if (match.Groups["tmdbid"].Success)
                    {
                        tmdbid = int.TryParse(match.Groups["tmdbid"].Value, out var tmdb) ? tmdb : null;
                    }

                    if (match.Groups["doubanid"].Success)
                    {
                        doubanid = int.TryParse(match.Groups["doubanid"].Value, out var tmdb) ? tmdb : null;
                    }

                    if (match.Groups["season"].Success)
                    {
                        season = int.TryParse(match.Groups["season"].Value, out var seasonParsed) ? seasonParsed : null;
                    }

                    if (match.Groups["imdbid"].Success)
                    {
                        imdbid = match.Groups["imdbid"].Value;
                    }

                    if (match.Groups["episode"].Success)
                    {
                        ep = match.Groups["episode"].Value;
                    }

                    q = q.Replace(match.Value, "");
                }
            }

            if (t == "movie")
            {
                var matches = MovieRegex.Matches(q);

                foreach (Match match in matches)
                {
                    if (match.Groups["tmdbid"].Success)
                    {
                        tmdbid = int.TryParse(match.Groups["tmdbid"].Value, out var tmdb) ? tmdb : null;
                    }

                    if (match.Groups["doubanid"].Success)
                    {
                        doubanid = int.TryParse(match.Groups["doubanid"].Value, out var tmdb) ? tmdb : null;
                    }

                    if (match.Groups["imdbid"].Success)
                    {
                        imdbid = match.Groups["imdbid"].Value;
                    }

                    q = q.Replace(match.Value, "").Trim();
                }
            }

            if (t == "music")
            {
                var matches = MusicRegex.Matches(q);

                foreach (Match match in matches)
                {
                    if (match.Groups["artist"].Success)
                    {
                        artist = match.Groups["artist"].Value;
                    }

                    if (match.Groups["album"].Success)
                    {
                        album = match.Groups["album"].Value;
                    }

                    if (match.Groups["track"].Success)
                    {
                        track = match.Groups["track"].Value;
                    }

                    if (match.Groups["label"].Success)
                    {
                        label = match.Groups["label"].Value;
                    }

                    q = q.Replace(match.Value, "").Trim();
                }
            }

            if (t == "book")
            {
                var matches = BookRegex.Matches(q);

                foreach (Match match in matches)
                {
                    if (match.Groups["author"].Success)
                    {
                        author = match.Groups["author"].Value;
                    }

                    if (match.Groups["title"].Success)
                    {
                        title = match.Groups["title"].Value;
                    }

                    if (match.Groups["publisher"].Success)
                    {
                        publisher = match.Groups["publisher"].Value;
                    }

                    q = q.Replace(match.Value, "").Trim();
                }
            }
        }
    }
}
