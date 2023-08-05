using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Applications
{
    public interface IApplicationFactory : IProviderFactory<IApplication, ApplicationDefinition>
    {
        List<IApplication> SyncEnabled(bool filterBlockedIndexers = true);
    }

    public class ApplicationFactory : ProviderFactory<IApplication, ApplicationDefinition>, IApplicationFactory
    {
        private readonly IApplicationStatusService _applicationStatusService;
        private readonly Logger _logger;

        public ApplicationFactory(IApplicationStatusService applicationStatusService,
                                  IApplicationsRepository providerRepository,
                                  IEnumerable<IApplication> providers,
                                  IServiceProvider container,
                                  IEventAggregator eventAggregator,
                                  Logger logger)
            : base(providerRepository, providers, container, eventAggregator, logger)
        {
            _applicationStatusService = applicationStatusService;
            _logger = logger;
        }

        public List<IApplication> SyncEnabled(bool filterBlockedClients = true)
        {
            var enabledClients = GetAvailableProviders().Where(n => ((ApplicationDefinition)n.Definition).Enable);

            if (filterBlockedClients)
            {
                return FilterBlockedApplications(enabledClients).ToList();
            }

            return enabledClients.ToList();
        }

        private IEnumerable<IApplication> FilterBlockedApplications(IEnumerable<IApplication> applications)
        {
            var blockedApplications = _applicationStatusService.GetBlockedProviders().ToDictionary(v => v.ProviderId, v => v);

            foreach (var application in applications)
            {
                if (blockedApplications.TryGetValue(application.Definition.Id, out var blockedApplicationStatus))
                {
                    _logger.Debug("Temporarily ignoring application {0} till {1} due to recent failures.", application.Definition.Name, blockedApplicationStatus.DisabledTill.Value.ToLocalTime());
                    continue;
                }

                yield return application;
            }
        }

        public override ValidationResult Test(ApplicationDefinition definition)
        {
            var result = base.Test(definition);

            if (definition.Id == 0)
            {
                return result;
            }

            if (result == null || result.IsValid)
            {
                _applicationStatusService.RecordSuccess(definition.Id);
            }
            else
            {
                _applicationStatusService.RecordFailure(definition.Id);
            }

            return result;
        }
    }
}
