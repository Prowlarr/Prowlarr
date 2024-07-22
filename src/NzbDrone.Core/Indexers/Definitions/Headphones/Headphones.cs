using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Headphones
{
    public class Headphones : UsenetIndexerBase<HeadphonesSettings>
    {
        public override string Name => "Headphones VIP";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override string[] IndexerUrls => new string[] { "https://indexer.codeshy.com" };
        public override string Description => "A Private Usenet indexer for music";
        public override bool SupportsPagination => true;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new HeadphonesRequestGenerator()
            {
                PageSize = PageSize,
                Settings = Settings,
                Capabilities = Capabilities
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new HeadphonesRssParser(Capabilities.Categories);
        }

        public Headphones(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, IValidateNzbs nzbValidationService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, nzbValidationService, logger)
        {
        }

        protected override async Task Test(List<ValidationFailure> failures)
        {
            await base.Test(failures);

            if (failures.Any())
            {
                return;
            }
        }

        public override async Task<IndexerDownloadResponse> Download(Uri link)
        {
            var requestBuilder = new HttpRequestBuilder(link.AbsoluteUri);

            var request = requestBuilder.Build();

            request.Credentials = new BasicNetworkCredential(Settings.Username, Settings.Password);

            byte[] downloadBytes;
            long elapsedTime;

            try
            {
                var response = await _httpClient.ExecuteProxiedAsync(request, Definition);
                downloadBytes = response.ResponseData;
                elapsedTime = response.ElapsedTime;
            }
            catch (Exception)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.Error("Download failed");
                throw;
            }

            ValidateDownloadData(downloadBytes);

            return new IndexerDownloadResponse(downloadBytes, elapsedTime);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q
                       },
            };

            caps.Categories.AddCategoryMapping(3000, NewznabStandardCategory.Audio);
            caps.Categories.AddCategoryMapping(3010, NewznabStandardCategory.AudioMP3);
            caps.Categories.AddCategoryMapping(3040, NewznabStandardCategory.AudioLossless);

            return caps;
        }
    }
}
