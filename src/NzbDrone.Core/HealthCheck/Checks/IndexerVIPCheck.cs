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
    [CheckOn(typeof(ProviderBulkUpdatedEvent<IIndexer>))]
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
            var indexers = _indexerFactory.Enabled(false);
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

                if (expiration.IsNotNullOrWhiteSpace() &&
                    DateTime.Parse(expiration).Between(DateTime.Now, DateTime.Now.AddDays(7)))
                {
                    expiringProviders.Add(provider);
                }
            }

            if (!expiringProviders.Empty())
            {
                return new HealthCheck(GetType(),
                    HealthCheckResult.Warning,
                    HealthCheckReason.IndexerVIPExpiring,
                    _localizationService.GetLocalizedString("IndexerVipExpiringHealthCheckMessage", new Dictionary<string, object>
                    {
                        { "indexerNames", string.Join(", ", expiringProviders.Select(v => v.Definition.Name).ToArray()) }
                    }),
                    "#indexer-vip-expiring");
            }

            return new HealthCheck(GetType());
        }
    }
}
