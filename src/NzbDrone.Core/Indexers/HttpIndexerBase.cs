using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentValidation.Results;
using MonoTorrent;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
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

        protected readonly IIndexerHttpClient _httpClient;
        protected readonly IEventAggregator _eventAggregator;
        public IDictionary<string, string> Cookies { get; set; }

        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;
        public override bool SupportsRedirect => false;
        public override bool SupportsPagination => false;

        public override Encoding Encoding => Encoding.UTF8;
        public override string Language => "en-US";
        public override string[] LegacyUrls => Array.Empty<string>();

        public override bool FollowRedirect => false;
        public override IndexerCapabilities Capabilities { get; protected set; }
        public virtual int PageSize => 0;
        public virtual TimeSpan RateLimit => TimeSpan.FromSeconds(2);

        public abstract IIndexerRequestGenerator GetRequestGenerator();
        public abstract IParseIndexerResponse GetParser();

        public HttpIndexerBase(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(indexerStatusService, configService, logger)
        {
            _httpClient = httpClient;
            _eventAggregator = eventAggregator;
        }

        public override Task<IndexerPageableQueryResult> Fetch(MovieSearchCriteria searchCriteria)
        {
            if (!SupportsSearch && !SupportsRss)
            {
                return Task.FromResult(new IndexerPageableQueryResult());
            }

            if (!SupportsPagination && searchCriteria.Offset is > 0)
            {
                return Task.FromResult(new IndexerPageableQueryResult());
            }

            var caps = GetCapabilities();

            if ((searchCriteria.ImdbId.IsNotNullOrWhiteSpace() && !caps.MovieSearchImdbAvailable) ||
                (searchCriteria.TmdbId.HasValue && !caps.MovieSearchTmdbAvailable) ||
                (searchCriteria.TraktId.HasValue && !caps.MovieSearchTraktAvailable) ||
                (searchCriteria.DoubanId.HasValue && !caps.MovieSearchDoubanAvailable) ||
                (searchCriteria.Genre.IsNotNullOrWhiteSpace() && !caps.MovieSearchGenreAvailable) ||
                (searchCriteria.Year.HasValue && !caps.MovieSearchYearAvailable))
            {
                _logger.Debug("Movie search skipped due to unsupported capabilities used: {0}", Definition.Name);

                return Task.FromResult(new IndexerPageableQueryResult());
            }

            return FetchReleases(g => SetCookieFunctions(g).GetSearchRequests(searchCriteria), searchCriteria);
        }

        public override Task<IndexerPageableQueryResult> Fetch(MusicSearchCriteria searchCriteria)
        {
            if (!SupportsSearch && !SupportsRss)
            {
                return Task.FromResult(new IndexerPageableQueryResult());
            }

            if (!SupportsPagination && searchCriteria.Offset is > 0)
            {
                return Task.FromResult(new IndexerPageableQueryResult());
            }

            var caps = GetCapabilities();

            if ((searchCriteria.Album.IsNotNullOrWhiteSpace() && !caps.MusicSearchAlbumAvailable) ||
                (searchCriteria.Artist.IsNotNullOrWhiteSpace() && !caps.MusicSearchArtistAvailable) ||
                (searchCriteria.Label.IsNotNullOrWhiteSpace() && !caps.MusicSearchLabelAvailable) ||
                (searchCriteria.Track.IsNotNullOrWhiteSpace() && !caps.MusicSearchTrackAvailable) ||
                (searchCriteria.Genre.IsNotNullOrWhiteSpace() && !caps.MusicSearchGenreAvailable) ||
                (searchCriteria.Year.HasValue && !caps.MusicSearchYearAvailable))
            {
                _logger.Debug("Music search skipped due to unsupported capabilities used: {0}", Definition.Name);

                return Task.FromResult(new IndexerPageableQueryResult());
            }

            return FetchReleases(g => SetCookieFunctions(g).GetSearchRequests(searchCriteria), searchCriteria);
        }

        public override Task<IndexerPageableQueryResult> Fetch(TvSearchCriteria searchCriteria)
        {
            if (!SupportsSearch && !SupportsRss)
            {
                return Task.FromResult(new IndexerPageableQueryResult());
            }

            if (!SupportsPagination && searchCriteria.Offset is > 0)
            {
                return Task.FromResult(new IndexerPageableQueryResult());
            }

            var caps = GetCapabilities();

            if ((searchCriteria.ImdbId.IsNotNullOrWhiteSpace() && !caps.TvSearchImdbAvailable) ||
                (searchCriteria.TvdbId.HasValue && !caps.TvSearchTvdbAvailable) ||
                (searchCriteria.RId.HasValue && !caps.TvSearchTvRageAvailable) ||
                (searchCriteria.TvMazeId.HasValue && !caps.TvSearchTvMazeAvailable) ||
                (searchCriteria.TraktId.HasValue && !caps.TvSearchTraktAvailable) ||
                (searchCriteria.TmdbId.HasValue && !caps.TvSearchTmdbAvailable) ||
                (searchCriteria.DoubanId.HasValue && !caps.TvSearchDoubanAvailable) ||
                (searchCriteria.Genre.IsNotNullOrWhiteSpace() && !caps.TvSearchGenreAvailable) ||
                (searchCriteria.Year.HasValue && !caps.TvSearchYearAvailable))
            {
                _logger.Debug("TV search skipped due to unsupported capabilities used: {0}", Definition.Name);

                return Task.FromResult(new IndexerPageableQueryResult());
            }

            return FetchReleases(g => SetCookieFunctions(g).GetSearchRequests(searchCriteria), searchCriteria);
        }

        public override Task<IndexerPageableQueryResult> Fetch(BookSearchCriteria searchCriteria)
        {
            if (!SupportsSearch && !SupportsRss)
            {
                return Task.FromResult(new IndexerPageableQueryResult());
            }

            if (!SupportsPagination && searchCriteria.Offset is > 0)
            {
                return Task.FromResult(new IndexerPageableQueryResult());
            }

            var caps = GetCapabilities();

            if ((searchCriteria.Title.IsNotNullOrWhiteSpace() && !caps.BookSearchTitleAvailable) ||
                (searchCriteria.Author.IsNotNullOrWhiteSpace() && !caps.BookSearchAuthorAvailable) ||
                (searchCriteria.Publisher.IsNotNullOrWhiteSpace() && !caps.BookSearchPublisherAvailable) ||
                (searchCriteria.Genre.IsNotNullOrWhiteSpace() && !caps.BookSearchGenreAvailable) ||
                (searchCriteria.Year.HasValue && !caps.BookSearchYearAvailable))
            {
                _logger.Debug("Book search skipped due to unsupported capabilities used: {0}", Definition.Name);

                return Task.FromResult(new IndexerPageableQueryResult());
            }

            return FetchReleases(g => SetCookieFunctions(g).GetSearchRequests(searchCriteria), searchCriteria);
        }

        public override Task<IndexerPageableQueryResult> Fetch(BasicSearchCriteria searchCriteria)
        {
            if (!SupportsSearch && !SupportsRss)
            {
                return Task.FromResult(new IndexerPageableQueryResult());
            }

            if (!SupportsPagination && searchCriteria.Offset is > 0)
            {
                return Task.FromResult(new IndexerPageableQueryResult());
            }

            return FetchReleases(g => SetCookieFunctions(g).GetSearchRequests(searchCriteria), searchCriteria);
        }

        public override async Task<byte[]> Download(Uri link)
        {
            Cookies = GetCookies();

            var request = await GetDownloadRequest(link);

            if (request.Url.Scheme == "magnet")
            {
                ValidateMagnet(request.Url.FullUri);
                return Encoding.UTF8.GetBytes(request.Url.FullUri);
            }

            if (request.RateLimit < RateLimit)
            {
                request.RateLimit = RateLimit;
            }

            byte[] fileData;

            try
            {
                var response = await _httpClient.ExecuteProxiedAsync(request, Definition);
                fileData = response.ResponseData;

                _logger.Debug("Downloaded for release finished ({0} bytes from {1})", fileData.Length, link.AbsoluteUri);
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.Error(ex, "Downloading file for release failed since it no longer exists ({0})", link.AbsoluteUri);
                    throw new ReleaseUnavailableException("Download failed", ex);
                }

                if (ex.Response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _logger.Error("API Grab Limit reached for {0}", link.AbsoluteUri);
                }
                else
                {
                    _logger.Error(ex, "Downloading for release failed ({0})", link.AbsoluteUri);
                }

                throw new ReleaseDownloadException("Download failed", ex);
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Downloading for release failed ({0})", link.AbsoluteUri);

                throw new ReleaseDownloadException("Download failed", ex);
            }
            catch (Exception)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.Error("Download failed");
                throw;
            }

            ValidateDownloadData(fileData);

            return fileData;
        }

        protected virtual Task<HttpRequest> GetDownloadRequest(Uri link)
        {
            var requestBuilder = new HttpRequestBuilder(link.AbsoluteUri)
            {
                AllowAutoRedirect = FollowRedirect
            };

            if (Cookies != null)
            {
                requestBuilder.SetCookies(Cookies);
            }

            var request = requestBuilder.Build();

            return Task.FromResult(request);
        }

        protected virtual void ValidateDownloadData(byte[] fileData)
        {
        }

        protected void ValidateMagnet(string link)
        {
            MagnetLink.Parse(link);
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

        protected virtual async Task<IndexerPageableQueryResult> FetchReleases(Func<IIndexerRequestGenerator, IndexerPageableRequestChain> pageableRequestChainSelector, SearchCriteriaBase searchCriteria, bool isRecent = false)
        {
            var releases = new List<ReleaseInfo>();
            var result = new IndexerPageableQueryResult();
            var url = string.Empty;
            var minimumBackoff = TimeSpan.FromHours(1);

            try
            {
                var generator = GetRequestGenerator();
                var parser = GetParser();
                parser.CookiesUpdater = (cookies, expiration) =>
                {
                    _indexerStatusService.UpdateCookies(Definition.Id, cookies, expiration);
                };

                var pageableRequestChain = pageableRequestChainSelector(generator);

                for (var i = 0; i < pageableRequestChain.Tiers; i++)
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
                if (webException.Status is WebExceptionStatus.NameResolutionFailure or WebExceptionStatus.ConnectFailure)
                {
                    _indexerStatusService.RecordConnectionFailure(Definition.Id);
                }
                else
                {
                    _indexerStatusService.RecordFailure(Definition.Id);
                }

                if (webException.Message.Contains("502") || webException.Message.Contains("503") ||
                    webException.Message.Contains("504") || webException.Message.Contains("timed out"))
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
                result.Queries.Add(new IndexerQueryResult { Response = ex.Response });

                var retryTime = ex.RetryAfter != TimeSpan.Zero ? ex.RetryAfter : minimumBackoff;

                _indexerStatusService.RecordFailure(Definition.Id, retryTime);
                _logger.Warn("Request Limit reached for {0}. Disabled for {1}", this, retryTime);
            }
            catch (HttpException ex)
            {
                result.Queries.Add(new IndexerQueryResult { Response = ex.Response });
                _indexerStatusService.RecordFailure(Definition.Id);

                if (ex.Response.HasHttpServerError)
                {
                    _logger.Warn("Unable to connect to {0} at [{1}]. Indexer's server is unavailable. Try again later. {2}", this, url, ex.Message);
                }
                else
                {
                    _logger.Warn("{0} {1}", this, ex.Message);
                }
            }
            catch (RequestLimitReachedException ex)
            {
                result.Queries.Add(new IndexerQueryResult { Response = ex.Response.HttpResponse });
                _indexerStatusService.RecordFailure(Definition.Id, minimumBackoff);
                _logger.Warn("Request Limit reached for {0}. Disabled for {1}", this, minimumBackoff);
            }
            catch (IndexerAuthException ex)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.Warn(ex, "Invalid Credentials for {0} {1}", this, url);
            }
            catch (CloudFlareProtectionException ex)
            {
                result.Queries.Add(new IndexerQueryResult { Response = ex.Response });
                _indexerStatusService.RecordFailure(Definition.Id);
                ex.WithData("FeedUrl", url);
                _logger.Error(ex, "Cloudflare protection detected for {0}, Flaresolverr may be required.", this);
            }
            catch (IndexerException ex)
            {
                result.Queries.Add(new IndexerQueryResult { Response = ex.Response.HttpResponse });
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.Warn(ex, "{0}", url);
            }
            catch (HttpRequestException ex)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.Warn(ex, "Unable to connect to indexer, please check your DNS settings and ensure IPv6 is working or disabled. {0}", url);
            }
            catch (TaskCanceledException ex)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.Warn(ex, "Unable to connect to indexer, possibly due to a timeout. {0}", url);
            }
            catch (Exception ex)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                ex.WithData("FeedUrl", url);
                _logger.Error(ex, "An error occurred while processing indexer feed. {0}", url);
            }

            result.Releases = CleanupReleases(releases, searchCriteria);

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
            var response = await FetchIndexerResponse(request);

            try
            {
                var releases = parser.ParseResponse(response).ToList();

                if (releases.Count == 0)
                {
                    _logger.Trace("No releases found. Response: {0}", response.Content);
                }

                return new IndexerQueryResult
                {
                    Releases = releases,
                    Response = response.HttpResponse
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
            request.HttpRequest.Encoding ??= Encoding;

            var response = await _httpClient.ExecuteProxiedAsync(request.HttpRequest, Definition);

            // Check response to see if auth is needed, if needed try again
            if (CheckIfLoginNeeded(response))
            {
                _logger.Trace("Attempting to re-auth based on indexer search response");

                await DoLogin();

                request.HttpRequest.Url = originalUrl;
                ModifyRequest(request);

                response = await _httpClient.ExecuteProxiedAsync(request.HttpRequest, Definition);
            }

            if (CloudFlareDetectionService.IsCloudflareProtected(response))
            {
                throw new CloudFlareProtectionException(response);
            }

            // Throw common http errors here before we try to parse
            if (response.HasHttpError && (request.HttpRequest.SuppressHttpErrorStatusCodes == null || !request.HttpRequest.SuppressHttpErrorStatusCodes.Contains(response.StatusCode)))
            {
                if (response.Request.LogHttpError)
                {
                    _logger.Warn("HTTP Error - {0}", response);
                }

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    throw new TooManyRequestsException(request.HttpRequest, response);
                }

                if (response.HasHttpServerError)
                {
                    throw new HttpException(request.HttpRequest, response);
                }
            }

            UpdateCookies(request.HttpRequest.Cookies, DateTime.Now.AddDays(30));

            return new IndexerResponse(request, response);
        }

        protected async Task<HttpResponse> ExecuteAuth(HttpRequest request)
        {
            request.Encoding = Encoding;

            if (request.RequestTimeout == TimeSpan.Zero)
            {
                request.RequestTimeout = TimeSpan.FromSeconds(15);
            }

            if (request.RateLimit < RateLimit)
            {
                request.RateLimit = RateLimit;
            }

            var response = await _httpClient.ExecuteProxiedAsync(request, Definition);

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

                var testCriteria = new BasicSearchCriteria { SearchType = "search" };

                if (!SupportsRss)
                {
                    testCriteria.SearchTerm = "test";
                }

                var firstRequest = generator.GetSearchRequests(testCriteria).GetAllTiers().FirstOrDefault()?.FirstOrDefault();

                if (firstRequest == null)
                {
                    return new ValidationFailure(string.Empty, "No rss feed query available. This may be an issue with the indexer or your indexer category settings.");
                }

                var releases = await FetchPage(firstRequest, parser);

                if (releases.Releases.Empty())
                {
                    return new ValidationFailure(string.Empty, "Query successful, but no results were returned from your indexer. This may be an issue with the indexer, your indexer category settings, or other indexer settings such as search freeleech only etc.");
                }
            }
            catch (IndexerAuthException ex)
            {
                _logger.Warn("Indexer returned result for RSS URL, Credentials appears to be invalid. Response: " + ex.Message);

                return new ValidationFailure("", "Indexer returned result for RSS URL, Credentials appears to be invalid. Response: " + ex.Message);
            }
            catch (RequestLimitReachedException ex)
            {
                _logger.Warn("Request limit reached: " + ex.Message);
            }
            catch (CloudFlareProtectionException ex)
            {
                return new ValidationFailure(string.Empty, ex.Message);
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

                _logger.Warn(ex, "Unable to connect to indexer");

                if (ex.Response.HasHttpServerError)
                {
                    return new ValidationFailure(string.Empty, "Unable to connect to indexer, indexer's server is unavailable. Try again later. " + ex.Message);
                }

                return new ValidationFailure(string.Empty, "Unable to connect to indexer, check the log above the ValidationFailure for more details. " + ex.Message);
            }
            catch (HttpRequestException ex)
            {
                _logger.Warn(ex, "Unable to connect to indexer");

                return new ValidationFailure(string.Empty, "Unable to connect to indexer, please check your DNS settings and ensure IPv6 is working or disabled. " + ex.Message);
            }
            catch (TaskCanceledException ex)
            {
                _logger.Warn(ex, "Unable to connect to indexer");

                return new ValidationFailure(string.Empty, "Unable to connect to indexer, possibly due to a timeout. Try again or check your network settings. " + ex.Message);
            }
            catch (WebException webException)
            {
                _logger.Warn("Unable to connect to indexer.");

                if (webException.Status is WebExceptionStatus.NameResolutionFailure or WebExceptionStatus.ConnectFailure)
                {
                    return new ValidationFailure(string.Empty, "Unable to connect to indexer connection failure. Check your connection to the indexer's server and DNS." + webException.Message);
                }

                if (webException.Message.Contains("502") || webException.Message.Contains("503") ||
                    webException.Message.Contains("504") || webException.Message.Contains("timed out"))
                {
                    return new ValidationFailure(string.Empty, "Unable to connect to indexer, indexer's server is unavailable. Try again later. " + webException.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to connect to indexer");

                return new ValidationFailure(string.Empty, "Unable to connect to indexer, check the log above the ValidationFailure for more details. " + ex.Message);
            }

            return null;
        }
    }
}
