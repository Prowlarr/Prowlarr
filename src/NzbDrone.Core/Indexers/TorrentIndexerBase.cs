using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MonoTorrent;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers
{
    public abstract class TorrentIndexerBase<TSettings> : HttpIndexerBase<TSettings>
        where TSettings : IProviderConfig, new()
    {
        protected TorrentIndexerBase(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override async Task<byte[]> Download(Uri link)
        {
            Cookies = GetCookies();

            if (link.Scheme == "magnet")
            {
                ValidateMagnet(link.OriginalString);
                return Encoding.UTF8.GetBytes(link.OriginalString);
            }

            var requestBuilder = new HttpRequestBuilder(link.AbsoluteUri);

            if (Cookies != null)
            {
                requestBuilder.SetCookies(Cookies);
            }

            var request = requestBuilder.Build();
            request.AllowAutoRedirect = FollowRedirect;

            byte[] torrentData;

            try
            {
                var response = await _httpClient.ExecuteAsync(request);
                torrentData = response.ResponseData;
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.Error(ex, "Downloading torrent file for release failed since it no longer exists ({0})", link.AbsoluteUri);
                    throw new ReleaseUnavailableException("Downloading torrent failed", ex);
                }

                if ((int)ex.Response.StatusCode == 429)
                {
                    _logger.Error("API Grab Limit reached for {0}", link.AbsoluteUri);
                }
                else
                {
                    _logger.Error(ex, "Downloading torrent file for release failed ({0})", link.AbsoluteUri);
                }

                throw new ReleaseDownloadException("Downloading torrent failed", ex);
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Downloading torrent file for release failed ({0})", link.AbsoluteUri);

                throw new ReleaseDownloadException("Downloading torrent failed", ex);
            }
            catch (Exception)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.Error("Downloading torrent failed");
                throw;
            }

            return torrentData;
        }

        protected void ValidateMagnet(string link)
        {
            MagnetLink.Parse(link);
        }
    }
}
