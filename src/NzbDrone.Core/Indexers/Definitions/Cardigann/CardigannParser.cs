using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Xml.Parser;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.Cardigann.Exceptions;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Cardigann
{
    public class CardigannParser : CardigannBase, IParseIndexerResponse
    {
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        protected override string SiteLink => ResolveSiteLink();

        public CardigannParser(IConfigService configService,
                               CardigannDefinition definition,
                               Logger logger)
        : base(configService, definition, logger)
        {
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releases = new List<ReleaseInfo>();

            _logger.Debug("Parsing");

            var indexerLogging = _configService.LogIndexerResponse;
            var indexerResponseStatus = indexerResponse.HttpResponse.StatusCode;

            if (indexerResponseStatus != HttpStatusCode.OK)
            {
                // Remove cookie cache
                if (indexerResponse.HttpResponse.HasHttpRedirect && indexerResponse.HttpResponse.RedirectUrl
                        .ContainsIgnoreCase("login.php"))
                {
                    CookiesUpdater(null, null);
                    throw new IndexerException(indexerResponse, "We are being redirected to the login page. Most likely your session expired or was killed. Try testing the indexer in the settings.");
                }

                // Catch common http exceptions before parsing
                switch (indexerResponseStatus)
                {
                    case HttpStatusCode.BadRequest:
                    case HttpStatusCode.Forbidden:
                    case HttpStatusCode.BadGateway:
                    case HttpStatusCode.ServiceUnavailable:
                    case HttpStatusCode.GatewayTimeout:
                        throw new HttpException(indexerResponse.HttpResponse);
                    case HttpStatusCode.Unauthorized:
                        throw new IndexerAuthException(indexerResponse.HttpResponse.Content.ToString());
                    default:
                        throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from API request");
                }
            }

            var results = indexerResponse.Content;
            var request = indexerResponse.Request as CardigannRequest;
            var variables = request.Variables;
            var search = _definition.Search;

            var searchUrlUri = new Uri(request.Url.FullUri);

            if (request.SearchPath.Response != null && request.SearchPath.Response.Type.Equals("json"))
            {
                if (request.SearchPath.Response != null && request.SearchPath.Response.NoResultsMessage != null && (request.SearchPath.Response.NoResultsMessage.Equals(results) || (request.SearchPath.Response.NoResultsMessage == string.Empty && results == string.Empty)))
                {
                    return releases;
                }

                var parsedJson = JToken.Parse(results);
                if (parsedJson == null)
                {
                    throw new IndexerException(indexerResponse, "Error Parsing Json Response");
                }

                if (search.Rows.Count != null)
                {
                    var countVal = HandleJsonSelector(search.Rows.Count, parsedJson, variables);
                    if (int.TryParse(countVal, out var count))
                    {
                        if (count < 1)
                        {
                            return releases;
                        }
                    }
                }

                var rowsArray = JsonParseRowsSelector(parsedJson, search.Rows.Selector);
                if (rowsArray == null)
                {
                    throw new IndexerException(indexerResponse, "Error Parsing Rows Selector");
                }

                foreach (var row in rowsArray)
                {
                    var selObj = search.Rows.Attribute != null ? row.SelectToken(search.Rows.Attribute).Value<JToken>() : row;
                    var mulRows = search.Rows.Multiple ? selObj.Values<JObject>() : new List<JObject> { selObj.Value<JObject>() };

                    foreach (var mulRow in mulRows)
                    {
                        var release = new TorrentInfo();

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
                            var isOptional = OptionalFields.Contains(field.Key) || fieldModifiers.Contains("optional") || field.Value.Optional;
                            try
                            {
                                var parentObj = mulRow;
                                if (field.Value.Selector != null && field.Value.Selector.StartsWith(".."))
                                {
                                    parentObj = row.Value<JObject>();
                                }

                                value = HandleJsonSelector(field.Value, parentObj, variables, !isOptional);
                                if (isOptional && string.IsNullOrWhiteSpace(value))
                                {
                                    variables[variablesKey] = null;
                                    continue;
                                }

                                variables[variablesKey] = ParseFields(value, fieldName, release, fieldModifiers, searchUrlUri);
                            }
                            catch (Exception ex)
                            {
                                if (!variables.ContainsKey(variablesKey))
                                {
                                    variables[variablesKey] = null;
                                }

                                if (isOptional)
                                {
                                    variables[variablesKey] = null;
                                    continue;
                                }

                                throw new CardigannException(string.Format("Error while parsing field={0}, selector={1}, value={2}: {3}", field.Key, field.Value.Selector, value ?? "<null>", ex.Message));
                            }

                            var filters = search.Rows.Filters;
                            var skipRelease = ParseRowFilters(filters, release, variables, row);

                            if (skipRelease)
                            {
                                continue;
                            }
                        }

                        releases.Add(release);
                    }
                }
            }
            else
            {
                try
                {
                    IHtmlCollection<IElement> rowsDom;

                    if (request.SearchPath.Response != null && request.SearchPath.Response.Type.Equals("xml"))
                    {
                        var searchResultParser = new XmlParser();
                        var searchResultDocument = searchResultParser.ParseDocument(results);

                        if (search.Preprocessingfilters != null)
                        {
                            results = ApplyFilters(results, search.Preprocessingfilters, variables);
                            searchResultDocument = searchResultParser.ParseDocument(results);
                            _logger.Trace(string.Format("CardigannIndexer ({0}): result after preprocessingfilters: {1}", _definition.Id, results));
                        }

                        var rowsSelector = ApplyGoTemplateText(search.Rows.Selector, variables);
                        rowsDom = searchResultDocument.QuerySelectorAll(rowsSelector);
                    }
                    else
                    {
                        var searchResultParser = new HtmlParser();
                        var searchResultDocument = searchResultParser.ParseDocument(results);

                        if (search.Preprocessingfilters != null)
                        {
                            results = ApplyFilters(results, search.Preprocessingfilters, variables);
                            searchResultDocument = searchResultParser.ParseDocument(results);
                            _logger.Trace(string.Format("CardigannIndexer ({0}): result after preprocessingfilters: {1}", _definition.Id, results));
                        }

                        var rowsSelector = ApplyGoTemplateText(search.Rows.Selector, variables);
                        rowsDom = searchResultDocument.QuerySelectorAll(rowsSelector);
                    }

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
                            var release = new TorrentInfo();

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
                                var isOptional = OptionalFields.Contains(field.Key) || fieldModifiers.Contains("optional") || field.Value.Optional;
                                try
                                {
                                    value = HandleSelector(field.Value, row, variables, !isOptional);

                                    if (isOptional && string.IsNullOrWhiteSpace(value))
                                    {
                                        variables[variablesKey] = null;
                                        continue;
                                    }

                                    variables[variablesKey] = ParseFields(value, fieldName, release, fieldModifiers, searchUrlUri);
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
                            var skipRelease = ParseRowFilters(filters, release, variables, row);

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
                                    throw new CardigannException(string.Format("No date header row found for {0}", release.ToString()));
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
            }

            /*
            if (query.Limit > 0)
            {
                releases = releases.Take(query.Limit).ToList();
            }*/

            releases.ForEach(c =>
            {
                // generate magnet link from info hash (not allowed for private sites)
                if (((TorrentInfo)c).MagnetUrl == null && !string.IsNullOrWhiteSpace(((TorrentInfo)c).InfoHash) && _definition.Type != "private")
                {
                    ((TorrentInfo)c).MagnetUrl = MagnetLinkBuilder.BuildPublicMagnetLink(((TorrentInfo)c).InfoHash, c.Title);
                }

                // generate info hash from magnet link
                if (((TorrentInfo)c).MagnetUrl != null && string.IsNullOrWhiteSpace(((TorrentInfo)c).InfoHash))
                {
                    ((TorrentInfo)c).InfoHash = MagnetLinkBuilder.GetInfoHashFromMagnet(((TorrentInfo)c).MagnetUrl);
                }
            });

            _logger.Debug($"Got {releases.Count} releases");

            return releases;
        }

        private string ParseFields(string value, string fieldName, TorrentInfo release, List<string> fieldModifiers, Uri searchUrlUri)
        {
            switch (fieldName)
            {
                case "download":
                    if (string.IsNullOrEmpty(value))
                    {
                        value = null;
                        release.DownloadUrl = null;
                        break;
                    }

                    if (value.StartsWith("magnet:"))
                    {
                        release.MagnetUrl = value;
                        value = release.MagnetUrl;
                    }
                    else
                    {
                        release.DownloadUrl = ResolvePath(value, searchUrlUri).AbsoluteUri;
                        value = release.DownloadUrl;
                    }

                    release.Guid = value;

                    break;
                case "magnet":
                    var magnetUri = value;
                    release.MagnetUrl = magnetUri;
                    value = magnetUri.ToString();
                    break;
                case "infohash":
                    release.InfoHash = value;
                    break;
                case "details":
                    var url = ResolvePath(value, searchUrlUri)?.AbsoluteUri;
                    release.InfoUrl = url;
                    value = url.ToString();
                    break;
                case "comments":
                    var commentsUrl = ResolvePath(value, searchUrlUri);
                    if (release.CommentUrl == null)
                    {
                        release.CommentUrl = commentsUrl.AbsoluteUri;
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
                        if (release.Categories == null || fieldModifiers.Contains("noappend"))
                        {
                            release.Categories = cats;
                        }
                        else
                        {
                            release.Categories = release.Categories.Union(cats).ToList();
                        }
                    }

                    value = release.Categories.ToString();
                    break;
                case "categorydesc":
                    var catsDesc = MapTrackerCatDescToNewznab(value);
                    if (catsDesc.Any())
                    {
                        if (release.Categories == null || fieldModifiers.Contains("noappend"))
                        {
                            release.Categories = catsDesc;
                        }
                        else
                        {
                            release.Categories = release.Categories.Union(catsDesc).ToList();
                        }
                    }

                    value = release.Categories.ToString();
                    break;
                case "size":
                    release.Size = ParseUtil.GetBytes(value);
                    value = release.Size.ToString();
                    break;
                case "leechers":
                    var leechers = ParseUtil.CoerceLong(value);
                    leechers = leechers < 5000000L ? leechers : 0; // to fix #6558
                    if (release.Peers == null)
                    {
                        release.Peers = (int)leechers;
                    }
                    else
                    {
                        release.Peers += (int)leechers;
                    }

                    value = leechers.ToString();
                    break;
                case "seeders":
                    release.Seeders = ParseUtil.CoerceInt(value);
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
                    release.Files = ParseUtil.CoerceInt(value);
                    value = release.Files.ToString();
                    break;
                case "grabs":
                    release.Grabs = ParseUtil.CoerceInt(value);
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
                case "imdbid":
                    release.ImdbId = (int)ParseUtil.GetLongFromString(value);
                    value = release.ImdbId.ToString();
                    break;
                case "tmdbid":
                    var tmdbIDRegEx = new Regex(@"(\d+)", RegexOptions.Compiled);
                    var tmdbIDMatch = tmdbIDRegEx.Match(value);
                    var tmdbID = tmdbIDMatch.Groups[1].Value;
                    release.TmdbId = (int)ParseUtil.CoerceLong(tmdbID);
                    value = release.TmdbId.ToString();
                    break;
                case "rageid":
                    var rageIDRegEx = new Regex(@"(\d+)", RegexOptions.Compiled);
                    var rageIDMatch = rageIDRegEx.Match(value);
                    var rageID = rageIDMatch.Groups[1].Value;
                    release.TvRageId = (int)ParseUtil.CoerceLong(rageID);
                    value = release.TvRageId.ToString();
                    break;
                case "traktid":
                    var traktIDRegEx = new Regex(@"(\d+)", RegexOptions.Compiled);
                    var traktIDMatch = traktIDRegEx.Match(value);
                    var traktID = traktIDMatch.Groups[1].Value;
                    release.TraktId = (int)ParseUtil.CoerceLong(traktID);
                    value = release.TraktId.ToString();
                    break;
                case "tvdbid":
                    var tvdbIdRegEx = new Regex(@"(\d+)", RegexOptions.Compiled);
                    var tvdbIdMatch = tvdbIdRegEx.Match(value);
                    var tvdbId = tvdbIdMatch.Groups[1].Value;
                    release.TvdbId = (int)ParseUtil.CoerceLong(tvdbId);
                    value = release.TvdbId.ToString();
                    break;
                case "poster":
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        var poster = ResolvePath(value, searchUrlUri);
                        release.PosterUrl = poster.AbsoluteUri;
                    }

                    value = release.PosterUrl;
                    break;
                case "genre":
                    release.Genres = release.Genres.Union(value.Split(',')).ToList();
                    value = release.Genres.ToString();
                    break;
                case "year":
                    release.Year = ParseUtil.CoerceInt(value);
                    value = release.Year.ToString();
                    break;
                case "author":
                    release.Author = value;
                    break;
                case "booktitle":
                    release.BookTitle = value;
                    break;
                case "artist":
                    release.Artist = value;
                    break;
                case "album":
                    release.Album = value;
                    break;
                default:
                    break;
            }

            return value;
        }

        private bool ParseRowFilters(List<FilterBlock> filters, ReleaseInfo release, Dictionary<string, object> variables, object row)
        {
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

                            var queryKeywords = variables[".Keywords"] as string;

                            break;
                        case "strdump":
                            // for debugging
                            _logger.Debug(string.Format("CardigannIndexer ({0}): row strdump: {1}", _definition.Id, row.ToString()));
                            break;
                        default:
                            _logger.Error(string.Format("CardigannIndexer ({0}): Unsupported rows filter: {1}", _definition.Id, filter.Name));
                            break;
                    }
                }
            }

            return skipRelease;
        }
    }
}
