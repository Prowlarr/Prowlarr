using System;
using System.Net;
using System.Threading.Tasks;
using MonoTorrent;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Download
{
    public abstract class TorrentClientBase<TSettings> : DownloadClientBase<TSettings>
        where TSettings : IProviderConfig, new()
    {
        protected readonly IHttpClient _httpClient;
        protected readonly ITorrentFileInfoReader _torrentFileInfoReader;

        protected TorrentClientBase(ITorrentFileInfoReader torrentFileInfoReader,
                                    IHttpClient httpClient,
                                    IConfigService configService,
                                    IDiskProvider diskProvider,
                                    Logger logger)
            : base(configService, diskProvider, logger)
        {
            _httpClient = httpClient;
            _torrentFileInfoReader = torrentFileInfoReader;
        }

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public virtual bool PreferTorrentFile => false;

        protected abstract string AddFromMagnetLink(ReleaseInfo release, string hash, string magnetLink);
        protected abstract string AddFromTorrentFile(ReleaseInfo release, string hash, string filename, byte[] fileContent);
        protected abstract string AddFromTorrentLink(ReleaseInfo release, string hash, string torrentLink);

        public override async Task<string> Download(ReleaseInfo release, bool redirect, IIndexer indexer)
        {
            var torrentInfo = release as TorrentInfo;

            string magnetUrl = null;
            string torrentUrl = null;

            if (release.DownloadUrl.IsNotNullOrWhiteSpace() && release.DownloadUrl.StartsWith("magnet:"))
            {
                magnetUrl = release.DownloadUrl;
            }
            else
            {
                torrentUrl = release.DownloadUrl;
            }

            if (torrentInfo != null && !torrentInfo.MagnetUrl.IsNullOrWhiteSpace())
            {
                magnetUrl = torrentInfo.MagnetUrl;
            }

            if (PreferTorrentFile)
            {
                if (torrentUrl.IsNotNullOrWhiteSpace())
                {
                    try
                    {
                        return await DownloadFromWebUrl(release, indexer, torrentUrl);
                    }
                    catch (Exception ex)
                    {
                        if (!magnetUrl.IsNullOrWhiteSpace())
                        {
                            throw;
                        }

                        _logger.Debug("Torrent download failed, trying magnet. ({0})", ex.Message);
                    }
                }

                if (magnetUrl.IsNotNullOrWhiteSpace())
                {
                    try
                    {
                        return DownloadFromMagnetUrl(release, magnetUrl);
                    }
                    catch (NotSupportedException ex)
                    {
                        throw new ReleaseDownloadException("Magnet not supported by download client. ({0})", ex.Message);
                    }
                }
            }
            else
            {
                if (magnetUrl.IsNotNullOrWhiteSpace())
                {
                    try
                    {
                        return DownloadFromMagnetUrl(release, magnetUrl);
                    }
                    catch (NotSupportedException ex)
                    {
                        if (torrentUrl.IsNullOrWhiteSpace())
                        {
                            throw new ReleaseDownloadException("Magnet not supported by download client. ({0})", ex.Message);
                        }

                        _logger.Debug("Magnet not supported by download client, trying torrent. ({0})", ex.Message);
                    }
                }

                if (torrentUrl.IsNotNullOrWhiteSpace())
                {
                    return await DownloadFromWebUrl(release, indexer, torrentUrl);
                }
            }

            return null;
        }

        private async Task<string> DownloadFromWebUrl(ReleaseInfo release, IIndexer indexer, string torrentUrl)
        {
            byte[] torrentFile = null;

            torrentFile = await indexer.Download(new Uri(torrentUrl));

            var filename = string.Format("{0}.torrent", StringUtil.CleanFileName(release.Title));
            var hash = _torrentFileInfoReader.GetHashFromTorrentFile(torrentFile);
            var actualHash = AddFromTorrentFile(release, hash, filename, torrentFile);

            if (actualHash.IsNotNullOrWhiteSpace() && hash != actualHash)
            {
                _logger.Debug(
                    "{0} did not return the expected InfoHash for '{1}', Prowlarr could potentially lose track of the download in progress.",
                    Definition.Implementation,
                    release.DownloadUrl);
            }

            return actualHash;
        }

        private string DownloadFromMagnetUrl(ReleaseInfo release, string magnetUrl)
        {
            string hash = null;
            string actualHash = null;

            try
            {
                hash = MagnetLink.Parse(magnetUrl).InfoHash.ToHex();
            }
            catch (FormatException ex)
            {
                _logger.Error(ex, "Failed to parse magnetlink for release '{0}': '{1}'", release.Title, magnetUrl);

                return null;
            }

            if (hash != null)
            {
                actualHash = AddFromMagnetLink(release, hash, magnetUrl);
            }

            if (actualHash.IsNotNullOrWhiteSpace() && hash != actualHash)
            {
                _logger.Debug(
                    "{0} did not return the expected InfoHash for '{1}', Prowlarr could potentially lose track of the download in progress.",
                    Definition.Implementation,
                    release.DownloadUrl);
            }

            return actualHash;
        }
    }
}
