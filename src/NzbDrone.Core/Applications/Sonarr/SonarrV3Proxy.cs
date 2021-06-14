using System;
using System.Collections.Generic;
using System.Net;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Applications.Sonarr
{
    public interface ISonarrV3Proxy
    {
        SonarrIndexer AddIndexer(SonarrIndexer indexer, SonarrSettings settings);
        List<SonarrIndexer> GetIndexers(SonarrSettings settings);
        SonarrIndexer GetIndexer(int indexerId, SonarrSettings settings);
        List<SonarrIndexer> GetIndexerSchema(SonarrSettings settings);
        void RemoveIndexer(int indexerId, SonarrSettings settings);
        SonarrIndexer UpdateIndexer(SonarrIndexer indexer, SonarrSettings settings);
        ValidationFailure TestConnection(SonarrIndexer indexer, SonarrSettings settings);
    }

    public class SonarrV3Proxy : ISonarrV3Proxy
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public SonarrV3Proxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public SonarrStatus GetStatus(SonarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v3/system/status", HttpMethod.GET);
            return Execute<SonarrStatus>(request);
        }

        public List<SonarrIndexer> GetIndexers(SonarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v3/indexer", HttpMethod.GET);
            return Execute<List<SonarrIndexer>>(request);
        }

        public SonarrIndexer GetIndexer(int indexerId, SonarrSettings settings)
        {
            try
            {
                var request = BuildRequest(settings, $"/api/v3/indexer/{indexerId}", HttpMethod.GET);
                return Execute<SonarrIndexer>(request);
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

        public void RemoveIndexer(int indexerId, SonarrSettings settings)
        {
            var request = BuildRequest(settings, $"/api/v3/indexer/{indexerId}", HttpMethod.DELETE);
            _httpClient.Execute(request);
        }

        public List<SonarrIndexer> GetIndexerSchema(SonarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v3/indexer/schema", HttpMethod.GET);
            return Execute<List<SonarrIndexer>>(request);
        }

        public SonarrIndexer AddIndexer(SonarrIndexer indexer, SonarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v3/indexer", HttpMethod.POST);

            request.SetContent(indexer.ToJson());

            return Execute<SonarrIndexer>(request);
        }

        public SonarrIndexer UpdateIndexer(SonarrIndexer indexer, SonarrSettings settings)
        {
            var request = BuildRequest(settings, $"/api/v3/indexer/{indexer.Id}", HttpMethod.PUT);

            request.SetContent(indexer.ToJson());

            return Execute<SonarrIndexer>(request);
        }

        public ValidationFailure TestConnection(SonarrIndexer indexer, SonarrSettings settings)
        {
            var request = BuildRequest(settings, $"/api/v3/indexer/test", HttpMethod.POST);

            request.SetContent(indexer.ToJson());

            try
            {
                Execute<SonarrIndexer>(request);
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
                    return new ValidationFailure("ProwlarrUrl", "Prowlarr url is invalid, Sonarr cannot connect to Prowlarr");
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

        private HttpRequest BuildRequest(SonarrSettings settings, string resource, HttpMethod method)
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
