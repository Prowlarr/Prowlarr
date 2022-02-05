using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Test.IndexerTests
{
    public class TestIndexer : UsenetIndexerBase<TestIndexerSettings>
    {
        public override string Name => "Test Indexer";
        public override string[] IndexerUrls => new string[] { "http://testindexer.com" };
        public override string Description => "";

        public override DownloadProtocol Protocol => DownloadProtocol.Usenet;

        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public int _supportedPageSize;
        public override int PageSize => _supportedPageSize;

        public TestIndexer(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, IValidateNzbs nzbValidationService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, nzbValidationService, logger)
        {
        }

        public IIndexerRequestGenerator _requestGenerator;
        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return _requestGenerator;
        }

        public IParseIndexerResponse _parser;
        public override IParseIndexerResponse GetParser()
        {
            return _parser;
        }
    }
}
