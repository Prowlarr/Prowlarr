using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Http.CloudFlare;
using NzbDrone.Core.Indexers.Events;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers
{
    public abstract class HttpIndexerBase<TSettings> : IndexerBase<TSettings>
        where TSettings : IIndexerSettings, new()
    {
        protected const int MaxNumResultsPerQuery = 1000;

        protected readonly IHttpClient _httpClient;
        protected readonly IEventAggregator _eventAggregator;
        public IDictionary<string, string> Cookies { get; set; }

        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;
        public override bool SupportsRedirect => false;

        public override Encoding Encoding => Encoding.UTF8;
        public override string Language => "en-US";

        public override bool FollowRedirect => false;
        public override IndexerCapabilities Capabilities { get; protected set; }
        public virtual int PageSize => 0;
        public virtual TimeSpan RateLimit => TimeSpan.FromSeconds(2);

        public abstract IIndexerRequestGenerator GetRequestGenerator();
        public abstract IParseIndexerResponse GetParser();

        public HttpIndexerBase(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(indexerStatusService, configService, logger)
        {
            _httpClient = httpClient;
            _eventAggregator = eventAggregator;
        }

        public override Task<IndexerPageableQueryResult> Fetch(MovieSearchCriteria searchCriteria)
        {
            if (!SupportsSearch)
            {
                return Task.FromResult(new IndexerPageableQueryResult());
            }

            return FetchReleases(g => SetCookieFunctions(g).GetSearchRequests(searchCriteria));
        }

        public override Task<IndexerPageableQueryResult> Fetch(MusicSearchCriteria searchCriteria)
        {
            if (!SupportsSearch)
            {
                return Task.FromResult(new IndexerPageableQueryResult());
            }

            return FetchReleases(g => SetCookieFunctions(g).GetSearchRequests(searchCriteria));
        }

        public override Task<IndexerPageableQueryResult> Fetch(TvSearchCriteria searchCriteria)
        {
            if (!SupportsSearch)
            {
                return Task.FromResult(new IndexerPageableQueryResult());
            }

            return FetchReleases(g => SetCookieFunctions(g).GetSearchRequests(searchCriteria));
        }

        public override Task<IndexerPageableQueryResult> Fetch(BookSearchCriteria searchCriteria)
        {
            if (!SupportsSearch)
            {
                return Task.FromResult(new IndexerPageableQueryResult());
            }

            return FetchReleases(g => SetCookieFunctions(g).GetSearchRequests(searchCriteria));
        }

        public override Task<IndexerPageableQueryResult> Fetch(BasicSearchCriteria searchCriteria)
        {
            if (!SupportsSearch)
            {
                return Task.FromResult(new IndexerPageableQueryResult());
            }

            return FetchReleases(g => SetCookieFunctions(g).GetSearchRequests(searchCriteria));
        }

        protected IIndexerRequestGenerator SetCookieFunctions(IIndexerRequestGenerator generator)
        {
            //A func ensures cookies are always updated to the latest. This way, the first page could update the cookies and then can be reused by the second page.
            generator.GetCookies = () =>
            {
                var cookies = _indexerStatusService.GetIndexerCookies(Definition.Id);
                var expiration = _indexerStatusService.GetIndexerCookiesExpirationDate(Definition.Id);
                if (expiration < DateTime.Now)
                {
                    cookies = null;
                }

                return cookies;
            };

            generator.CookiesUpdater = (cookies, expiration) =>
            {
                UpdateCookies(cookies, expiration);
            };

            return generator;
        }

        protected virtual IDictionary<string, string> GetCookies()
        {
            var cookies = _indexerStatusService.GetIndexerCookies(Definition.Id);
            var expiration = _indexerStatusService.GetIndexerCookiesExpirationDate(Definition.Id);
            if (expiration < DateTime.Now)
            {
                cookies = null;
            }

            return cookies;
        }

        protected void UpdateCookies(IDictionary<string, string> cookies, DateTime? expiration)
        {
            Cookies = cookies;
            _indexerStatusService.UpdateCookies(Definition.Id, cookies, expiration);
        }

        protected virtual async Task<IndexerPageableQueryResult> FetchReleases(Func<IIndexerRequestGenerator, IndexerPageableRequestChain> pageableRequestChainSelector, bool isRecent = false)
        {
            var releases = new List<ReleaseInfo>();
            var result = new IndexerPageableQueryResult();
            var url = string.Empty;

            try
            {
                var generator = GetRequestGenerator();
                var parser = GetParser();
                parser.CookiesUpdater = (cookies, expiration) =>
                {
                    _indexerStatusService.UpdateCookies(Definition.Id, cookies, expiration);
                };

                var pageableRequestChain = pageableRequestChainSelector(generator);

                for (int i = 0; i < pageableRequestChain.Tiers; i++)
                {
                    var pageableRequests = pageableRequestChain.GetTier(i);

                    foreach (var pageableRequest in pageableRequests)
                    {
                        var pagedReleases = new List<ReleaseInfo>();

                        var pageSize = PageSize;

                        foreach (var request in pageableRequest)
                        {
                            url = request.Url.FullUri;

                            var page = await FetchPage(request, parser);

                            pageSize = pageSize == 1 ? page.Releases.Count : pageSize;

                            result.Queries.Add(page);

                            pagedReleases.AddRange(page.Releases);

                            if (!IsFullPage(page.Releases, pageSize))
                            {
                                break;
                            }
                        }

                        releases.AddRange(pagedReleases);
                    }

                    if (releases.Any())
                    {
                        break;
                    }
                }

                _indexerStatusService.RecordSuccess(Definition.Id);
            }
            catch (WebException webException)
            {
                if (webException.Status == WebExceptionStatus.NameResolutionFailure ||
                    webException.Status == WebExceptionStatus.ConnectFailure)
                {
                    _indexerStatusService.RecordConnectionFailure(Definition.Id);
                }
                else
                {
                    _indexerStatusService.RecordFailure(Definition.Id);
                }

                if (webException.Message.Contains("502") || webException.Message.Contains("503") ||
                    webException.Message.Contains("timed out"))
                {
                    _logger.Warn("{0} server is currently unavailable. {1} {2}", this, url, webException.Message);
                }
                else
                {
                    _logger.Warn("{0} {1} {2}", this, url, webException.Message);
                }
            }
            catch (TooManyRequestsException ex)
            {
                result.Queries.Add(new IndexerQueryResult { ElapsedTime = ex.Response.ElapsedTime, StatusCode = (int)ex.Response.StatusCode });

                if (ex.RetryAfter != TimeSpan.Zero)
                {
                    _indexerStatusService.RecordFailure(Definition.Id, ex.RetryAfter);
                }
                else
                {
                    _indexerStatusService.RecordFailure(Definition.Id, TimeSpan.FromHours(1));
                }

                _logger.Warn("API Request Limit reached for {0}", this);
            }
            catch (HttpException ex)
            {
                result.Queries.Add(new IndexerQueryResult { ElapsedTime = ex.Response.ElapsedTime, StatusCode = (int)ex.Response.StatusCode });
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.Warn("{0} {1}", this, ex.Message);
            }
            catch (RequestLimitReachedException ex)
            {
                result.Queries.Add(new IndexerQueryResult { ElapsedTime = ex.Response.HttpResponse.ElapsedTime, StatusCode = (int)ex.Response.HttpResponse.StatusCode });
                _indexerStatusService.RecordFailure(Definition.Id, TimeSpan.FromHours(1));
                _logger.Warn("API Request Limit reached for {0}", this);
            }
            catch (IndexerAuthException)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.Warn("Invalid Credentials for {0} {1}", this, url);
            }
            catch (CloudFlareCaptchaException ex)
            {
                result.Queries.Add(new IndexerQueryResult { ElapsedTime = ex.Response.ElapsedTime, StatusCode = (int)ex.Response.StatusCode });
                _indexerStatusService.RecordFailure(Definition.Id);
                ex.WithData("FeedUrl", url);
                if (ex.IsExpired)
                {
                    _logger.Error(ex, "Expired CAPTCHA token for {0}, please refresh in indexer settings.", this);
                }
                else
                {
                    _logger.Error(ex, "CAPTCHA token required for {0}, check indexer settings.", this);
                }
            }
            catch (IndexerException ex)
            {
                result.Queries.Add(new IndexerQueryResult { ElapsedTime = ex.Response.HttpResponse.ElapsedTime, StatusCode = (int)ex.Response.HttpResponse.StatusCode });
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.Warn(ex, "{0}", url);
            }
            catch (Exception ex)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                ex.WithData("FeedUrl", url);
                _logger.Error(ex, "An error occurred while processing indexer feed. {0}", url);
            }

            result.Releases = CleanupReleases(releases);

            return result;
        }

        public override IndexerCapabilities GetCapabilities()
        {
            return Capabilities ?? ((IndexerDefinition)Definition).Capabilities;
        }

        protected virtual bool IsFullPage(IList<ReleaseInfo> page, int pageSize)
        {
            return pageSize != 0 && page.Count >= pageSize;
        }

        protected virtual async Task<IndexerQueryResult> FetchPage(IndexerRequest request, IParseIndexerResponse parser)
        {
            //request.HttpRequest.Encoding = Encoding;
            var response = await FetchIndexerResponse(request);

            try
            {
                var releases = parser.ParseResponse(response).ToList();

                if (releases.Count == 0)
                {
                    _logger.Trace(response.Content);
                }

                return new IndexerQueryResult
                {
                    Releases = releases,
                    ElapsedTime = response.HttpResponse.ElapsedTime,
                    StatusCode = (int)response.HttpResponse.StatusCode
                };
            }
            catch (Exception ex)
            {
                ex.WithData(response.HttpResponse, 128 * 1024);
                _logger.Trace("Unexpected Response content ({0} bytes): {1}", response.HttpResponse.ResponseData.Length, response.HttpResponse.Content);
                throw;
            }
        }

        protected virtual bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                return true;
            }

            return false;
        }

        protected virtual Task DoLogin()
        {
            return Task.CompletedTask;
        }

        protected virtual void ModifyRequest(IndexerRequest request)
        {
            request.HttpRequest.Cookies.Clear();

            if (Cookies != null)
            {
                foreach (var cookie in Cookies)
                {
                    request.HttpRequest.Cookies.Add(cookie.Key, cookie.Value);
                }
            }
        }

        protected virtual async Task<IndexerResponse> FetchIndexerResponse(IndexerRequest request)
        {
            _logger.Debug("Downloading Feed " + request.HttpRequest.ToString(false));

            if (request.HttpRequest.RateLimit < RateLimit)
            {
                request.HttpRequest.RateLimit = RateLimit;
            }

            if (_configService.LogIndexerResponse)
            {
                request.HttpRequest.LogResponseContent = true;
            }

            request.HttpRequest.AllowAutoRedirect = FollowRedirect;

            var originalUrl = request.Url;

            Cookies = GetCookies();

            if (Cookies != null)
            {
                foreach (var cookie in Cookies)
                {
                    request.HttpRequest.Cookies.Add(cookie.Key, cookie.Value);
                }
            }

            request.HttpRequest.SuppressHttpError = true;
            var response = await _httpClient.ExecuteAsync(request.HttpRequest);

            // Check reponse to see if auth is needed, if needed try again
            if (CheckIfLoginNeeded(response))
            {
                _logger.Trace("Attempting to re-auth based on indexer search response");

                await DoLogin();

                request.HttpRequest.Url = originalUrl;
                ModifyRequest(request);

                response = await _httpClient.ExecuteAsync(request.HttpRequest);
            }

            // Throw common http errors here before we try to parse
            if (response.HasHttpError)
            {
                _logger.Warn("HTTP Error - {0}", response);

                if ((int)response.StatusCode == 429)
                {
                    throw new TooManyRequestsException(request.HttpRequest, response);
                }
            }

            UpdateCookies(Cookies, DateTime.Now + TimeSpan.FromDays(30));

            return new IndexerResponse(request, response);
        }

        protected async Task<HttpResponse> ExecuteAuth(HttpRequest request)
        {
            var response = await _httpClient.ExecuteAsync(request);

            _eventAggregator.PublishEvent(new IndexerAuthEvent(Definition.Id, !response.HasHttpError, response.ElapsedTime));

            return response;
        }

        protected override async Task Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(await TestConnection());
        }

        protected virtual async Task<ValidationFailure> TestConnection()
        {
            try
            {
                var parser = GetParser();
                parser.CookiesUpdater = (cookies, expiration) =>
                {
                    _indexerStatusService.UpdateCookies(Definition.Id, cookies, expiration);
                };

                var generator = GetRequestGenerator();

                generator = SetCookieFunctions(generator);

                var firstRequest = generator.GetSearchRequests(new BasicSearchCriteria { SearchType = "search" }).GetAllTiers().FirstOrDefault()?.FirstOrDefault();

                if (firstRequest == null)
                {
                    return new ValidationFailure(string.Empty, "No rss feed query available. This may be an issue with the indexer or your indexer category settings.");
                }

                var releases = await FetchPage(firstRequest, parser);

                if (releases.Releases.Empty())
                {
                    return new ValidationFailure(string.Empty, "Query successful, but no results were returned from your indexer. This may be an issue with the indexer or your indexer category settings.");
                }
            }
            catch (IndexerAuthException ex)
            {
                _logger.Warn("Indexer returned result for RSS URL, Credentials appears to be invalid: " + ex.Message);

                return new ValidationFailure("", ex.Message);
            }
            catch (RequestLimitReachedException ex)
            {
                _logger.Warn("Request limit reached: " + ex.Message);
            }
            catch (CloudFlareCaptchaException ex)
            {
                if (ex.IsExpired)
                {
                    return new ValidationFailure("CaptchaToken", "CloudFlare CAPTCHA token expired, please Refresh.");
                }
                else
                {
                    return new ValidationFailure("CaptchaToken", "Site protected by CloudFlare CAPTCHA. Valid CAPTCHA token required.");
                }
            }
            catch (UnsupportedFeedException ex)
            {
                _logger.Warn(ex, "Indexer feed is not supported");

                return new ValidationFailure(string.Empty, "Indexer feed is not supported: " + ex.Message);
            }
            catch (IndexerException ex)
            {
                _logger.Warn(ex, "Unable to connect to indexer");

                return new ValidationFailure(string.Empty, "Unable to connect to indexer. " + ex.Message);
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.BadRequest &&
                    ex.Response.Content.Contains("not support the requested query"))
                {
                    _logger.Warn(ex, "Indexer does not support the query");
                    return new ValidationFailure(string.Empty, "Indexer does not support the current query. Check if the categories and or searching for movies are supported. Check the log for more details.");
                }
                else
                {
                    _logger.Warn(ex, "Unable to connect to indexer");

                    return new ValidationFailure(string.Empty, "Unable to connect to indexer, check the log for more details");
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to connect to indexer");

                return new ValidationFailure(string.Empty, "Unable to connect to indexer, check the log for more details");
            }

            return null;
        }
    }
}
