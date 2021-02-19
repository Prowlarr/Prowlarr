using System;
using NLog;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers
{
    public interface IDownloadService
    {
        byte[] DownloadReport(string link, int indexerId, string source);
    }

    public class DownloadService : IDownloadService
    {
        private readonly IIndexerFactory _indexerFactory;
        private readonly IIndexerStatusService _indexerStatusService;
        private readonly IRateLimitService _rateLimitService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public DownloadService(IIndexerFactory indexerFactory,
                               IIndexerStatusService indexerStatusService,
                               IRateLimitService rateLimitService,
                               IEventAggregator eventAggregator,
                               Logger logger)
        {
            _indexerFactory = indexerFactory;
            _indexerStatusService = indexerStatusService;
            _rateLimitService = rateLimitService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public byte[] DownloadReport(string link, int indexerId, string source)
        {
            var url = new HttpUri(link);

            // Limit grabs to 2 per second.
            if (link.IsNotNullOrWhiteSpace() && !link.StartsWith("magnet:"))
            {
                _rateLimitService.WaitAndPulse(url.Host, TimeSpan.FromSeconds(2));
            }

            var indexer = _indexerFactory.GetInstance(_indexerFactory.Get(indexerId));
            var success = false;
            var downloadedBytes = Array.Empty<byte>();

            try
            {
                downloadedBytes = indexer.Download(url);
                _indexerStatusService.RecordSuccess(indexerId);
                success = true;
            }
            catch (ReleaseUnavailableException)
            {
                _logger.Trace("Release {0} no longer available on indexer.", link);
                _eventAggregator.PublishEvent(new IndexerDownloadEvent(indexerId, success, source));
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

                _eventAggregator.PublishEvent(new IndexerDownloadEvent(indexerId, success, source));
                throw;
            }

            _eventAggregator.PublishEvent(new IndexerDownloadEvent(indexerId, success, source));
            return downloadedBytes;
        }
    }
}
