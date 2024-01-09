using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Localization;
using NzbDrone.Core.ThingiProvider.Events;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(ProviderAddedEvent<IIndexer>))]
    [CheckOn(typeof(ProviderUpdatedEvent<IIndexer>))]
    [CheckOn(typeof(ProviderDeletedEvent<IIndexer>))]
    [CheckOn(typeof(ProviderBulkDeletedEvent<IIndexer>))]
    public class IndexerVIPCheck : HealthCheckBase
    {
        private readonly IIndexerFactory _indexerFactory;

        public IndexerVIPCheck(IIndexerFactory indexerFactory, ILocalizationService localizationService)
            : base(localizationService)
        {
            _indexerFactory = indexerFactory;
        }

        public override HealthCheck Check()
        {
            var indexers = _indexerFactory.AllProviders(false);
            var expiringProviders = new List<IIndexer>();

            foreach (var provider in indexers)
            {
                var settingsType = provider.Definition.Settings.GetType();
                var vipProp = settingsType.GetProperty("VipExpiration");

                if (vipProp == null)
                {
                    continue;
                }

                var expiration = (string)vipProp.GetValue(provider.Definition.Settings);

                if (expiration.IsNullOrWhiteSpace())
                {
                    continue;
                }

                if (DateTime.Parse(expiration).Between(DateTime.Now, DateTime.Now.AddDays(7)))
                {
                    expiringProviders.Add(provider);
                }
            }

            if (!expiringProviders.Empty())
            {
                return new HealthCheck(GetType(),
                HealthCheckResult.Warning,
                string.Format(_localizationService.GetLocalizedString("IndexerVipCheckExpiringClientMessage"),
                    string.Join(", ", expiringProviders.Select(v => v.Definition.Name))),
                "#indexer-vip-expiring")
                {
                    IndexerIds = expiringProviders.Select(p => p.Definition.Id).ToList()
                };
            }

            return new HealthCheck(GetType());
        }
    }
}
