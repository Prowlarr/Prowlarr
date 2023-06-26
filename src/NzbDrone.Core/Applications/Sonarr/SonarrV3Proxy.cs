using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
        private static Version MinimumApplicationVersion => new (3, 0, 5, 0);

        private const string AppApiRoute = "/api/v3";
        private const string AppIndexerApiRoute = $"{AppApiRoute}/indexer";

        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public SonarrV3Proxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public SonarrStatus GetStatus(SonarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppApiRoute}/system/status", HttpMethod.Get);
            return Execute<SonarrStatus>(request);
        }

        public List<SonarrIndexer> GetIndexers(SonarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}", HttpMethod.Get);
            return Execute<List<SonarrIndexer>>(request);
        }

        public SonarrIndexer GetIndexer(int indexerId, SonarrSettings settings)
        {
            try
            {
                var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexerId}", HttpMethod.Get);
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
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexerId}", HttpMethod.Delete);
            _httpClient.Execute(request);
        }

        public List<SonarrIndexer> GetIndexerSchema(SonarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/schema", HttpMethod.Get);
            return Execute<List<SonarrIndexer>>(request);
        }

        public SonarrIndexer AddIndexer(SonarrIndexer indexer, SonarrSettings settings)
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

        public SonarrIndexer UpdateIndexer(SonarrIndexer indexer, SonarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexer.Id}", HttpMethod.Put);

            request.Url = request.Url.AddQueryParam("forceSave", "true");
            request.SetContent(indexer.ToJson());

            return ExecuteIndexerRequest(request);
        }

        public ValidationFailure TestConnection(SonarrIndexer indexer, SonarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/test", HttpMethod.Post);

            request.SetContent(indexer.ToJson());

            var applicationVersion = _httpClient.Post(request).Headers.GetSingleValue("X-Application-Version");

            if (applicationVersion == null)
            {
                return new ValidationFailure(string.Empty, "Failed to fetch Sonarr version");
            }

            if (new Version(applicationVersion) < MinimumApplicationVersion)
            {
                return new ValidationFailure(string.Empty, $"Sonarr version should be at least {MinimumApplicationVersion.ToString(3)}. Version reported is {applicationVersion}", applicationVersion);
            }

            return null;
        }

        private SonarrIndexer ExecuteIndexerRequest(HttpRequest request)
        {
            try
            {
                return Execute<SonarrIndexer>(request);
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

        private HttpRequest BuildRequest(SonarrSettings settings, string resource, HttpMethod method)
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
