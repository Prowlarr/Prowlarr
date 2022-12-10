using System;
using System.Collections.Generic;
using System.IO;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.Clients.Blackhole
{
    public class UsenetBlackhole : UsenetClientBase<UsenetBlackholeSettings>
    {
        public UsenetBlackhole(IHttpClient httpClient,
                               IConfigService configService,
                               IDiskProvider diskProvider,
                               Logger logger)
            : base(httpClient, configService, diskProvider, logger)
        {
        }

        protected override string AddFromLink(ReleaseInfo release)
        {
            throw new NotSupportedException("Blackhole does not support redirected indexers.");
        }

        protected override string AddFromNzbFile(ReleaseInfo release, string filename, byte[] fileContent)
        {
            var title = release.Title;

            title = title.CleanFileName();

            var filepath = Path.Combine(Settings.NzbFolder, title + ".nzb");

            using (var stream = _diskProvider.OpenWriteStream(filepath))
            {
                stream.Write(fileContent, 0, fileContent.Length);
            }

            _logger.Debug("NZB Download succeeded, saved to: {0}", filepath);

            return null;
        }

        public override string Name => "Usenet Blackhole";
        public override bool SupportsCategories => false;

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(TestFolder(Settings.NzbFolder, "NzbFolder"));
        }
    }
}
