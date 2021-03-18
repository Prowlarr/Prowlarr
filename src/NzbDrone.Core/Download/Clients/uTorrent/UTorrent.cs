using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.UTorrent
{
    public class UTorrent : TorrentClientBase<UTorrentSettings>
    {
        private readonly IUTorrentProxy _proxy;
        private readonly ICached<UTorrentTorrentCache> _torrentCache;

        public UTorrent(IUTorrentProxy proxy,
                        ICacheManager cacheManager,
                        ITorrentFileInfoReader torrentFileInfoReader,
                        IHttpClient httpClient,
                        IConfigService configService,
                        IDiskProvider diskProvider,
                        Logger logger)
            : base(torrentFileInfoReader, httpClient, configService, diskProvider, logger)
        {
            _proxy = proxy;

            _torrentCache = cacheManager.GetCache<UTorrentTorrentCache>(GetType(), "differentialTorrents");
        }

        protected override string AddFromMagnetLink(ReleaseInfo release, string hash, string magnetLink)
        {
            _proxy.AddTorrentFromUrl(magnetLink, Settings);

            //_proxy.SetTorrentSeedingConfiguration(hash, release.SeedConfiguration, Settings);
            if (Settings.Category.IsNotNullOrWhiteSpace())
            {
                _proxy.SetTorrentLabel(hash, Settings.Category, Settings);
            }

            if (Settings.Priority == (int)UTorrentPriority.First)
            {
                _proxy.MoveTorrentToTopInQueue(hash, Settings);
            }

            _proxy.SetState(hash, (UTorrentState)Settings.IntialState, Settings);

            return hash;
        }

        protected override string AddFromTorrentFile(ReleaseInfo release, string hash, string filename, byte[] fileContent)
        {
            _proxy.AddTorrentFromFile(filename, fileContent, Settings);

            //_proxy.SetTorrentSeedingConfiguration(hash, release.SeedConfiguration, Settings);
            if (Settings.Category.IsNotNullOrWhiteSpace())
            {
                _proxy.SetTorrentLabel(hash, Settings.Category, Settings);
            }

            if (Settings.Priority == (int)UTorrentPriority.First)
            {
                _proxy.MoveTorrentToTopInQueue(hash, Settings);
            }

            _proxy.SetState(hash, (UTorrentState)Settings.IntialState, Settings);

            return hash;
        }

        public override string Name => "uTorrent";

        private List<UTorrentTorrent> GetTorrents()
        {
            List<UTorrentTorrent> torrents;

            var cacheKey = string.Format("{0}:{1}:{2}", Settings.Host, Settings.Port, Settings.Category);
            var cache = _torrentCache.Find(cacheKey);

            var response = _proxy.GetTorrents(cache == null ? null : cache.CacheID, Settings);

            if (cache != null && response.Torrents == null)
            {
                var removedAndUpdated = new HashSet<string>(response.TorrentsChanged.Select(v => v.Hash).Concat(response.TorrentsRemoved));

                torrents = cache.Torrents
                    .Where(v => !removedAndUpdated.Contains(v.Hash))
                    .Concat(response.TorrentsChanged)
                    .ToList();
            }
            else
            {
                torrents = response.Torrents;
            }

            cache = new UTorrentTorrentCache
            {
                CacheID = response.CacheNumber,
                Torrents = torrents
            };

            _torrentCache.Set(cacheKey, cache, TimeSpan.FromMinutes(15));

            return torrents;
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
            if (failures.HasErrors())
            {
                return;
            }

            failures.AddIfNotNull(TestGetTorrents());
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                var version = _proxy.GetVersion(Settings);

                if (version < 25406)
                {
                    return new ValidationFailure(string.Empty, "Old uTorrent client with unsupported API, need 3.0 or higher");
                }
            }
            catch (DownloadClientAuthenticationException ex)
            {
                _logger.Error(ex, ex.Message);
                return new NzbDroneValidationFailure("Username", "Authentication failure")
                {
                    DetailedDescription = "Please verify your username and password."
                };
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Unable to connect to uTorrent");
                if (ex.Status == WebExceptionStatus.ConnectFailure)
                {
                    return new NzbDroneValidationFailure("Host", "Unable to connect")
                    {
                        DetailedDescription = "Please verify the hostname and port."
                    };
                }

                return new NzbDroneValidationFailure(string.Empty, "Unknown exception: " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to test uTorrent");

                return new NzbDroneValidationFailure("Host", "Unable to connect to uTorrent")
                       {
                           DetailedDescription = ex.Message
                       };
            }

            return null;
        }

        private ValidationFailure TestGetTorrents()
        {
            try
            {
                _proxy.GetTorrents(null, Settings);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get torrents");
                return new NzbDroneValidationFailure(string.Empty, "Failed to get the list of torrents: " + ex.Message);
            }

            return null;
        }

        protected override string AddFromTorrentLink(ReleaseInfo release, string hash, string torrentLink)
        {
            throw new NotImplementedException();
        }
    }
}
