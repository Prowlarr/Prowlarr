using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Migration
{
    public interface IJackettMigrationService
    {
        List<JackettIndexerDefinition> GetJackettIndexers(string jackettPath, string jackettApi);
        JackettIndexerConfigDefintion GetJackettIndexerConfig(JackettIndexerDefinition jackettIndexerDefinition, string jackettPath, string jackettApi);
        void MigrateJackettIndexer(JackettIndexerDefinition jackettIndexer, JackettIndexerConfigDefintion jackettIndexerConfig);
    }

    public class JackettMigrationService : IJackettMigrationService
    {
        private readonly Logger _logger;
        public IHttpClient HttpClient { get; set; }
        public IHttpRequestBuilderFactory RequestBuilder { get; set; }
        public JackettMigrationService(Logger logger)
        {
            _logger = logger;
        }

        public List<JackettIndexerDefinition> GetJackettIndexers(string jackettPath, string jackettApi)
        {
            _logger.Info("Getting all configured Jackett Indexers");

            var requestBuilder = BuildRequest(jackettPath, jackettApi, true);

            var jsonResponse = JsonConvert.DeserializeObject<List<JackettIndexerDefinition>>(HttpClient.Execute(requestBuilder.Build()).Content);

            return jsonResponse;
        }

        public JackettIndexerConfigDefintion GetJackettIndexerConfig(JackettIndexerDefinition jackettIndexerDefinition, string jackettPath, string jackettApi)
        {
            _logger.Debug($"Getting config from Jackett for {jackettIndexerDefinition.Name}");

            var requestBuilder = BuildRequest(jackettPath, jackettApi, jackettIndexerDefinition.Id);

            var jsonResponse = JsonConvert.DeserializeObject<JackettIndexerConfigDefintion>(HttpClient.Execute(requestBuilder.Build()).Content);

            return jsonResponse;
        }

        public void MigrateJackettIndexer(JackettIndexerDefinition jackettIndexer, JackettIndexerConfigDefintion jackettIndexerConfig)
        {
            _logger.Info($"Creating {jackettIndexer.Name} in Prowlarr");

            //TODO: Creation Logic of indexers
        }

        protected HttpRequestBuilder BuildRequest(string jackettPath, string jackettApi, bool configuredIndexers)
        {
            var url = jackettPath + "api/v2.0/indexers";
            var requestBuilder = new HttpRequestBuilder(url);
            requestBuilder.AddQueryParam("apiKey", jackettApi);
            requestBuilder.AddQueryParam("configured", configuredIndexers);

            return requestBuilder;
        }

        protected HttpRequestBuilder BuildRequest(string jackettPath, string jackettApi, string indexerId)
        {
            var url = jackettPath + "api/v2.0/indexers/" + indexerId + "/config";
            var requestBuilder = new HttpRequestBuilder(url);
            requestBuilder.AddQueryParam("apiKey", jackettApi);

            return requestBuilder;
        }
    }
}
