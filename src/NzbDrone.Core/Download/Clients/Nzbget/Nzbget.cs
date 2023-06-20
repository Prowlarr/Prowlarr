using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.Nzbget
{
    public class Nzbget : UsenetClientBase<NzbgetSettings>
    {
        private readonly INzbgetProxy _proxy;
        private readonly string[] _successStatus = { "SUCCESS", "NONE" };
        private readonly string[] _deleteFailedStatus = { "HEALTH", "DUPE", "SCAN", "COPY", "BAD" };

        public Nzbget(INzbgetProxy proxy,
                      IHttpClient httpClient,
                      IConfigService configService,
                      IDiskProvider diskProvider,
                      Logger logger)
            : base(httpClient, configService, diskProvider, logger)
        {
            _proxy = proxy;
        }

        protected override string AddFromNzbFile(ReleaseInfo release, string filename, byte[] fileContent)
        {
            var category = GetCategoryForRelease(release) ?? Settings.Category;

            var priority = Settings.Priority;

            var addpaused = Settings.AddPaused;
            var response = _proxy.DownloadNzb(fileContent, filename, category, priority, addpaused, Settings);

            if (response == null)
            {
                throw new DownloadClientRejectedReleaseException(release, "NZBGet rejected the NZB for an unknown reason");
            }

            return response;
        }

        protected override string AddFromLink(ReleaseInfo release)
        {
            var category = GetCategoryForRelease(release) ?? Settings.Category;

            var priority = Settings.Priority;

            var addpaused = Settings.AddPaused;
            var response = _proxy.DownloadNzbByLink(release.DownloadUrl, category, priority, addpaused, Settings);

            if (response == null)
            {
                throw new DownloadClientRejectedReleaseException(release, "NZBGet rejected the NZB for an unknown reason");
            }

            return response;
        }

        public override string Name => "NZBGet";
        public override bool SupportsCategories => true;

        protected IEnumerable<NzbgetCategory> GetCategories(Dictionary<string, string> config)
        {
            for (var i = 1; i < 100; i++)
            {
                var name = config.GetValueOrDefault("Category" + i + ".Name");

                if (name == null)
                {
                    yield break;
                }

                var destDir = config.GetValueOrDefault("Category" + i + ".DestDir");

                if (destDir.IsNullOrWhiteSpace())
                {
                    var mainDir = config.GetValueOrDefault("MainDir");
                    destDir = config.GetValueOrDefault("DestDir", string.Empty).Replace("${MainDir}", mainDir);

                    if (config.GetValueOrDefault("AppendCategoryDir", "yes") == "yes")
                    {
                        destDir = Path.Combine(destDir, name);
                    }
                }

                yield return new NzbgetCategory
                {
                    Name = name,
                    DestDir = destDir,
                    Unpack = config.GetValueOrDefault("Category" + i + ".Unpack") == "yes",
                    DefScript = config.GetValueOrDefault("Category" + i + ".DefScript"),
                    Aliases = config.GetValueOrDefault("Category" + i + ".Aliases"),
                };
            }
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
            failures.AddIfNotNull(TestCategory());
            failures.AddIfNotNull(TestSettings());
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                var version = _proxy.GetVersion(Settings).Split('-')[0];

                if (Version.Parse(version) < Version.Parse("12.0"))
                {
                    return new ValidationFailure(string.Empty, "NZBGet version too low, need 12.0 or higher");
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.ContainsIgnoreCase("Authentication failed"))
                {
                    return new ValidationFailure("Username", "Authentication failed");
                }

                _logger.Error(ex, "Unable to connect to NZBGet");
                return new ValidationFailure("Host", "Unable to connect to NZBGet");
            }

            return null;
        }

        private ValidationFailure TestCategory()
        {
            var config = _proxy.GetConfig(Settings);
            var categories = GetCategories(config);

            foreach (var category in Categories)
            {
                if (!category.ClientCategory.IsNullOrWhiteSpace() && !categories.Any(v => v.Name == category.ClientCategory))
                {
                    return new NzbDroneValidationFailure(string.Empty, "Category does not exist")
                    {
                        InfoLink = _proxy.GetBaseUrl(Settings),
                        DetailedDescription = "A mapped category you entered doesn't exist in NZBGet. Go to NZBGet to create it."
                    };
                }
            }

            if (!Settings.Category.IsNullOrWhiteSpace() && !categories.Any(v => v.Name == Settings.Category))
            {
                return new NzbDroneValidationFailure("Category", "Category does not exist")
                {
                    InfoLink = _proxy.GetBaseUrl(Settings),
                    DetailedDescription = "The category you entered doesn't exist in NZBGet. Go to NZBGet to create it."
                };
            }

            return null;
        }

        private ValidationFailure TestSettings()
        {
            var config = _proxy.GetConfig(Settings);

            var keepHistory = config.GetValueOrDefault("KeepHistory", "7");
            if (!int.TryParse(keepHistory, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value == 0)
            {
                return new NzbDroneValidationFailure(string.Empty, "NzbGet setting KeepHistory should be greater than 0")
                {
                    InfoLink = _proxy.GetBaseUrl(Settings),
                    DetailedDescription = "NzbGet setting KeepHistory is set to 0. Which prevents Prowlarr from seeing completed downloads."
                };
            }
            else if (value > 25000)
            {
                return new NzbDroneValidationFailure(string.Empty, "NzbGet setting KeepHistory should be less than 25000")
                {
                    InfoLink = _proxy.GetBaseUrl(Settings),
                    DetailedDescription = "NzbGet setting KeepHistory is set too high."
                };
            }

            return null;
        }
    }
}
