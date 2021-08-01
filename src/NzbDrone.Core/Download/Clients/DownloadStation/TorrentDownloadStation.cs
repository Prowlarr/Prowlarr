using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download.Clients.DownloadStation.Proxies;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.DownloadStation
{
    public class TorrentDownloadStation : TorrentClientBase<DownloadStationSettings>
    {
        protected readonly IDownloadStationInfoProxy _dsInfoProxy;
        protected readonly IDownloadStationTaskProxySelector _dsTaskProxySelector;
        protected readonly ISharedFolderResolver _sharedFolderResolver;
        protected readonly ISerialNumberProvider _serialNumberProvider;
        protected readonly IFileStationProxy _fileStationProxy;

        public TorrentDownloadStation(ISharedFolderResolver sharedFolderResolver,
                                      ISerialNumberProvider serialNumberProvider,
                                      IFileStationProxy fileStationProxy,
                                      IDownloadStationInfoProxy dsInfoProxy,
                                      IDownloadStationTaskProxySelector dsTaskProxySelector,
                                      ITorrentFileInfoReader torrentFileInfoReader,
                                      ISeedConfigProvider seedConfigProvider,
                                      IConfigService configService,
                                      IDiskProvider diskProvider,
                                      Logger logger)
            : base(torrentFileInfoReader, seedConfigProvider, configService, diskProvider, logger)
        {
            _dsInfoProxy = dsInfoProxy;
            _dsTaskProxySelector = dsTaskProxySelector;
            _fileStationProxy = fileStationProxy;
            _sharedFolderResolver = sharedFolderResolver;
            _serialNumberProvider = serialNumberProvider;
        }

        public override string Name => "Download Station";
        public override bool SupportsCategories => false;

        public override ProviderMessage Message => new ProviderMessage("Prowlarr is unable to connect to Download Station if 2-Factor Authentication is enabled on your DSM account", ProviderMessageType.Warning);

        private IDownloadStationTaskProxy DsTaskProxy => _dsTaskProxySelector.GetProxy(Settings);

        protected IEnumerable<DownloadStationTask> GetTasks()
        {
            return DsTaskProxy.GetTasks(Settings).Where(v => v.Type.ToLower() == DownloadStationTaskType.BT.ToString().ToLower());
        }

        protected override string AddFromMagnetLink(TorrentInfo release, string hash, string magnetLink)
        {
            var hashedSerialNumber = _serialNumberProvider.GetSerialNumber(Settings);

            DsTaskProxy.AddTaskFromUrl(magnetLink, GetDownloadDirectory(), Settings);

            var item = GetTasks().SingleOrDefault(t => t.Additional.Detail["uri"] == magnetLink);

            if (item != null)
            {
                _logger.Debug("{0} added correctly", release);
                return CreateDownloadId(item.Id, hashedSerialNumber);
            }

            _logger.Debug("No such task {0} in Download Station", magnetLink);

            throw new DownloadClientException("Failed to add magnet task to Download Station");
        }

        protected override string AddFromTorrentFile(TorrentInfo release, string hash, string filename, byte[] fileContent)
        {
            var hashedSerialNumber = _serialNumberProvider.GetSerialNumber(Settings);

            DsTaskProxy.AddTaskFromData(fileContent, filename, GetDownloadDirectory(), Settings);

            var items = GetTasks().Where(t => t.Additional.Detail["uri"] == Path.GetFileNameWithoutExtension(filename));

            var item = items.SingleOrDefault();

            if (item != null)
            {
                _logger.Debug("{0} added correctly", release);
                return CreateDownloadId(item.Id, hashedSerialNumber);
            }

            _logger.Debug("No such task {0} in Download Station", filename);

            throw new DownloadClientException("Failed to add torrent task to Download Station");
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
            if (failures.HasErrors())
            {
                return;
            }

            failures.AddIfNotNull(TestOutputPath());
        }

        protected bool IsFinished(DownloadStationTask torrent)
        {
            return torrent.Status == DownloadStationTaskStatus.Finished;
        }

        protected string GetMessage(DownloadStationTask torrent)
        {
            if (torrent.StatusExtra != null)
            {
                if (torrent.Status == DownloadStationTaskStatus.Extracting)
                {
                    return $"Extracting: {int.Parse(torrent.StatusExtra["unzip_progress"])}%";
                }

                if (torrent.Status == DownloadStationTaskStatus.Error)
                {
                    return torrent.StatusExtra["error_detail"];
                }
            }

            return null;
        }

        protected long GetRemainingSize(DownloadStationTask torrent)
        {
            var downloadedString = torrent.Additional.Transfer["size_downloaded"];

            if (downloadedString.IsNullOrWhiteSpace() || !long.TryParse(downloadedString, out var downloadedSize))
            {
                _logger.Debug("Torrent {0} has invalid size_downloaded: {1}", torrent.Title, downloadedString);
                downloadedSize = 0;
            }

            return torrent.Size - Math.Max(0, downloadedSize);
        }

        protected TimeSpan? GetRemainingTime(DownloadStationTask torrent)
        {
            var speedString = torrent.Additional.Transfer["speed_download"];

            if (speedString.IsNullOrWhiteSpace() || !long.TryParse(speedString, out var downloadSpeed))
            {
                _logger.Debug("Torrent {0} has invalid speed_download: {1}", torrent.Title, speedString);
                downloadSpeed = 0;
            }

            if (downloadSpeed <= 0)
            {
                return null;
            }

            var remainingSize = GetRemainingSize(torrent);

            return TimeSpan.FromSeconds(remainingSize / downloadSpeed);
        }

        protected double? GetSeedRatio(DownloadStationTask torrent)
        {
            var downloaded = torrent.Additional.Transfer["size_downloaded"].ParseInt64();
            var uploaded = torrent.Additional.Transfer["size_uploaded"].ParseInt64();

            if (downloaded.HasValue && uploaded.HasValue)
            {
                return downloaded <= 0 ? 0 : (double)uploaded.Value / downloaded.Value;
            }

            return null;
        }

        protected ValidationFailure TestOutputPath()
        {
            try
            {
                var downloadDir = GetDefaultDir();

                if (downloadDir == null)
                {
                    return new NzbDroneValidationFailure(nameof(Settings.TvDirectory), "No default destination")
                    {
                        DetailedDescription = $"You must login into your Diskstation as {Settings.Username} and manually set it up into DownloadStation settings under BT/HTTP/FTP/NZB -> Location."
                    };
                }

                downloadDir = GetDownloadDirectory();

                if (downloadDir != null)
                {
                    var sharedFolder = downloadDir.Split('\\', '/')[0];
                    var fieldName = Settings.TvDirectory.IsNotNullOrWhiteSpace() ? nameof(Settings.TvDirectory) : nameof(Settings.Category);

                    var folderInfo = _fileStationProxy.GetInfoFileOrDirectory($"/{downloadDir}", Settings);

                    if (folderInfo.Additional == null)
                    {
                        return new NzbDroneValidationFailure(fieldName, $"Shared folder does not exist")
                        {
                            DetailedDescription = $"The Diskstation does not have a Shared Folder with the name '{sharedFolder}', are you sure you specified it correctly?"
                        };
                    }

                    if (!folderInfo.IsDir)
                    {
                        return new NzbDroneValidationFailure(fieldName, $"Folder does not exist")
                        {
                            DetailedDescription = $"The folder '{downloadDir}' does not exist, it must be created manually inside the Shared Folder '{sharedFolder}'."
                        };
                    }
                }

                return null;
            }
            catch (DownloadClientAuthenticationException ex)
            {
                // User could not have permission to access to downloadstation
                _logger.Error(ex, ex.Message);
                return new NzbDroneValidationFailure(string.Empty, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error testing Torrent Download Station");
                return new NzbDroneValidationFailure(string.Empty, $"Unknown exception: {ex.Message}");
            }
        }

        protected ValidationFailure TestConnection()
        {
            try
            {
                return ValidateVersion();
            }
            catch (DownloadClientAuthenticationException ex)
            {
                _logger.Error(ex, ex.Message);
                return new NzbDroneValidationFailure("Username", "Authentication failure")
                {
                    DetailedDescription = $"Please verify your username and password. Also verify if the host running Prowlarr isn't blocked from accessing {Name} by WhiteList limitations in the {Name} configuration."
                };
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Unable to connect to Torrent Download Station");

                if (ex.Status == WebExceptionStatus.ConnectFailure)
                {
                    return new NzbDroneValidationFailure("Host", "Unable to connect")
                    {
                        DetailedDescription = "Please verify the hostname and port."
                    };
                }

                return new NzbDroneValidationFailure(string.Empty, $"Unknown exception: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error testing Torrent Download Station");

                return new NzbDroneValidationFailure("Host", "Unable to connect to Torrent Download Station")
                       {
                           DetailedDescription = ex.Message
                       };
            }
        }

        protected ValidationFailure ValidateVersion()
        {
            var info = DsTaskProxy.GetApiInfo(Settings);

            _logger.Debug("Download Station api version information: Min {0} - Max {1}", info.MinVersion, info.MaxVersion);

            if (info.MinVersion > 2 || info.MaxVersion < 2)
            {
                return new ValidationFailure(string.Empty, $"Download Station API version not supported, should be at least 2. It supports from {info.MinVersion} to {info.MaxVersion}");
            }

            return null;
        }

        protected string ParseDownloadId(string id)
        {
            return id.Split(':')[1];
        }

        protected string CreateDownloadId(string id, string hashedSerialNumber)
        {
            return $"{hashedSerialNumber}:{id}";
        }

        protected string GetDefaultDir()
        {
            var config = _dsInfoProxy.GetConfig(Settings);

            var path = config["default_destination"] as string;

            return path;
        }

        protected string GetDownloadDirectory()
        {
            if (Settings.TvDirectory.IsNotNullOrWhiteSpace())
            {
                return Settings.TvDirectory.TrimStart('/');
            }

            var destDir = GetDefaultDir();

            if (destDir.IsNotNullOrWhiteSpace() && Settings.Category.IsNotNullOrWhiteSpace())
            {
                return $"{destDir.TrimEnd('/')}/{Settings.Category}";
            }

            return destDir.TrimEnd('/');
        }

        protected override string AddFromTorrentLink(TorrentInfo release, string hash, string torrentLink)
        {
            throw new NotImplementedException();
        }
    }
}
