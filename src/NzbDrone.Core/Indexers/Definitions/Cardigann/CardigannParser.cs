using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Cardigann
{
    public class CardigannParser : CardigannBase, IParseIndexerResponse
    {
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public CardigannParser(IConfigService configService,
                               CardigannDefinition definition,
                               Logger logger)
        : base(configService, definition, logger)
        {
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releases = new List<CardigannReleaseInfo>();

            _logger.Debug("Parsing");

            var indexerLogging = _configService.LogIndexerResponse;

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                // Remove cookie cache
                if (indexerResponse.HttpResponse.HasHttpRedirect && indexerResponse.HttpResponse.Headers["Location"]
                        .ContainsIgnoreCase("login.php"))
                {
                    CookiesUpdater(null, null);
                    throw new IndexerException(indexerResponse, "We are being redirected to the PTP login page. Most likely your session expired or was killed. Try testing the indexer in the settings.");
                }

                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from API request");
            }

            var results = indexerResponse.Content;
            var request = indexerResponse.Request as CardigannRequest;
            var variables = request.Variables;
            var search = _definition.Search;

            var searchUrlUri = new Uri(request.Url.FullUri);

            try
            {
                var searchResultParser = new HtmlParser();
                var searchResultDocument = searchResultParser.ParseDocument(results);

                /* checkForError(response, Definition.Search.Error); */

                if (search.Preprocessingfilters != null)
                {
                    results = ApplyFilters(results, search.Preprocessingfilters, variables);
                    searchResultDocument = searchResultParser.ParseDocument(results);
                    _logger.Trace(string.Format("CardigannIndexer ({0}): result after preprocessingfilters: {1}", _definition.Id, results));
                }

                var rowsSelector = ApplyGoTemplateText(search.Rows.Selector, variables);
                var rowsDom = searchResultDocument.QuerySelectorAll(rowsSelector);
                var rows = new List<IElement>();
                foreach (var rowDom in rowsDom)
                {
                    rows.Add(rowDom);
                }

                // merge following rows for After selector
                var after = search.Rows.After;
                if (after > 0)
                {
                    for (var i = 0; i < rows.Count; i += 1)
                    {
                        var currentRow = rows[i];
                        for (var j = 0; j < after; j += 1)
                        {
                            var mergeRowIndex = i + j + 1;
                            var mergeRow = rows[mergeRowIndex];
                            var mergeNodes = new List<INode>();
                            foreach (var node in mergeRow.ChildNodes)
                            {
                                mergeNodes.Add(node);
                            }

                            currentRow.Append(mergeNodes.ToArray());
                        }

                        rows.RemoveRange(i + 1, after);
                    }
                }

                foreach (var row in rows)
                {
                    try
                    {
                        var release = new CardigannReleaseInfo();

                        // Parse fields
                        foreach (var field in search.Fields)
                        {
                            var fieldParts = field.Key.Split('|');
                            var fieldName = fieldParts[0];
                            var fieldModifiers = new List<string>();
                            for (var i = 1; i < fieldParts.Length; i++)
                            {
                                fieldModifiers.Add(fieldParts[i]);
                            }

                            string value = null;
                            var variablesKey = ".Result." + fieldName;
                            try
                            {
                                value = HandleSelector(field.Value, row, variables);
                                switch (fieldName)
                                {
                                    case "download":
                                        if (string.IsNullOrEmpty(value))
                                        {
                                            value = null;
                                            release.Link = null;
                                            break;
                                        }

                                        if (value.StartsWith("magnet:"))
                                        {
                                            release.MagnetUri = new Uri(value);
                                            value = release.MagnetUri.ToString();
                                        }
                                        else
                                        {
                                            release.Link = ResolvePath(value, searchUrlUri);
                                            value = release.Link.ToString();
                                        }

                                        break;
                                    case "magnet":
                                        var magnetUri = new Uri(value);
                                        release.MagnetUri = magnetUri;
                                        value = magnetUri.ToString();
                                        if (release.Guid == null)
                                        {
                                            release.Guid = magnetUri;
                                        }

                                        break;
                                    case "details":
                                        var url = ResolvePath(value, searchUrlUri);
                                        release.Guid = url;
                                        release.Comments = url;
                                        if (release.Guid == null)
                                        {
                                            release.Guid = url;
                                        }

                                        value = url.ToString();
                                        break;
                                    case "comments":
                                        var commentsUrl = ResolvePath(value, searchUrlUri);
                                        if (release.Comments == null)
                                        {
                                            release.Comments = commentsUrl;
                                        }

                                        if (release.Guid == null)
                                        {
                                            release.Guid = commentsUrl;
                                        }

                                        value = commentsUrl.ToString();
                                        break;
                                    case "title":
                                        if (fieldModifiers.Contains("append"))
                                        {
                                            release.Title += value;
                                        }
                                        else
                                        {
                                            release.Title = value;
                                        }

                                        value = release.Title;
                                        break;
                                    case "description":
                                        if (fieldModifiers.Contains("append"))
                                        {
                                            release.Description += value;
                                        }
                                        else
                                        {
                                            release.Description = value;
                                        }

                                        value = release.Description;
                                        break;
                                    case "category":
                                        var cats = MapTrackerCatToNewznab(value);
                                        if (cats.Any())
                                        {
                                            if (release.Category == null || fieldModifiers.Contains("noappend"))
                                            {
                                                release.Category = cats;
                                            }
                                            else
                                            {
                                                release.Category = release.Category.Union(cats).ToList();
                                            }
                                        }

                                        value = release.Category.ToString();
                                        break;
                                    case "size":
                                        release.Size = CardigannReleaseInfo.GetBytes(value);
                                        value = release.Size.ToString();
                                        break;
                                    case "leechers":
                                        var leechers = ParseUtil.CoerceLong(value);
                                        leechers = leechers < 5000000L ? leechers : 0; // to fix #6558
                                        if (release.Peers == null)
                                        {
                                            release.Peers = leechers;
                                        }
                                        else
                                        {
                                            release.Peers += leechers;
                                        }

                                        value = leechers.ToString();
                                        break;
                                    case "seeders":
                                        release.Seeders = ParseUtil.CoerceLong(value);
                                        release.Seeders = release.Seeders < 5000000L ? release.Seeders : 0; // to fix #6558
                                        if (release.Peers == null)
                                        {
                                            release.Peers = release.Seeders;
                                        }
                                        else
                                        {
                                            release.Peers += release.Seeders;
                                        }

                                        value = release.Seeders.ToString();
                                        break;
                                    case "date":
                                        release.PublishDate = DateTimeUtil.FromUnknown(value);
                                        value = release.PublishDate.ToString(DateTimeUtil.Rfc1123ZPattern);
                                        break;
                                    case "files":
                                        release.Files = ParseUtil.CoerceLong(value);
                                        value = release.Files.ToString();
                                        break;
                                    case "grabs":
                                        release.Grabs = ParseUtil.CoerceLong(value);
                                        value = release.Grabs.ToString();
                                        break;
                                    case "downloadvolumefactor":
                                        release.DownloadVolumeFactor = ParseUtil.CoerceDouble(value);
                                        value = release.DownloadVolumeFactor.ToString();
                                        break;
                                    case "uploadvolumefactor":
                                        release.UploadVolumeFactor = ParseUtil.CoerceDouble(value);
                                        value = release.UploadVolumeFactor.ToString();
                                        break;
                                    case "minimumratio":
                                        release.MinimumRatio = ParseUtil.CoerceDouble(value);
                                        value = release.MinimumRatio.ToString();
                                        break;
                                    case "minimumseedtime":
                                        release.MinimumSeedTime = ParseUtil.CoerceLong(value);
                                        value = release.MinimumSeedTime.ToString();
                                        break;
                                    case "imdb":
                                        release.Imdb = ParseUtil.GetLongFromString(value);
                                        value = release.Imdb.ToString();
                                        break;
                                    case "tmdbid":
                                        var tmdbIDRegEx = new Regex(@"(\d+)", RegexOptions.Compiled);
                                        var tmdbIDMatch = tmdbIDRegEx.Match(value);
                                        var tmdbID = tmdbIDMatch.Groups[1].Value;
                                        release.TMDb = ParseUtil.CoerceLong(tmdbID);
                                        value = release.TMDb.ToString();
                                        break;
                                    case "rageid":
                                        var rageIDRegEx = new Regex(@"(\d+)", RegexOptions.Compiled);
                                        var rageIDMatch = rageIDRegEx.Match(value);
                                        var rageID = rageIDMatch.Groups[1].Value;
                                        release.RageID = ParseUtil.CoerceLong(rageID);
                                        value = release.RageID.ToString();
                                        break;
                                    case "tvdbid":
                                        var tvdbIdRegEx = new Regex(@"(\d+)", RegexOptions.Compiled);
                                        var tvdbIdMatch = tvdbIdRegEx.Match(value);
                                        var tvdbId = tvdbIdMatch.Groups[1].Value;
                                        release.TVDBId = ParseUtil.CoerceLong(tvdbId);
                                        value = release.TVDBId.ToString();
                                        break;
                                    case "author":
                                        release.Author = value;
                                        break;
                                    case "booktitle":
                                        release.BookTitle = value;
                                        break;
                                    case "banner":
                                        if (!string.IsNullOrWhiteSpace(value))
                                        {
                                            var bannerurl = ResolvePath(value, searchUrlUri);
                                            release.BannerUrl = bannerurl;
                                        }

                                        value = release.BannerUrl.ToString();
                                        break;
                                    default:
                                        break;
                                }

                                variables[variablesKey] = value;
                            }
                            catch (Exception ex)
                            {
                                if (!variables.ContainsKey(variablesKey))
                                {
                                    variables[variablesKey] = null;
                                }

                                if (OptionalFields.Contains(field.Key) || fieldModifiers.Contains("optional") || field.Value.Optional)
                                {
                                    variables[variablesKey] = null;
                                    continue;
                                }

                                if (indexerLogging)
                                {
                                    _logger.Trace("Error while parsing field={0}, selector={1}, value={2}: {3}", field.Key, field.Value.Selector, value == null ? "<null>" : value, ex.Message);
                                }
                            }
                        }

                        var filters = search.Rows.Filters;
                        var skipRelease = false;
                        if (filters != null)
                        {
                            foreach (var filter in filters)
                            {
                                switch (filter.Name)
                                {
                                    case "andmatch":
                                        var characterLimit = -1;
                                        if (filter.Args != null)
                                        {
                                            characterLimit = int.Parse(filter.Args);
                                        }

                                        /*
                                        if (query.ImdbID != null && TorznabCaps.SupportsImdbMovieSearch)
                                        {
                                            break; // skip andmatch filter for imdb searches
                                        }

                                        if (query.TmdbID != null && TorznabCaps.SupportsTmdbMovieSearch)
                                        {
                                            break; // skip andmatch filter for tmdb searches
                                        }

                                        if (query.TvdbID != null && TorznabCaps.SupportsTvdbSearch)
                                        {
                                            break; // skip andmatch filter for tvdb searches
                                        }

                                        var queryKeywords = variables[".Keywords"] as string;

                                        if (!query.MatchQueryStringAND(release.Title, characterLimit, queryKeywords))
                                        {
                                            _logger.Debug(string.Format("CardigannIndexer ({0}): skipping {1} (andmatch filter)", _definition.Id, release.Title));
                                            skipRelease = true;
                                        }
                                        */

                                        break;
                                    case "strdump":
                                        // for debugging
                                        _logger.Debug(string.Format("CardigannIndexer ({0}): row strdump: {1}", _definition.Id, row.ToHtmlPretty()));
                                        break;
                                    default:
                                        _logger.Error(string.Format("CardigannIndexer ({0}): Unsupported rows filter: {1}", _definition.Id, filter.Name));
                                        break;
                                }
                            }
                        }

                        if (skipRelease)
                        {
                            continue;
                        }

                        // if DateHeaders is set go through the previous rows and look for the header selector
                        var dateHeaders = _definition.Search.Rows.Dateheaders;
                        if (release.PublishDate == DateTime.MinValue && dateHeaders != null)
                        {
                            var prevRow = row.PreviousElementSibling;
                            string value = null;
                            if (prevRow == null)
                            {
                                // continue with parent
                                var parent = row.ParentElement;
                                if (parent != null)
                                {
                                    prevRow = parent.PreviousElementSibling;
                                }
                            }

                            while (prevRow != null)
                            {
                                var curRow = prevRow;
                                _logger.Debug(prevRow.OuterHtml);
                                try
                                {
                                    value = HandleSelector(dateHeaders, curRow);
                                    break;
                                }
                                catch (Exception)
                                {
                                    // do nothing
                                }

                                prevRow = curRow.PreviousElementSibling;
                                if (prevRow == null)
                                {
                                    // continue with parent
                                    var parent = curRow.ParentElement;
                                    if (parent != null)
                                    {
                                        prevRow = parent.PreviousElementSibling;
                                    }
                                }
                            }

                            if (value == null && dateHeaders.Optional == false)
                            {
                                throw new Exception(string.Format("No date header row found for {0}", release.ToString()));
                            }

                            if (value != null)
                            {
                                release.PublishDate = DateTimeUtil.FromUnknown(value);
                            }
                        }

                        releases.Add(release);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "CardigannIndexer ({0}): Error while parsing row '{1}':\n\n{2}", _definition.Id, row.ToHtmlPretty());
                    }
                }
            }
            catch (Exception)
            {
                // OnParseError(results, ex);
                throw;
            }

            /*
            if (query.Limit > 0)
            {
                releases = releases.Take(query.Limit).ToList();
            }*/

            var result = new List<ReleaseInfo>();

            result.AddRange(releases.Select(x => new TorrentInfo
            {
                PublishDate = x.PublishDate,
                Guid = x.Guid.ToString(),
                Title = x.Title,
                Size = x.Size.Value,
                DownloadUrl = x.Link.AbsoluteUri,
                CommentUrl = x.Comments?.ToString(),
                InfoUrl = x.Link?.ToString(),
                MagnetUrl = x.MagnetUri?.AbsoluteUri,
                InfoHash = x.InfoHash,
                Seeders = (int?)x.Seeders,
                Peers = (int?)x.Peers,
                Grabs = (int?)x.Grabs,
                Files = (int?)x.Files,
                Category = x.Category
            }));

            _logger.Debug($"Got {result.Count} releases");

            return result;
        }
    }
}
