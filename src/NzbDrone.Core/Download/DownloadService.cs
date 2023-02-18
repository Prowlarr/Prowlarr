using System;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Download.Clients;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Indexers.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download
{
    public interface IDownloadService
    {
        Task SendReportToClient(ReleaseInfo release, string source, string host, bool redirect);
        Task<byte[]> DownloadReport(string link, int indexerId, string source, string host, string title);
        void RecordRedirect(string link, int indexerId, string source, string host, string title);
    }

    public class DownloadService : IDownloadService
    {
        private readonly IProvideDownloadClient _downloadClientProvider;
        private readonly IDownloadClientStatusService _downloadClientStatusService;
        private readonly IIndexerFactory _indexerFactory;
        private readonly IIndexerStatusService _indexerStatusService;
        private readonly IRateLimitService _rateLimitService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public DownloadService(IProvideDownloadClient downloadClientProvider,
                               IDownloadClientStatusService downloadClientStatusService,
                               IIndexerFactory indexerFactory,
                               IIndexerStatusService indexerStatusService,
                               IRateLimitService rateLimitService,
                               IEventAggregator eventAggregator,
                               Logger logger)
        {
            _downloadClientProvider = downloadClientProvider;
            _downloadClientStatusService = downloadClientStatusService;
            _indexerFactory = indexerFactory;
            _indexerStatusService = indexerStatusService;
            _rateLimitService = rateLimitService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public async Task SendReportToClient(ReleaseInfo release, string source, string host, bool redirect)
        {
            var downloadTitle = release.Title;
            var downloadClient = _downloadClientProvider.GetDownloadClient(release.DownloadProtocol);

            if (downloadClient == null)
            {
                throw new DownloadClientUnavailableException($"{release.DownloadProtocol} Download client isn't configured yet");
            }

            // Get the seed configuration for this release.
            // remoteMovie.SeedConfiguration = _seedConfigProvider.GetSeedConfiguration(remoteMovie);

            // Limit grabs to 2 per second.
            if (release.DownloadUrl.IsNotNullOrWhiteSpace() && !release.DownloadUrl.StartsWith("magnet:"))
            {
                var url = new HttpUri(release.DownloadUrl);
                _rateLimitService.WaitAndPulse(url.Host, TimeSpan.FromSeconds(2));
            }

            var indexer = _indexerFactory.GetInstance(_indexerFactory.Get(release.IndexerId));

            string downloadClientId;
            try
            {
                downloadClientId = await downloadClient.Download(release, redirect, indexer);
                _downloadClientStatusService.RecordSuccess(downloadClient.Definition.Id);
                _indexerStatusService.RecordSuccess(release.IndexerId);
            }
            catch (ReleaseUnavailableException)
            {
                _logger.Trace("Release {0} no longer available on indexer.", release);
                _eventAggregator.PublishEvent(new IndexerDownloadEvent(release.IndexerId, false, source, host, release.Title, release.DownloadUrl, redirect));
                throw;
            }
            catch (DownloadClientRejectedReleaseException)
            {
                _logger.Trace("Release {0} rejected by download client, possible duplicate.", release);
                _eventAggregator.PublishEvent(new IndexerDownloadEvent(release.IndexerId, false, source, host, release.Title, release.DownloadUrl, redirect));
                throw;
            }
            catch (ReleaseDownloadException ex)
            {
                var http429 = ex.InnerException as TooManyRequestsException;
                if (http429 != null)
                {
                    _indexerStatusService.RecordFailure(release.IndexerId, http429.RetryAfter);
                }
                else
                {
                    _indexerStatusService.RecordFailure(release.IndexerId);
                }

                _eventAggregator.PublishEvent(new IndexerDownloadEvent(release.IndexerId, false, source, host, release.Title, release.DownloadUrl, redirect));

                throw;
            }

            _logger.ProgressInfo("Report sent to {0}. {1}", downloadClient.Definition.Name, downloadTitle);

            _eventAggregator.PublishEvent(new IndexerDownloadEvent(release.IndexerId, true, source, host, release.Title, release.DownloadUrl, redirect));
        }

        public async Task<byte[]> DownloadReport(string link, int indexerId, string source, string host, string title)
        {
            _logger.Trace("Attempting download of {0}", link);
            var url = new Uri(link);

            // Limit grabs to 2 per second.
            if (link.IsNotNullOrWhiteSpace() && !link.StartsWith("magnet:"))
            {
                await _rateLimitService.WaitAndPulseAsync(url.Host, TimeSpan.FromSeconds(2));
            }

            var indexer = _indexerFactory.GetInstance(_indexerFactory.Get(indexerId));
            var success = false;
            var downloadedBytes = Array.Empty<byte>();

            try
            {
                downloadedBytes = await indexer.Download(url);
                _indexerStatusService.RecordSuccess(indexerId);
                success = true;
            }
            catch (ReleaseUnavailableException)
            {
                _logger.Trace("Release {0} no longer available on indexer.", link);
                _eventAggregator.PublishEvent(new IndexerDownloadEvent(indexerId, success, source, host, title, url.AbsoluteUri));
                throw;
            }
            catch (ReleaseDownloadException ex)
            {
                var http429 = ex.InnerException as TooManyRequestsException;
                if (http429 != null)
                {
                    _indexerStatusService.RecordFailure(indexerId, http429.RetryAfter);
                }
                else
                {
                    _indexerStatusService.RecordFailure(indexerId);
                }

                _eventAggregator.PublishEvent(new IndexerDownloadEvent(indexerId, success, source, host, title, url.AbsoluteUri));
                throw;
            }

            _logger.Trace("Downloaded {0} bytes from {1}", downloadedBytes.Length, link);
            _eventAggregator.PublishEvent(new IndexerDownloadEvent(indexerId, success, source, host, title, url.AbsoluteUri));

            return downloadedBytes;
        }

        public void RecordRedirect(string link, int indexerId, string source, string host, string title)
        {
            _eventAggregator.PublishEvent(new IndexerDownloadEvent(indexerId, true, source, host, title, link, true));
        }
    }
}
