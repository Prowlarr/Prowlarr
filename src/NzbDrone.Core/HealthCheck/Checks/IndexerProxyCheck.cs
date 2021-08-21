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
    public class IndexerProxyCheck : HealthCheckBase
    {
        private readonly IIndexerProxyFactory _proxyFactory;

        public IndexerProxyCheck(IIndexerProxyFactory proxyFactory,
            ILocalizationService localizationService)
            : base(localizationService)
        {
            _proxyFactory = proxyFactory;
        }

        public override HealthCheck Check()
        {
            var enabledProviders = _proxyFactory.GetAvailableProviders();

            var badProxies = enabledProviders.Where(p => p.Test().IsValid == false).ToList();

            if (enabledProviders.Empty() || badProxies.Count == 0)
            {
                return new HealthCheck(GetType());
            }

            if (badProxies.Count == enabledProviders.Count)
            {
                return new HealthCheck(GetType(),
                    HealthCheckResult.Error,
                    _localizationService.GetLocalizedString("IndexerProxyStatusCheckAllClientMessage"),
                    "#proxies-are-unavailable-due-to-failures");
            }

            return new HealthCheck(GetType(),
                HealthCheckResult.Warning,
                string.Format(_localizationService.GetLocalizedString("IndexerProxyStatusCheckSingleClientMessage"),
                    string.Join(", ", badProxies.Select(v => v.Definition.Name))),
                "#proxies-are-unavailable-due-to-failures");
        }
    }
}
