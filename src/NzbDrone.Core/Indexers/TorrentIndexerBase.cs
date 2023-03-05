using System.Text;
using MonoTorrent;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers
{
    public abstract class TorrentIndexerBase<TSettings> : HttpIndexerBase<TSettings>
        where TSettings : IIndexerSettings, new()
    {
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
            catch
            {
                _logger.Trace("Invalid torrent file contents: {0}", Encoding.ASCII.GetString(fileData));
                throw;
            }
        }
    }
}
