using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Analytics;
using NzbDrone.Core.Indexers.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.IndexerSearch
{
    public class ReleaseAnalyticsService : IHandleAsync<IndexerQueryEvent>
    {
        private readonly IHttpClient _httpClient;
        private readonly IHttpRequestBuilderFactory _requestBuilder;
        private readonly IAnalyticsService _analyticsService;
        private readonly Logger _logger;

        public ReleaseAnalyticsService(IHttpClient httpClient, IProwlarrCloudRequestBuilder requestBuilder, IAnalyticsService analyticsService, Logger logger)
        {
            _analyticsService = analyticsService;
            _requestBuilder = requestBuilder.Releases;
            _httpClient = httpClient;
            _logger = logger;
        }

        public void HandleAsync(IndexerQueryEvent message)
        {
            if (_analyticsService.IsEnabled && message.QueryResult?.Releases != null)
            {
                var request = _requestBuilder.Create().Resource("release/push").Build();
                request.Method = HttpMethod.Post;
                request.Headers.ContentType = "application/json";
                request.SuppressHttpError = true;

                var body = message.QueryResult.Releases.Select(x => new
                {
                    Title = x.Title,
                    Categories = x.Categories?.Where(c => c.Id < 10000).Select(c => c.Id) ?? new List<int>(),
                    Protocol = x.DownloadProtocol.ToString(),
                    Size = x.Size,
                    PublishDate = x.PublishDate
                });

                try
                {
                    request.SetContent(body.ToJson());
                    _httpClient.Post(request);
                }
                catch
                {
                    _logger.Trace("Analytics push failed");
                }
            }
        }
    }
}
