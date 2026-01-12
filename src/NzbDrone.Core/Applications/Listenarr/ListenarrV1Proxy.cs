using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Applications.Listenarr
{
    public interface IListenarrV1Proxy
    {
        ListenarrStatus GetStatus(ListenarrSettings settings);
        List<ListenarrIndexer> GetIndexers(ListenarrSettings settings);
        ListenarrIndexer AddIndexer(ListenarrIndexer indexer, ListenarrSettings settings);
        void UpdateIndexer(ListenarrIndexer indexer, ListenarrSettings settings);
        void RemoveIndexer(int indexerId, ListenarrSettings settings);
        List<ListenarrTag> GetTags(ListenarrSettings settings);
    }

    public class ListenarrV1Proxy : IListenarrV1Proxy
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public ListenarrV1Proxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public ListenarrStatus GetStatus(ListenarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/system/status", HttpMethod.Get);
            return Execute<ListenarrStatus>(request);
        }

        public List<ListenarrIndexer> GetIndexers(ListenarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/indexer", HttpMethod.Get);
            return Execute<List<ListenarrIndexer>>(request);
        }

        public ListenarrIndexer AddIndexer(ListenarrIndexer indexer, ListenarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/indexer", HttpMethod.Post);
            request.SetContent(indexer.ToJson());
            request.ContentSummary = indexer.ToJson(Formatting.None);
            return Execute<ListenarrIndexer>(request);
        }

        public void UpdateIndexer(ListenarrIndexer indexer, ListenarrSettings settings)
        {
            var request = BuildRequest(settings, $"/api/indexer/{indexer.Id}", HttpMethod.Put);
            request.SetContent(indexer.ToJson());
            request.ContentSummary = indexer.ToJson(Formatting.None);
            Execute<ListenarrIndexer>(request);
        }

        public void RemoveIndexer(int indexerId, ListenarrSettings settings)
        {
            var request = BuildRequest(settings, $"/api/indexer/{indexerId}", HttpMethod.Delete);
            _httpClient.Execute(request);
        }

        public List<ListenarrTag> GetTags(ListenarrSettings settings)
        {
            var request = BuildRequest(settings, "/api/tag", HttpMethod.Get);
            return Execute<List<ListenarrTag>>(request);
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

        private T Execute<T>(HttpRequest request)
            where T : new()
        {
            try
            {
                var response = _httpClient.Execute(request);

                if ((int)response.StatusCode >= 300)
                {
                    throw new HttpException(response);
                }

                return Json.Deserialize<T>(response.Content);
            }
            catch (HttpException ex)
            {
                if (ex.Response != null && ex.Response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.Error(ex, "API Key is invalid");
                    throw new ApplicationException("API Key is invalid");
                }

                throw;
            }
        }
    }
}
