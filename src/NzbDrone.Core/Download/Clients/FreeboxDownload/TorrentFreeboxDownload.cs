using System.Collections.Generic;
using System.Text.RegularExpressions;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.Clients.FreeboxDownload
{
    public class TorrentFreeboxDownload : TorrentClientBase<FreeboxDownloadSettings>
    {
        private readonly IFreeboxDownloadProxy _proxy;

        public TorrentFreeboxDownload(IFreeboxDownloadProxy proxy,
            ITorrentFileInfoReader torrentFileInfoReader,
            ISeedConfigProvider seedConfigProvider,
            IConfigService configService,
            IDiskProvider diskProvider,
            Logger logger)
            : base(torrentFileInfoReader, seedConfigProvider, configService, diskProvider, logger)
        {
            _proxy = proxy;
        }

        public override string Name => "Freebox Download";
        public override bool SupportsCategories => true;

        protected override string AddFromMagnetLink(TorrentInfo release, string hash, string magnetLink)
        {
            return _proxy.AddTaskFromUrl(magnetLink,
                                         GetDownloadDirectory(release).EncodeBase64(),
                                         ToBePaused(),
                                         ToBeQueuedFirst(),
                                         GetSeedRatio(release),
                                         Settings);
        }

        protected override string AddFromTorrentFile(TorrentInfo release, string hash, string filename, byte[] fileContent)
        {
            return _proxy.AddTaskFromFile(filename,
                                          fileContent,
                                          GetDownloadDirectory(release).EncodeBase64(),
                                          ToBePaused(),
                                          ToBeQueuedFirst(),
                                          GetSeedRatio(release),
                                          Settings);
        }

        protected override string AddFromTorrentLink(TorrentInfo release, string hash, string torrentLink)
        {
            return _proxy.AddTaskFromUrl(torrentLink,
                                         GetDownloadDirectory(release).EncodeBase64(),
                                         ToBePaused(),
                                         ToBeQueuedFirst(),
                                         GetSeedRatio(release),
                                         Settings);
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            try
            {
                _proxy.Authenticate(Settings);
            }
            catch (DownloadClientUnavailableException ex)
            {
                failures.Add(new ValidationFailure("Host", ex.Message));
                failures.Add(new ValidationFailure("Port", ex.Message));
            }
            catch (DownloadClientAuthenticationException ex)
            {
                failures.Add(new ValidationFailure("AppId", ex.Message));
                failures.Add(new ValidationFailure("AppToken", ex.Message));
            }
            catch (FreeboxDownloadException ex)
            {
                failures.Add(new ValidationFailure("ApiUrl", ex.Message));
            }
        }

        protected override void ValidateCategories(List<ValidationFailure> failures)
        {
            base.ValidateCategories(failures);

            foreach (var label in Categories)
            {
                if (!Regex.IsMatch(label.ClientCategory, "^\\.?[-a-z]*$"))
                {
                    failures.AddIfNotNull(new ValidationFailure(string.Empty, "Mapped Categories allowed characters a-z and -"));
                }
            }
        }

        private string GetDownloadDirectory(ReleaseInfo release)
        {
            if (Settings.DestinationDirectory.IsNotNullOrWhiteSpace())
            {
                return Settings.DestinationDirectory.TrimEnd('/');
            }

            var destDir = _proxy.GetDownloadConfiguration(Settings).DecodedDownloadDirectory.TrimEnd('/');

            var category = GetCategoryForRelease(release) ?? Settings.Category;

            if (category.IsNotNullOrWhiteSpace())
            {
                destDir = $"{destDir}/{category}";
            }

            return destDir;
        }

        private bool ToBePaused()
        {
            return Settings.AddPaused;
        }

        private bool ToBeQueuedFirst()
        {
            if (Settings.Priority == (int)FreeboxDownloadPriority.First)
            {
                return true;
            }

            return false;
        }

        private double? GetSeedRatio(TorrentInfo release)
        {
            if (release.SeedConfiguration == null || release.SeedConfiguration.Ratio == null)
            {
                return null;
            }

            return release.SeedConfiguration.Ratio.Value * 100;
        }
    }
}
