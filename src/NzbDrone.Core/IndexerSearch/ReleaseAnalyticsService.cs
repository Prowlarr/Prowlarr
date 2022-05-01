using System.Linq;
using System.Net.Http;
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

        public ReleaseAnalyticsService(IHttpClient httpClient, IProwlarrCloudRequestBuilder requestBuilder, IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
            _requestBuilder = requestBuilder.Releases;
            _httpClient = httpClient;
        }

        public void HandleAsync(IndexerQueryEvent message)
        {
            if (_analyticsService.IsEnabled)
            {
                var request = _requestBuilder.Create().Resource("release/push").Build();
                request.Method = HttpMethod.Post;
                request.Headers.ContentType = "application/json";
                request.SuppressHttpError = true;

                var body = message.QueryResult.Releases.Select(x => new
                {
                    Title = x.Title,
                    Categories = x.Categories.Where(c => c.Id < 10000).Select(c => c.Id),
                    Protocol = x.DownloadProtocol.ToString(),
                    Size = x.Size,
                    PublishDate = x.PublishDate
                });

                request.SetContent(body.ToJson());
                _httpClient.Post(request);
            }
        }
    }
}
