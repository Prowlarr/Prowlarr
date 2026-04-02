using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.Applications
{
    public static class IndexerNameTemplateDefaults
    {
        public const string DefaultTemplate = "{name} ({instance})";
        public const string DefaultInstanceName = "Prowlarr";
    }

    public interface IIndexerNameTemplateService
    {
        string FormatIndexerName(string indexerName, string instanceName);
    }

    public class IndexerNameTemplateService : IIndexerNameTemplateService
    {
        private readonly IConfigService _configService;

        public IndexerNameTemplateService(IConfigService configService)
        {
            _configService = configService;
        }

        public string FormatIndexerName(string indexerName, string instanceName)
        {
            if (string.IsNullOrWhiteSpace(indexerName))
            {
                return string.Empty;
            }

            var template = _configService.IndexerNameTemplate;

            if (string.IsNullOrWhiteSpace(template))
            {
                return indexerName;
            }

            var finalInstanceName = !string.IsNullOrWhiteSpace(instanceName) ? instanceName : IndexerNameTemplateDefaults.DefaultInstanceName;

            return template
                .Replace("{name}", indexerName)
                .Replace("{instance}", finalInstanceName);
        }
    }
}
