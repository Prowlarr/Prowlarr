using NzbDrone.Core.Configuration;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1.Config
{
    public class DevelopmentConfigResource : RestResource
    {
        //Loggings
        public string ConsoleLogLevel { get; set; }
        public bool LogSql { get; set; }
        public bool LogIndexerResponse { get; set; }
        public int LogRotate { get; set; }
        public bool FilterSentryEvents { get; set; }
    }

    public static class DevelopmentConfigResourceMapper
    {
        public static DevelopmentConfigResource ToResource(this IConfigFileProvider model, IConfigService configService)
        {
            return new DevelopmentConfigResource
            {
                ConsoleLogLevel = model.ConsoleLogLevel,
                LogSql = model.LogSql,
                LogIndexerResponse = configService.LogIndexerResponse,
                LogRotate = model.LogRotate,
                FilterSentryEvents = model.FilterSentryEvents
            };
        }
    }
}
