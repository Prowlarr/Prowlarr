using System;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Security;

namespace NzbDrone.Core.Download
{
    public interface IDownloadMappingService
    {
        Uri ConvertToProxyLink(Uri link, string serverUrl, int indexerId, string file = "t");
        string ConvertToNormalLink(string link);
    }

    public class DownloadMappingService : IDownloadMappingService
    {
        private readonly IProtectionService _protectionService;
        private readonly IConfigFileProvider _configFileProvider;

        public DownloadMappingService(IProtectionService protectionService, IConfigFileProvider configFileProvider)
        {
            _protectionService = protectionService;
            _configFileProvider = configFileProvider;
        }

        public Uri ConvertToProxyLink(Uri link, string serverUrl, int indexerId, string file = "t")
        {
            var urlBase = _configFileProvider.UrlBase;

            if (urlBase.IsNotNullOrWhiteSpace() && !urlBase.StartsWith("/"))
            {
                urlBase = "/" + urlBase;
            }

            var encryptedLink = _protectionService.Protect(link.ToString());
            var encodedLink = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(encryptedLink));
            var urlEncodedFile = Uri.EscapeDataString(file);
            var proxyLink = $"{serverUrl}{urlBase}/{indexerId}/download?apikey={_configFileProvider.ApiKey}&link={encodedLink}&file={urlEncodedFile}";
            return new Uri(proxyLink);
        }

        public string ConvertToNormalLink(string link)
        {
            var encodedLink = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(link));
            var decryptedLink = _protectionService.UnProtect(encodedLink);

            return decryptedLink;
        }
    }
}
