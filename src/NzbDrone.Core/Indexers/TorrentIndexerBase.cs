using System;
using System.Text;
using MonoTorrent;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers
{
    public abstract class TorrentIndexerBase<TSettings> : HttpIndexerBase<TSettings>
        where TSettings : IIndexerSettings, new()
    {
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        protected TorrentIndexerBase(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        protected override void ValidateDownloadData(byte[] fileData)
        {
            try
            {
                Torrent.Load(fileData);
            }
            catch (Exception ex)
            {
                _logger.Debug("Invalid torrent file contents: {0}", Encoding.ASCII.GetString(fileData));
                throw new ReleaseUnavailableException($"Invalid torrent file contents. Reason: {ex.Message}", ex);
            }
        }
    }
}
