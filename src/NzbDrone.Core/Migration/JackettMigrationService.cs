using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Migration
{
    public interface IJackettMigrationService
    {
        List<JackettIndexerDefinition> GetJackettIndexers(string jackettPath, string jackettApi);
        List<JackettIndexerConfigDefintion> GetJackettIndexerConfig(JackettIndexerDefinition jackettIndexerDefinition, string jackettPath, string jackettApi);
        void MigrateJackettIndexer(JackettIndexerDefinition jackettIndexer, List<JackettIndexerConfigDefintion> jackettIndexerConfig);
    }

    public class JackettMigrationService : IJackettMigrationService
    {
        private readonly IIndexerFactory _indexerFactory;
        private readonly Logger _logger;
        public IHttpClient HttpClient { get; set; }
        public IHttpRequestBuilderFactory RequestBuilder { get; set; }

        public JackettMigrationService(IIndexerFactory indexerFactory, Logger logger)
        {
            _logger = logger;
            _indexerFactory = indexerFactory;
        }

        public List<JackettIndexerDefinition> GetJackettIndexers(string jackettPath, string jackettApi)
        {
            _logger.Info("Getting all configured Jackett Indexers");

            var requestBuilder = BuildRequest(jackettPath, jackettApi, true);

            var jsonResponse = JsonConvert.DeserializeObject<List<JackettIndexerDefinition>>(HttpClient.Execute(requestBuilder.Build()).Content);

            return jsonResponse;
        }

        public List<JackettIndexerConfigDefintion> GetJackettIndexerConfig(JackettIndexerDefinition jackettIndexerDefinition, string jackettPath, string jackettApi)
        {
            _logger.Debug($"Getting config from Jackett for {jackettIndexerDefinition.Name}");

            var requestBuilder = BuildRequest(jackettPath, jackettApi, jackettIndexerDefinition.Id);

            var jsonResponse = JsonConvert.DeserializeObject<List<JackettIndexerConfigDefintion>>(HttpClient.Execute(requestBuilder.Build()).Content);

            return jsonResponse;
        }

        public void MigrateJackettIndexer(JackettIndexerDefinition jackettIndexer, List<JackettIndexerConfigDefintion> jackettIndexerConfig)
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
