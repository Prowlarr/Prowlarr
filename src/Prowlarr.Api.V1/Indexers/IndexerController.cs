using System.Collections.Generic;
using System.Net.Http;
using FluentValidation;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
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

            // Rejected at save time rather than on every request: an unparseable value otherwise saves
            // cleanly and then fails each fetch as "Unable to connect to indexer", which points at the
            // indexer instead of at the User-Agent that was just entered.
            SharedValidator.RuleFor(c => c.UserAgent)
                .Must(userAgent => userAgent.IsNullOrWhiteSpace() || IsValidUserAgent(userAgent))
                .WithMessage("Is not a valid User-Agent");
            SharedValidator.RuleFor(c => c.DownloadClientId).SetValidator(downloadClientExistsValidator);
        }

        private static bool IsValidUserAgent(string userAgent)
        {
            using var request = new HttpRequestMessage();

            return request.Headers.UserAgent.TryParseAdd(userAgent);
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
