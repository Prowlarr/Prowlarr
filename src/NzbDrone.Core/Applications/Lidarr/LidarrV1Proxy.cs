using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
        LidarrIndexer GetIndexer(int indexerId, LidarrSettings settings);
        List<LidarrIndexer> GetIndexerSchema(LidarrSettings settings);
        void RemoveIndexer(int indexerId, LidarrSettings settings);
        LidarrIndexer UpdateIndexer(LidarrIndexer indexer, LidarrSettings settings);
        ValidationFailure TestConnection(LidarrIndexer indexer, LidarrSettings settings);
    }

    public class LidarrV1Proxy : ILidarrV1Proxy
    {
        private static Version MinimumApplicationVersion => new (1, 0, 2, 0);

        private const string AppApiRoute = "/api/v1";
        private const string AppIndexerApiRoute = $"{AppApiRoute}/indexer";

        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public LidarrV1Proxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public LidarrStatus GetStatus(LidarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppApiRoute}/system/status", HttpMethod.Get);
            return Execute<LidarrStatus>(request);
        }

        public List<LidarrIndexer> GetIndexers(LidarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}", HttpMethod.Get);
            return Execute<List<LidarrIndexer>>(request);
        }

        public LidarrIndexer GetIndexer(int indexerId, LidarrSettings settings)
        {
            try
            {
                var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexerId}", HttpMethod.Get);
                return Execute<LidarrIndexer>(request);
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

        public void RemoveIndexer(int indexerId, LidarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexerId}", HttpMethod.Delete);
            _httpClient.Execute(request);
        }

        public List<LidarrIndexer> GetIndexerSchema(LidarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/schema", HttpMethod.Get);
            return Execute<List<LidarrIndexer>>(request);
        }

        public LidarrIndexer AddIndexer(LidarrIndexer indexer, LidarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}", HttpMethod.Post);

            request.SetContent(indexer.ToJson());

            return ExecuteIndexerRequest(request);
        }

        public LidarrIndexer UpdateIndexer(LidarrIndexer indexer, LidarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexer.Id}", HttpMethod.Put);

            request.SetContent(indexer.ToJson());

            return ExecuteIndexerRequest(request);
        }

        public ValidationFailure TestConnection(LidarrIndexer indexer, LidarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/test", HttpMethod.Post);

            request.SetContent(indexer.ToJson());

            try
            {
                var applicationVersion = _httpClient.Post<LidarrIndexer>(request).Headers.GetSingleValue("X-Application-Version");

                if (applicationVersion == null)
                {
                    return new ValidationFailure(string.Empty, "Failed to fetch Lidarr version");
                }

                if (new Version(applicationVersion) < MinimumApplicationVersion)
                {
                    return new ValidationFailure(string.Empty, $"Lidarr version should be at least {MinimumApplicationVersion.ToString(3)}. Version reported is {applicationVersion}", applicationVersion);
                }
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
                    return new ValidationFailure("ProwlarrUrl", "Prowlarr url is invalid, Lidarr cannot connect to Prowlarr");
                }

                if (ex.Response.StatusCode == HttpStatusCode.SeeOther)
                {
                    _logger.Error(ex, "Lidarr returned redirect and is invalid");
                    return new ValidationFailure("BaseUrl", "Lidarr url is invalid, Prowlarr cannot connect to Lidarr - are you missing a url base?");
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

        private LidarrIndexer ExecuteIndexerRequest(HttpRequest request)
        {
            try
            {
                return Execute<LidarrIndexer>(request);
            }
            catch (HttpException ex)
            {
                switch (ex.Response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        _logger.Error(ex, "API Key is invalid");
                        break;
                    case HttpStatusCode.BadRequest:
                        if (ex.Response.Content.Contains("Query successful, but no results in the configured categories were returned from your indexer.", StringComparison.InvariantCultureIgnoreCase))
                        {
                            _logger.Error(ex, "No Results in configured categories. See FAQ Entry: Prowlarr will not sync X Indexer to App");
                            break;
                        }

                        _logger.Error(ex, "Invalid Request");
                        break;
                    case HttpStatusCode.SeeOther:
                        _logger.Error(ex, "App returned redirect and is invalid. Check App URL");
                        break;
                    case HttpStatusCode.NotFound:
                        _logger.Error(ex, "Remote indexer not found");
                        break;
                    default:
                        _logger.Error(ex, "Unexpected response status code: {0}", ex.Response.StatusCode);
                        throw;
                }
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
