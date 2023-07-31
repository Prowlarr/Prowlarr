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
    }

    public class RadarrV3Proxy : IRadarrV3Proxy
    {
        private static Version MinimumApplicationV4Version => new (4, 0, 4, 0);
        private static Version MinimumApplicationV3Version => new (3, 1, 1, 0);

        private const string AppApiRoute = "/api/v3";
        private const string AppIndexerApiRoute = $"{AppApiRoute}/indexer";

        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public RadarrV3Proxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public RadarrStatus GetStatus(RadarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppApiRoute}/system/status", HttpMethod.Get);
            return Execute<RadarrStatus>(request);
        }

        public List<RadarrIndexer> GetIndexers(RadarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}", HttpMethod.Get);
            return Execute<List<RadarrIndexer>>(request);
        }

        public RadarrIndexer GetIndexer(int indexerId, RadarrSettings settings)
        {
            try
            {
                var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexerId}", HttpMethod.Get);
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
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexerId}", HttpMethod.Delete);
            _httpClient.Execute(request);
        }

        public List<RadarrIndexer> GetIndexerSchema(RadarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/schema", HttpMethod.Get);
            return Execute<List<RadarrIndexer>>(request);
        }

        public RadarrIndexer AddIndexer(RadarrIndexer indexer, RadarrSettings settings)
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

        public RadarrIndexer UpdateIndexer(RadarrIndexer indexer, RadarrSettings settings)
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

        public ValidationFailure TestConnection(RadarrIndexer indexer, RadarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/test", HttpMethod.Post);

            request.SetContent(indexer.ToJson());

            var applicationVersion = _httpClient.Post(request).Headers.GetSingleValue("X-Application-Version");

            if (applicationVersion == null)
            {
                return new ValidationFailure(string.Empty, "Failed to fetch Radarr version");
            }

            var version = new Version(applicationVersion);

            if (version.Major == 3)
            {
                if (version < MinimumApplicationV3Version)
                {
                    return new ValidationFailure(string.Empty, $"Radarr version should be at least {MinimumApplicationV3Version.ToString(3)}. Version reported is {applicationVersion}", applicationVersion);
                }
            }
            else
            {
                if (version < MinimumApplicationV4Version)
                {
                    return new ValidationFailure(string.Empty, $"Radarr version should be at least {MinimumApplicationV4Version.ToString(3)}. Version reported is {applicationVersion}", applicationVersion);
                }
            }

            return null;
        }

        private RadarrIndexer ExecuteIndexerRequest(HttpRequest request)
        {
            try
            {
                return Execute<RadarrIndexer>(request);
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

        private HttpRequest BuildRequest(RadarrSettings settings, string resource, HttpMethod method)
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
