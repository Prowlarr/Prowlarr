using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Applications.Gamarr
{
    public interface IGamarrV3Proxy
    {
        GamarrIndexer AddIndexer(GamarrIndexer indexer, GamarrSettings settings);
        List<GamarrIndexer> GetIndexers(GamarrSettings settings);
        GamarrIndexer GetIndexer(int indexerId, GamarrSettings settings);
        List<GamarrIndexer> GetIndexerSchema(GamarrSettings settings);
        void RemoveIndexer(int indexerId, GamarrSettings settings);
        GamarrIndexer UpdateIndexer(GamarrIndexer indexer, GamarrSettings settings);
        ValidationFailure TestConnection(GamarrIndexer indexer, GamarrSettings settings);
    }

    public class GamarrV3Proxy : IGamarrV3Proxy
    {
        private const string AppApiRoute = "/api/v3";
        private const string AppIndexerApiRoute = $"{AppApiRoute}/indexer";
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public GamarrV3Proxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public GamarrStatus GetStatus(GamarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppApiRoute}/system/status", HttpMethod.Get);
            return Execute<GamarrStatus>(request);
        }

        public List<GamarrIndexer> GetIndexers(GamarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}", HttpMethod.Get);
            return Execute<List<GamarrIndexer>>(request);
        }

        public GamarrIndexer GetIndexer(int indexerId, GamarrSettings settings)
        {
            try
            {
                var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexerId}", HttpMethod.Get);
                return Execute<GamarrIndexer>(request);
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

        public void RemoveIndexer(int indexerId, GamarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexerId}", HttpMethod.Delete);
            _httpClient.Execute(request);
        }

        public List<GamarrIndexer> GetIndexerSchema(GamarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/schema", HttpMethod.Get);
            return Execute<List<GamarrIndexer>>(request);
        }

        public GamarrIndexer AddIndexer(GamarrIndexer indexer, GamarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}", HttpMethod.Post);

            request.SetContent(indexer.ToJson());
            request.ContentSummary = indexer.ToJson(Formatting.None);

            try
            {
                return ExecuteIndexerRequest(request);
            }
            catch (HttpException ex) when (ex.Response.StatusCode == HttpStatusCode.BadRequest)
            {
                request.Url = request.Url.AddQueryParam("forceSave", "true");

                return ExecuteIndexerRequest(request);
            }
        }

        public GamarrIndexer UpdateIndexer(GamarrIndexer indexer, GamarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexer.Id}", HttpMethod.Put);

            request.SetContent(indexer.ToJson());
            request.ContentSummary = indexer.ToJson(Formatting.None);

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

        public ValidationFailure TestConnection(GamarrIndexer indexer, GamarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/test", HttpMethod.Post);

            request.SetContent(indexer.ToJson());
            request.ContentSummary = indexer.ToJson(Formatting.None);

            _httpClient.Post(request);

            return null;
        }

        private GamarrIndexer ExecuteIndexerRequest(HttpRequest request)
        {
            try
            {
                return Execute<GamarrIndexer>(request);
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
                    case HttpStatusCode.TemporaryRedirect:
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

        private HttpRequest BuildRequest(GamarrSettings settings, string resource, HttpMethod method)
        {
            var baseUrl = settings.BaseUrl.TrimEnd('/');

            var requestBuilder = new HttpRequestBuilder(baseUrl)
                .Resource(resource)
                .Accept(HttpAccept.Json)
                .SetHeader("X-Api-Key", settings.ApiKey);

            if (settings.AuthUsername.IsNotNullOrWhiteSpace() || settings.AuthPassword.IsNotNullOrWhiteSpace())
            {
                requestBuilder.NetworkCredential = new BasicNetworkCredential(settings.AuthUsername, settings.AuthPassword);
            }

            var request = requestBuilder.Build();

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
