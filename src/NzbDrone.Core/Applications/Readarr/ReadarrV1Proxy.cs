using System;
using System.Collections.Generic;
using System.Net;
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
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public ReadarrV1Proxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public ReadarrStatus GetStatus(ReadarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v1/system/status", HttpMethod.GET);
            return Execute<ReadarrStatus>(request);
        }

        public List<ReadarrIndexer> GetIndexers(ReadarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v1/indexer", HttpMethod.GET);
            return Execute<List<ReadarrIndexer>>(request);
        }

        public ReadarrIndexer GetIndexer(int indexerId, ReadarrSettings settings)
        {
            try
            {
                var request = BuildRequest(settings, $"/api/v1/indexer/{indexerId}", HttpMethod.GET);
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
            var request = BuildRequest(settings, $"/api/v1/indexer/{indexerId}", HttpMethod.DELETE);
            _httpClient.Execute(request);
        }

        public List<ReadarrIndexer> GetIndexerSchema(ReadarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v1/indexer/schema", HttpMethod.GET);
            return Execute<List<ReadarrIndexer>>(request);
        }

        public ReadarrIndexer AddIndexer(ReadarrIndexer indexer, ReadarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v1/indexer", HttpMethod.POST);

            request.SetContent(indexer.ToJson());

            return Execute<ReadarrIndexer>(request);
        }

        public ReadarrIndexer UpdateIndexer(ReadarrIndexer indexer, ReadarrSettings settings)
        {
            var request = BuildRequest(settings, $"/api/v1/indexer/{indexer.Id}", HttpMethod.PUT);

            request.SetContent(indexer.ToJson());

            return Execute<ReadarrIndexer>(request);
        }

        public ValidationFailure TestConnection(ReadarrIndexer indexer, ReadarrSettings settings)
        {
            var request = BuildRequest(settings, $"/api/v1/indexer/test", HttpMethod.POST);

            request.SetContent(indexer.ToJson());

            try
            {
                Execute<ReadarrIndexer>(request);
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.Error(ex, "API Key is invalid");
                    return new ValidationFailure("ApiKey", "API Key is invalid");
                }

                if (ex.Response.StatusCode == HttpStatusCode.BadRequest)
                {
                    _logger.Error(ex, "Prowlarr URL is invalid");
                    return new ValidationFailure("ProwlarrUrl", "Prowlarr url is invalid, Readarr cannot connect to Prowlarr");
                }

                _logger.Error(ex, "Unable to send test message");
                return new ValidationFailure("BaseUrl", "Unable to complete application test");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to send test message");
                return new ValidationFailure("", "Unable to send test message");
            }

            return null;
        }

        private HttpRequest BuildRequest(ReadarrSettings settings, string resource, HttpMethod method)
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
