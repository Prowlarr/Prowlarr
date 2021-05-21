using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace NzbDrone.Core.Indexers
{
    public enum TvSearchParam
    {
        Q,
        Season,
        Ep,
        ImdbId,
        TvdbId,
        RId,
        TvMazeId
    }

    public enum MovieSearchParam
    {
        Q,
        ImdbId,
        TmdbId,
        ImdbTitle,
        ImdbYear,
        TraktId,
    }

    public enum MusicSearchParam
    {
        Q,
        Album,
        Artist,
        Label,
        Year
    }

    public enum SearchParam
    {
        Q
    }

    public enum BookSearchParam
    {
        Q,
        Title,
        Author
    }

    public class IndexerCapabilities
    {
        public int? LimitsMax { get; set; }
        public int? LimitsDefault { get; set; }

        public List<SearchParam> SearchParams;
        public bool SearchAvailable => SearchParams.Count > 0;

        public List<TvSearchParam> TvSearchParams;
        public bool TvSearchAvailable => TvSearchParams.Count > 0;
        public bool TvSearchSeasonAvailable => TvSearchParams.Contains(TvSearchParam.Season);
        public bool TvSearchEpAvailable => TvSearchParams.Contains(TvSearchParam.Ep);
        public bool TvSearchImdbAvailable => TvSearchParams.Contains(TvSearchParam.ImdbId);
        public bool TvSearchTvdbAvailable => TvSearchParams.Contains(TvSearchParam.TvdbId);
        public bool TvSearchTvRageAvailable => TvSearchParams.Contains(TvSearchParam.RId);
        public bool TvSearchTvMazeAvailable => TvSearchParams.Contains(TvSearchParam.TvMazeId);

        public List<MovieSearchParam> MovieSearchParams;
        public bool MovieSearchAvailable => MovieSearchParams.Count > 0;
        public bool MovieSearchImdbAvailable => MovieSearchParams.Contains(MovieSearchParam.ImdbId);
        public bool MovieSearchTmdbAvailable => MovieSearchParams.Contains(MovieSearchParam.TmdbId);
        public bool MovieSearchTraktAvailable => MovieSearchParams.Contains(MovieSearchParam.TraktId);

        public List<MusicSearchParam> MusicSearchParams;
        public bool MusicSearchAvailable => MusicSearchParams.Count > 0;
        public bool MusicSearchAlbumAvailable => MusicSearchParams.Contains(MusicSearchParam.Album);
        public bool MusicSearchArtistAvailable => MusicSearchParams.Contains(MusicSearchParam.Artist);
        public bool MusicSearchLabelAvailable => MusicSearchParams.Contains(MusicSearchParam.Label);
        public bool MusicSearchYearAvailable => MusicSearchParams.Contains(MusicSearchParam.Year);

        public List<BookSearchParam> BookSearchParams;
        public bool BookSearchAvailable => BookSearchParams.Count > 0;
        public bool BookSearchTitleAvailable => BookSearchParams.Contains(BookSearchParam.Title);
        public bool BookSearchAuthorAvailable => BookSearchParams.Contains(BookSearchParam.Author);

        public readonly IndexerCapabilitiesCategories Categories;
        public List<IndexerFlag> Flags;

        public IndexerCapabilities()
        {
            SearchParams = new List<SearchParam> { SearchParam.Q };
            TvSearchParams = new List<TvSearchParam>();
            MovieSearchParams = new List<MovieSearchParam>();
            MusicSearchParams = new List<MusicSearchParam>();
            BookSearchParams = new List<BookSearchParam>();
            Categories = new IndexerCapabilitiesCategories();
            Flags = new List<IndexerFlag>();
            LimitsDefault = 100;
            LimitsMax = 100;
        }

        public void ParseCardigannSearchModes(Dictionary<string, List<string>> modes)
        {
            if (modes == null || !modes.Any())
            {
                throw new Exception("At least one search mode is required");
            }

            if (!modes.ContainsKey("search"))
            {
                throw new Exception("The search mode 'search' is mandatory");
            }

            foreach (var entry in modes)
            {
                switch (entry.Key)
                {
                    case "search":
                        if (entry.Value == null || entry.Value.Count != 1 || entry.Value[0] != "q")
                        {
                            throw new Exception("In search mode 'search' only 'q' parameter is supported and it's mandatory");
                        }

                        SearchParams.Add(SearchParam.Q);
                        break;
                    case "tv-search":
                        ParseTvSearchParams(entry.Value);
                        break;
                    case "movie-search":
                        ParseMovieSearchParams(entry.Value);
                        break;
                    case "music-search":
                        ParseMusicSearchParams(entry.Value);
                        break;
                    case "book-search":
                        ParseBookSearchParams(entry.Value);
                        break;
                    default:
                        throw new Exception($"Unsupported search mode: {entry.Key}");
                }
            }
        }

        public void ParseTvSearchParams(IEnumerable<string> paramsList)
        {
            if (paramsList == null)
            {
                return;
            }

            foreach (var paramStr in paramsList)
            {
                if (Enum.TryParse(paramStr, true, out TvSearchParam param))
                {
                    if (!TvSearchParams.Contains(param))
                    {
                        TvSearchParams.Add(param);
                    }
                    else
                    {
                        throw new Exception($"Duplicate tv-search param: {paramStr}");
                    }
                }
                else
                {
                    throw new Exception($"Not supported tv-search param: {paramStr}");
                }
            }
        }

        public void ParseMovieSearchParams(IEnumerable<string> paramsList)
        {
            if (paramsList == null)
            {
                return;
            }

            foreach (var paramStr in paramsList)
            {
                if (Enum.TryParse(paramStr, true, out MovieSearchParam param))
                {
                    if (!MovieSearchParams.Contains(param))
                    {
                        MovieSearchParams.Add(param);
                    }
                    else
                    {
                        throw new Exception($"Duplicate movie-search param: {paramStr}");
                    }
                }
                else
                {
                    throw new Exception($"Not supported movie-search param: {paramStr}");
                }
            }
        }

        public void ParseMusicSearchParams(IEnumerable<string> paramsList)
        {
            if (paramsList == null)
            {
                return;
            }

            foreach (var paramStr in paramsList)
            {
                if (Enum.TryParse(paramStr, true, out MusicSearchParam param))
                {
                    if (!MusicSearchParams.Contains(param))
                    {
                        MusicSearchParams.Add(param);
                    }
                    else
                    {
                        throw new Exception($"Duplicate music-search param: {paramStr}");
                    }
                }
                else
                {
                    throw new Exception($"Not supported Music-search param: {paramStr}");
                }
            }
        }

        private void ParseBookSearchParams(IEnumerable<string> paramsList)
        {
            if (paramsList == null)
            {
                return;
            }

            foreach (var paramStr in paramsList)
            {
                if (Enum.TryParse(paramStr, true, out BookSearchParam param))
                {
                    if (!BookSearchParams.Contains(param))
                    {
                        BookSearchParams.Add(param);
                    }
                    else
                    {
                        throw new Exception($"Duplicate book-search param: {paramStr}");
                    }
                }
                else
                {
                    throw new Exception($"Not supported book-search param: {paramStr}");
                }
            }
        }

        private string SupportedTvSearchParams()
        {
            var parameters = new List<string> { "q" }; // q is always enabled
            if (TvSearchSeasonAvailable)
            {
                parameters.Add("season");
            }

            if (TvSearchEpAvailable)
            {
                parameters.Add("ep");
            }

            if (TvSearchImdbAvailable)
            {
                parameters.Add("imdbid");
            }

            if (TvSearchTvdbAvailable)
            {
                parameters.Add("tvdbid");
            }

            if (TvSearchTvRageAvailable)
            {
                parameters.Add("rid");
            }

            if (TvSearchTvMazeAvailable)
            {
                parameters.Add("tvmazeid");
            }

            return string.Join(",", parameters);
        }

        private string SupportedSearchParams()
        {
            var parameters = new List<string> { "q" }; // q is always enabled

            return string.Join(",", parameters);
        }

        private string SupportedMovieSearchParams()
        {
            var parameters = new List<string> { "q" }; // q is always enabled
            if (MovieSearchImdbAvailable)
            {
                parameters.Add("imdbid");
            }

            if (MovieSearchTmdbAvailable)
            {
                parameters.Add("tmdbid");
            }

            return string.Join(",", parameters);
        }

        private string SupportedMusicSearchParams()
        {
            var parameters = new List<string> { "q" }; // q is always enabled
            if (MusicSearchAlbumAvailable)
            {
                parameters.Add("album");
            }

            if (MusicSearchArtistAvailable)
            {
                parameters.Add("artist");
            }

            if (MusicSearchLabelAvailable)
            {
                parameters.Add("label");
            }

            if (MusicSearchYearAvailable)
            {
                parameters.Add("year");
            }

            return string.Join(",", parameters);
        }

        private string SupportedBookSearchParams()
        {
            var parameters = new List<string> { "q" }; // q is always enabled
            if (BookSearchTitleAvailable)
            {
                parameters.Add("title");
            }

            if (BookSearchAuthorAvailable)
            {
                parameters.Add("author");
            }

            return string.Join(",", parameters);
        }

        public XDocument GetXDocument()
        {
            var xdoc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("caps",
                    new XElement("server",
                        new XAttribute("title", "Prowlarr")),
                    LimitsMax != null || LimitsDefault != null ?
                        new XElement("limits",
                            LimitsMax != null ? new XAttribute("max", LimitsMax) : null,
                            LimitsDefault != null ? new XAttribute("default", LimitsDefault) : null)
                    : null,
                    new XElement("searching",
                        new XElement("search",
                            new XAttribute("available", SearchAvailable ? "yes" : "no"),
                            new XAttribute("supportedParams", SupportedSearchParams())),
                        new XElement("tv-search",
                            new XAttribute("available", TvSearchAvailable ? "yes" : "no"),
                            new XAttribute("supportedParams", SupportedTvSearchParams())),
                        new XElement("movie-search",
                            new XAttribute("available", MovieSearchAvailable ? "yes" : "no"),
                            new XAttribute("supportedParams", SupportedMovieSearchParams())),
                        new XElement("music-search",
                            new XAttribute("available", MusicSearchAvailable ? "yes" : "no"),
                            new XAttribute("supportedParams", SupportedMusicSearchParams())),
                        new XElement("audio-search",
                            new XAttribute("available", MusicSearchAvailable ? "yes" : "no"),
                            new XAttribute("supportedParams", SupportedMusicSearchParams())),
                        new XElement("book-search",
                            new XAttribute("available", BookSearchAvailable ? "yes" : "no"),
                            new XAttribute("supportedParams", SupportedBookSearchParams()))),
                    new XElement("categories",
                        from c in Categories.GetTorznabCategoryTree(true)
                        select new XElement("category",
                            new XAttribute("id", c.Id),
                            new XAttribute("name", c.Name),
                            from sc in c.SubCategories
                            select new XElement("subcat",
                                new XAttribute("id", sc.Id),
                                new XAttribute("name", sc.Name)))),
                    new XElement("tags",
                        from c in Flags
                        select new XElement("tag",
                            new XAttribute("name", c.Name),
                            new XAttribute("description", c.Description)))));
            return xdoc;
        }

        public string ToXml() =>
            GetXDocument().Declaration + Environment.NewLine + GetXDocument();

        public static IndexerCapabilities Concat(IndexerCapabilities left, IndexerCapabilities right)
        {
            left.SearchParams = left.SearchParams.Union(right.SearchParams).ToList();
            left.TvSearchParams = left.TvSearchParams.Union(right.TvSearchParams).ToList();
            left.MovieSearchParams = left.MovieSearchParams.Union(right.MovieSearchParams).ToList();
            left.MusicSearchParams = left.MusicSearchParams.Union(right.MusicSearchParams).ToList();
            left.BookSearchParams = left.BookSearchParams.Union(right.BookSearchParams).ToList();
            left.Categories.Concat(right.Categories);
            return left;
        }
    }
}
