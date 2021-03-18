using System.Net;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Download
{
    public abstract class UsenetClientBase<TSettings> : DownloadClientBase<TSettings>
        where TSettings : IProviderConfig, new()
    {
        protected readonly IHttpClient _httpClient;
        private readonly IValidateNzbs _nzbValidationService;

        protected UsenetClientBase(IHttpClient httpClient,
                                   IConfigService configService,
                                   IDiskProvider diskProvider,
                                   IValidateNzbs nzbValidationService,
                                   Logger logger)
            : base(configService, diskProvider, logger)
        {
            _httpClient = httpClient;
            _nzbValidationService = nzbValidationService;
        }

        public override DownloadProtocol Protocol => DownloadProtocol.Usenet;

        protected abstract string AddFromNzbFile(ReleaseInfo release, string filename, byte[] fileContents);
        protected abstract string AddFromLink(ReleaseInfo release);

        public override string Download(ReleaseInfo release, bool redirect)
        {
            var url = release.DownloadUrl;

            if (redirect)
            {
                return AddFromLink(release);
            }

            var filename = StringUtil.CleanFileName(release.Title) + ".nzb";

            byte[] nzbData;

            try
            {
                var request = new HttpRequest(url);
                nzbData = _httpClient.Get(request).ResponseData;

                _logger.Debug("Downloaded nzb for release '{0}' finished ({1} bytes from {2})", release.Title, nzbData.Length, url);
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.Error(ex, "Downloading nzb file for release '{0}' failed since it no longer exists ({1})", release.Title, url);
                    throw new ReleaseUnavailableException(release, "Downloading nzb failed", ex);
                }

                if ((int)ex.Response.StatusCode == 429)
                {
                    _logger.Error("API Grab Limit reached for {0}", url);
                }
                else
                {
                    _logger.Error(ex, "Downloading nzb for release '{0}' failed ({1})", release.Title, url);
                }

                throw new ReleaseDownloadException(release, "Downloading nzb failed", ex);
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Downloading nzb for release '{0}' failed ({1})", release.Title, url);

                throw new ReleaseDownloadException(release, "Downloading nzb failed", ex);
            }

            _nzbValidationService.Validate(filename, nzbData);

            _logger.Info("Adding report [{0}] to the queue.", release.Title);
            return AddFromNzbFile(release, filename, nzbData);
        }
    }
}
