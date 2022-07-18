using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Analytics;
using NzbDrone.Core.Indexers.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.IndexerSearch
{
    public class ReleaseAnalyticsService : IHandleAsync<IndexerQueryEvent>
    {
        private readonly IHttpClient _httpClient;
        private readonly IHttpRequestBuilderFactory _requestBuilder;
        private readonly IAnalyticsService _analyticsService;
        private readonly Debouncer _debouncer;
        private readonly Logger _logger;
        private readonly List<ReleaseInfo> _pendingUpdates;

        public ReleaseAnalyticsService(IHttpClient httpClient, IProwlarrCloudRequestBuilder requestBuilder, IAnalyticsService analyticsService, Logger logger)
        {
            _debouncer = new Debouncer(SendReleases, TimeSpan.FromMinutes(10));
            _analyticsService = analyticsService;
            _requestBuilder = requestBuilder.Releases;
            _httpClient = httpClient;
            _logger = logger;

            _pendingUpdates = new List<ReleaseInfo>();
        }

        public void HandleAsync(IndexerQueryEvent message)
        {
            if (message.QueryResult?.Releases != null)
            {
                lock (_pendingUpdates)
                {
                    _pendingUpdates.AddRange(message.QueryResult.Releases.Where(r => r.Title.IsNotNullOrWhiteSpace()));
                }

                _debouncer.Execute();
            }
        }

        public void SendReleases()
        {
            lock (_pendingUpdates)
            {
                var pendingUpdates = _pendingUpdates.ToArray();
                _pendingUpdates.Clear();

                var request = _requestBuilder.Create().Resource("release/push").Build();
                request.Method = HttpMethod.Post;
                request.Headers.ContentType = "application/json";
                request.SuppressHttpError = true;
                request.LogHttpError = false;

                var body = pendingUpdates.DistinctBy(r => r.Title).Select(x => new
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
