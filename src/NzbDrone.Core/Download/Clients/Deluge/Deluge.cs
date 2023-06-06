using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.Deluge
{
    public class Deluge : TorrentClientBase<DelugeSettings>
    {
        private readonly IDelugeProxy _proxy;

        public Deluge(IDelugeProxy proxy,
                      ITorrentFileInfoReader torrentFileInfoReader,
                      ISeedConfigProvider seedConfigProvider,
                      IConfigService configService,
                      IDiskProvider diskProvider,
                      Logger logger)
            : base(torrentFileInfoReader, seedConfigProvider, configService, diskProvider, logger)
        {
            _proxy = proxy;
        }

        protected override string AddFromMagnetLink(TorrentInfo release, string hash, string magnetLink)
        {
            var actualHash = _proxy.AddTorrentFromMagnet(magnetLink, Settings);

            if (actualHash.IsNullOrWhiteSpace())
            {
                throw new DownloadClientException("Deluge failed to add magnet " + magnetLink);
            }

            _proxy.SetTorrentSeedingConfiguration(actualHash, release.SeedConfiguration, Settings);

            var category = GetCategoryForRelease(release) ?? Settings.Category;

            if (category.IsNotNullOrWhiteSpace())
            {
                _proxy.SetTorrentLabel(actualHash, category, Settings);
            }

            if (Settings.Priority == (int)DelugePriority.First)
            {
                _proxy.MoveTorrentToTopInQueue(actualHash, Settings);
            }

            return actualHash.ToUpper();
        }

        protected override string AddFromTorrentFile(TorrentInfo release, string hash, string filename, byte[] fileContent)
        {
            var actualHash = _proxy.AddTorrentFromFile(filename, fileContent, Settings);

            if (actualHash.IsNullOrWhiteSpace())
            {
                throw new DownloadClientException("Deluge failed to add torrent " + filename);
            }

            _proxy.SetTorrentSeedingConfiguration(actualHash, release.SeedConfiguration, Settings);

            var category = GetCategoryForRelease(release) ?? Settings.Category;

            if (category.IsNotNullOrWhiteSpace())
            {
                _proxy.SetTorrentLabel(actualHash, category, Settings);
            }

            if (Settings.Priority == (int)DelugePriority.First)
            {
                _proxy.MoveTorrentToTopInQueue(actualHash, Settings);
            }

            return actualHash.ToUpper();
        }

        public override string Name => "Deluge";
        public override bool SupportsCategories => true;

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
            if (failures.HasErrors())
            {
                return;
            }

            failures.AddIfNotNull(TestCategory());
            failures.AddIfNotNull(TestGetTorrents());
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                _proxy.GetVersion(Settings);
            }
            catch (DownloadClientAuthenticationException ex)
            {
                _logger.Error(ex, ex.Message);

                return new NzbDroneValidationFailure("Password", "Authentication failed");
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Unable to test connection");
                switch (ex.Status)
                {
                    case WebExceptionStatus.ConnectFailure:
                        return new NzbDroneValidationFailure("Host", "Unable to connect")
                        {
                            DetailedDescription = "Please verify the hostname and port."
                        };
                    case WebExceptionStatus.ConnectionClosed:
                        return new NzbDroneValidationFailure("UseSsl", "Verify SSL settings")
                        {
                            DetailedDescription = "Please verify your SSL configuration on both Deluge and Prowlarr."
                        };
                    case WebExceptionStatus.SecureChannelFailure:
                        return new NzbDroneValidationFailure("UseSsl", "Unable to connect through SSL")
                        {
                            DetailedDescription = "Prowlarr is unable to connect to Deluge using SSL. This problem could be computer related. Please try to configure both Prowlarr and Deluge to not use SSL."
                        };
                    default:
                        return new NzbDroneValidationFailure(string.Empty, "Unknown exception: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to test connection");

                return new NzbDroneValidationFailure("Host", "Unable to connect to Deluge")
                       {
                           DetailedDescription = ex.Message
                       };
            }

            return null;
        }

        private ValidationFailure TestCategory()
        {
            if (Categories.Count == 0)
            {
                return null;
            }

            var enabledPlugins = _proxy.GetEnabledPlugins(Settings);

            if (!enabledPlugins.Contains("Label"))
            {
                return new NzbDroneValidationFailure("Category", "Label plugin not activated")
                {
                    DetailedDescription = "You must have the Label plugin enabled in Deluge to use categories."
                };
            }

            var labels = _proxy.GetAvailableLabels(Settings);

            var categories = Categories.Select(c => c.ClientCategory).ToList();
            categories.Add(Settings.Category);

            foreach (var category in categories)
            {
                if (category.IsNotNullOrWhiteSpace() && !labels.Contains(category))
                {
                    _proxy.AddLabel(category, Settings);
                    labels = _proxy.GetAvailableLabels(Settings);

                    if (!labels.Contains(category))
                    {
                        return new NzbDroneValidationFailure("Category", "Configuration of label failed")
                        {
                            DetailedDescription = "Prowlarr was unable to add the label to Deluge."
                        };
                    }
                }
            }

            return null;
        }

        protected override void ValidateCategories(List<ValidationFailure> failures)
        {
            base.ValidateCategories(failures);

            foreach (var label in Categories)
            {
                if (!Regex.IsMatch(label.ClientCategory, "^[-a-z0-9]*$"))
                {
                    failures.AddIfNotNull(new ValidationFailure(string.Empty, "Mapped Categories allowed characters a-z, 0-9 and -"));
                }
            }
        }

        private ValidationFailure TestGetTorrents()
        {
            try
            {
                _proxy.GetTorrents(Settings);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to get torrents");
                return new NzbDroneValidationFailure(string.Empty, "Failed to get the list of torrents: " + ex.Message);
            }

            return null;
        }

        protected override string AddFromTorrentLink(TorrentInfo release, string hash, string torrentLink)
        {
            throw new NotImplementedException();
        }
    }
}
