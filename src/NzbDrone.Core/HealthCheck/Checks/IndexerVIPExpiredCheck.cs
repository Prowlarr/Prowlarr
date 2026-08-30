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
    public class IndexerVIPExpiredCheck : HealthCheckBase
    {
        private readonly IIndexerFactory _indexerFactory;

        public IndexerVIPExpiredCheck(IIndexerFactory indexerFactory, ILocalizationService localizationService)
            : base(localizationService)
        {
            _indexerFactory = indexerFactory;
        }

        public override HealthCheck Check()
        {
            var indexers = _indexerFactory.Enabled(false);
            var expiredProviders = new List<IIndexer>();

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
                    DateTime.Parse(expiration).Before(DateTime.Now))
                {
                    expiredProviders.Add(provider);
                }
            }

            if (!expiredProviders.Empty())
            {
                return new HealthCheck(GetType(),
                    HealthCheckResult.Error,
                    HealthCheckReason.IndexerVIPExpired,
                    _localizationService.GetLocalizedString("IndexerVipExpiredHealthCheckMessage", new Dictionary<string, object>
                    {
                        { "indexerNames", string.Join(", ", expiredProviders.Select(v => v.Definition.Name).ToArray()) }
                    }),
                    "#indexer-vip-expired");
            }

            return new HealthCheck(GetType());
        }
    }
}
