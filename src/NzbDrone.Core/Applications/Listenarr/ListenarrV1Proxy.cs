using System;
using System.Collections.Generic;
using System.Linq;
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
        ListenarrStatus GetStatus(ListenarrSettings settings);
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
        private static Version MinimumApplicationVersion => new(0, 2, 47, 0);

        private const string AppApiRoute = "/api/v1";
        private const string AppIndexerApiRoute = $"{AppApiRoute}/indexer";

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

            try
            {
                return Execute<List<ListenarrIndexer>>(request);
            }
            catch (HttpException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                // Fallback to plural resource if the app exposes /indexers
                var fallback = BuildRequest(settings, $"{AppApiRoute}/indexers", HttpMethod.Get);
                return Execute<List<ListenarrIndexer>>(fallback);
            }
        }

        public ListenarrIndexer GetIndexer(int indexerId, ListenarrSettings settings)
        {
            try
            {
                var fallback = BuildRequest(settings, $"{AppApiRoute}/indexers/{indexerId}", HttpMethod.Get);
                return Execute<ListenarrIndexer>(fallback);
            }
            catch (HttpException)
            {
                return null;
            }
        }

        public void RemoveIndexer(int indexerId, ListenarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/{indexerId}", HttpMethod.Delete);

            try
            {
                _httpClient.Execute(request);
            }
            catch (HttpException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                // Try plural endpoint as fallback
                var fallback = BuildRequest(settings, $"{AppApiRoute}/indexers/{indexerId}", HttpMethod.Delete);
                _httpClient.Execute(fallback);
            }
        }

        public List<ListenarrIndexer> GetIndexerSchema(ListenarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/schema", HttpMethod.Get);

            try
            {
                var response = _httpClient.Execute(request);

                if ((int)response.StatusCode >= 300)
                {
                    throw new HttpException(response);
                }

                // Parse and normalize flexible schema responses
                var token = Newtonsoft.Json.Linq.JToken.Parse(response.Content);

                // If the schema is a single object, wrap into a list (and expand implementations arrays)
                if (token.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                {
                    var obj = (Newtonsoft.Json.Linq.JObject)token;

                    // Normalize fields when they are returned as an object instead of an array
                    if (obj["fields"] is Newtonsoft.Json.Linq.JObject fieldsObj)
                    {
                        var fieldsArray = new Newtonsoft.Json.Linq.JArray();

                        foreach (var prop in fieldsObj.Properties())
                        {
                            if (prop.Value.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                            {
                                var item = (Newtonsoft.Json.Linq.JObject)prop.Value;
                                item["name"] = prop.Name;
                                fieldsArray.Add(item);
                            }
                            else
                            {
                                // Primitive -> wrap into value
                                var item = new Newtonsoft.Json.Linq.JObject { ["name"] = prop.Name, ["value"] = prop.Value };
                                fieldsArray.Add(item);
                            }
                        }

                        obj["fields"] = fieldsArray;
                    }

                    // If implementations is an array of strings, expand into separate schema entries
                    if (obj["implementations"] is Newtonsoft.Json.Linq.JArray implsArray && implsArray.Count > 0)
                    {
                        var results = new List<ListenarrIndexer>();

                        foreach (var impl in implsArray)
                        {
                            var copy = (Newtonsoft.Json.Linq.JObject)obj.DeepClone();
                            copy.Property("implementations")?.Remove();
                            copy["implementation"] = impl;
                            results.Add(copy.ToObject<ListenarrIndexer>());
                        }

                        return results;
                    }

                    return new List<ListenarrIndexer> { obj.ToObject<ListenarrIndexer>() };
                }

                // If it's already an array, parse each item, normalize fields and expand implementations arrays
                if (token.Type == Newtonsoft.Json.Linq.JTokenType.Array)
                {
                    var list = new List<ListenarrIndexer>();

                    foreach (var item in (Newtonsoft.Json.Linq.JArray)token)
                    {
                        if (item.Type != Newtonsoft.Json.Linq.JTokenType.Object)
                        {
                            throw new JsonReaderException("Unexpected JSON token while parsing Listenarr schema array");
                        }

                        var obj = (Newtonsoft.Json.Linq.JObject)item;

                        // Normalize fields if needed
                        if (obj["fields"] is Newtonsoft.Json.Linq.JObject fieldsObj2)
                        {
                            var fieldsArray = new Newtonsoft.Json.Linq.JArray();

                            foreach (var prop in fieldsObj2.Properties())
                            {
                                if (prop.Value.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                                {
                                    var fieldItem = (Newtonsoft.Json.Linq.JObject)prop.Value;
                                    fieldItem["name"] = prop.Name;
                                    fieldsArray.Add(fieldItem);
                                }
                                else
                                {
                                    var fieldItem = new Newtonsoft.Json.Linq.JObject { ["name"] = prop.Name, ["value"] = prop.Value };
                                    fieldsArray.Add(fieldItem);
                                }
                            }

                            obj["fields"] = fieldsArray;
                        }

                        if (obj["implementations"] is Newtonsoft.Json.Linq.JArray impls)
                        {
                            foreach (var impl in impls)
                            {
                                var copy = (Newtonsoft.Json.Linq.JObject)obj.DeepClone();
                                copy.Property("implementations")?.Remove();
                                copy["implementation"] = impl;
                                list.Add(copy.ToObject<ListenarrIndexer>());
                            }
                        }
                        else
                        {
                            list.Add(obj.ToObject<ListenarrIndexer>());
                        }
                    }

                    return list;
                }

                // Unexpected token type
                throw new JsonReaderException("Unexpected JSON token while parsing Listenarr schema");
            }
            catch (HttpException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                var fallback = BuildRequest(settings, $"{AppApiRoute}/indexers/schema", HttpMethod.Get);
                var fallbackResponse = _httpClient.Execute(fallback);

                if ((int)fallbackResponse.StatusCode >= 300)
                {
                    throw new HttpException(fallbackResponse);
                }

                var token = Newtonsoft.Json.Linq.JToken.Parse(fallbackResponse.Content);

                if (token.Type == Newtonsoft.Json.Linq.JTokenType.Array)
                {
                    return token.ToObject<List<ListenarrIndexer>>();
                }

                if (token.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                {
                    var obj = (Newtonsoft.Json.Linq.JObject)token;

                    if (obj["fields"] is Newtonsoft.Json.Linq.JObject fieldsObj)
                    {
                        var fieldsArray = new Newtonsoft.Json.Linq.JArray();

                        foreach (var prop in fieldsObj.Properties())
                        {
                            if (prop.Value.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                            {
                                var item = (Newtonsoft.Json.Linq.JObject)prop.Value;
                                item["name"] = prop.Name;
                                fieldsArray.Add(item);
                            }
                            else
                            {
                                var item = new Newtonsoft.Json.Linq.JObject { ["name"] = prop.Name, ["value"] = prop.Value };
                                fieldsArray.Add(item);
                            }
                        }

                        obj["fields"] = fieldsArray;
                    }

                    if (obj["implementations"] is Newtonsoft.Json.Linq.JArray implsArray && implsArray.Count > 0)
                    {
                        var results = new List<ListenarrIndexer>();

                        foreach (var impl in implsArray)
                        {
                            var copy = (Newtonsoft.Json.Linq.JObject)obj.DeepClone();
                            copy.Property("implementations")?.Remove();
                            copy["implementation"] = impl;
                            results.Add(copy.ToObject<ListenarrIndexer>());
                        }

                        return results;
                    }

                    return new List<ListenarrIndexer> { obj.ToObject<ListenarrIndexer>() };
                }

                throw new JsonReaderException("Unexpected JSON token while parsing Listenarr schema (fallback)");
            }
        }

        public ListenarrIndexer AddIndexer(ListenarrIndexer indexer, ListenarrSettings settings)
        {
            // Defensive check: avoid creating duplicates if an indexer with the same baseUrl already exists on the remote app.
            try
            {
                var incomingBaseUrl = indexer?.Fields?.FirstOrDefault(f => f.Name == "baseUrl")?.Value as string;
                if (!string.IsNullOrWhiteSpace(incomingBaseUrl))
                {
                    var existing = GetIndexers(settings);
                    if (existing != null)
                    {
                        var match = existing.FirstOrDefault(e =>
                            string.Equals(
                                (e.Fields?.FirstOrDefault(f => f.Name == "baseUrl")?.Value as string)?.TrimEnd('/'),
                                incomingBaseUrl.TrimEnd('/'),
                                StringComparison.InvariantCultureIgnoreCase));

                        if (match != null)
                        {
                            _logger.Debug("Found existing remote indexer matching baseUrl; skipping add and returning existing id {0}", match.Id);
                            return match;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If the existence check fails for any reason, proceed with the add flow and let any resulting errors bubble up.
                _logger.Debug(ex, "Failed to run pre-flight existence check before AddIndexer; proceeding to create");
            }

            var request = BuildRequest(settings, $"{AppIndexerApiRoute}", HttpMethod.Post);

            request.SetContent(indexer.ToJson());
            request.ContentSummary = indexer.ToJson(Formatting.None);

            try
            {
                _logger.Debug("Request payload: {0}", request.ContentSummary);
                return Execute<ListenarrIndexer>(request);
            }
            catch (HttpException ex) when (ex.Response.StatusCode == HttpStatusCode.BadRequest)
            {
                _logger.Debug("Retrying to add indexer forcefully. Original response: {0}", ex.Response?.Content ?? string.Empty);
                request.Url = request.Url.AddQueryParam("forceSave", "true");
                _logger.Debug("Retry payload: {0}", request.ContentSummary);
                return ExecuteIndexerRequest(request);
            }
            catch (HttpException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                // Try plural form as a fallback
                var fallback = BuildRequest(settings, $"{AppApiRoute}/indexers", HttpMethod.Post);
                fallback.SetContent(indexer.ToJson());
                fallback.ContentSummary = indexer.ToJson(Formatting.None);

                try
                {
                    return ExecuteIndexerRequest(fallback);
                }
                catch (HttpException)
                {
                    throw;
                }
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
            catch (HttpException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                // Try plural form as a fallback
                var fallback = BuildRequest(settings, $"{AppApiRoute}/indexers/{indexer.Id}", HttpMethod.Put);
                fallback.SetContent(indexer.ToJson());
                fallback.ContentSummary = indexer.ToJson(Formatting.None);

                try
                {
                    return ExecuteIndexerRequest(fallback);
                }
                catch (HttpException)
                {
                    throw;
                }
            }
        }

        public ValidationFailure TestConnection(ListenarrIndexer indexer, ListenarrSettings settings)
        {
            var request = BuildRequest(settings, $"{AppIndexerApiRoute}/test", HttpMethod.Post);

            request.SetContent(indexer.ToJson());
            request.ContentSummary = indexer.ToJson(Formatting.None);

            try
            {
                var applicationVersion = _httpClient.Post(request).Headers.GetSingleValue("X-Application-Version");

                if (applicationVersion == null)
                {
                    // Try plural endpoint as a fallback
                    var fallback = BuildRequest(settings, $"{AppApiRoute}/indexers/test", HttpMethod.Post);
                    fallback.SetContent(indexer.ToJson());
                    fallback.ContentSummary = indexer.ToJson(Formatting.None);

                    applicationVersion = _httpClient.Post(fallback).Headers.GetSingleValue("X-Application-Version");
                }

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
            catch (HttpException)
            {
                // Bubble HttpExceptions to be handled by the caller similar to other proxies
                throw;
            }
        }

        private ListenarrIndexer ExecuteIndexerRequest(HttpRequest request)
        {
            try
            {
                return Execute<ListenarrIndexer>(request);
            }
            catch (HttpException ex)
            {
                var responseContent = ex.Response?.Content ?? string.Empty;

                switch (ex.Response.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        _logger.Warn(ex, "API Key is invalid. Response: {0}", responseContent);
                        break;
                    case HttpStatusCode.BadRequest:
                        if (responseContent.Contains("Query successful, but no results in the configured categories were returned from your indexer.", StringComparison.InvariantCultureIgnoreCase))
                        {
                            _logger.Warn(ex, "No Results in configured categories. See FAQ Entry: Prowlarr will not sync X Indexer to App. Response: {0}", responseContent);
                            break;
                        }

                        _logger.Error(ex, "Invalid Request. Response: {0}", responseContent);
                        break;
                    case HttpStatusCode.SeeOther:
                    case HttpStatusCode.TemporaryRedirect:
                        _logger.Warn(ex, "App returned redirect and is invalid. Check App URL. Response: {0}", responseContent);
                        break;
                    case HttpStatusCode.NotFound:
                        _logger.Warn(ex, "Remote indexer not found. Response: {0}", responseContent);
                        break;
                    default:
                        _logger.Error(ex, "Unexpected response status code: {0}. Response: {1}", ex.Response.StatusCode, responseContent);
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
