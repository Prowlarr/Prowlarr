using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.History;
using Prowlarr.Http;
using Prowlarr.Http.Extensions;

namespace Prowlarr.Api.V1.History
{
    [V1ApiController]
    public class HistoryController : Controller
    {
        private readonly IHistoryService _historyService;

        public HistoryController(IHistoryService historyService)
        {
            _historyService = historyService;
        }

        protected HistoryResource MapToResource(NzbDrone.Core.History.History model)
        {
            var resource = model.ToResource();

            return resource;
        }

        [HttpGet]
        public PagingResource<HistoryResource> GetHistory()
        {
            var pagingResource = Request.ReadPagingResourceFromRequest<HistoryResource>();
            var pagingSpec = pagingResource.MapToPagingSpec<HistoryResource, NzbDrone.Core.History.History>("date", SortDirection.Descending);

            var eventTypeFilter = pagingResource.Filters.FirstOrDefault(f => f.Key == "eventType");
            var downloadIdFilter = pagingResource.Filters.FirstOrDefault(f => f.Key == "downloadId");

            if (eventTypeFilter != null)
            {
                var filterValue = (HistoryEventType)Convert.ToInt32(eventTypeFilter.Value);
                pagingSpec.FilterExpressions.Add(v => v.EventType == filterValue);
            }

            if (downloadIdFilter != null)
            {
                var downloadId = downloadIdFilter.Value;
                pagingSpec.FilterExpressions.Add(h => h.DownloadId == downloadId);
            }

            return pagingSpec.ApplyToPage(_historyService.Paged, h => MapToResource(h));
        }

        [HttpGet("since")]
        public List<HistoryResource> GetHistorySince(DateTime date, HistoryEventType? eventType = null)
        {
            return _historyService.Since(date, eventType).Select(h => MapToResource(h)).ToList();
        }

        [HttpGet("indexer")]
        public List<HistoryResource> GetIndexerHistory(int indexerId, HistoryEventType? eventType = null)
        {
            return _historyService.GetByIndexerId(indexerId, eventType).Select(h => MapToResource(h)).ToList();
        }
    }
}
