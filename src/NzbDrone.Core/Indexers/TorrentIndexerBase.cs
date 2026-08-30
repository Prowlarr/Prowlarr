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

                if (LooksLikeHtml(fileData))
                {
                    throw new ReleaseUnavailableException($"Invalid torrent file contents, tracker returned a web page instead of a torrent (release likely removed). Reason: {ex.Message}", ex);
                }

                throw new NotSupportedException($"Invalid torrent file contents. Reason: {ex.Message}", ex);
            }
        }

        private static bool LooksLikeHtml(byte[] fileData)
        {
            // Some trackers redirect a removed/unavailable release to a web page (e.g. PTP's
            // "log" page, see #2556) and respond with HTTP 200 and an HTML body instead of a 404
            // or a valid torrent file. MonoTorrent's Torrent.Load correctly rejects that as
            // invalid, but a parse failure alone doesn't say *why* it failed - sniffing for an
            // HTML preamble narrows "unavailable" to the case that's actually evidenced, rather
            // than reclassifying every parse failure (a genuinely corrupted download, a
            // different indexer bug, etc.) as "not a real failure".
            var text = Encoding.ASCII.GetString(fileData, 0, Math.Min(fileData.Length, 512)).TrimStart();

            return text.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
                   text.StartsWith("<html", StringComparison.OrdinalIgnoreCase);
        }
    }
}
