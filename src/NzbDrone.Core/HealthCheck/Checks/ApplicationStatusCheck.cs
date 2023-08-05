using System;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Applications;
using NzbDrone.Core.Localization;
using NzbDrone.Core.ThingiProvider.Events;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(ProviderUpdatedEvent<IApplication>))]
    [CheckOn(typeof(ProviderDeletedEvent<IApplication>))]
    [CheckOn(typeof(ProviderBulkUpdatedEvent<IApplication>))]
    [CheckOn(typeof(ProviderBulkDeletedEvent<IApplication>))]
    [CheckOn(typeof(ProviderStatusChangedEvent<IApplication>))]
    public class ApplicationStatusCheck : HealthCheckBase
    {
        private readonly IApplicationFactory _providerFactory;
        private readonly IApplicationStatusService _providerStatusService;

        public ApplicationStatusCheck(IApplicationFactory providerFactory, IApplicationStatusService providerStatusService, ILocalizationService localizationService)
            : base(localizationService)
        {
            _providerFactory = providerFactory;
            _providerStatusService = providerStatusService;
        }

        public override HealthCheck Check()
        {
            var enabledProviders = _providerFactory.GetAvailableProviders();
            var backOffProviders = enabledProviders.Join(_providerStatusService.GetBlockedProviders(),
                    i => i.Definition.Id,
                    s => s.ProviderId,
                    (i, s) => new { Provider = i, Status = s })
                .Where(p => p.Status.InitialFailure.HasValue &&
                            p.Status.InitialFailure.Value.After(DateTime.UtcNow.AddHours(-6)))
                .ToList();

            if (backOffProviders.Empty())
            {
                return new HealthCheck(GetType());
            }

            if (backOffProviders.Count == enabledProviders.Count)
            {
                return new HealthCheck(GetType(),
                    HealthCheckResult.Error,
                    _localizationService.GetLocalizedString("ApplicationStatusCheckAllClientMessage"),
                    "#applications-are-unavailable-due-to-failures");
            }

            return new HealthCheck(GetType(),
                HealthCheckResult.Warning,
                string.Format(_localizationService.GetLocalizedString("ApplicationStatusCheckSingleClientMessage"),
                    string.Join(", ", backOffProviders.Select(v => v.Provider.Definition.Name))),
                "#applications-are-unavailable-due-to-failures");
        }
    }
}
