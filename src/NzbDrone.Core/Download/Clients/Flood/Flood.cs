using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download.Clients.Flood.Models;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Download.Clients.Flood
{
    public class Flood : TorrentClientBase<FloodSettings>
    {
        private readonly IFloodProxy _proxy;

        public Flood(IFloodProxy proxy,
                        ITorrentFileInfoReader torrentFileInfoReader,
                        ISeedConfigProvider seedConfigProvider,
                        IConfigService configService,
                        IDiskProvider diskProvider,
                        Logger logger)
            : base(torrentFileInfoReader, seedConfigProvider, configService, diskProvider, logger)
        {
            _proxy = proxy;
        }

        private static IEnumerable<string> HandleTags(ReleaseInfo release, FloodSettings settings, string mappedCategory)
        {
            var result = new HashSet<string>();

            if (settings.Tags.Any())
            {
                result.UnionWith(settings.Tags);
            }

            if (mappedCategory != null)
            {
                result.Add(mappedCategory);
            }

            if (settings.AdditionalTags.Any())
            {
                foreach (var additionalTag in settings.AdditionalTags)
                {
                    switch (additionalTag)
                    {
                        case (int)AdditionalTags.Indexer:
                            result.Add(release.Indexer);
                            break;
                        default:
                            throw new DownloadClientException("Unexpected additional tag ID");
                    }
                }
            }

            return result.Where(t => t.IsNotNullOrWhiteSpace());
        }

        public override string Name => "Flood";
        public override bool SupportsCategories => true;
        public override ProviderMessage Message => new ProviderMessage("Prowlarr is unable to remove torrents that have finished seeding when using Flood", ProviderMessageType.Warning);

        protected override string AddFromTorrentFile(TorrentInfo release, string hash, string filename, byte[] fileContent)
        {
            _proxy.AddTorrentByFile(Convert.ToBase64String(fileContent), HandleTags(release, Settings, GetCategoryForRelease(release)), Settings);

            return hash;
        }

        protected override string AddFromMagnetLink(TorrentInfo release, string hash, string magnetLink)
        {
            _proxy.AddTorrentByUrl(magnetLink, HandleTags(release, Settings, GetCategoryForRelease(release)), Settings);

            return hash;
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            try
            {
                _proxy.AuthVerify(Settings);
            }
            catch (DownloadClientAuthenticationException ex)
            {
                failures.Add(new ValidationFailure("Password", ex.Message));
            }
            catch (Exception ex)
            {
                failures.Add(new ValidationFailure("Host", ex.Message));
            }
        }

        protected override string AddFromTorrentLink(TorrentInfo release, string hash, string torrentLink)
        {
            throw new NotImplementedException();
        }
    }
}
