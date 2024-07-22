using System;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Instrumentation.Extensions;
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
        Task SendReportToClient(ReleaseInfo release, string source, string host, bool redirect, int? downloadClientId);
        Task<byte[]> DownloadReport(string link, int indexerId, string source, string host, string title);
        void RecordRedirect(string link, int indexerId, string source, string host, string title);
    }

    public class DownloadService : IDownloadService
    {
        private readonly IProvideDownloadClient _downloadClientProvider;
        private readonly IDownloadClientStatusService _downloadClientStatusService;
        private readonly IIndexerFactory _indexerFactory;
        private readonly IIndexerStatusService _indexerStatusService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public DownloadService(IProvideDownloadClient downloadClientProvider,
                               IDownloadClientStatusService downloadClientStatusService,
                               IIndexerFactory indexerFactory,
                               IIndexerStatusService indexerStatusService,
                               IEventAggregator eventAggregator,
                               Logger logger)
        {
            _downloadClientProvider = downloadClientProvider;
            _downloadClientStatusService = downloadClientStatusService;
            _indexerFactory = indexerFactory;
            _indexerStatusService = indexerStatusService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public async Task SendReportToClient(ReleaseInfo release, string source, string host, bool redirect, int? downloadClientId)
        {
            var downloadClient = downloadClientId.HasValue
                ? _downloadClientProvider.Get(downloadClientId.Value)
                : _downloadClientProvider.GetDownloadClient(release.DownloadProtocol, release.IndexerId);

            await SendReportToClient(release, source, host, redirect, downloadClient);
        }

        private async Task SendReportToClient(ReleaseInfo release, string source, string host, bool redirect, IDownloadClient downloadClient)
        {
            var downloadTitle = release.Title;

            if (downloadClient == null)
            {
                throw new DownloadClientUnavailableException($"{release.DownloadProtocol} Download client isn't configured yet");
            }

            var indexer = _indexerFactory.GetInstance(_indexerFactory.Get(release.IndexerId));

            var grabEvent = new IndexerDownloadEvent(release, true, source, host, release.Title, release.DownloadUrl)
            {
                DownloadClient = downloadClient.Name,
                DownloadClientId = downloadClient.Definition.Id,
                DownloadClientName = downloadClient.Definition.Name,
                Redirect = redirect,
                Indexer = indexer,
                GrabTrigger = source == "Prowlarr" ? GrabTrigger.Manual : GrabTrigger.Api
            };

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
                grabEvent.Successful = false;
                _eventAggregator.PublishEvent(grabEvent);
                throw;
            }
            catch (DownloadClientRejectedReleaseException)
            {
                _logger.Trace("Release {0} rejected by download client, possible duplicate.", release);
                grabEvent.Successful = false;
                _eventAggregator.PublishEvent(grabEvent);
                throw;
            }
            catch (ReleaseDownloadException ex)
            {
                if (ex.InnerException is TooManyRequestsException http429)
                {
                    _indexerStatusService.RecordFailure(release.IndexerId, http429.RetryAfter);
                }
                else
                {
                    _indexerStatusService.RecordFailure(release.IndexerId);
                }

                grabEvent.Successful = false;

                _eventAggregator.PublishEvent(grabEvent);

                throw;
            }

            _logger.ProgressInfo("Report sent to {0}. {1}", downloadClient.Definition.Name, downloadTitle);

            if (!string.IsNullOrWhiteSpace(downloadClientId))
            {
                grabEvent.DownloadId = downloadClientId;
            }

            _eventAggregator.PublishEvent(grabEvent);
        }

        public async Task<byte[]> DownloadReport(string link, int indexerId, string source, string host, string title)
        {
            _logger.Trace("Attempting download of {0}", link);
            var url = new Uri(link);

            var indexer = _indexerFactory.GetInstance(_indexerFactory.Get(indexerId));

            var release = new ReleaseInfo
            {
                Title = title,
                DownloadUrl = link,
                IndexerId = indexerId,
                Indexer = indexer.Definition.Name,
                DownloadProtocol = indexer.Protocol
            };

            var grabEvent = new IndexerDownloadEvent(release, false, source, host, release.Title, release.DownloadUrl)
            {
                Indexer = indexer,
                GrabTrigger = source == "Prowlarr" ? GrabTrigger.Manual : GrabTrigger.Api
            };

            byte[] downloadedBytes;

            try
            {
                var downloadResponse = await indexer.Download(url);
                downloadedBytes = downloadResponse.Data;
                _indexerStatusService.RecordSuccess(indexerId);
                grabEvent.Successful = true;
                grabEvent.ElapsedTime = downloadResponse.ElapsedTime;
            }
            catch (ReleaseUnavailableException)
            {
                _logger.Trace("Release {0} no longer available on indexer.", link);
                _eventAggregator.PublishEvent(grabEvent);
                throw;
            }
            catch (ReleaseDownloadException ex)
            {
                if (ex.InnerException is TooManyRequestsException http429)
                {
                    _indexerStatusService.RecordFailure(indexerId, http429.RetryAfter);
                }
                else
                {
                    _indexerStatusService.RecordFailure(indexerId);
                }

                _eventAggregator.PublishEvent(grabEvent);
                throw;
            }

            _logger.Trace("Downloaded {0} bytes from {1}", downloadedBytes.Length, link);
            _eventAggregator.PublishEvent(grabEvent);

            return downloadedBytes;
        }

        public void RecordRedirect(string link, int indexerId, string source, string host, string title)
        {
            var indexer = _indexerFactory.GetInstance(_indexerFactory.Get(indexerId));

            var release = new ReleaseInfo
            {
                Title = title,
                DownloadUrl = link,
                IndexerId = indexerId,
                Indexer = indexer.Definition.Name,
                DownloadProtocol = indexer.Protocol
            };

            var grabEvent = new IndexerDownloadEvent(release, true, source, host, release.Title, release.DownloadUrl)
            {
                Redirect = true,
                Indexer = indexer,
                GrabTrigger = source == "Prowlarr" ? GrabTrigger.Manual : GrabTrigger.Api
            };

            _eventAggregator.PublishEvent(grabEvent);
        }
    }
}
