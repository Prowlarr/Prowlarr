using System;
using System.Collections.Generic;
using System.Net;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Applications.Lidarr
{
    public interface ILidarrV1Proxy
    {
        LidarrIndexer AddIndexer(LidarrIndexer indexer, LidarrSettings settings);
        List<LidarrIndexer> GetIndexers(LidarrSettings settings);
        List<LidarrIndexer> GetIndexerSchema(LidarrSettings settings);
        ValidationFailure Test(LidarrSettings settings);
    }

    public class LidarrV1Proxy : ILidarrV1Proxy
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public LidarrV1Proxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public LidarrStatus GetStatus(LidarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v1/system/status", HttpMethod.GET);
            return Execute<LidarrStatus>(request);
        }

        public List<LidarrIndexer> GetIndexers(LidarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v1/indexer", HttpMethod.GET);
            return Execute<List<LidarrIndexer>>(request);
        }

        public void RemoveIndexer(int indexerId, LidarrSettings settings)
        {
            var request = BuildRequest(settings, $"/api/v1/indexer/{indexerId}", HttpMethod.DELETE);
            var response = _httpClient.Execute(request);
        }

        public List<LidarrIndexer> GetIndexerSchema(LidarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v1/indexer/schema", HttpMethod.GET);
            return Execute<List<LidarrIndexer>>(request);
        }

        public LidarrIndexer AddIndexer(LidarrIndexer indexer, LidarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v1/indexer", HttpMethod.POST);

            request.SetContent(indexer.ToJson());

            return Execute<LidarrIndexer>(request);
        }

        public ValidationFailure Test(LidarrSettings settings)
        {
            try
            {
                GetStatus(settings);
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.Error(ex, "API Key is invalid");
                    return new ValidationFailure("ApiKey", "API Key is invalid");
                }

                _logger.Error(ex, "Unable to send test message");
                return new ValidationFailure("ApiKey", "Unable to send test message");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to send test message");
                return new ValidationFailure("", "Unable to send test message");
            }

            return null;
        }

        private HttpRequest BuildRequest(LidarrSettings settings, string resource, HttpMethod method)
        {
            var baseUrl = settings.BaseUrl.TrimEnd('/');

            var request = new HttpRequestBuilder(baseUrl).Resource(resource)
                .SetHeader("X-Api-Key", settings.ApiKey)
                .Build();

            request.Headers.ContentType = "application/json";

            request.Method = method;

            return request;
        }

        private TResource Execute<TResource>(HttpRequest request)
             where TResource : new()
        {
            var response = _httpClient.Execute(request);

            var results = JsonConvert.DeserializeObject<TResource>(response.Content);

            return results;
        }
    }
}
