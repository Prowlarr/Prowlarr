using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Common.Http.Dispatchers;
using NzbDrone.Common.TPL;
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
        public IndexerHttpClient(IIndexerProxyFactory indexerProxyFactory,
            IEnumerable<IHttpRequestInterceptor> requestInterceptors,
            ICacheManager cacheManager,
            IRateLimitService rateLimitService,
            IHttpDispatcher httpDispatcher,
            Logger logger)
            : base(requestInterceptors, cacheManager, rateLimitService, httpDispatcher, logger)
        {
            _indexerProxyFactory = indexerProxyFactory;
        }

        public async Task<HttpResponse> ExecuteProxiedAsync(HttpRequest request, ProviderDefinition definition)
        {
            var selectedProxy = GetProxy(definition);

            request = PreRequest(request, selectedProxy);

            return PostResponse(await ExecuteAsync(request), selectedProxy);
        }

        public HttpResponse ExecuteProxied(HttpRequest request, ProviderDefinition definition)
        {
            var selectedProxy = GetProxy(definition);

            request = PreRequest(request, selectedProxy);

            return PostResponse(Execute(request), selectedProxy);
        }

        private IIndexerProxy GetProxy(ProviderDefinition definition)
        {
            //Skip DB call if no tags on the indexers
            if (definition.Tags.Count == 0 && definition.Id > 0)
            {
                return null;
            }

            var proxies = _indexerProxyFactory.GetAvailableProviders();
            IIndexerProxy selectedProxy = null;

            foreach (var proxy in proxies)
            {
                if (definition.Tags.Intersect(proxy.Definition.Tags).Any())
                {
                    selectedProxy = proxy;
                    break;
                }
            }

            if (selectedProxy == null && definition.Id == 0)
            {
                selectedProxy = proxies.FirstOrDefault(p => p is FlareSolverr);
            }

            return selectedProxy;
        }

        private HttpRequest PreRequest(HttpRequest request, IIndexerProxy selectedProxy)
        {
            if (selectedProxy != null)
            {
                request = selectedProxy.PreRequest(request);
            }

            return request;
        }

        private HttpResponse PostResponse(HttpResponse response, IIndexerProxy selectedProxy)
        {
            if (selectedProxy != null)
            {
                response = selectedProxy.PostResponse(response);
            }

            return response;
        }
    }
}
