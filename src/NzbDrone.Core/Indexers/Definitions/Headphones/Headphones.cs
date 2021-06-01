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

        public override DownloadProtocol Protocol => DownloadProtocol.Usenet;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override string BaseUrl => "https://indexer.codeshy.com";
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new HeadphonesRequestGenerator()
            {
                PageSize = PageSize,
                Settings = Settings,
                Capabilities = Capabilities,
                BaseUrl = BaseUrl
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new HeadphonesRssParser(Capabilities.Categories);
        }

        public Headphones(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, IValidateNzbs nzbValidationService, Logger logger)
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

        public override async Task<byte[]> Download(Uri link)
        {
            var requestBuilder = new HttpRequestBuilder(link.AbsoluteUri);

            var downloadBytes = Array.Empty<byte>();

            var request = requestBuilder.Build();

            request.AddBasicAuthentication(Settings.Username, Settings.Password);

            try
            {
                var response = await _httpClient.ExecuteAsync(request);
                downloadBytes = response.ResponseData;
            }
            catch (Exception)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.Error("Download failed");
            }

            return downloadBytes;
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
