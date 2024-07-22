using System;
using System.Text;
using System.Threading.Tasks;
using MonoTorrent;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
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
        private readonly ITorrentFileInfoReader _torrentFileInfoReader;
        private readonly ISeedConfigProvider _seedConfigProvider;

        protected TorrentClientBase(ITorrentFileInfoReader torrentFileInfoReader,
                                    ISeedConfigProvider seedConfigProvider,
                                    IConfigService configService,
                                    IDiskProvider diskProvider,
                                    Logger logger)
            : base(configService, diskProvider, logger)
        {
            _torrentFileInfoReader = torrentFileInfoReader;
            _seedConfigProvider = seedConfigProvider;
        }

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public virtual bool PreferTorrentFile => false;

        protected abstract string AddFromMagnetLink(TorrentInfo release, string hash, string magnetLink);
        protected abstract string AddFromTorrentFile(TorrentInfo release, string hash, string filename, byte[] fileContent);
        protected abstract string AddFromTorrentLink(TorrentInfo release, string hash, string torrentLink);

        public override async Task<string> Download(ReleaseInfo release, bool redirect, IIndexer indexer)
        {
            var torrentInfo = release as TorrentInfo;

            if (torrentInfo != null)
            {
                // Get the seed configuration for this release.
                torrentInfo.SeedConfiguration = _seedConfigProvider.GetSeedConfiguration(release);
            }

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
                        return await DownloadFromWebUrl(torrentInfo, indexer, torrentUrl);
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
                        return DownloadFromMagnetUrl(torrentInfo, magnetUrl);
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
                        return DownloadFromMagnetUrl(torrentInfo, magnetUrl);
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
                    return await DownloadFromWebUrl(torrentInfo, indexer, torrentUrl);
                }
            }

            return null;
        }

        private async Task<string> DownloadFromWebUrl(TorrentInfo release, IIndexer indexer, string torrentUrl)
        {
            var downloadResponse = await indexer.Download(new Uri(torrentUrl));
            var torrentFile = downloadResponse.Data;

            // handle magnet URLs
            if (torrentFile.Length >= 7
                && torrentFile[0] == 0x6d
                && torrentFile[1] == 0x61
                && torrentFile[2] == 0x67
                && torrentFile[3] == 0x6e
                && torrentFile[4] == 0x65
                && torrentFile[5] == 0x74
                && torrentFile[6] == 0x3a)
            {
                var magnetUrl = Encoding.UTF8.GetString(torrentFile);
                return DownloadFromMagnetUrl(release, magnetUrl);
            }

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

        private string DownloadFromMagnetUrl(TorrentInfo release, string magnetUrl)
        {
            string hash = null;
            string actualHash = null;

            try
            {
                hash = MagnetLink.Parse(magnetUrl).InfoHash.ToHex();
            }
            catch (FormatException ex)
            {
                throw new ReleaseDownloadException("Failed to parse magnetlink for release '{0}': '{1}'", ex, release.Title, magnetUrl);
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
