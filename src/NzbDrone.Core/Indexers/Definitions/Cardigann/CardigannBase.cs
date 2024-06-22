using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Indexers.Definitions.Cardigann
{
    public class CardigannBase
    {
        protected readonly CardigannDefinition _definition;
        protected readonly Logger _logger;
        protected readonly Encoding _encoding;
        protected readonly IConfigService _configService;

        protected virtual string SiteLink { get; private set; }

        protected readonly IndexerCapabilitiesCategories _categories = new ();
        protected readonly List<string> _defaultCategories = new ();

        protected readonly string[] OptionalFields = new string[] { "imdb", "imdbid", "tmdbid", "rageid", "tvdbid", "tvmazeid", "traktid", "doubanid", "poster", "banner", "description", "genre" };

        protected static readonly string[] _SupportedLogicFunctions =
        {
            "and",
            "or",
            "eq",
            "ne"
        };

        protected static readonly string[] _LogicFunctionsUsingStringLiterals =
        {
            "eq",
            "ne"
        };

        // Matches a logic function above and 2 or more of (.varname) or .varname or "string literal" in any combination
        protected static readonly Regex _LogicFunctionRegex = new Regex(
            $@"\b({string.Join("|", _SupportedLogicFunctions.Select(Regex.Escape))})(?:\s+(\(?\.[^\)\s]+\)?|""[^""]+"")){{2,}}");

        // Matches CSS selectors for the JSON parser
        protected static readonly Regex _jsonSelectorRegex = new Regex(@"\:(?<filter>.+?)\((?<key>.+?)\)(?=:|\z)", RegexOptions.Compiled);

        public CardigannSettings Settings { get; set;  }

        public CardigannBase(IConfigService configService,
                             CardigannDefinition definition,
                             Logger logger)
        {
            _configService = configService;
            _definition = definition;
            _encoding = Encoding.GetEncoding(definition.Encoding);
            _logger = logger;

            SiteLink = definition.Links.First();

            if (_definition.Caps.Categories != null && _definition.Caps.Categories.Any())
            {
                foreach (var category in _definition.Caps.Categories)
                {
                    var cat = NewznabStandardCategory.GetCatByName(category.Value);

                    if (cat == null)
                    {
                        _logger.Error("CardigannIndexer ({0}): invalid Torznab category for id {1}: {2}", _definition.Id, category.Key, category.Value);

                        continue;
                    }

                    _categories.AddCategoryMapping(category.Key, cat);
                }
            }

            if (_definition.Caps.Categorymappings != null && _definition.Caps.Categorymappings.Any())
            {
                foreach (var categoryMapping in _definition.Caps.Categorymappings)
                {
                    IndexerCategory torznabCat = null;

                    if (categoryMapping.Cat != null)
                    {
                        torznabCat = NewznabStandardCategory.GetCatByName(categoryMapping.Cat);

                        if (torznabCat == null)
                        {
                            _logger.Error("CardigannIndexer ({0}): invalid Torznab category for id {1}: {2}", _definition.Id, categoryMapping.Id, categoryMapping.Cat);

                            continue;
                        }
                    }

                    _categories.AddCategoryMapping(categoryMapping.Id, torznabCat, categoryMapping.Desc);

                    if (categoryMapping.Default)
                    {
                        _defaultCategories.Add(categoryMapping.Id);
                    }
                }
            }
        }

        protected IElement QuerySelector(IElement element, string selector)
        {
            // AngleSharp doesn't support the :root pseudo selector, so we check for it manually
            if (selector.StartsWith(":root"))
            {
                selector = selector.Substring(5);
                while (element.ParentElement != null)
                {
                    element = element.ParentElement;
                }
            }

            return element.QuerySelector(selector);
        }

        protected string HandleSelector(SelectorBlock selector, IElement dom, Dictionary<string, object> variables = null, bool required = true)
        {
            if (selector.Text != null)
            {
                return ApplyFilters(ApplyGoTemplateText(selector.Text, variables), selector.Filters, variables);
            }

            var selection = dom;
            string value = null;

            if (selector.Selector != null)
            {
                var selectorSelector = ApplyGoTemplateText(selector.Selector, variables);

                selection = dom.Matches(selectorSelector) ? dom : QuerySelector(dom, selectorSelector);

                if (selection == null)
                {
                    if (required)
                    {
                        throw new Exception($"Selector \"{selectorSelector}\" didn't match {dom.ToHtmlPretty()}");
                    }

                    return null;
                }
            }

            if (selector.Remove != null)
            {
                foreach (var i in selection.QuerySelectorAll(selector.Remove))
                {
                    i.Remove();
                }
            }

            if (selector.Case != null)
            {
                foreach (var @case in selector.Case)
                {
                    if (selection.Matches(@case.Key) || QuerySelector(selection, @case.Key) != null)
                    {
                        value = ApplyGoTemplateText(@case.Value, variables);
                        break;
                    }
                }

                if (value == null)
                {
                    if (required)
                    {
                        throw new Exception($"None of the case selectors \"{string.Join(",", selector.Case)}\" matched {selection.ToHtmlPretty()}");
                    }

                    return null;
                }
            }
            else if (selector.Attribute != null)
            {
                value = selection.GetAttribute(selector.Attribute);
                if (value == null)
                {
                    if (required)
                    {
                        throw new Exception($"Attribute \"{selector.Attribute}\" is not set for element {selection.ToHtmlPretty()}");
                    }

                    return null;
                }
            }
            else
            {
                value = selection.TextContent;
            }

            return ApplyFilters(value.Trim(), selector.Filters, variables);
        }

        protected string HandleJsonSelector(SelectorBlock selector, JToken parentObj, Dictionary<string, object> variables = null, bool required = true)
        {
            if (selector.Text != null)
            {
                return ApplyFilters(ApplyGoTemplateText(selector.Text, variables), selector.Filters, variables);
            }

            string value = null;

            if (selector.Selector != null)
            {
                var selectorSelector = ApplyGoTemplateText(selector.Selector.TrimStart('.'), variables);
                selectorSelector = JsonParseFieldSelector(parentObj, selectorSelector);

                JToken selection = null;
                if (selectorSelector != null)
                {
                    selection = parentObj.SelectToken(selectorSelector);
                }

                if (selection == null)
                {
                    if (required)
                    {
                        throw new Exception(string.Format("Selector \"{0}\" didn't match {1}", selectorSelector, parentObj.ToString()));
                    }

                    return null;
                }

                if (selection.Type is JTokenType.Array)
                {
                    // turn this json array into a comma delimited string
                    var valueArray = selection.Value<JArray>();
                    value = string.Join(",", valueArray);
                }
                else
                {
                    value = selection.Value<string>();
                }
            }

            if (selector.Case != null)
            {
                foreach (var @case in selector.Case)
                {
                    if ((value != null && value.Equals(@case.Key)) || @case.Key.Equals("*"))
                    {
                        value = ApplyGoTemplateText(@case.Value, variables);
                        break;
                    }
                }

                if (value == null)
                {
                    if (required)
                    {
                        throw new Exception($"None of the case selectors \"{string.Join(",", selector.Case)}\" matched {parentObj}");
                    }

                    return null;
                }
            }

            return ApplyFilters(value?.Trim(), selector.Filters, variables) ?? null;
        }

        protected Dictionary<string, object> GetBaseTemplateVariables()
        {
            var indexerLogging = _configService.LogIndexerResponse;
            var variables = new Dictionary<string, object>
            {
                [".Config.sitelink"] = SiteLink,
                [".True"] = "True",
                [".False"] = null,
                [".Today.Year"] = DateTime.Today.Year.ToString()
            };

            _logger.Trace("Populating config vars");

            foreach (var setting in _definition.Settings)
            {
                var name = ".Config." + setting.Name;

                object defaultValue = setting.Type switch
                {
                    "select" => setting.Options.OrderBy(x => x.Key).Select(x => x.Key).ToList().IndexOf(setting.Default).ToString().ParseInt64() ?? 0,
                    _ => setting.Default
                };

                var value = Settings.ExtraFieldData.GetValueOrDefault(setting.Name, defaultValue);

                if (indexerLogging && setting.Type != "password" && setting.Name != "apikey" && setting.Name != "rsskey")
                {
                    _logger.Trace($"{name} got value {value.ToJson()}");
                }

                switch (setting.Type)
                {
                    case "text":
                    case "password":
                        variables[name] = value;
                        break;
                    case "checkbox":
                        if (value is string stringValue && bool.TryParse(stringValue, out var result))
                        {
                            value = result;
                        }

                        variables[name] = (bool)value ? ".True" : null;
                        break;
                    case "select":
                        if (indexerLogging)
                        {
                            _logger.Trace($"Setting options: {setting.Options.ToJson()}");
                        }

                        var sorted = setting.Options.OrderBy(x => x.Key).ToList();
                        var selected = sorted[(int)(long)value];

                        if (indexerLogging)
                        {
                            _logger.Debug($"Selected option: {selected.ToJson()}");
                        }

                        variables[name] = selected.Key;
                        break;
                    case "info":
                    case "info_cookie":
                    case "info_flaresolverr":
                    case "info_useragent":
                    case "info_category_8000":
                    case "cardigannCaptcha":
                        // no-op
                        break;
                    default:
                        throw new NotSupportedException($"Type {setting.Type} is not supported.");
                }

                if (indexerLogging && setting.Type != "password" && setting.Name != "apikey" && setting.Name != "rsskey" && variables.ContainsKey(name))
                {
                    _logger.Debug($"Setting {setting.Name} to {variables[name].ToJson()}");
                }
            }

            return variables;
        }

        public delegate string TemplateTextModifier(string str);

        public string ApplyGoTemplateText(string template, Dictionary<string, object> variables = null, TemplateTextModifier modifier = null)
        {
            if (template.IsNullOrWhiteSpace() || !template.Contains("{{"))
            {
                return template;
            }

            variables ??= GetBaseTemplateVariables();

            // handle re_replace expression
            // Example: {{ re_replace .Query.Keywords "[^a-zA-Z0-9]+" "%" }}
            var reReplaceRegex = new Regex(@"{{\s*re_replace\s+(\..+?)\s+""(.*?)""\s+""(.*?)""\s*}}");
            var reReplaceRegexMatches = reReplaceRegex.Match(template);

            while (reReplaceRegexMatches.Success)
            {
                var all = reReplaceRegexMatches.Groups[0].Value;
                var variable = reReplaceRegexMatches.Groups[1].Value;
                var regexp = reReplaceRegexMatches.Groups[2].Value;
                var newvalue = reReplaceRegexMatches.Groups[3].Value;

                var replaceRegex = new Regex(regexp);
                var input = (string)variables[variable] ?? string.Empty;
                var expanded = replaceRegex.Replace(input, newvalue);

                if (modifier != null)
                {
                    expanded = modifier(expanded);
                }

                template = template.Replace(all, expanded);
                reReplaceRegexMatches = reReplaceRegexMatches.NextMatch();
            }

            // handle join expression
            // Example: {{ join .Categories "," }}
            var joinRegex = new Regex(@"{{\s*join\s+(\..+?)\s+""(.*?)""\s*}}");
            var joinMatches = joinRegex.Match(template);

            while (joinMatches.Success)
            {
                var all = joinMatches.Groups[0].Value;
                var variable = joinMatches.Groups[1].Value;
                var delimiter = joinMatches.Groups[2].Value;

                var input = (ICollection<string>)variables[variable];
                var expanded = string.Join(delimiter, input);

                if (modifier != null)
                {
                    expanded = modifier(expanded);
                }

                template = template.Replace(all, expanded);
                joinMatches = joinMatches.NextMatch();
            }

            var logicMatch = _LogicFunctionRegex.Match(template);

            while (logicMatch.Success)
            {
                var functionStartIndex = logicMatch.Groups[0].Index;
                var functionLength = logicMatch.Groups[0].Length;
                var functionName = logicMatch.Groups[1].Value;

                // Use Group.Captures to get each matching string in a repeating Match.Group
                // Strip () around variable names here, as they are optional. Use quotes to differentiate variables and literals
                var parameters = logicMatch.Groups[2].Captures.Cast<Capture>().Select(c => c.Value.Trim('(', ')')).ToList();
                var functionResult = "";

                // If the function can't use string literals, fail silently by removing the literals.
                if (!_LogicFunctionsUsingStringLiterals.Contains(functionName))
                {
                    parameters.RemoveAll(param => param.StartsWith("\""));
                }

                switch (functionName)
                {
                    case "and": // returns first null or empty, else last variable
                    case "or": // returns first not null or empty, else last variable
                        var isAnd = functionName == "and";
                        foreach (var parameter in parameters)
                        {
                            functionResult = parameter;

                            // (null as string) == null
                            // (if null or empty) break if and, continue if or
                            // (if neither null nor empty) continue if and, break if or
                            if (string.IsNullOrWhiteSpace(variables[parameter] as string) == isAnd)
                            {
                                break;
                            }
                        }

                        break;
                    case "eq": // Returns .True if equal
                    case "ne": // Returns .False if equal
                    {
                        var wantEqual = functionName == "eq";

                        // eq/ne take exactly 2 params. Update the length to match
                        // This removes the whitespace between params 2 and 3.
                        // It shouldn't matter because the match starts at a word boundary
                        if (parameters.Count > 2)
                        {
                            functionLength = logicMatch.Groups[2].Captures[2].Index - functionStartIndex;
                        }

                        // Take first two parameters, convert vars to values and strip quotes on string literals
                        // Counting distinct gives us 1 if equal and 2 if not.
                        var isEqual =
                            parameters.Take(2).Select(param => param.StartsWith("\"") ? param.Trim('"') : variables[param] as string)
                                      .Distinct().Count() == 1;

                        functionResult = isEqual == wantEqual ? ".True" : ".False";
                        break;
                    }
                }

                template = template.Remove(functionStartIndex, functionLength)
                                   .Insert(functionStartIndex, functionResult);

                // Rerunning match instead of using nextMatch allows us to support nested functions
                // like {{if and eq (.Var1) "string1" eq (.Var2) "string2"}}
                // No performance is lost because Match/NextMatch are lazy evaluated and pause execution after first match
                logicMatch = _LogicFunctionRegex.Match(template);
            }

            // handle if ... else ... expression
            var ifElseRegex = new Regex(@"{{\s*if\s*(.+?)\s*}}(.*?){{\s*else\s*}}(.*?){{\s*end\s*}}");
            var ifElseRegexMatches = ifElseRegex.Match(template);

            while (ifElseRegexMatches.Success)
            {
                string conditionResult = null;

                var all = ifElseRegexMatches.Groups[0].Value;
                var condition = ifElseRegexMatches.Groups[1].Value;
                var onTrue = ifElseRegexMatches.Groups[2].Value;
                var onFalse = ifElseRegexMatches.Groups[3].Value;

                if (condition.StartsWith("."))
                {
                    var conditionResultState = false;
                    var value = variables[condition];

                    if (value == null)
                    {
                        conditionResultState = false;
                    }
                    else if (value is string)
                    {
                        conditionResultState = !string.IsNullOrWhiteSpace((string)value);
                    }
                    else if (value is ICollection)
                    {
                        conditionResultState = ((ICollection)value).Count > 0;
                    }
                    else
                    {
                        throw new Exception(string.Format("Unexpceted type for variable {0}: {1}", condition, value.GetType()));
                    }

                    if (conditionResultState)
                    {
                        conditionResult = onTrue;
                    }
                    else
                    {
                        conditionResult = onFalse;
                    }
                }
                else
                {
                    throw new NotImplementedException("CardigannIndexer: Condition operation '" + condition + "' not implemented");
                }

                template = template.Replace(all, conditionResult);
                ifElseRegexMatches = ifElseRegexMatches.NextMatch();
            }

            // handle range expression
            var rangeRegex = new Regex(@"{{\s*range\s*(((?<index>\$.+?),)((\s*(?<element>.+?)\s*(:=)\s*)))?(?<variable>.+?)\s*}}(?<prefix>.*?){{\.}}(?<postfix>.*?){{end}}");
            var rangeRegexMatches = rangeRegex.Match(template);

            while (rangeRegexMatches.Success)
            {
                var expanded = string.Empty;

                var all = rangeRegexMatches.Groups[0].Value;
                var index = rangeRegexMatches.Groups["index"].Value;
                var variable = rangeRegexMatches.Groups["variable"].Value;
                var prefix = rangeRegexMatches.Groups["prefix"].Value;
                var postfix = rangeRegexMatches.Groups["postfix"].Value;

                var arrayIndex = 0;
                var indexReplace = "{{" + index + "}}";

                foreach (var value in (ICollection<string>)variables[variable])
                {
                    var newvalue = value;
                    if (modifier != null)
                    {
                        newvalue = modifier(newvalue);
                    }

                    var indexValue = arrayIndex++;

                    if (index.IsNotNullOrWhiteSpace())
                    {
                        expanded += prefix.Replace(indexReplace, indexValue.ToString()) + newvalue + postfix.Replace(indexReplace, indexValue.ToString());
                    }
                    else
                    {
                        expanded += prefix + newvalue + postfix;
                    }
                }

                template = template.Replace(all, expanded);
                rangeRegexMatches = rangeRegexMatches.NextMatch();
            }

            // handle simple variables
            var variablesRegEx = new Regex(@"{{\s*(\..+?)\s*}}");
            var variablesRegExMatches = variablesRegEx.Match(template);

            while (variablesRegExMatches.Success)
            {
                var expanded = string.Empty;

                var all = variablesRegExMatches.Groups[0].Value;
                var variable = variablesRegExMatches.Groups[1].Value;

                //We store tmdbid and others as int32, thus conversion with .tostring is needed
                var value = variables[variable]?.ToString() ?? null;
                if (modifier != null)
                {
                    value = modifier(value);
                }

                template = template.Replace(all, value);
                variablesRegExMatches = variablesRegExMatches.NextMatch();
            }

            return template;
        }

        protected string ApplyFilters(string data, List<FilterBlock> filters, Dictionary<string, object> variables = null)
        {
            if (filters == null)
            {
                return data;
            }

            foreach (var filter in filters)
            {
                switch (filter.Name)
                {
                    case "querystring":
                        var param = (string)filter.Args;

                        data = ParseUtil.GetArgumentFromQueryString(data, param);
                        break;
                    case "timeparse":
                    case "dateparse":
                        var layout = (string)filter.Args;

                        try
                        {
                            var date = DateTimeUtil.ParseDateTimeGoLang(data, layout);
                            data = date.ToString(DateTimeUtil.Rfc1123ZPattern, CultureInfo.InvariantCulture);
                        }
                        catch (InvalidDateException ex)
                        {
                            _logger.Debug("{0}: {1}", _definition.Id, ex.Message);
                        }

                        break;
                    case "regexp":
                        var pattern = (string)filter.Args;
                        var regexp = new Regex(pattern);
                        var match = regexp.Match(data);
                        data = match.Groups[1].Value;
                        break;
                    case "re_replace":
                        var regexpreplace_pattern = (string)filter.Args[0];
                        var regexpreplace_replacement = (string)filter.Args[1];
                        regexpreplace_replacement = ApplyGoTemplateText(regexpreplace_replacement, variables);
                        var regexpreplace_regex = new Regex(regexpreplace_pattern);
                        data = regexpreplace_regex.Replace(data, regexpreplace_replacement);
                        break;
                    case "split":
                        var sep = (string)filter.Args[0];
                        var pos = (string)filter.Args[1];
                        var posInt = int.Parse(pos);
                        var strParts = data.Split(sep[0]);
                        if (posInt < 0)
                        {
                            posInt += strParts.Length;
                        }

                        data = strParts[posInt];
                        break;
                    case "replace":
                        var from = (string)filter.Args[0];
                        var to = (string)filter.Args[1];
                        to = ApplyGoTemplateText(to, variables);
                        data = data.Replace(from, to);
                        break;
                    case "trim":
                        var cutset = (string)filter.Args;
                        data = cutset != null ? data.Trim(cutset[0]) : data.Trim();
                        break;
                    case "prepend":
                        var prependstr = (string)filter.Args;
                        data = ApplyGoTemplateText(prependstr, variables) + data;
                        break;
                    case "append":
                        var str = (string)filter.Args;
                        data += ApplyGoTemplateText(str, variables);
                        break;
                    case "tolower":
                        data = data.ToLower();
                        break;
                    case "toupper":
                        data = data.ToUpper();
                        break;
                    case "urldecode":
                        data = data.UrlDecode(_encoding);
                        break;
                    case "urlencode":
                        data = data.UrlEncode(_encoding);
                        break;
                    case "htmldecode":
                        data = WebUtility.HtmlDecode(data);
                        break;
                    case "htmlencode":
                        data = WebUtility.HtmlEncode(data);
                        break;
                    case "timeago":
                    case "reltime":
                        data = DateTimeUtil.FromTimeAgo(data).ToString(DateTimeUtil.Rfc1123ZPattern, CultureInfo.InvariantCulture);
                        break;
                    case "fuzzytime":
                        data = DateTimeUtil.FromUnknown(data).ToString(DateTimeUtil.Rfc1123ZPattern, CultureInfo.InvariantCulture);
                        break;
                    case "validfilename":
                        data = StringUtil.MakeValidFileName(data, '_', false);
                        break;
                    case "diacritics":
                        var diacriticsOp = (string)filter.Args;
                        if (diacriticsOp == "replace")
                        {
                            // Should replace diacritics charcaters with their base character
                            // It's not perfect, e.g. "ŠĐĆŽ - šđčćž" becomes "SĐCZ-sđccz"
                            var stFormD = data.Normalize(NormalizationForm.FormD);
                            var len = stFormD.Length;
                            var sb = new StringBuilder();
                            for (var i = 0; i < len; i++)
                            {
                                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(stFormD[i]);
                                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                                {
                                    sb.Append(stFormD[i]);
                                }
                            }

                            data = sb.ToString().Normalize(NormalizationForm.FormC);
                        }
                        else
                        {
                            throw new Exception("unsupported diacritics filter argument");
                        }

                        break;
                    case "jsonjoinarray":
                        var jsonjoinarrayJSONPath = (string)filter.Args[0];
                        var jsonjoinarraySeparator = (string)filter.Args[1];
                        var jsonjoinarrayO = JObject.Parse(data);
                        var jsonjoinarrayOResult = jsonjoinarrayO.SelectToken(jsonjoinarrayJSONPath);
                        var jsonjoinarrayOResultStrings = jsonjoinarrayOResult.Select(j => j.ToString());
                        data = string.Join(jsonjoinarraySeparator, jsonjoinarrayOResultStrings);
                        break;
                    case "hexdump":
                        // this is mainly for debugging invisible special char related issues
                        var hexData = string.Join("", data.Select(c => c + "(" + ((int)c).ToString("X2") + ")"));
                        _logger.Debug(string.Format("CardigannIndexer ({0}): strdump: {1}", _definition.Id, hexData));
                        break;
                    case "strdump":
                        // for debugging
                        var debugData = data.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\xA0", "\\xA0");
                        var strTag = (string)filter.Args;
                        strTag = strTag != null ? $"({strTag}):" : ":";

                        _logger.Debug($"CardigannIndexer ({_definition.Id}): strdump{strTag} {debugData}");
                        break;
                    case "validate":
                        char[] delimiters = { ',', ' ', '/', ')', '(', '.', ';', '[', ']', '"', '|', ':' };
                        var args = (string)filter.Args;
                        var argsList = args.ToLower().Split(delimiters, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                        var validList = argsList.ToList();
                        var validIntersect = validList.Intersect(data.ToLower().Split(delimiters, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)).ToList();
                        data = string.Join(", ", validIntersect);
                        break;
                    default:
                        _logger.Error($"CardigannIndexer ({_definition.Id}): Unsupported field filter: {filter.Name}");
                        break;
                }
            }

            return data;
        }

        protected Dictionary<string, string> ParseCustomHeaders(Dictionary<string, List<string>> customHeaders, Dictionary<string, object> variables)
        {
            var headers = new Dictionary<string, string>();

            if (customHeaders == null)
            {
                return headers;
            }

            // FIXME: fix jackett header handling (allow it to specifiy the same header multipe times)
            foreach (var header in customHeaders)
            {
                headers.Add(header.Key, ApplyGoTemplateText(header.Value[0], variables));
            }

            return headers;
        }

        protected IDictionary<string, object> AddTemplateVariablesFromUri(IDictionary<string, object> variables, Uri uri, string prefix = "")
        {
            variables[prefix + ".AbsoluteUri"] = uri.AbsoluteUri;
            variables[prefix + ".AbsolutePath"] = uri.AbsolutePath;
            variables[prefix + ".Scheme"] = uri.Scheme;
            variables[prefix + ".Host"] = uri.Host;
            variables[prefix + ".Port"] = uri.Port.ToString();
            variables[prefix + ".PathAndQuery"] = uri.PathAndQuery;
            variables[prefix + ".Query"] = uri.Query;
            var queryString = QueryHelpers.ParseQuery(uri.Query);

            foreach (var key in queryString.Keys)
            {
                //If we have supplied the same query string multiple time, just take the first.
                variables[prefix + ".Query." + key] = queryString[key].First();
            }

            return variables;
        }

        protected Uri ResolvePath(string path, Uri currentUrl = null)
        {
            return new Uri(currentUrl ?? new Uri(SiteLink), path);
        }

        protected string ResolveSiteLink()
        {
            var settingsBaseUrl = Settings?.BaseUrl;
            var defaultLink = _definition.Links.First();

            if (settingsBaseUrl == null)
            {
                return defaultLink;
            }

            if (_definition?.Legacylinks?.Contains(settingsBaseUrl) ?? false)
            {
                _logger.Trace("Changing legacy site link from {0} to {1}", settingsBaseUrl, defaultLink);
                return defaultLink;
            }

            return settingsBaseUrl;
        }

        protected JArray JsonParseRowsSelector(JToken parsedJson, string rowSelector)
        {
            rowSelector = ApplyGoTemplateText(rowSelector);
            var selector = rowSelector.Split(':')[0];

            try
            {
                var rowsObj = parsedJson.SelectToken(selector).Value<JArray>();

                return new JArray(rowsObj.Where(t => JsonParseFieldSelector(t.Value<JObject>(), rowSelector.Remove(0, selector.Length)) != null));
            }
            catch (Exception ex)
            {
                _logger.Trace(ex, "Failed to parse JSON rows for selector \"{0}\"", rowSelector);

                return null;
            }
        }

        private string JsonParseFieldSelector(JToken parsedJson, string rowSelector)
        {
            var selector = rowSelector.Split(':')[0];
            JToken parsedObject;
            if (string.IsNullOrWhiteSpace(selector))
            {
                parsedObject = parsedJson;
            }
            else if (parsedJson.SelectToken(selector) != null)
            {
                parsedObject = parsedJson.SelectToken(selector);
            }
            else
            {
                return null;
            }

            foreach (Match match in _jsonSelectorRegex.Matches(rowSelector))
            {
                var filter = match.Result("${filter}");
                var key = match.Result("${key}");
                Match innerMatch;
                switch (filter)
                {
                    case "has":
                        innerMatch = _jsonSelectorRegex.Match(key);
                        if (innerMatch.Success)
                        {
                            if (JsonParseFieldSelector(parsedObject, key) == null)
                            {
                                return null;
                            }
                        }
                        else
                        {
                            if (parsedObject.SelectToken(key) == null)
                            {
                                return null;
                            }
                        }

                        break;
                    case "not":
                        innerMatch = _jsonSelectorRegex.Match(key);
                        if (innerMatch.Success)
                        {
                            if (JsonParseFieldSelector(parsedObject, key) != null)
                            {
                                return null;
                            }
                        }
                        else
                        {
                            if (parsedObject.SelectToken(key) != null)
                            {
                                return null;
                            }
                        }

                        break;
                    case "contains":
                        if (!parsedObject.ToString().Contains(key))
                        {
                            return null;
                        }

                        break;
                    default:
                        _logger.Error(string.Format("CardigannIndexer ({0}): Unsupported selector: {1}", _definition.Id, rowSelector));
                        continue;
                }
            }

            return selector;
        }
    }
}
