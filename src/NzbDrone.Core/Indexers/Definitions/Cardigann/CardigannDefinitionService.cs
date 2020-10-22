using System.Collections.Generic;
using NzbDrone.Common.Http;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NzbDrone.Core.Indexers.Cardigann
{
    public interface ICardigannDefinitionService
    {
        List<CardigannMetaDefinition> All();
        CardigannDefinition GetDefinition(string id);
    }

    public class CardigannDefinitionService : ICardigannDefinitionService
    {
        private const int DEFINITION_VERSION = 1;

        private readonly IHttpClient _httpClient;

        private readonly IDeserializer _deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        public CardigannDefinitionService(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public List<CardigannMetaDefinition> All()
        {
            var request = new HttpRequest($"https://indexers.prowlarr.com/master/{DEFINITION_VERSION}");
            var response = _httpClient.Get<List<CardigannMetaDefinition>>(request);
            return response.Resource;
        }

        public CardigannDefinition GetDefinition(string id)
        {
            var req = new HttpRequest($"https://indexers.prowlarr.com/master/{DEFINITION_VERSION}/{id}");
            var response = _httpClient.Get(req);
            return _deserializer.Deserialize<CardigannDefinition>(response.Content);
        }
    }
}
