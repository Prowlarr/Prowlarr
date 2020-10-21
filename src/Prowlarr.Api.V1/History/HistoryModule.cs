using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.History;
using Prowlarr.Http;
using Prowlarr.Http.Extensions;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1.History
{
    public class HistoryModule : ProwlarrRestModule<HistoryResource>
    {
        private readonly IHistoryService _historyService;

        public HistoryModule(IHistoryService historyService)
        {
            _historyService = historyService;
            GetResourcePaged = GetHistory;

            Get("/since", x => GetHistorySince());
            Get("/indexer", x => GetIndexerHistory());
        }

        protected HistoryResource MapToResource(NzbDrone.Core.History.History model, bool includeMovie)
        {
            var resource = model.ToResource();

            return resource;
        }

        private PagingResource<HistoryResource> GetHistory(PagingResource<HistoryResource> pagingResource)
        {
            var pagingSpec = pagingResource.MapToPagingSpec<HistoryResource, NzbDrone.Core.History.History>("date", SortDirection.Descending);
            var includeMovie = Request.GetBooleanQueryParameter("includeMovie");

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

            return ApplyToPage(_historyService.Paged, pagingSpec, h => MapToResource(h, includeMovie));
        }

        private List<HistoryResource> GetHistorySince()
        {
            var queryDate = Request.Query.Date;
            var queryEventType = Request.Query.EventType;

            if (!queryDate.HasValue)
            {
                throw new BadRequestException("date is missing");
            }

            DateTime date = DateTime.Parse(queryDate.Value);
            HistoryEventType? eventType = null;
            var includeMovie = Request.GetBooleanQueryParameter("includeMovie");

            if (queryEventType.HasValue)
            {
                eventType = (HistoryEventType)Convert.ToInt32(queryEventType.Value);
            }

            return _historyService.Since(date, eventType).Select(h => MapToResource(h, includeMovie)).ToList();
        }

        private List<HistoryResource> GetIndexerHistory()
        {
            var queryIndexerId = Request.Query.IndexerId;
            var queryEventType = Request.Query.EventType;

            if (!queryIndexerId.HasValue)
            {
                throw new BadRequestException("indexerId is missing");
            }

            int indexerId = Convert.ToInt32(queryIndexerId.Value);
            HistoryEventType? eventType = null;
            var includeIndexer = Request.GetBooleanQueryParameter("includeIndexer");

            if (queryEventType.HasValue)
            {
                eventType = (HistoryEventType)Convert.ToInt32(queryEventType.Value);
            }

            return _historyService.GetByIndexerId(indexerId, eventType).Select(h => MapToResource(h, includeIndexer)).ToList();
        }
    }
}
