using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Applications.Whisparr
{
    public interface IWhisparrV3Proxy
    {
        WhisparrIndexer AddIndexer(WhisparrIndexer indexer, WhisparrSettings settings);
        List<WhisparrIndexer> GetIndexers(WhisparrSettings settings);
        WhisparrIndexer GetIndexer(int indexerId, WhisparrSettings settings);
        List<WhisparrIndexer> GetIndexerSchema(WhisparrSettings settings);
        void RemoveIndexer(int indexerId, WhisparrSettings settings);
        WhisparrIndexer UpdateIndexer(WhisparrIndexer indexer, WhisparrSettings settings);
        ValidationFailure TestConnection(WhisparrIndexer indexer, WhisparrSettings settings);
    }

    public class WhisparrV3Proxy : IWhisparrV3Proxy
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public WhisparrV3Proxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public WhisparrStatus GetStatus(WhisparrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v3/system/status", HttpMethod.Get);
            return Execute<WhisparrStatus>(request);
        }

        public List<WhisparrIndexer> GetIndexers(WhisparrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v3/indexer", HttpMethod.Get);
            return Execute<List<WhisparrIndexer>>(request);
        }

        public WhisparrIndexer GetIndexer(int indexerId, WhisparrSettings settings)
        {
            try
            {
                var request = BuildRequest(settings, $"/api/v3/indexer/{indexerId}", HttpMethod.Get);
                return Execute<WhisparrIndexer>(request);
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

        public void RemoveIndexer(int indexerId, WhisparrSettings settings)
        {
            var request = BuildRequest(settings, $"/api/v3/indexer/{indexerId}", HttpMethod.Delete);
            _httpClient.Execute(request);
        }

        public List<WhisparrIndexer> GetIndexerSchema(WhisparrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v3/indexer/schema", HttpMethod.Get);
            return Execute<List<WhisparrIndexer>>(request);
        }

        public WhisparrIndexer AddIndexer(WhisparrIndexer indexer, WhisparrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v3/indexer", HttpMethod.Post);

            request.SetContent(indexer.ToJson());

            return Execute<WhisparrIndexer>(request);
        }

        public WhisparrIndexer UpdateIndexer(WhisparrIndexer indexer, WhisparrSettings settings)
        {
            var request = BuildRequest(settings, $"/api/v3/indexer/{indexer.Id}", HttpMethod.Put);

            request.SetContent(indexer.ToJson());

            return Execute<WhisparrIndexer>(request);
        }

        public ValidationFailure TestConnection(WhisparrIndexer indexer, WhisparrSettings settings)
        {
            var request = BuildRequest(settings, $"/api/v3/indexer/test", HttpMethod.Post);

            request.SetContent(indexer.ToJson());

            try
            {
                Execute<WhisparrIndexer>(request);
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
                    return new ValidationFailure("ProwlarrUrl", "Prowlarr url is invalid, Whisparr cannot connect to Prowlarr");
                }

               if (ex.Response.StatusCode == HttpStatusCode.SeeOther)
                {
                    _logger.Error(ex, "Whisparr returned redirect and is invalid");
                    return new ValidationFailure("BaseUrl", "Whisparr url is invalid, Prowlarr cannot connect to Whisparr - are you missing a url base?");
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

        private HttpRequest BuildRequest(WhisparrSettings settings, string resource, HttpMethod method)
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
