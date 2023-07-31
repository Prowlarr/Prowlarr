using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Applications.Readarr
{
    public interface IReadarrV1Proxy
    {
        ReadarrIndexer AddIndexer(ReadarrIndexer indexer, ReadarrSettings settings);
        List<ReadarrIndexer> GetIndexers(ReadarrSettings settings);
        ReadarrIndexer GetIndexer(int indexerId, ReadarrSettings settings);
        List<ReadarrIndexer> GetIndexerSchema(ReadarrSettings settings);
        void RemoveIndexer(int indexerId, ReadarrSettings settings);
        ReadarrIndexer UpdateIndexer(ReadarrIndexer indexer, ReadarrSettings settings);
        ValidationFailure TestConnection(ReadarrIndexer indexer, ReadarrSettings settings);
    }

    public class ReadarrV1Proxy : IReadarrV1Proxy
    {
        private const string AppApiRoute = "/api/v1";
        private const string AppIndexerApiRoute = $"{AppApiRoute}/indexer";
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public ReadarrV1Proxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public ReadarrStatus GetStatus(ReadarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppApiRoute}/system/status", HttpMethod.Get);
            return Execute<ReadarrStatus>(request);
        }

        public List<ReadarrIndexer> GetIndexers(ReadarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}", HttpMethod.Get);
            return Execute<List<ReadarrIndexer>>(request);
        }

        public ReadarrIndexer GetIndexer(int indexerId, ReadarrSettings settings)
        {
            try
            {
                var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexerId}", HttpMethod.Get);
                return Execute<ReadarrIndexer>(request);
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }

            return null;
        }

        public void RemoveIndexer(int indexerId, ReadarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexerId}", HttpMethod.Delete);
            _httpClient.Execute(request);
        }

        public List<ReadarrIndexer> GetIndexerSchema(ReadarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/schema", HttpMethod.Get);
            return Execute<List<ReadarrIndexer>>(request);
        }

        public ReadarrIndexer AddIndexer(ReadarrIndexer indexer, ReadarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}", HttpMethod.Post);

            request.SetContent(indexer.ToJson());

            try
            {
                return ExecuteIndexerRequest(request);
            }
            catch (HttpException ex) when (ex.Response.StatusCode == HttpStatusCode.BadRequest)
            {
                _logger.Debug("Retrying to add indexer forcefully");

                request.Url = request.Url.AddQueryParam("forceSave", "true");

                return ExecuteIndexerRequest(request);
            }
        }

        public ReadarrIndexer UpdateIndexer(ReadarrIndexer indexer, ReadarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexer.Id}", HttpMethod.Put);

            request.SetContent(indexer.ToJson());

            try
            {
                return ExecuteIndexerRequest(request);
            }
            catch (HttpException ex) when (ex.Response.StatusCode == HttpStatusCode.BadRequest)
            {
                _logger.Debug("Retrying to update indexer forcefully");

                request.Url = request.Url.AddQueryParam("forceSave", "true");

                return ExecuteIndexerRequest(request);
            }
        }

        public ValidationFailure TestConnection(ReadarrIndexer indexer, ReadarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/test", HttpMethod.Post);

            request.SetContent(indexer.ToJson());

            _httpClient.Post(request);

            return null;
        }

        private ReadarrIndexer ExecuteIndexerRequest(HttpRequest request)
        {
            try
            {
                return Execute<ReadarrIndexer>(request);
            }
            catch (HttpException ex)
            {
                switch (ex.Response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        _logger.Warn(ex, "API Key is invalid");
                        break;
                    case HttpStatusCode.BadRequest:
                        if (ex.Response.Content.Contains("Query successful, but no results in the configured categories were returned from your indexer.", StringComparison.InvariantCultureIgnoreCase))
                        {
                            _logger.Warn(ex, "No Results in configured categories. See FAQ Entry: Prowlarr will not sync X Indexer to App");
                            break;
                        }

                        _logger.Error(ex, "Invalid Request");
                        break;
                    case HttpStatusCode.SeeOther:
                        _logger.Warn(ex, "App returned redirect and is invalid. Check App URL");
                        break;
                    case HttpStatusCode.NotFound:
                        _logger.Warn(ex, "Remote indexer not found");
                        break;
                    default:
                        _logger.Error(ex, "Unexpected response status code: {0}", ex.Response.StatusCode);
                        break;
                }

                throw;
            }
            catch (JsonReaderException ex)
            {
                _logger.Error(ex, "Unable to parse JSON response from application");
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to add or update indexer");
                throw;
            }
        }

        private HttpRequest BuildRequest(ReadarrSettings settings, string resource, HttpMethod method)
        {
            var baseUrl = settings.BaseUrl.TrimEnd('/');

            var request = new HttpRequestBuilder(baseUrl)
                .Resource(resource)
                .Accept(HttpAccept.Json)
                .SetHeader("X-Api-Key", settings.ApiKey)
                .Build();

            request.Headers.ContentType = "application/json";

            request.Method = method;
            request.AllowAutoRedirect = true;

            return request;
        }

        private TResource Execute<TResource>(HttpRequest request)
             where TResource : new()
        {
            var response = _httpClient.Execute(request);

            if ((int)response.StatusCode >= 300)
            {
                throw new HttpException(response);
            }

            return Json.Deserialize<TResource>(response.Content);
        }
    }
}
