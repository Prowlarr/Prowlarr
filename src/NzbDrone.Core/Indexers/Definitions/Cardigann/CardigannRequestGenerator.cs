using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Definitions.Cardigann;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers.Cardigann
{
    public class CardigannRequestGenerator : CardigannBase, IIndexerRequestGenerator
    {
        public IIndexerHttpClient HttpClient { get; set; }
        public ProviderDefinition Definition { get; set; }
        public IDictionary<string, string> Cookies { get; set; }
        protected HttpResponse landingResult;
        protected IHtmlDocument landingResultDocument;
        protected override string SiteLink => Settings?.BaseUrl ?? _definition.Links.First();

        public CardigannRequestGenerator(IConfigService configService,
                                         CardigannDefinition definition,
                                         Logger logger)
        : base(configService, definition, logger)
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
            variables[".Query.IMDBID"] = searchCriteria.FullImdbId;
            variables[".Query.IMDBIDShort"] = searchCriteria.ImdbId;
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
            variables[".Query.Ep"] = searchCriteria.Episode;
            variables[".Query.Season"] = searchCriteria.Season;
            variables[".Query.IMDBID"] = searchCriteria.FullImdbId;
            variables[".Query.IMDBIDShort"] = searchCriteria.ImdbId;
            variables[".Query.TVDBID"] = searchCriteria.TvdbId;
            variables[".Query.TVRageID"] = searchCriteria.RId;
            variables[".Query.TVMazeID"] = searchCriteria.TvMazeId;
            variables[".Query.TraktID"] = searchCriteria.TraktId;
            variables[".Query.Episode"] = searchCriteria.EpisodeSearchString;

            pageableRequests.Add(GetRequest(variables));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var variables = GetQueryVariableDefaults(searchCriteria);

            variables[".Query.Author"] = searchCriteria.Author;
            variables[".Query.Title"] = searchCriteria.Title;

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
            variables[".Query.Episode"] = null;

            //Music
            variables[".Query.Album"] = null;
            variables[".Query.Artist"] = null;
            variables[".Query.Label"] = null;
            variables[".Query.Track"] = null;

            //Book
            variables[".Query.Author"] = null;
            variables[".Query.Title"] = null;

            return variables;
        }

        public async Task DoLogin()
        {
            var login = _definition.Login;

            if (login.Method == "post")
            {
                var pairs = new Dictionary<string, string>();

                foreach (var input in login.Inputs)
                {
                    var value = ApplyGoTemplateText(input.Value);
                    pairs.Add(input.Key, value);
                }

                var loginUrl = ResolvePath(login.Path).ToString();

                CookiesUpdater(null, null);

                var requestBuilder = new HttpRequestBuilder(loginUrl)
                {
                    LogResponseContent = true,
                    Method = HttpMethod.POST,
                    AllowAutoRedirect = true,
                    SuppressHttpError = true,
                };

                foreach (var pair in pairs)
                {
                    requestBuilder.AddFormParameter(pair.Key, pair.Value);
                }

                requestBuilder.Headers.Add("Referer", SiteLink);

                var response = await HttpClient.ExecuteProxiedAsync(requestBuilder.Build(), Definition);

                Cookies = response.GetCookies();

                CheckForError(response, login.Error);

                CookiesUpdater(Cookies, DateTime.Now + TimeSpan.FromDays(30));
            }
            else if (login.Method == "form")
            {
                var loginUrl = ResolvePath(login.Path).ToString();

                var queryCollection = new NameValueCollection();
                var pairs = new Dictionary<string, string>();

                var formSelector = login.Form;
                if (formSelector == null)
                {
                    formSelector = "form";
                }

                // landingResultDocument might not be initiated if the login is caused by a relogin during a query
                if (landingResultDocument == null)
                {
                    await GetConfigurationForSetup(true);
                }

                var form = landingResultDocument.QuerySelector(formSelector);
                if (form == null)
                {
                    throw new CardigannConfigException(_definition, string.Format("Login failed: No form found on {0} using form selector {1}", loginUrl, formSelector));
                }

                var inputs = form.QuerySelectorAll("input");
                if (inputs == null)
                {
                    throw new CardigannConfigException(_definition, string.Format("Login failed: No inputs found on {0} using form selector {1}", loginUrl, formSelector));
                }

                var submitUrlstr = form.GetAttribute("action");
                if (login.Submitpath != null)
                {
                    submitUrlstr = login.Submitpath;
                }

                foreach (var input in inputs)
                {
                    var name = input.GetAttribute("name");
                    if (name == null)
                    {
                        continue;
                    }

                    var value = input.GetAttribute("value");
                    if (value == null)
                    {
                        value = "";
                    }

                    pairs[name] = value;
                }

                foreach (var input in login.Inputs)
                {
                    var value = ApplyGoTemplateText(input.Value);
                    var inputKey = input.Key;
                    if (login.Selectors)
                    {
                        var inputElement = landingResultDocument.QuerySelector(input.Key);
                        if (inputElement == null)
                        {
                            throw new CardigannConfigException(_definition, string.Format("Login failed: No input found using selector {0}", input.Key));
                        }

                        inputKey = inputElement.GetAttribute("name");
                    }

                    pairs[inputKey] = value;
                }

                // selector inputs
                if (login.Selectorinputs != null)
                {
                    foreach (var selectorinput in login.Selectorinputs)
                    {
                        string value = null;
                        try
                        {
                            value = HandleSelector(selectorinput.Value, landingResultDocument.FirstElementChild);
                            pairs[selectorinput.Key] = value;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(string.Format("Error while parsing selector input={0}, selector={1}, value={2}: {3}", selectorinput.Key, selectorinput.Value.Selector, value, ex.Message));
                        }
                    }
                }

                // getselector inputs
                if (login.Getselectorinputs != null)
                {
                    foreach (var selectorinput in login.Getselectorinputs)
                    {
                        string value = null;
                        try
                        {
                            value = HandleSelector(selectorinput.Value, landingResultDocument.FirstElementChild);
                            queryCollection[selectorinput.Key] = value;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(string.Format("Error while parsing get selector input={0}, selector={1}, value={2}: {3}", selectorinput.Key, selectorinput.Value.Selector, value, ex.Message));
                        }
                    }
                }

                if (queryCollection.Count > 0)
                {
                    submitUrlstr += "?" + queryCollection.GetQueryString();
                }

                var submitUrl = ResolvePath(submitUrlstr, new Uri(loginUrl));

                // automatically solve simpleCaptchas, if used
                var simpleCaptchaPresent = landingResultDocument.QuerySelector("script[src*=\"simpleCaptcha\"]");
                if (simpleCaptchaPresent != null)
                {
                    var captchaUrl = ResolvePath("simpleCaptcha.php?numImages=1");

                    var requestBuilder = new HttpRequestBuilder(captchaUrl.ToString())
                    {
                        LogResponseContent = true,
                        Method = HttpMethod.GET
                    };

                    requestBuilder.Headers.Add("Referer", loginUrl);

                    var simpleCaptchaResult = await HttpClient.ExecuteProxiedAsync(requestBuilder.Build(), Definition);

                    var simpleCaptchaJSON = JObject.Parse(simpleCaptchaResult.Content);
                    var captchaSelection = simpleCaptchaJSON["images"][0]["hash"].ToString();
                    pairs["captchaSelection"] = captchaSelection;
                    pairs["submitme"] = "X";
                }

                if (login.Captcha != null)
                {
                    var captcha = login.Captcha;
                    Settings.ExtraFieldData.TryGetValue("CAPTCHA", out var captchaText);
                    if (captchaText != null)
                    {
                        var input = captcha.Input;
                        if (login.Selectors)
                        {
                            var inputElement = landingResultDocument.QuerySelector(captcha.Input);
                            if (inputElement == null)
                            {
                                throw new CardigannConfigException(_definition, string.Format("Login failed: No captcha input found using {0}", captcha.Input));
                            }

                            input = inputElement.GetAttribute("name");
                        }

                        pairs[input] = (string)captchaText;
                    }
                }

                // clear landingResults/Document, otherwise we might use an old version for a new relogin (if GetConfigurationForSetup() wasn't called before)
                landingResult = null;
                landingResultDocument = null;

                HttpResponse loginResult = null;
                var enctype = form.GetAttribute("enctype");
                if (enctype == "multipart/form-data")
                {
                    var headers = new Dictionary<string, string>();
                    var boundary = "---------------------------" + DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString().Replace(".", "");
                    var bodyParts = new List<string>();

                    foreach (var pair in pairs)
                    {
                        var part = "--" + boundary + "\r\n" +
                          "Content-Disposition: form-data; name=\"" + pair.Key + "\"\r\n" +
                          "\r\n" +
                          pair.Value;
                        bodyParts.Add(part);
                    }

                    bodyParts.Add("--" + boundary + "--");

                    headers.Add("Content-Type", "multipart/form-data; boundary=" + boundary);
                    var body = string.Join("\r\n", bodyParts);

                    var requestBuilder = new HttpRequestBuilder(submitUrl.ToString())
                    {
                        LogResponseContent = true,
                        Method = HttpMethod.POST,
                        AllowAutoRedirect = true
                    };

                    requestBuilder.Headers.Add("Referer", SiteLink);

                    requestBuilder.SetCookies(Cookies);

                    foreach (var pair in pairs)
                    {
                        requestBuilder.AddFormParameter(pair.Key, pair.Value);
                    }

                    foreach (var header in headers)
                    {
                        requestBuilder.SetHeader(header.Key, header.Value);
                    }

                    var request = requestBuilder.Build();
                    request.SetContent(body);

                    loginResult = await HttpClient.ExecuteProxiedAsync(request, Definition);
                }
                else
                {
                    var requestBuilder = new HttpRequestBuilder(submitUrl.ToString())
                    {
                        LogResponseContent = true,
                        Method = HttpMethod.POST,
                        AllowAutoRedirect = true,
                        SuppressHttpError = true
                    };

                    requestBuilder.SetCookies(Cookies);
                    requestBuilder.Headers.Add("Referer", loginUrl);

                    foreach (var pair in pairs)
                    {
                        requestBuilder.AddFormParameter(pair.Key, pair.Value);
                    }

                    loginResult = await HttpClient.ExecuteProxiedAsync(requestBuilder.Build(), Definition);
                }

                Cookies = loginResult.GetCookies();
                CheckForError(loginResult, login.Error);
                CookiesUpdater(Cookies, DateTime.Now + TimeSpan.FromDays(30));
            }
            else if (login.Method == "cookie")
            {
                CookiesUpdater(null, null);
                Settings.ExtraFieldData.TryGetValue("cookie", out var cookies);
                CookiesUpdater(CookieUtil.CookieHeaderToDictionary((string)cookies), DateTime.Now + TimeSpan.FromDays(30));
            }
            else if (login.Method == "get")
            {
                var queryCollection = new NameValueCollection();
                foreach (var input in login.Inputs)
                {
                    var value = ApplyGoTemplateText(input.Value);
                    queryCollection.Add(input.Key, value);
                }

                var loginUrl = ResolvePath(login.Path + "?" + queryCollection.GetQueryString()).ToString();

                CookiesUpdater(null, null);

                var requestBuilder = new HttpRequestBuilder(loginUrl)
                {
                    LogResponseContent = true,
                    Method = HttpMethod.GET,
                    SuppressHttpError = true
                };

                requestBuilder.Headers.Add("Referer", SiteLink);

                var response = await HttpClient.ExecuteProxiedAsync(requestBuilder.Build(), Definition);

                Cookies = response.GetCookies();

                CheckForError(response, login.Error);

                CookiesUpdater(Cookies, DateTime.Now + TimeSpan.FromDays(30));
            }
            else if (login.Method == "oneurl")
            {
                var oneUrl = ApplyGoTemplateText(login.Inputs["oneurl"]);
                var loginUrl = ResolvePath(login.Path + oneUrl).ToString();

                CookiesUpdater(null, null);

                var requestBuilder = new HttpRequestBuilder(loginUrl)
                {
                    LogResponseContent = true,
                    Method = HttpMethod.GET,
                    SuppressHttpError = true
                };

                requestBuilder.Headers.Add("Referer", SiteLink);

                var response = await HttpClient.ExecuteProxiedAsync(requestBuilder.Build(), Definition);

                Cookies = response.GetCookies();

                CheckForError(response, login.Error);

                CookiesUpdater(Cookies, DateTime.Now + TimeSpan.FromDays(30));
            }
            else
            {
                throw new NotImplementedException("Login method " + login.Method + " not implemented");
            }
        }

        protected bool CheckForError(HttpResponse loginResult, IList<ErrorBlock> errorBlocks)
        {
            if (loginResult.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new HttpException(loginResult);
            }

            if (errorBlocks == null)
            {
                return true;
            }

            var resultParser = new HtmlParser();
            var resultDocument = resultParser.ParseDocument(loginResult.Content);
            foreach (var error in errorBlocks)
            {
                var selection = resultDocument.QuerySelector(error.Selector);
                if (selection != null)
                {
                    var errorMessage = selection.TextContent;
                    if (error.Message != null)
                    {
                        errorMessage = HandleSelector(error.Message, resultDocument.FirstElementChild);
                    }

                    throw new CardigannConfigException(_definition, string.Format("Error: {0}", errorMessage.Trim()));
                }
            }

            return true;
        }

        public async Task<Captcha> GetConfigurationForSetup(bool automaticlogin)
        {
            var login = _definition.Login;

            if (login == null || login.Method != "form")
            {
                return null;
            }

            var loginUrl = ResolvePath(login.Path);

            Cookies = null;

            if (login.Cookies != null)
            {
                Cookies = CookieUtil.CookieHeaderToDictionary(string.Join("; ", login.Cookies));
            }

            var requestBuilder = new HttpRequestBuilder(loginUrl.AbsoluteUri)
            {
                LogResponseContent = true,
                Method = HttpMethod.GET
            };

            requestBuilder.Headers.Add("Referer", SiteLink);

            if (Cookies != null)
            {
                requestBuilder.SetCookies(Cookies);
            }

            var request = requestBuilder.Build();

            landingResult = await HttpClient.ExecuteProxiedAsync(request, Definition);

            Cookies = landingResult.GetCookies();

            // Some sites have a temporary redirect before the login page, we need to process it.
            //if (_definition.Followredirect)
            //{
            //    await FollowIfRedirect(landingResult, loginUrl.AbsoluteUri, overrideCookies: landingResult.Cookies, accumulateCookies: true);
            //}
            var htmlParser = new HtmlParser();
            landingResultDocument = htmlParser.ParseDocument(landingResult.Content);

            Captcha captcha = null;

            if (login.Captcha != null)
            {
                captcha = await GetCaptcha(login);
            }

            if (captcha != null && automaticlogin)
            {
                _logger.Error(string.Format("CardigannIndexer ({0}): Found captcha during automatic login, aborting", _definition.Id));
            }

            return captcha;
        }

        private async Task<Captcha> GetCaptcha(LoginBlock login)
        {
            var captcha = login.Captcha;

            if (captcha.Type == "image")
            {
                var captchaElement = landingResultDocument.QuerySelector(captcha.Selector);
                if (captchaElement != null)
                {
                    var loginUrl = ResolvePath(login.Path);
                    var captchaUrl = ResolvePath(captchaElement.GetAttribute("src"), loginUrl);

                    var request = new HttpRequestBuilder(captchaUrl.ToString())
                        .SetCookies(landingResult.GetCookies())
                        .SetHeader("Referer", loginUrl.AbsoluteUri)
                        .Build();

                    var response = await HttpClient.ExecuteProxiedAsync(request, Definition);

                    return new Captcha
                    {
                        ContentType = response.Headers.ContentType,
                        ImageData = response.ResponseData
                    };
                }
                else
                {
                    _logger.Debug(string.Format("CardigannIndexer ({0}): No captcha image found", _definition.Id));
                }
            }
            else
            {
                throw new NotImplementedException(string.Format("Captcha type \"{0}\" is not implemented", captcha.Type));
            }

            return null;
        }

        protected string GetRedirectDomainHint(string requestUrl, string redirectUrl)
        {
            if (requestUrl.StartsWith(SiteLink) && !redirectUrl.StartsWith(SiteLink))
            {
                var uri = new HttpUri(redirectUrl);
                return uri.Scheme + "://" + uri.Host + "/";
            }

            return null;
        }

        protected string GetRedirectDomainHint(HttpResponse result) => GetRedirectDomainHint(result.Request.Url.ToString(), result.RedirectUrl);

        protected async Task<HttpResponse> HandleRequest(RequestBlock request, Dictionary<string, object> variables = null, string referer = null)
        {
            var requestLinkStr = ResolvePath(ApplyGoTemplateText(request.Path, variables)).ToString();
            _logger.Debug($"CardigannIndexer ({_definition.Id}): handleRequest() requestLinkStr= {requestLinkStr}");

            Dictionary<string, string> pairs = null;
            var queryCollection = new NameValueCollection();

            var method = HttpMethod.GET;
            if (string.Equals(request.Method, "post", StringComparison.OrdinalIgnoreCase))
            {
                method = HttpMethod.POST;
                pairs = new Dictionary<string, string>();
            }

            foreach (var input in request.Inputs)
            {
                var value = ApplyGoTemplateText(input.Value, variables);
                if (method == HttpMethod.GET)
                {
                    queryCollection.Add(input.Key, value);
                }
                else if (method == HttpMethod.POST)
                {
                    pairs.Add(input.Key, value);
                }
            }

            if (queryCollection.Count > 0)
            {
                if (!requestLinkStr.Contains("?"))
                {
                    // TODO Need Encoding here if we add it back
                    requestLinkStr += "?" + queryCollection.GetQueryString(separator: request.Queryseparator).Substring(1);
                }
                else
                {
                    requestLinkStr += queryCollection.GetQueryString(separator: request.Queryseparator);
                }
            }

            var httpRequest = new HttpRequestBuilder(requestLinkStr)
                .SetCookies(Cookies ?? new Dictionary<string, string>())
                .SetHeader("Referer", referer);

            httpRequest.Method = method;

            // Add form data for POST requests
            if (method == HttpMethod.POST)
            {
                foreach (var param in pairs)
                {
                    httpRequest.AddFormParameter(param.Key, param.Value);
                }
            }

            var response = await HttpClient.ExecuteProxiedAsync(httpRequest.Build(), Definition);

            _logger.Debug($"CardigannIndexer ({_definition.Id}): handleRequest() remote server returned {response.StatusCode.ToString()}");
            return response;
        }

        public async Task<HttpRequest> DownloadRequest(Uri link)
        {
            Cookies = GetCookies();
            var method = HttpMethod.GET;

            if (_definition.Download != null)
            {
                var download = _definition.Download;
                var variables = GetBaseTemplateVariables();

                AddTemplateVariablesFromUri(variables, link, ".DownloadUri");

                if (download.Before != null)
                {
                    await HandleRequest(download.Before, variables, link.ToString());
                }

                if (download.Method == "post")
                {
                    method = HttpMethod.POST;
                }

                if (download.Selector != null)
                {
                    var selector = ApplyGoTemplateText(download.Selector, variables);
                    var headers = ParseCustomHeaders(_definition.Search?.Headers, variables);

                    var request = new HttpRequestBuilder(link.ToString())
                        .SetCookies(Cookies ?? new Dictionary<string, string>())
                        .SetHeaders(headers ?? new Dictionary<string, string>())
                        .Build();

                    request.AllowAutoRedirect = true;

                    var response = await HttpClient.ExecuteProxiedAsync(request, Definition);

                    var results = response.Content;
                    var searchResultParser = new HtmlParser();
                    var searchResultDocument = searchResultParser.ParseDocument(results);
                    var downloadElement = searchResultDocument.QuerySelector(selector);
                    if (downloadElement != null)
                    {
                        _logger.Debug(string.Format("CardigannIndexer ({0}): Download selector {1} matched:{2}", _definition.Id, selector, downloadElement.ToHtmlPretty()));

                        var href = "";
                        if (download.Attribute != null)
                        {
                            href = downloadElement.GetAttribute(download.Attribute);
                            if (href == null)
                            {
                                throw new Exception(string.Format("Attribute \"{0}\" is not set for element {1}", download.Attribute, downloadElement.ToHtmlPretty()));
                            }
                        }
                        else
                        {
                            href = downloadElement.TextContent;
                        }

                        href = ApplyFilters(href, download.Filters, variables);
                        link = ResolvePath(href, link);
                    }
                    else
                    {
                        _logger.Error(string.Format("CardigannIndexer ({0}): Download selector {1} didn't match:\n{2}", _definition.Id, download.Selector, results));
                        throw new Exception(string.Format("Download selector {0} didn't match", download.Selector));
                    }
                }
            }

            var downloadRequest = new HttpRequestBuilder(link.AbsoluteUri)
                .SetCookies(Cookies ?? new Dictionary<string, string>())
                .Build();

            downloadRequest.Method = method;

            return downloadRequest;
        }

        public bool CheckIfLoginIsNeeded(HttpResponse response)
        {
            if (response.HasHttpRedirect)
            {
                var domainHint = GetRedirectDomainHint(response);
                if (domainHint != null)
                {
                    var errormessage = "Got redirected to another domain. Try changing the indexer URL to " + domainHint + ".";

                    throw new Exception(errormessage);
                }

                return true;
            }

            if (_definition.Login == null || _definition.Login.Test == null)
            {
                return false;
            }

            if (response.HasHttpError)
            {
                return true;
            }

            var parser = new HtmlParser();
            var document = parser.ParseDocument(response.Content);

            if (_definition.Login.Test.Selector != null)
            {
                var selection = document.QuerySelectorAll(_definition.Login.Test.Selector);
                if (selection.Length == 0)
                {
                    return true;
                }
            }

            return false;
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

                var requestbuilder = new HttpRequestBuilder(searchUrl);

                requestbuilder.Method = method;

                // Add FormData for searchs that POST
                if (method == HttpMethod.POST)
                {
                    foreach (var param in queryCollection)
                    {
                        requestbuilder.AddFormParameter(param.Key, param.Value);
                    }
                }

                var request = new CardigannRequest(requestbuilder.Build(), variables);

                // send HTTP request
                if (search.Headers != null)
                {
                    foreach (var header in search.Headers)
                    {
                        request.HttpRequest.Headers.Add(header.Key, header.Value[0]);
                    }
                }

                yield return request;
            }
        }
    }
}
