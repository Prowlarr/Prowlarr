using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Applications.Qui
{
    public interface IQuiProxy
    {
        QuiIndexer AddIndexer(QuiIndexer indexer, QuiSettings settings);
        List<QuiIndexer> GetIndexers(QuiSettings settings);
        QuiIndexer GetIndexer(int indexerId, QuiSettings settings);
        void RemoveIndexer(int indexerId, QuiSettings settings);
        QuiIndexer UpdateIndexer(QuiIndexer indexer, QuiSettings settings);
        ValidationFailure TestConnection(QuiSettings settings);
    }

    public class QuiProxy : IQuiProxy
    {
        private const string AppIndexerApiRoute = "/api/torznab/indexers";

        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public QuiProxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public List<QuiIndexer> GetIndexers(QuiSettings settings)
        {
            var request = BuildRequest(settings, AppIndexerApiRoute, HttpMethod.Get);
            return Execute<List<QuiIndexer>>(request);
        }

        public QuiIndexer GetIndexer(int indexerId, QuiSettings settings)
        {
            try
            {
                var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexerId}", HttpMethod.Get);
                return Execute<QuiIndexer>(request);
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

        public QuiIndexer AddIndexer(QuiIndexer indexer, QuiSettings settings)
        {
            var request = BuildRequest(settings, AppIndexerApiRoute, HttpMethod.Post);

            request.SetContent(indexer.ToJson());
            request.ContentSummary = indexer.ToJson(Formatting.None);

            return Execute<QuiIndexer>(request);
        }

        public QuiIndexer UpdateIndexer(QuiIndexer indexer, QuiSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexer.Id}", HttpMethod.Put);

            request.SetContent(indexer.ToJson());
            request.ContentSummary = indexer.ToJson(Formatting.None);

            return Execute<QuiIndexer>(request);
        }

        public void RemoveIndexer(int indexerId, QuiSettings settings)
        {
            try
            {
                var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexerId}", HttpMethod.Delete);
                _httpClient.Execute(request);
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }
        }

        public ValidationFailure TestConnection(QuiSettings settings)
        {
            try
            {
                GetIndexers(settings);
            }
            catch (HttpException ex)
            {
                _logger.Error(ex, "Unable to complete application test");

                switch (ex.Response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        return new ValidationFailure("ApiKey", "API Key is invalid");
                    case HttpStatusCode.NotFound:
                        return new ValidationFailure("BaseUrl", "qui URL is invalid, Prowlarr cannot connect to qui. Is qui running and accessible?");
                    default:
                        return new ValidationFailure("BaseUrl", $"Unable to complete application test, cannot connect to qui. {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to complete application test");
                return new ValidationFailure("", $"Unable to complete application test. {ex.Message}");
            }

            return null;
        }

        private HttpRequest BuildRequest(QuiSettings settings, string resource, HttpMethod method)
        {
            var baseUrl = settings.BaseUrl.TrimEnd('/');

            var request = new HttpRequestBuilder(baseUrl)
                .Resource(resource)
                .Accept(HttpAccept.Json)
                .SetHeader("X-API-Key", settings.ApiKey)
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
