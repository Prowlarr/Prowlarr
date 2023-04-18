using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using FluentValidation.Results;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Applications.Readarr
{
    public interface IReadarrV1Proxy
    {
        ReadarrIndexer AddIndexer(ReadarrIndexer indexer, ReadarrSettings settings);
        List<ReadarrIndexer> GetIndexers(ReadarrSettings settings);
        ReadarrIndexer GetIndexer(int indexerId, ReadarrSettings settings);
        List<ReadarrIndexer> GetIndexerSchema(ReadarrSettings settings);
        void RemoveIndexer(int indexerId, ReadarrSettings settings);
        ReadarrIndexer UpdateIndexer(ReadarrIndexer indexer, ReadarrSettings settings);
        ValidationFailure TestConnection(ReadarrIndexer indexer, ReadarrSettings settings);
    }

    public class ReadarrV1Proxy : IReadarrV1Proxy
    {
        private const string AppApiRoute = "/api/v1";
        private const string AppIndexerApiRoute = $"{AppApiRoute}/indexer";
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public ReadarrV1Proxy(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public ReadarrStatus GetStatus(ReadarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppApiRoute}/system/status", HttpMethod.Get);
            return Execute<ReadarrStatus>(request);
        }

        public List<ReadarrIndexer> GetIndexers(ReadarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}", HttpMethod.Get);
            return Execute<List<ReadarrIndexer>>(request);
        }

        public ReadarrIndexer GetIndexer(int indexerId, ReadarrSettings settings)
        {
            try
            {
                var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexerId}", HttpMethod.Get);
                return Execute<ReadarrIndexer>(request);
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

        public void RemoveIndexer(int indexerId, ReadarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexerId}", HttpMethod.Delete);
            _httpClient.Execute(request);
        }

        public List<ReadarrIndexer> GetIndexerSchema(ReadarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/schema", HttpMethod.Get);
            return Execute<List<ReadarrIndexer>>(request);
        }

        public ReadarrIndexer AddIndexer(ReadarrIndexer indexer, ReadarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}", HttpMethod.Post);

            request.SetContent(indexer.ToJson());

            return ExecuteIndexerRequest(request);
        }

        public ReadarrIndexer UpdateIndexer(ReadarrIndexer indexer, ReadarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexer.Id}", HttpMethod.Put);

            request.SetContent(indexer.ToJson());

            return ExecuteIndexerRequest(request);
        }

        public ValidationFailure TestConnection(ReadarrIndexer indexer, ReadarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/test", HttpMethod.Post);

            request.SetContent(indexer.ToJson());

            try
            {
                Execute<ReadarrIndexer>(request);
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
                    return new ValidationFailure("ProwlarrUrl", "Prowlarr url is invalid, Readarr cannot connect to Prowlarr");
                }

                if (ex.Response.StatusCode == HttpStatusCode.SeeOther)
                {
                    _logger.Error(ex, "Readarr returned redirect and is invalid");
                    return new ValidationFailure("BaseUrl", "Readarr url is invalid, Prowlarr cannot connect to Readarr - are you missing a url base?");
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

        private ReadarrIndexer ExecuteIndexerRequest(HttpRequest request)
        {
            try
            {
                return Execute<ReadarrIndexer>(request);
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

        private HttpRequest BuildRequest(ReadarrSettings settings, string resource, HttpMethod method)
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
