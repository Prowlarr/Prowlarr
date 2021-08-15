using System;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
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
        private readonly Logger _logger;
        private readonly IIndexerProxyFactory _proxyFactory;
        private readonly IHttpClient _client;

        private readonly IHttpRequestBuilderFactory _cloudRequestBuilder;

        public IndexerProxyCheck(IProwlarrCloudRequestBuilder cloudRequestBuilder,
            IHttpClient client,
            IIndexerProxyFactory proxyFactory,
            ILocalizationService localizationService,
            Logger logger)
            : base(localizationService)
        {
            _proxyFactory = proxyFactory;
            _cloudRequestBuilder = cloudRequestBuilder.Services;
            _logger = logger;
            _client = client;
        }

        public override HealthCheck Check()
        {
            var enabledProviders = _proxyFactory.GetAvailableProviders();

            var badProxies = enabledProviders.Where(p => !IsProxyWorking(p)).ToList();

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

        private bool IsProxyWorking(IIndexerProxy indexerProxy)
        {
            var request = _cloudRequestBuilder.Create()
                                              .Resource("/ping")
                                              .Build();

            try
            {
                var addresses = Dns.GetHostAddresses(((IIndexerProxySettings)indexerProxy.Definition.Settings).Host);
                if (!addresses.Any())
                {
                    return false;
                }

                var response = _client.Execute(request);

                // We only care about 400 responses, other error codes can be ignored
                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    _logger.Error("Proxy Health Check failed: {0}", response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Proxy Health Check failed");
                return false;
            }

            return true;
        }
    }
}
