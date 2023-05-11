using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download.Clients.FreeboxDownload.Responses;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.Clients.FreeboxDownload
{
    public class TorrentFreeboxDownload : TorrentClientBase<FreeboxDownloadSettings>
    {
        private readonly IFreeboxDownloadProxy _proxy;

        public TorrentFreeboxDownload(IFreeboxDownloadProxy proxy,
            ITorrentFileInfoReader torrentFileInfoReader,
            IHttpClient httpClient,
            IConfigService configService,
            IDiskProvider diskProvider,
            Logger logger)
            : base(torrentFileInfoReader, httpClient, configService, diskProvider, logger)
        {
            _proxy = proxy;
        }

        public override string Name => "Freebox Download";

        public override bool SupportsCategories => true;

        protected IEnumerable<FreeboxDownloadTask> GetTorrents()
        {
            return _proxy.GetTasks(Settings).Where(v => v.Type.ToLower() == FreeboxDownloadTaskType.Bt.ToString().ToLower());
        }

        protected override string AddFromMagnetLink(ReleaseInfo release, string hash, string magnetLink)
        {
            return _proxy.AddTaskFromUrl(magnetLink,
                                         GetDownloadDirectory(release).EncodeBase64(),
                                         ToBePaused(),
                                         ToBeQueuedFirst(),
                                         Settings);
        }

        protected override string AddFromTorrentFile(ReleaseInfo release, string hash, string filename, byte[] fileContent)
        {
            return _proxy.AddTaskFromFile(filename,
                                          fileContent,
                                          GetDownloadDirectory(release).EncodeBase64(),
                                          ToBePaused(),
                                          ToBeQueuedFirst(),
                                          Settings);
        }

        protected override string AddFromTorrentLink(ReleaseInfo release, string hash, string torrentLink)
        {
            return _proxy.AddTaskFromUrl(torrentLink,
                                         GetDownloadDirectory(release).EncodeBase64(),
                                         ToBePaused(),
                                         ToBeQueuedFirst(),
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

            if (Settings.Category.IsNotNullOrWhiteSpace())
            {
                var category = GetCategoryForRelease(release) ?? Settings.Category;

                destDir = $"{destDir}/{category}";
            }

            return destDir;
        }

        private bool ToBeQueuedFirst()
        {
            if (Settings.Priority == (int)FreeboxDownloadPriority.First)
            {
                return true;
            }

            return false;
        }

        private bool ToBePaused()
        {
            return Settings.AddPaused;
        }
    }
}
