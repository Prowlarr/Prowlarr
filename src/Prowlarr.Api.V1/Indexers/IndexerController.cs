using System.Collections.Generic;
using FluentValidation;
using FluentValidation.Results;
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

            SharedValidator.RuleFor(c => c.Priority).InclusiveBetween(1, 50);
            SharedValidator.RuleFor(c => c.DownloadClientId).SetValidator(downloadClientExistsValidator);
        }

        protected override void Validate(IndexerDefinition definition, bool includeWarnings)
        {
            var instance = _providerFactory.GetInstance(definition);

            // Ensure Redirect is true for Usenet protocols
            if (instance is { Protocol: DownloadProtocol.Usenet, SupportsRedirect: true } && definition is { Redirect: false })
            {
                throw new ValidationException(new List<ValidationFailure>
                {
                    new("Redirect", "Redirect must be enabled for Usenet indexers")
                });
            }

            base.Validate(definition, includeWarnings);
        }
    }
}
