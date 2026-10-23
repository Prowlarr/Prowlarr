using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Applications.Listenarr
{
    public interface IListenarrV1Proxy
    {
        ListenarrIndexer AddIndexer(ListenarrIndexer indexer, ListenarrSettings settings);
        List<ListenarrIndexer> GetIndexers(ListenarrSettings settings);
        ListenarrIndexer GetIndexer(int indexerId, ListenarrSettings settings);
        List<ListenarrIndexer> GetIndexerSchema(ListenarrSettings settings);
        void RemoveIndexer(int indexerId, ListenarrSettings settings);
        ListenarrIndexer UpdateIndexer(ListenarrIndexer indexer, ListenarrSettings settings);
        ValidationFailure TestConnection(ListenarrIndexer indexer, ListenarrSettings settings);
    }

    public class ListenarrV1Proxy : IListenarrV1Proxy
    {
        private static Version MinimumApplicationVersion => new(0, 2, 66, 0);

        private const string AppApiRoute = "/api/v1";
        private const string AppIndexerApiRoute = $"{AppApiRoute}/prowlarr/indexer";

        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public ListenarrV1Proxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public ListenarrStatus GetStatus(ListenarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppApiRoute}/system/status", HttpMethod.Get);
            return Execute<ListenarrStatus>(request);
        }

        public List<ListenarrIndexer> GetIndexers(ListenarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}", HttpMethod.Get);
            return Execute<List<ListenarrIndexer>>(request);
        }

        public ListenarrIndexer GetIndexer(int indexerId, ListenarrSettings settings)
        {
            try
            {
                var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexerId}", HttpMethod.Get);
                return Execute<ListenarrIndexer>(request);
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

        public void RemoveIndexer(int indexerId, ListenarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexerId}", HttpMethod.Delete);
            _httpClient.Execute(request);
        }

        public List<ListenarrIndexer> GetIndexerSchema(ListenarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/schema", HttpMethod.Get);
            return Execute<List<ListenarrIndexer>>(request);
        }

        public ListenarrIndexer AddIndexer(ListenarrIndexer indexer, ListenarrSettings settings)
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
                _logger.Debug("Retrying to add indexer forcefully");

                request.Url = request.Url.AddQueryParam("forceSave", "true");

                return ExecuteIndexerRequest(request);
            }
        }

        public ListenarrIndexer UpdateIndexer(ListenarrIndexer indexer, ListenarrSettings settings)
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

        public ValidationFailure TestConnection(ListenarrIndexer indexer, ListenarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/test", HttpMethod.Post);

            request.SetContent(indexer.ToJson());
            request.ContentSummary = indexer.ToJson(Formatting.None);

            var applicationVersion = _httpClient.Post(request).Headers.GetSingleValue("X-Application-Version");

            if (applicationVersion == null)
            {
                return new ValidationFailure(string.Empty, "Failed to fetch Listenarr version");
            }

            if (new Version(applicationVersion) < MinimumApplicationVersion)
            {
                return new ValidationFailure(string.Empty, $"Listenarr version should be at least {MinimumApplicationVersion.ToString(3)}. Version reported is {applicationVersion}", applicationVersion);
            }

            return null;
        }

        private ListenarrIndexer ExecuteIndexerRequest(HttpRequest request)
        {
            try
            {
                return Execute<ListenarrIndexer>(request);
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

        private HttpRequest BuildRequest(ListenarrSettings settings, string resource, HttpMethod method)
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
