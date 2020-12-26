using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Indexers.Cardigann
{
    public class CardigannBase
    {
        protected readonly CardigannDefinition _definition;
        protected readonly CardigannSettings _settings;
        protected readonly Logger _logger;
        protected readonly Encoding _encoding;

        protected string SiteLink { get; private set; }

        protected readonly List<CategoryMapping> _categoryMapping = new List<CategoryMapping>();
        protected readonly List<string> _defaultCategories = new List<string>();

        protected readonly string[] OptionalFields = new string[] { "imdb", "rageid", "tvdbid", "banner" };

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

        public CardigannBase(CardigannDefinition definition,
                             CardigannSettings settings,
                             Logger logger)
        {
            _definition = definition;
            _settings = settings;
            _encoding = Encoding.GetEncoding(definition.Encoding);
            _logger = logger;

            SiteLink = definition.Links.First();

            if (_definition.Caps.Categories != null)
            {
                foreach (var category in _definition.Caps.Categories)
                {
                    var cat = TorznabCatType.GetCatByName(category.Value);
                    if (cat == null)
                    {
                        _logger.Error(string.Format("CardigannIndexer ({0}): invalid Torznab category for id {1}: {2}", _definition.Id, category.Key, category.Value));
                        continue;
                    }

                    AddCategoryMapping(category.Key, cat);
                }
            }

            if (_definition.Caps.Categorymappings != null)
            {
                foreach (var categorymapping in _definition.Caps.Categorymappings)
                {
                    IndexerCategory torznabCat = null;

                    if (categorymapping.cat != null)
                    {
                        torznabCat = TorznabCatType.GetCatByName(categorymapping.cat);
                        if (torznabCat == null)
                        {
                            _logger.Error(string.Format("CardigannIndexer ({0}): invalid Torznab category for id {1}: {2}", _definition.Id, categorymapping.id, categorymapping.cat));
                            continue;
                        }
                    }

                    AddCategoryMapping(categorymapping.id, torznabCat, categorymapping.desc);

                    if (categorymapping.Default)
                    {
                        _defaultCategories.Add(categorymapping.id);
                    }
                }
            }
        }

        public void AddCategoryMapping(string trackerCategory, IndexerCategory torznabCategory, string trackerCategoryDesc = null)
        {
            _categoryMapping.Add(new CategoryMapping(trackerCategory, trackerCategoryDesc, torznabCategory.Id));

            if (trackerCategoryDesc == null)
            {
                return;
            }

            // create custom cats (1:1 categories) if trackerCategoryDesc is defined
            // - if trackerCategory is "integer" we use that number to generate custom category id
            // - if trackerCategory is "string" we compute a hash to generate fixed integer id for the custom category
            //   the hash is not perfect but it should work in most cases. we can't use sequential numbers because
            //   categories are updated frequently and the id must be fixed to work in 3rd party apps
            if (!int.TryParse(trackerCategory, out var trackerCategoryInt))
            {
                var hashed = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(trackerCategory));
                trackerCategoryInt = BitConverter.ToUInt16(hashed, 0); // id between 0 and 65535 < 100000
            }

            var customCat = new IndexerCategory(trackerCategoryInt + 100000, trackerCategoryDesc);
            _categoryMapping.Add(new CategoryMapping(trackerCategory, trackerCategoryDesc, customCat.Id));
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

        protected string HandleSelector(SelectorBlock selector, IElement dom, Dictionary<string, object> variables = null)
        {
            if (selector.Text != null)
            {
                return ApplyFilters(ApplyGoTemplateText(selector.Text, variables), selector.Filters, variables);
            }

            var selection = dom;
            string value = null;

            if (selector.Selector != null)
            {
                if (dom.Matches(selector.Selector))
                {
                    selection = dom;
                }
                else
                {
                    selection = QuerySelector(dom, selector.Selector);
                }

                if (selection == null)
                {
                    throw new Exception(string.Format("Selector \"{0}\" didn't match {1}", selector.Selector, dom.ToHtmlPretty()));
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
                        value = @case.Value;
                        break;
                    }
                }

                if (value == null)
                {
                    throw new Exception(string.Format("None of the case selectors \"{0}\" matched {1}", string.Join(",", selector.Case), selection.ToHtmlPretty()));
                }
            }
            else if (selector.Attribute != null)
            {
                value = selection.GetAttribute(selector.Attribute);
                if (value == null)
                {
                    throw new Exception(string.Format("Attribute \"{0}\" is not set for element {1}", selector.Attribute, selection.ToHtmlPretty()));
                }
            }
            else
            {
                value = selection.TextContent;
            }

            return ApplyFilters(ParseUtil.NormalizeSpace(value), selector.Filters, variables);
        }

        protected Dictionary<string, object> GetBaseTemplateVariables()
        {
            var variables = new Dictionary<string, object>
            {
                [".Config.sitelink"] = SiteLink,
                [".True"] = "True",
                [".False"] = null,
                [".Today.Year"] = DateTime.Today.Year.ToString()
            };

            _logger.Debug("Populating config vars");

            foreach (var setting in _definition.Settings)
            {
                var name = ".Config." + setting.Name;
                var value = _settings.ExtraFieldData.GetValueOrDefault(setting.Name, setting.Default);

                _logger.Debug($"{name} got value {value.ToJson()}");

                if (setting.Type == "text")
                {
                    variables[name] = value;
                }
                else if (setting.Type == "checkbox")
                {
                    variables[name] = ((bool)value) ? ".True" : ".False";
                }
                else if (setting.Type == "select")
                {
                    _logger.Debug($"setting options: {setting.Options.ToJson()}");
                    var sorted = setting.Options.OrderBy(x => x.Key).ToList();
                    var selected = sorted[(int)(long)value];

                    _logger.Debug($"selected option: {selected.ToJson()}");

                    variables[name] = selected.Value;
                }
                else
                {
                    throw new NotSupportedException();
                }

                _logger.Debug($"Setting {setting.Name} to {variables[name]}");
            }

            return variables;
        }

        protected ICollection<IndexerCategory> MapTrackerCatToNewznab(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new List<IndexerCategory>();
            }

            var cats = _categoryMapping
                       .Where(m =>
                           !string.IsNullOrWhiteSpace(m.TrackerCategory) &&
                           string.Equals(m.TrackerCategory, input, StringComparison.InvariantCultureIgnoreCase))
                       .Select(c => TorznabCatType.AllCats.FirstOrDefault(n => n.Id == c.NewzNabCategory) ?? new IndexerCategory { Id = c.NewzNabCategory })
                       .ToList();
            return cats;
        }

        public List<string> MapTorznabCapsToTrackers(int[] searchCategories, bool mapChildrenCatsToParent = false)
        {
            if (searchCategories == null)
            {
                return new List<string>();
            }

            var results = new List<string>();

            results.AddRange(_categoryMapping
                             .Where(c => searchCategories.Contains(c.NewzNabCategory))
                             .Select(mapping => mapping.TrackerCategory).Distinct().ToList());

            return results;
        }

        protected delegate string TemplateTextModifier(string str);

        protected string ApplyGoTemplateText(string template, Dictionary<string, object> variables = null, TemplateTextModifier modifier = null)
        {
            if (variables == null)
            {
                variables = GetBaseTemplateVariables();
            }

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
                var input = (string)variables[variable];
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
            var rangeRegex = new Regex(@"{{\s*range\s*(.+?)\s*}}(.*?){{\.}}(.*?){{end}}");
            var rangeRegexMatches = rangeRegex.Match(template);

            while (rangeRegexMatches.Success)
            {
                var expanded = string.Empty;

                var all = rangeRegexMatches.Groups[0].Value;
                var variable = rangeRegexMatches.Groups[1].Value;
                var prefix = rangeRegexMatches.Groups[2].Value;
                var postfix = rangeRegexMatches.Groups[3].Value;

                foreach (var value in (ICollection<string>)variables[variable])
                {
                    var newvalue = value;
                    if (modifier != null)
                    {
                        newvalue = modifier(newvalue);
                    }

                    expanded += prefix + newvalue + postfix;
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

                var value = (string)variables[variable];
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

                        // data = ParseUtil.GetArgumentFromQueryString(data, param);
                        break;
                    case "timeparse":
                    case "dateparse":
                        var layout = (string)filter.Args;
                        try
                        {
                            var date = DateTimeUtil.ParseDateTimeGoLang(data, layout);
                            data = date.ToString(DateTimeUtil.Rfc1123ZPattern);
                        }
                        catch (FormatException ex)
                        {
                            _logger.Debug(ex.Message);
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
                        if (cutset != null)
                        {
                            data = data.Trim(cutset[0]);
                        }
                        else
                        {
                            data = data.Trim();
                        }

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
                        data = WebUtilityHelpers.UrlDecode(data, _encoding);
                        break;
                    case "urlencode":
                        data = WebUtilityHelpers.UrlEncode(data, _encoding);
                        break;
                    case "timeago":
                    case "reltime":
                        data = DateTimeUtil.FromTimeAgo(data).ToString(DateTimeUtil.Rfc1123ZPattern);
                        break;
                    case "fuzzytime":
                        data = DateTimeUtil.FromUnknown(data).ToString(DateTimeUtil.Rfc1123ZPattern);
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
                        if (strTag != null)
                        {
                            strTag = string.Format("({0}):", strTag);
                        }
                        else
                        {
                            strTag = ":";
                        }

                        _logger.Debug(string.Format("CardigannIndexer ({0}): strdump{1} {2}", _definition.Id, strTag, debugData));
                        break;
                    default:
                        break;
                }
            }

            return data;
        }

        protected Uri ResolvePath(string path, Uri currentUrl = null)
        {
            return new Uri(currentUrl ?? new Uri(SiteLink), path);
        }
    }
}
