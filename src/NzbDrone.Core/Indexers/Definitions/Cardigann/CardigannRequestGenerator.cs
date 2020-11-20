using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.IndexerSearch.Definitions;

namespace NzbDrone.Core.Indexers.Cardigann
{
    public class CardigannRequestGenerator : CardigannBase, IIndexerRequestGenerator
    {
        public CardigannRequestGenerator(CardigannDefinition definition,
                                         CardigannSettings settings,
                                         Logger logger)
        : base(definition, settings, logger)
        {
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            _logger.Trace("Getting search");

            var pageableRequests = new IndexerPageableRequestChain();

            var variables = GetQueryVariableDefaults(searchCriteria);

            variables[".Query.Movie"] = null;
            variables[".Query.Year"] = null;
            variables[".Query.IMDBID"] = searchCriteria.ImdbId;
            variables[".Query.IMDBIDShort"] = searchCriteria.ImdbId?.TrimStart('t') ?? null;
            variables[".Query.TMDBID"] = searchCriteria.TmdbId;
            variables[".Query.TraktID"] = searchCriteria.TraktId;

            pageableRequests.Add(GetRequest(variables));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var variables = GetQueryVariableDefaults(searchCriteria);

            variables[".Query.Album"] = searchCriteria.Album;
            variables[".Query.Artist"] = searchCriteria.Artist;
            variables[".Query.Label"] = searchCriteria.Label;
            variables[".Query.Track"] = null;

            pageableRequests.Add(GetRequest(variables));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var variables = GetQueryVariableDefaults(searchCriteria);

            variables[".Query.Series"] = null;
            variables[".Query.Ep"] = searchCriteria.Ep;
            variables[".Query.Season"] = searchCriteria.Season;
            variables[".Query.IMDBID"] = searchCriteria.ImdbId;
            variables[".Query.IMDBIDShort"] = searchCriteria.ImdbId.Replace("tt", "");
            variables[".Query.TVDBID"] = searchCriteria.TvdbId;
            variables[".Query.TVRageID"] = searchCriteria.RId;
            variables[".Query.TVMazeID"] = searchCriteria.TvMazeId;
            variables[".Query.TraktID"] = searchCriteria.TraktId;

            pageableRequests.Add(GetRequest(variables));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var variables = GetQueryVariableDefaults(searchCriteria);

            variables[".Query.Author"] = null;
            variables[".Query.Title"] = null;

            pageableRequests.Add(GetRequest(variables));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var variables = GetQueryVariableDefaults(searchCriteria);

            pageableRequests.Add(GetRequest(variables));

            return pageableRequests;
        }

        private Dictionary<string, object> GetQueryVariableDefaults(SearchCriteriaBase searchCriteria)
        {
            var variables = GetBaseTemplateVariables();

            variables[".Query.Type"] = searchCriteria.SearchType;
            variables[".Query.Q"] = searchCriteria.SearchTerm;
            variables[".Query.Categories"] = searchCriteria.Categories;
            variables[".Query.Limit"] = searchCriteria.Limit;
            variables[".Query.Offset"] = searchCriteria.Offset;
            variables[".Query.Extended"] = null;
            variables[".Query.APIKey"] = null;

            //Movie
            variables[".Query.Movie"] = null;
            variables[".Query.Year"] = null;
            variables[".Query.IMDBID"] = null;
            variables[".Query.IMDBIDShort"] = null;
            variables[".Query.TMDBID"] = null;

            //Tv
            variables[".Query.Series"] = null;
            variables[".Query.Ep"] = null;
            variables[".Query.Season"] = null;
            variables[".Query.TVDBID"] = null;
            variables[".Query.TVRageID"] = null;
            variables[".Query.TVMazeID"] = null;
            variables[".Query.TraktID"] = null;

            //Music
            variables[".Query.Album"] = null;
            variables[".Query.Artist"] = null;
            variables[".Query.Label"] = null;
            variables[".Query.Track"] = null;
            variables[".Query.Episode"] = null;

            //Book
            variables[".Query.Author"] = null;
            variables[".Query.Title"] = null;

            return variables;
        }

        private IEnumerable<IndexerRequest> GetRequest(Dictionary<string, object> variables)
        {
            var search = _definition.Search;

            var mappedCategories = MapTorznabCapsToTrackers((int[])variables[".Query.Categories"]);
            if (mappedCategories.Count == 0)
            {
                mappedCategories = _defaultCategories;
            }

            variables[".Categories"] = mappedCategories;

            var keywordTokens = new List<string>();
            var keywordTokenKeys = new List<string> { "Q", "Series", "Movie", "Year" };
            foreach (var key in keywordTokenKeys)
            {
                var value = (string)variables[".Query." + key];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    keywordTokens.Add(value);
                }
            }

            if (!string.IsNullOrWhiteSpace((string)variables[".Query.Episode"]))
            {
                keywordTokens.Add((string)variables[".Query.Episode"]);
            }

            variables[".Query.Keywords"] = string.Join(" ", keywordTokens);
            variables[".Keywords"] = ApplyFilters((string)variables[".Query.Keywords"], search.Keywordsfilters);

            // TODO: prepare queries first and then send them parallel
            var searchPaths = search.Paths;
            foreach (var searchPath in searchPaths)
            {
                // skip path if categories don't match
                if (searchPath.Categories != null && mappedCategories.Count > 0)
                {
                    var invertMatch = searchPath.Categories[0] == "!";
                    var hasIntersect = mappedCategories.Intersect(searchPath.Categories).Any();
                    if (invertMatch)
                    {
                        hasIntersect = !hasIntersect;
                    }

                    if (!hasIntersect)
                    {
                        continue;
                    }
                }

                // build search URL
                // HttpUtility.UrlPathEncode seems to only encode spaces, we use UrlEncode and replace + with %20 as a workaround
                var searchUrl = ResolvePath(ApplyGoTemplateText(searchPath.Path, variables, WebUtility.UrlEncode).Replace("+", "%20")).AbsoluteUri;
                var queryCollection = new List<KeyValuePair<string, string>>();
                var method = HttpMethod.GET;

                if (string.Equals(searchPath.Method, "post", StringComparison.OrdinalIgnoreCase))
                {
                    method = HttpMethod.POST;
                }

                var inputsList = new List<Dictionary<string, string>>();
                if (searchPath.Inheritinputs)
                {
                    inputsList.Add(search.Inputs);
                }

                inputsList.Add(searchPath.Inputs);

                foreach (var inputs in inputsList)
                {
                    if (inputs != null)
                    {
                        foreach (var input in inputs)
                        {
                            if (input.Key == "$raw")
                            {
                                var rawStr = ApplyGoTemplateText(input.Value, variables, WebUtility.UrlEncode);
                                foreach (var part in rawStr.Split('&'))
                                {
                                    var parts = part.Split(new char[] { '=' }, 2);
                                    var key = parts[0];
                                    if (key.Length == 0)
                                    {
                                        continue;
                                    }

                                    var value = "";
                                    if (parts.Length == 2)
                                    {
                                        value = parts[1];
                                    }

                                    queryCollection.Add(key, value);
                                }
                            }
                            else
                            {
                                queryCollection.Add(input.Key, ApplyGoTemplateText(input.Value, variables));
                            }
                        }
                    }
                }

                if (method == HttpMethod.GET)
                {
                    if (queryCollection.Count > 0)
                    {
                        searchUrl += "?" + queryCollection.GetQueryString(_encoding);
                    }
                }

                _logger.Info($"Adding request: {searchUrl}");

                var request = new CardigannRequest(searchUrl, HttpAccept.Html, variables);

                // send HTTP request
                if (search.Headers != null)
                {
                    foreach (var header in search.Headers)
                    {
                        request.HttpRequest.Headers.Add(header.Key, header.Value[0]);
                    }
                }

                request.HttpRequest.Method = method;

                yield return request;
            }
        }
    }
}
