using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Download.Clients.NzbVortex
{
    public class NzbVortex : UsenetClientBase<NzbVortexSettings>
    {
        private readonly INzbVortexProxy _proxy;

        public NzbVortex(INzbVortexProxy proxy,
                       IHttpClient httpClient,
                       IConfigService configService,
                       IDiskProvider diskProvider,
                       Logger logger)
            : base(httpClient, configService, diskProvider, logger)
        {
            _proxy = proxy;
        }

        protected override string AddFromNzbFile(ReleaseInfo release, string filename, byte[] fileContents)
        {
            var priority = Settings.Priority;
            var category = GetCategoryForRelease(release) ?? Settings.Category;

            var response = _proxy.DownloadNzb(fileContents, filename, priority, Settings, category);

            if (response == null)
            {
                throw new DownloadClientException("Failed to add nzb {0}", filename);
            }

            return response;
        }

        public override string Name => "NZBVortex";
        public override bool SupportsCategories => true;

        protected List<NzbVortexGroup> GetGroups()
        {
            return _proxy.GetGroups(Settings);
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestConnection());
            failures.AddIfNotNull(TestApiVersion());
            failures.AddIfNotNull(TestAuthentication());
            failures.AddIfNotNull(TestCategory());
        }

        private ValidationFailure TestConnection()
        {
            try
            {
                _proxy.GetVersion(Settings);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to connect to NZBVortex");

                return new NzbDroneValidationFailure("Host", "Unable to connect to NZBVortex")
                       {
                           DetailedDescription = ex.Message
                       };
            }

            return null;
        }

        private ValidationFailure TestApiVersion()
        {
            try
            {
                var response = _proxy.GetApiVersion(Settings);
                var version = new Version(response.ApiLevel);

                if (version.Major < 2 || (version.Major == 2 && version.Minor < 3))
                {
                    return new ValidationFailure("Host", "NZBVortex needs to be updated");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
                return new ValidationFailure("Host", "Unable to connect to NZBVortex");
            }

            return null;
        }

        private ValidationFailure TestAuthentication()
        {
            try
            {
                _proxy.GetQueue(1, Settings);
            }
            catch (NzbVortexAuthenticationException)
            {
                return new ValidationFailure("ApiKey", "API Key Incorrect");
            }

            return null;
        }

        private ValidationFailure TestCategory()
        {
            var groups = GetGroups();

            foreach (var category in Categories)
            {
                if (!category.ClientCategory.IsNullOrWhiteSpace() && !groups.Any(v => v.GroupName == category.ClientCategory))
                {
                    return new NzbDroneValidationFailure(string.Empty, "Group does not exist")
                    {
                        DetailedDescription = "A mapped category you entered doesn't exist in NzbVortex. Go to NzbVortex to create it."
                    };
                }
            }

            if (!Settings.Category.IsNullOrWhiteSpace() && !groups.Any(v => v.GroupName == Settings.Category))
            {
                return new NzbDroneValidationFailure("Category", "Category does not exist")
                {
                    DetailedDescription = "The category you entered doesn't exist in NzbVortex. Go to NzbVortex to create it."
                };
            }

            return null;
        }

        protected override string AddFromLink(ReleaseInfo release)
        {
            throw new NotImplementedException();
        }
    }
}
