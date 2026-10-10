using Newtonsoft.Json;

namespace NzbDrone.Core.Applications.CrossSeed
{
    public class CrossSeedStatus
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("appName")]
        public string AppName { get; set; }
    }
}
