using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.IndexerProxies;
using NzbDrone.Core.Localization;
using NzbDrone.Core.ThingiProvider.Events;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(ProviderDeletedEvent<IIndexerProxy>))]
    [CheckOn(typeof(ProviderAddedEvent<IIndexerProxy>))]
    [CheckOn(typeof(ProviderUpdatedEvent<IIndexerProxy>))]
    public class IndexerProxyStatusCheck : HealthCheckBase
    {
        private readonly IIndexerProxyFactory _proxyFactory;

        public IndexerProxyStatusCheck(IIndexerProxyFactory proxyFactory,
            ILocalizationService localizationService)
            : base(localizationService)
        {
            _proxyFactory = proxyFactory;
        }

        public override HealthCheck Check()
        {
            var enabledProxies = _proxyFactory.GetAvailableProviders()
                .Where(n => ((IndexerProxyDefinition)n.Definition).Enable)
                .ToList();

            var badProxies = enabledProxies.Where(p => p.Test().IsValid == false).ToList();

            if (enabledProxies.Empty() || badProxies.Count == 0)
            {
                return new HealthCheck(GetType());
            }

            if (badProxies.Count == enabledProxies.Count)
            {
                return new HealthCheck(GetType(),
                    HealthCheckResult.Error,
                    _localizationService.GetLocalizedString("IndexerProxyStatusAllUnavailableHealthCheckMessage"),
                    "#indexer-proxies-are-unavailable-due-to-failures");
            }

            return new HealthCheck(GetType(),
                HealthCheckResult.Warning,
                _localizationService.GetLocalizedString("IndexerProxyStatusUnavailableHealthCheckMessage", new Dictionary<string, object>
                {
                    { "indexerProxyNames", string.Join(", ", badProxies.Select(v => v.Definition.Name)) }
                }),
                "#indexer-proxies-are-unavailable-due-to-failures");
        }
    }
}
