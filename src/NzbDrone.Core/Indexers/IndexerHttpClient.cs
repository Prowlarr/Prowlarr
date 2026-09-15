using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Http.Dispatchers;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerProxies;
using NzbDrone.Core.IndexerProxies.FlareSolverr;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers
{
    public interface IIndexerHttpClient : IHttpClient
    {
        Task<HttpResponse> ExecuteProxiedAsync(HttpRequest request, ProviderDefinition definition);
        HttpResponse ExecuteProxied(HttpRequest request, ProviderDefinition definition);
    }

    public class IndexerHttpClient : HttpClient, IIndexerHttpClient
    {
        private readonly IIndexerProxyFactory _indexerProxyFactory;
        private readonly IConfigService _configService;

        public IndexerHttpClient(IIndexerProxyFactory indexerProxyFactory,
            IConfigService configService,
            IEnumerable<IHttpRequestInterceptor> requestInterceptors,
            ICacheManager cacheManager,
            IRateLimitService rateLimitService,
            IHttpDispatcher httpDispatcher,
            Logger logger)
            : base(requestInterceptors, cacheManager, rateLimitService, httpDispatcher, logger)
        {
            _indexerProxyFactory = indexerProxyFactory;
            _configService = configService;
        }

        public async Task<HttpResponse> ExecuteProxiedAsync(HttpRequest request, ProviderDefinition definition)
        {
            var selectedProxies = GetProxies(definition);

            request = PreRequest(ApplyUserAgent(request, definition), selectedProxies);

            return PostResponse(await ExecuteAsync(request), selectedProxies);
        }

        public HttpResponse ExecuteProxied(HttpRequest request, ProviderDefinition definition)
        {
            var selectedProxies = GetProxies(definition);

            request = PreRequest(ApplyUserAgent(request, definition), selectedProxies);

            return PostResponse(Execute(request), selectedProxies);
        }

        // Applied before the proxies get the request so that a configured User-Agent wins over the one
        // FlareSolverr caches per host, which it only injects when the request has none.
        private HttpRequest ApplyUserAgent(HttpRequest request, ProviderDefinition definition)
        {
            var userAgent = definition is IndexerDefinition indexerDefinition && indexerDefinition.UserAgent.IsNotNullOrWhiteSpace()
                ? indexerDefinition.UserAgent
                : _configService.IndexerUserAgent;

            if (userAgent.IsNotNullOrWhiteSpace())
            {
                request.Headers.UserAgent = userAgent;
            }

            return request;
        }

        private IList<IIndexerProxy> GetProxies(ProviderDefinition definition)
        {
            // Skip DB call if no tags on the indexers
            if (definition is { Id: > 0 } && definition.Tags.Count == 0)
            {
                return Array.Empty<IIndexerProxy>();
            }

            var proxies = _indexerProxyFactory.GetAvailableProviders();

            var selectedProxies = proxies
                .Where(proxy => definition.Tags.Intersect(proxy.Definition.Tags).Any())
                .GroupBy(p => p is FlareSolverr)
                .Select(g => g.First())
                .OrderBy(p => p is FlareSolverr)
                .ToList();

            if (!selectedProxies.Any() && definition is not { Id: not 0 })
            {
                selectedProxies = new List<IIndexerProxy>();
                selectedProxies.AddIfNotNull(proxies.Find(p => p is FlareSolverr));
            }

            return selectedProxies;
        }

        private HttpRequest PreRequest(HttpRequest request, IList<IIndexerProxy> selectedProxies)
        {
            if (selectedProxies != null && selectedProxies.Any())
            {
                request = selectedProxies.Aggregate(request, (current, selectedProxy) => selectedProxy.PreRequest(current));
            }

            return request;
        }

        private HttpResponse PostResponse(HttpResponse response, IList<IIndexerProxy> selectedProxies)
        {
            if (selectedProxies != null && selectedProxies.Any())
            {
                response = selectedProxies.Aggregate(response, (current, selectedProxy) => selectedProxy.PostResponse(current));
            }

            return response;
        }
    }
}
