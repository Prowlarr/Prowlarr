using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
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

        [HttpGet]
        [Produces("application/json")]
        public PagingResource<HistoryResource> GetHistory([FromQuery] PagingRequestResource paging, [FromQuery(Name = "eventType")] int[] eventTypes, bool? successful, string downloadId, [FromQuery] int[] indexerIds = null)
        {
            var pagingResource = new PagingResource<HistoryResource>(paging);
            var pagingSpec = pagingResource.MapToPagingSpec<HistoryResource, NzbDrone.Core.History.History>("date", SortDirection.Descending);

            if (eventTypes != null && eventTypes.Any())
            {
                pagingSpec.FilterExpressions.Add(v => eventTypes.Contains((int)v.EventType));
            }

            if (successful.HasValue)
            {
                var filterValue = successful.Value;
                pagingSpec.FilterExpressions.Add(v => v.Successful == filterValue);
            }

            if (downloadId.IsNotNullOrWhiteSpace())
            {
                pagingSpec.FilterExpressions.Add(h => h.DownloadId == downloadId);
            }

            if (indexerIds != null && indexerIds.Any())
            {
                pagingSpec.FilterExpressions.Add(h => indexerIds.Contains(h.IndexerId));
            }

            return pagingSpec.ApplyToPage(h => _historyService.Paged(pagingSpec), MapToResource);
        }

        [HttpGet("since")]
        [Produces("application/json")]
        public List<HistoryResource> GetHistorySince(DateTime date, HistoryEventType? eventType = null)
        {
            return _historyService.Since(date, eventType).Select(MapToResource).ToList();
        }

        [HttpGet("indexer")]
        [Produces("application/json")]
        public List<HistoryResource> GetIndexerHistory(int indexerId, HistoryEventType? eventType = null, int? limit = null)
        {
            if (limit.HasValue)
            {
                return _historyService.GetByIndexerId(indexerId, eventType).Select(MapToResource).Take(limit.Value).ToList();
            }

            return _historyService.GetByIndexerId(indexerId, eventType).Select(MapToResource).ToList();
        }

        protected HistoryResource MapToResource(NzbDrone.Core.History.History model)
        {
            return model.ToResource();
        }
    }
}
