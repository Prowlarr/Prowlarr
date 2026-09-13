using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Applications.CrossSeed
{
    public interface ICrossSeedProxy
    {
        CrossSeedStatus GetStatus(CrossSeedSettings settings);
        List<CrossSeedIndexer> GetIndexers(CrossSeedSettings settings);
        CrossSeedIndexer GetIndexer(int id, CrossSeedSettings settings);
        CrossSeedIndexer AddIndexer(CrossSeedIndexer indexer, CrossSeedSettings settings);
        CrossSeedIndexer UpdateIndexer(CrossSeedIndexer indexer, CrossSeedSettings settings);
        void RemoveIndexer(int id, CrossSeedSettings settings);
        ValidationFailure TestConnection(CrossSeedSettings settings);
    }

    public class CrossSeedProxy : ICrossSeedProxy
    {
        private const string AppApiRoute = "/api/indexer/v1";

        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public CrossSeedProxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public CrossSeedStatus GetStatus(CrossSeedSettings settings)
        {
            var request = BuildRequest(settings, $"{AppApiRoute}/status", HttpMethod.Get);
            return Execute<CrossSeedStatus>(request);
        }

        public List<CrossSeedIndexer> GetIndexers(CrossSeedSettings settings)
        {
            var request = BuildRequest(settings, AppApiRoute, HttpMethod.Get);
            return Execute<List<CrossSeedIndexer>>(request);
        }

        public CrossSeedIndexer GetIndexer(int id, CrossSeedSettings settings)
        {
            var request = BuildRequest(settings, $"{AppApiRoute}/{id}", HttpMethod.Get);
            return Execute<CrossSeedIndexer>(request);
        }

        public CrossSeedIndexer AddIndexer(CrossSeedIndexer indexer, CrossSeedSettings settings)
        {
            var request = BuildRequest(settings, AppApiRoute, HttpMethod.Post);
            request.SetContent(indexer.ToJson());
            return Execute<CrossSeedIndexer>(request);
        }

        public CrossSeedIndexer UpdateIndexer(CrossSeedIndexer indexer, CrossSeedSettings settings)
        {
            var request = BuildRequest(settings, $"{AppApiRoute}/{indexer.Id}", HttpMethod.Put);
            request.SetContent(indexer.ToJson());
            return Execute<CrossSeedIndexer>(request);
        }

        public void RemoveIndexer(int id, CrossSeedSettings settings)
        {
            var request = BuildRequest(settings, $"{AppApiRoute}/{id}", HttpMethod.Delete);
            Execute<object>(request);
        }


        public ValidationFailure TestConnection(CrossSeedSettings settings)
        {
            try
            {
                // Step 1: Test basic connectivity and API key
                var status = GetStatus(settings);
                _logger.Debug("Successfully connected to cross-seed. Version: {0}", status.Version);
                
                // Step 2: Test indexer list access (verifies permissions)
                GetIndexers(settings);
                _logger.Debug("Successfully accessed cross-seed indexer list");
                
                return null;
            }
            catch (HttpException ex)
            {
                switch (ex.Response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        _logger.Warn(ex, "API Key is invalid");
                        return new ValidationFailure("ApiKey", "API Key is invalid");
                    case HttpStatusCode.BadRequest:
                        _logger.Warn(ex, "Prowlarr URL is invalid");
                        return new ValidationFailure("ProwlarrUrl", "Prowlarr URL is invalid, cross-seed cannot connect to Prowlarr");
                    case HttpStatusCode.NotFound:
                        _logger.Warn(ex, "cross-seed indexer management API not found - make sure cross-seed supports Prowlarr integration");
                        return new ValidationFailure("BaseUrl", "cross-seed indexer management API not found. Please ensure you're running cross-seed v7+ that supports Prowlarr integration.");
                    default:
                        _logger.Warn(ex, "Unable to complete application test");
                        return new ValidationFailure("BaseUrl", $"Unable to complete application test, cannot connect to cross-seed. {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to complete application test");
                return new ValidationFailure("BaseUrl", $"Unable to complete application test, cannot connect to cross-seed. {ex.Message}");
            }
        }

        private HttpRequest BuildRequest(CrossSeedSettings settings, string resource, HttpMethod method)
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
