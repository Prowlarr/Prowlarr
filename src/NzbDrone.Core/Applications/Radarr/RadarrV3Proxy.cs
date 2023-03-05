using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
        ValidationFailure TestConnection(RadarrIndexer indexer, RadarrSettings settings);
        List<RadarrTag> GetTagsFromApplication(RadarrSettings settings);
        RadarrTag CreateTag(RadarrSettings settings, string label);
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
            var request = BuildRequest(settings, "/api/v3/system/status", HttpMethod.Get);
            return Execute<RadarrStatus>(request);
        }

        public List<RadarrIndexer> GetIndexers(RadarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v3/indexer", HttpMethod.Get);
            return Execute<List<RadarrIndexer>>(request);
        }

        public RadarrIndexer GetIndexer(int indexerId, RadarrSettings settings)
        {
            try
            {
                var request = BuildRequest(settings, $"/api/v3/indexer/{indexerId}", HttpMethod.Get);
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
            var request = BuildRequest(settings, $"/api/v3/indexer/{indexerId}", HttpMethod.Delete);
            _httpClient.Execute(request);
        }

        public List<RadarrIndexer> GetIndexerSchema(RadarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v3/indexer/schema", HttpMethod.Get);
            return Execute<List<RadarrIndexer>>(request);
        }

        public RadarrIndexer AddIndexer(RadarrIndexer indexer, RadarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/v3/indexer", HttpMethod.Post);

            request.SetContent(indexer.ToJson());

            return Execute<RadarrIndexer>(request);
        }

        public RadarrIndexer UpdateIndexer(RadarrIndexer indexer, RadarrSettings settings)
        {
            var request = BuildRequest(settings, $"/api/v3/indexer/{indexer.Id}", HttpMethod.Put);

            request.SetContent(indexer.ToJson());

            return Execute<RadarrIndexer>(request);
        }

        public ValidationFailure TestConnection(RadarrIndexer indexer, RadarrSettings settings)
        {
            var request = BuildRequest(settings, $"/api/v3/indexer/test", HttpMethod.Post);

            request.SetContent(indexer.ToJson());

            try
            {
                Execute<RadarrIndexer>(request);
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
                    return new ValidationFailure("ProwlarrUrl", "Prowlarr url is invalid, Radarr cannot connect to Prowlarr");
                }

                if (ex.Response.StatusCode == HttpStatusCode.SeeOther)
                {
                    _logger.Error(ex, "Radarr returned redirect and is invalid");
                    return new ValidationFailure("BaseUrl", "Radarr url is invalid, Prowlarr cannot connect to Radarr - are you missing a url base?");
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

        public List<RadarrTag> GetTagsFromApplication(RadarrSettings settings)
        {
            var request = BuildRequest(settings, $"/api/v3/tag/detail", HttpMethod.Get);
            return Execute<List<RadarrTag>>(request);
        }

        public RadarrTag CreateTag(RadarrSettings settings, string label)
        {
            var request = BuildRequest(settings, $"/api/v3/tag", HttpMethod.Post);
            request.SetContent(new RadarrTag { Label = label }.ToJson());
            var tag = Execute<RadarrTag>(request);
            _logger.Info("Tag '{0}' created or already existed with ID '{1}'.", tag.Label, tag.Id);
            return tag;
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
