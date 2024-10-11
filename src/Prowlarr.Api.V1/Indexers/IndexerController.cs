using FluentValidation;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Validation;
using NzbDrone.SignalR;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Indexers
{
    [V1ApiController]
    public class IndexerController : ProviderControllerBase<IndexerResource, IndexerBulkResource, IIndexer, IndexerDefinition>
    {
        public IndexerController(IBroadcastSignalRMessage signalRBroadcaster,
            IndexerFactory indexerFactory,
            IndexerResourceMapper resourceMapper,
            IndexerBulkResourceMapper bulkResourceMapper,
            AppProfileExistsValidator appProfileExistsValidator,
            DownloadClientExistsValidator downloadClientExistsValidator)
            : base(signalRBroadcaster, indexerFactory, "indexer", resourceMapper, bulkResourceMapper)
        {
            SharedValidator.RuleFor(c => c.AppProfileId).Cascade(CascadeMode.Stop)
                .ValidId()
                .SetValidator(appProfileExistsValidator);

            SharedValidator.RuleFor(c => c.DownloadClientId).SetValidator(downloadClientExistsValidator);
        }
    }
}
