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
            Get("/movie", x => GetMovieHistory());
        }

        protected HistoryResource MapToResource(MovieHistory model, bool includeMovie)
        {
            var resource = model.ToResource();

            return resource;
        }

        private PagingResource<HistoryResource> GetHistory(PagingResource<HistoryResource> pagingResource)
        {
            var pagingSpec = pagingResource.MapToPagingSpec<HistoryResource, MovieHistory>("date", SortDirection.Descending);
            var includeMovie = Request.GetBooleanQueryParameter("includeMovie");

            var eventTypeFilter = pagingResource.Filters.FirstOrDefault(f => f.Key == "eventType");
            var downloadIdFilter = pagingResource.Filters.FirstOrDefault(f => f.Key == "downloadId");

            if (eventTypeFilter != null)
            {
                var filterValue = (MovieHistoryEventType)Convert.ToInt32(eventTypeFilter.Value);
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
            MovieHistoryEventType? eventType = null;
            var includeMovie = Request.GetBooleanQueryParameter("includeMovie");

            if (queryEventType.HasValue)
            {
                eventType = (MovieHistoryEventType)Convert.ToInt32(queryEventType.Value);
            }

            return _historyService.Since(date, eventType).Select(h => MapToResource(h, includeMovie)).ToList();
        }

        private List<HistoryResource> GetMovieHistory()
        {
            var queryMovieId = Request.Query.MovieId;
            var queryEventType = Request.Query.EventType;

            if (!queryMovieId.HasValue)
            {
                throw new BadRequestException("movieId is missing");
            }

            int movieId = Convert.ToInt32(queryMovieId.Value);
            MovieHistoryEventType? eventType = null;
            var includeMovie = Request.GetBooleanQueryParameter("includeMovie");

            if (queryEventType.HasValue)
            {
                eventType = (MovieHistoryEventType)Convert.ToInt32(queryEventType.Value);
            }

            return _historyService.GetByMovieId(movieId, eventType).Select(h => MapToResource(h, includeMovie)).ToList();
        }
    }
}
