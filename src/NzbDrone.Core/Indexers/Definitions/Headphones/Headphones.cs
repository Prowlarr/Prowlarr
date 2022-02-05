using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Headphones
{
    public class Headphones : UsenetIndexerBase<HeadphonesSettings>
    {
        public override string Name => "Headphones VIP";

        public override DownloadProtocol Protocol => DownloadProtocol.Usenet;

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

        public Headphones(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, IValidateNzbs nzbValidationService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, nzbValidationService, logger)
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

            request.Credentials = new BasicNetworkCredential(Settings.Username, Settings.Password);

            try
            {
                var response = await _httpClient.ExecuteProxiedAsync(request, Definition);
                downloadBytes = response.ResponseData;
            }
            catch (Exception)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.Error("Download failed");
            }

            return downloadBytes;
        }
    }
}
