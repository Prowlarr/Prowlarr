using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.Sabnzbd
{
    public class Sabnzbd : UsenetClientBase<SabnzbdSettings>
    {
        private readonly ISabnzbdProxy _proxy;

        public Sabnzbd(ISabnzbdProxy proxy,
                       IHttpClient httpClient,
                       IConfigService configService,
                       IDiskProvider diskProvider,
                       Logger logger)
            : base(httpClient, configService, diskProvider, logger)
        {
            _proxy = proxy;
        }

        // patch can be a number (releases) or 'x' (git)
        private static readonly Regex VersionRegex = new Regex(@"(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+|x)", RegexOptions.Compiled);

        protected override string AddFromNzbFile(ReleaseInfo release, string filename, byte[] fileContent)
        {
            var category = GetCategoryForRelease(release) ?? Settings.Category;
            var priority = Settings.Priority;

            var response = _proxy.DownloadNzb(fileContent, filename, category, priority, Settings);

            if (response == null || response.Ids.Empty())
            {
                throw new DownloadClientRejectedReleaseException(release, "SABnzbd rejected the NZB for an unknown reason");
            }

            return response.Ids.First();
        }

        protected override string AddFromLink(ReleaseInfo release)
        {
            var category = GetCategoryForRelease(release) ?? Settings.Category;
            var priority = Settings.Priority;

            var response = _proxy.DownloadNzbByUrl(release.DownloadUrl, category, priority, Settings);

            if (response == null || response.Ids.Empty())
            {
                throw new DownloadClientRejectedReleaseException(release, "SABnzbd rejected the NZB for an unknown reason");
            }

            return response.Ids.First();
        }

        public override string Name => "SABnzbd";
        public override bool SupportsCategories => true;

        protected IEnumerable<SabnzbdCategory> GetCategories(SabnzbdConfig config)
        {
            var completeDir = new OsPath(config.Misc.complete_dir);

            if (!completeDir.IsRooted)
            {
                if (HasVersion(2, 0))
                {
                    var status = _proxy.GetFullStatus(Settings);
                    completeDir = new OsPath(status.CompleteDir);
                }
                else
                {
                    var queue = _proxy.GetQueue(0, 1, Settings);
                    var defaultRootFolder = new OsPath(queue.DefaultRootFolder);

                    completeDir = defaultRootFolder + completeDir;
                }
            }

            foreach (var category in config.Categories)
            {
                var relativeDir = new OsPath(category.Dir.TrimEnd('*'));

                category.FullPath = completeDir + relativeDir;

                yield return category;
            }
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnectionAndVersion());
            failures.AddIfNotNull(TestAuthentication());
            failures.AddIfNotNull(TestGlobalConfig());
            failures.AddIfNotNull(TestCategory());
        }

        private bool HasVersion(int major, int minor, int patch = 0)
        {
            var rawVersion = _proxy.GetVersion(Settings);
            var version = ParseVersion(rawVersion);

            if (version == null)
            {
                return false;
            }

            if (version.Major > major)
            {
                return true;
            }
            else if (version.Major < major)
            {
                return false;
            }

            if (version.Minor > minor)
            {
                return true;
            }
            else if (version.Minor < minor)
            {
                return false;
            }

            if (version.Build > patch)
            {
                return true;
            }
            else if (version.Build < patch)
            {
                return false;
            }

            return true;
        }

        private Version ParseVersion(string version)
        {
            if (version.IsNullOrWhiteSpace())
            {
                return null;
            }

            var parsed = VersionRegex.Match(version);

            int major;
            int minor;
            int patch;

            if (parsed.Success)
            {
                major = Convert.ToInt32(parsed.Groups["major"].Value);
                minor = Convert.ToInt32(parsed.Groups["minor"].Value);
                patch = Convert.ToInt32(parsed.Groups["patch"].Value.Replace("x", "0"));
            }
            else
            {
                if (!version.Equals("develop", StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                major = 1;
                minor = 1;
                patch = 0;
            }

            return new Version(major, minor, patch);
        }

        private ValidationFailure TestConnectionAndVersion()
        {
            try
            {
                var rawVersion = _proxy.GetVersion(Settings);
                var version = ParseVersion(rawVersion);

                if (version == null)
                {
                    return new ValidationFailure("Version", "Unknown Version: " + rawVersion);
                }

                if (rawVersion.Equals("develop", StringComparison.InvariantCultureIgnoreCase))
                {
                    return new NzbDroneValidationFailure("Version", "SABnzbd develop version, assuming version 1.1.0 or higher.")
                    {
                        IsWarning = true,
                        DetailedDescription = "Prowlarr may not be able to support new features added to SABnzbd when running develop versions."
                    };
                }

                if (version.Major >= 1)
                {
                    return null;
                }

                if (version.Minor >= 7)
                {
                    return null;
                }

                return new ValidationFailure("Version", "Version 0.7.0+ is required, but found: " + version);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return new NzbDroneValidationFailure("Host", "Unable to connect to SABnzbd")
                       {
                           DetailedDescription = ex.Message
                       };
            }
        }

        private ValidationFailure TestAuthentication()
        {
            try
            {
                _proxy.GetConfig(Settings);
            }
            catch (Exception ex)
            {
                if (ex.Message.ContainsIgnoreCase("API Key Incorrect"))
                {
                    return new ValidationFailure("APIKey", "API Key Incorrect");
                }

                if (ex.Message.ContainsIgnoreCase("API Key Required"))
                {
                    return new ValidationFailure("APIKey", "API Key Required");
                }

                throw;
            }

            return null;
        }

        private ValidationFailure TestGlobalConfig()
        {
            var config = _proxy.GetConfig(Settings);
            if (config.Misc.pre_check && !HasVersion(1, 1))
            {
                return new NzbDroneValidationFailure("", "Disable 'Check before download' option in SABnzbd")
                {
                    InfoLink = _proxy.GetBaseUrl(Settings, "config/switches/"),
                    DetailedDescription = "Using Check before download affects Prowlarr ability to track new downloads. Also SABnzbd recommends 'Abort jobs that cannot be completed' instead since it's more effective."
                };
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
                        DetailedDescription = "A mapped category you entered doesn't exist in Sabnzbd. Go to Sabnzbd to create it."
                    };
                }
            }

            if (!Settings.Category.IsNullOrWhiteSpace() && !categories.Any(v => v.Name == Settings.Category))
            {
                return new NzbDroneValidationFailure("Category", "Category does not exist")
                {
                    InfoLink = _proxy.GetBaseUrl(Settings),
                    DetailedDescription = "The category you entered doesn't exist in Sabnzbd. Go to Sabnzbd to create it."
                };
            }

            if (config.Misc.enable_tv_sorting && ContainsCategory(config.Misc.tv_categories, Settings.Category))
            {
                return new NzbDroneValidationFailure("Category", "Disable TV Sorting")
                {
                    InfoLink = _proxy.GetBaseUrl(Settings, "config/sorting/"),
                    DetailedDescription = "You must disable SABnzbd TV Sorting for the category Prowlarr uses to prevent import issues. Go to SABnzbd to fix it."
                };
            }

            if (config.Misc.enable_movie_sorting && ContainsCategory(config.Misc.movie_categories, Settings.Category))
            {
                return new NzbDroneValidationFailure("Category", "Disable Movie Sorting")
                {
                    InfoLink = _proxy.GetBaseUrl(Settings, "config/sorting/"),
                    DetailedDescription = "You must disable SABnzbd Movie Sorting for the category Prowlarr uses to prevent import issues. Go to SABnzbd to fix it."
                };
            }

            if (config.Misc.enable_date_sorting && ContainsCategory(config.Misc.date_categories, Settings.Category))
            {
                return new NzbDroneValidationFailure("Category", "Disable Date Sorting")
                {
                    InfoLink = _proxy.GetBaseUrl(Settings, "config/sorting/"),
                    DetailedDescription = "You must disable SABnzbd Date Sorting for the category Prowlarr uses to prevent import issues. Go to SABnzbd to fix it."
                };
            }

            return null;
        }

        private bool ContainsCategory(IEnumerable<string> categories, string category)
        {
            if (categories == null || categories.Empty())
            {
                return true;
            }

            if (category.IsNullOrWhiteSpace())
            {
                category = "Default";
            }

            return categories.Contains(category);
        }
    }
}
