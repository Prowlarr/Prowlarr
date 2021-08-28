using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Applications.LazyLibrarian
{
    public interface ILazyLibrarianV1Proxy
    {
        LazyLibrarianIndexer AddIndexer(LazyLibrarianIndexer indexer, LazyLibrarianSettings settings);
        List<LazyLibrarianIndexer> GetIndexers(LazyLibrarianSettings settings);
        LazyLibrarianIndexer GetIndexer(string indexerName, LazyLibrarianProviderType indexerType, LazyLibrarianSettings settings);
        void RemoveIndexer(string indexerName, LazyLibrarianProviderType indexerType, LazyLibrarianSettings settings);
        LazyLibrarianIndexer UpdateIndexer(LazyLibrarianIndexer indexer, LazyLibrarianSettings settings);
        ValidationFailure TestConnection(LazyLibrarianSettings settings);
    }

    public class LazyLibrarianV1Proxy : ILazyLibrarianV1Proxy
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public LazyLibrarianV1Proxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public LazyLibrarianStatus GetStatus(LazyLibrarianSettings settings)
        {
            var request = BuildRequest(settings, "/api", "getCurrentVersion", HttpMethod.GET);
            return Execute<LazyLibrarianStatus>(request);
        }

        public List<LazyLibrarianIndexer> GetIndexers(LazyLibrarianSettings settings)
        {
            var request = BuildRequest(settings, "/api", "listNabProviders", HttpMethod.GET);

            var response = Execute<LazyLibrarianIndexerResponse>(request);

            if (!response.Success)
            {
                throw new LazyLibrarianException(string.Format("LazyLibrarian Error - Code {0}: {1}", response.Error.Code, response.Error.Message));
            }

            var indexers = new List<LazyLibrarianIndexer>();

            var torIndexers = response.Data.Torznabs;
            torIndexers.ForEach(i => i.Type = LazyLibrarianProviderType.Torznab);

            var nzbIndexers = response.Data.Newznabs;
            nzbIndexers.ForEach(i => i.Type = LazyLibrarianProviderType.Newznab);

            indexers.AddRange(torIndexers);
            indexers.AddRange(nzbIndexers);
            indexers.ForEach(i => i.Altername = i.Name);

            return indexers;
        }

        public LazyLibrarianIndexer GetIndexer(string indexerName, LazyLibrarianProviderType indexerType, LazyLibrarianSettings settings)
        {
            var indexers = GetIndexers(settings);

            return indexers.SingleOrDefault(i => i.Name == indexerName && i.Type == indexerType);
        }

        public void RemoveIndexer(string indexerName, LazyLibrarianProviderType indexerType, LazyLibrarianSettings settings)
        {
            var parameters = new Dictionary<string, string>
            {
                { "name", indexerName },
                { "providertype", indexerType.ToString().ToLower() }
            };

            var request = BuildRequest(settings, "/api", "delProvider", HttpMethod.GET, parameters);
            CheckForError(Execute<LazyLibrarianStatus>(request));
        }

        public LazyLibrarianIndexer AddIndexer(LazyLibrarianIndexer indexer, LazyLibrarianSettings settings)
        {
            var parameters = new Dictionary<string, string>
            {
                { "name", indexer.Name },
                { "altername", indexer.Altername },
                { "providertype", indexer.Type.ToString().ToLower() },
                { "host", indexer.Host },
                { "prov_apikey", indexer.Apikey },
                { "enabled", indexer.Enabled.ToString().ToLower() },
                { "categories", indexer.Categories }
            };

            var request = BuildRequest(settings, "/api", "addProvider", HttpMethod.GET, parameters);
            CheckForError(Execute<LazyLibrarianStatus>(request));
            return indexer;
        }

        public LazyLibrarianIndexer UpdateIndexer(LazyLibrarianIndexer indexer, LazyLibrarianSettings settings)
        {
            var parameters = new Dictionary<string, string>
            {
                { "NAME", indexer.Name },
                { "providertype", indexer.Type.ToString().ToLower() },
                { "HOST", indexer.Host },
                { "API", indexer.Apikey },
                { "ENABLED", indexer.Enabled.ToString().ToLower() },
                { "categories", indexer.Categories },
                { "DISPNAME", indexer.Altername }
            };

            var request = BuildRequest(settings, "/api", "changeProvider", HttpMethod.GET, parameters);
            CheckForError(Execute<LazyLibrarianStatus>(request));
            return indexer;
        }

        private void CheckForError(LazyLibrarianStatus response)
        {
            if (!response.Success)
            {
                throw new LazyLibrarianException(string.Format("LazyLibrarian Error - Code {0}: {1}", response.Error.Code, response.Error.Message));
            }
        }

        public ValidationFailure TestConnection(LazyLibrarianSettings settings)
        {
            try
            {
                var status = GetStatus(settings);

                if (!status.Success)
                {
                    return new ValidationFailure("ApiKey", status.Error.Message);
                }
            }
            catch (HttpException ex)
            {
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

        private HttpRequest BuildRequest(LazyLibrarianSettings settings, string resource, string command, HttpMethod method, Dictionary<string, string> parameters = null)
        {
            var baseUrl = settings.BaseUrl.TrimEnd('/');

            var requestBuilder = new HttpRequestBuilder(baseUrl).Resource(resource)
                .AddQueryParam("cmd", command)
                .AddQueryParam("apikey", settings.ApiKey);

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    requestBuilder.AddQueryParam(param.Key, param.Value);
                }
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

            var results = JsonConvert.DeserializeObject<TResource>(response.Content);

            return results;
        }
    }
}
