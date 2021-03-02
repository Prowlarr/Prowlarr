using System;
using System.Collections.Generic;
using System.Net;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Applications.Radarr
{
    public interface IRadarrV3Proxy
    {
        RadarrIndexer AddIndexer(RadarrIndexer indexer, RadarrSettings settings);
        List<RadarrIndexer> GetIndexers(RadarrSettings settings);
        RadarrIndexer GetIndexer(int indexerId, RadarrSettings settings);
        List<RadarrIndexer> GetIndexerSchema(RadarrSettings settings);
        void RemoveIndexer(int indexerId, RadarrSettings settings);
        RadarrIndexer UpdateIndexer(RadarrIndexer indexer, RadarrSettings settings);
        ValidationFailure Test(RadarrSettings settings);
    }

    public class RadarrV3Proxy : IRadarrV3Proxy
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public RadarrV3Proxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public RadarrStatus GetStatus(RadarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v3/system/status", HttpMethod.GET);
            return Execute<RadarrStatus>(request);
        }

        public List<RadarrIndexer> GetIndexers(RadarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v3/indexer", HttpMethod.GET);
            return Execute<List<RadarrIndexer>>(request);
        }

        public RadarrIndexer GetIndexer(int indexerId, RadarrSettings settings)
        {
            try
            {
                var request = BuildRequest(settings, $"/api/v3/indexer/{indexerId}", HttpMethod.GET);
                return Execute<RadarrIndexer>(request);
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

        public void RemoveIndexer(int indexerId, RadarrSettings settings)
        {
            var request = BuildRequest(settings, $"/api/v3/indexer/{indexerId}", HttpMethod.DELETE);
            var response = _httpClient.Execute(request);
        }

        public List<RadarrIndexer> GetIndexerSchema(RadarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v3/indexer/schema", HttpMethod.GET);
            return Execute<List<RadarrIndexer>>(request);
        }

        public RadarrIndexer AddIndexer(RadarrIndexer indexer, RadarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v3/indexer", HttpMethod.POST);

            request.SetContent(indexer.ToJson());

            return Execute<RadarrIndexer>(request);
        }

        public RadarrIndexer UpdateIndexer(RadarrIndexer indexer, RadarrSettings settings)
        {
            var request = BuildRequest(settings, $"/api/v3/indexer/{indexer.Id}", HttpMethod.PUT);

            request.SetContent(indexer.ToJson());

            return Execute<RadarrIndexer>(request);
        }

        public ValidationFailure Test(RadarrSettings settings)
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

        private HttpRequest BuildRequest(RadarrSettings settings, string resource, HttpMethod method)
        {
            var baseUrl = settings.BaseUrl.TrimEnd('/');

            var request = new HttpRequestBuilder(baseUrl).Resource(resource)
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

            var results = JsonConvert.DeserializeObject<TResource>(response.Content);

            return results;
        }
    }
}
