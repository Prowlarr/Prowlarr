using System;
using NzbDrone.Common.EnvironmentInfo;

namespace NzbDrone.Common.Http
{
    public interface IUserAgentBuilder
    {
        string GetUserAgent(bool simplified = false);
    }

    public class UserAgentBuilder : IUserAgentBuilder
    {
        private readonly string _userAgentSimplified;
        private readonly string _userAgent;

        public string GetUserAgent(bool simplified)
        {
            if (simplified)
            {
                return _userAgentSimplified;
            }

            return _userAgent;
        }

        public UserAgentBuilder(IOsInfo osInfo)
        {
            var osName = OsInfo.Os.ToString();

            if (!string.IsNullOrWhiteSpace(osInfo.Name))
            {
                osName = osInfo.Name.ToLower();
            }

            var osVersion = osInfo.Version?.ToLower();

            var enableDownloadCache = bool.TryParse(
                Environment.GetEnvironmentVariable("ENABLE_DOWNLOAD_CACHE"),
                out var enabled) && enabled;

            if (enableDownloadCache)
            {
                // Use Sonarr as the user agent if downloading through prowlarr
                _userAgent = $"Sonarr/4.0.19.2979 ({osName} {osVersion})";
                _userAgentSimplified = $"Sonarr/4.0";
            }
            else
            {
                _userAgent = $"{BuildInfo.AppName}/{BuildInfo.Version} ({osName} {osVersion})";
                _userAgentSimplified = $"{BuildInfo.AppName}/{BuildInfo.Version.ToString(2)}";
            }
        }
    }
}
