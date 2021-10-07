using System;
using System.Net;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers
{
    public abstract class UsenetIndexerBase<TSettings> : HttpIndexerBase<TSettings>
        where TSettings : IIndexerSettings, new()
    {
        private readonly IValidateNzbs _nzbValidationService;

        protected UsenetIndexerBase(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, IValidateNzbs nzbValidationService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
            _nzbValidationService = nzbValidationService;
        }

        public override async Task<byte[]> Download(Uri link)
        {
            Cookies = GetCookies();

            var requestBuilder = new HttpRequestBuilder(link.AbsoluteUri);

            if (Cookies != null)
            {
                requestBuilder.SetCookies(Cookies);
            }

            var request = requestBuilder.Build();
            request.AllowAutoRedirect = FollowRedirect;

            byte[] nzbData;

            try
            {
                var response = await _httpClient.ExecuteProxiedAsync(request, Definition);
                nzbData = response.ResponseData;

                _logger.Debug("Downloaded nzb for release finished ({0} bytes from {1})", nzbData.Length, link.AbsoluteUri);
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.Error(ex, "Downloading nzb file for release failed since it no longer exists ({0})", link.AbsoluteUri);
                    throw new ReleaseUnavailableException("Downloading nzb failed", ex);
                }

                if ((int)ex.Response.StatusCode == 429)
                {
                    _logger.Error("API Grab Limit reached for {0}", link.AbsoluteUri);
                }
                else
                {
                    _logger.Error(ex, "Downloading nzb for release failed ({0})", link.AbsoluteUri);
                }

                throw new ReleaseDownloadException("Downloading nzb failed", ex);
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Downloading nzb for release failed ({0})", link.AbsoluteUri);

                throw new ReleaseDownloadException("Downloading nzb failed", ex);
            }
            catch (Exception)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.Error("Downloading nzb failed");
                throw;
            }

            _nzbValidationService.Validate(nzbData);

            return nzbData;
        }
    }
}
