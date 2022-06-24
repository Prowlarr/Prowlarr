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
            var indexers = _indexerFactory.AllProviders(false);
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

                if (expiration.IsNullOrWhiteSpace())
                {
                    continue;
                }

                if (DateTime.Parse(expiration).Before(DateTime.Now))
                {
                    expiredProviders.Add(provider);
                }
            }

            if (!expiredProviders.Empty())
            {
                return new HealthCheck(GetType(),
                HealthCheckResult.Error,
                string.Format(_localizationService.GetLocalizedString("IndexerVipCheckExpiredClientMessage"),
                    string.Join(", ", expiredProviders.Select(v => v.Definition.Name))),
                "#indexer-vip-expired");
            }

            return new HealthCheck(GetType());
        }
    }
}
