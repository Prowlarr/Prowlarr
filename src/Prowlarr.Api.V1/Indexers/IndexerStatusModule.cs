using System.Collections.Generic;
using NzbDrone.Core.Indexers;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Indexers
{
    public class IndexerStatusModule : ProwlarrRestModule<IndexerStatusResource>
    {
        private readonly IIndexerStatusService _indexerStatusService;

        public IndexerStatusModule(IIndexerStatusService indexerStatusService)
        {
            _indexerStatusService = indexerStatusService;

            GetResourceAll = GetAll;
        }

        private List<IndexerStatusResource> GetAll()
        {
            return _indexerStatusService.GetBlockedProviders().ToResource();
        }
    }
}
