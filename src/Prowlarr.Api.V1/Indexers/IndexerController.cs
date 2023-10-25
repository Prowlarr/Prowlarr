using NzbDrone.Core.Indexers;
using NzbDrone.Core.Validation;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Indexers
{
    [V1ApiController]
    public class IndexerController : ProviderControllerBase<IndexerResource, IndexerBulkResource, IIndexer, IndexerDefinition>
    {
        public IndexerController(IndexerFactory indexerFactory,
            IndexerResourceMapper resourceMapper,
            IndexerBulkResourceMapper bulkResourceMapper,
            AppProfileExistsValidator appProfileExistsValidator,
            DownloadClientExistsValidator downloadClientExistsValidator)
            : base(indexerFactory, "indexer", resourceMapper, bulkResourceMapper)
        {
            Http.Validation.RuleBuilderExtensions.ValidId(SharedValidator.RuleFor(s => s.AppProfileId));
            Http.Validation.RuleBuilderExtensions.ValidId(SharedValidator.RuleFor(s => s.DownloadClientId));

            SharedValidator.RuleFor(c => c.AppProfileId).SetValidator(appProfileExistsValidator);
            SharedValidator.RuleFor(c => c.DownloadClientId).SetValidator(downloadClientExistsValidator);
        }
    }
}
