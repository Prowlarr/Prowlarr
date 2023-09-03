using System;
using System.Collections.Generic;
using NzbDrone.Core.History;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1.History
{
    public class HistoryResource : RestResource
    {
        public int IndexerId { get; set; }
        public DateTime Date { get; set; }
        public string DownloadId { get; set; }
        public bool Successful { get; set; }
        public HistoryEventType EventType { get; set; }
        public Dictionary<string, string> Data { get; set; }
    }

    public static class HistoryResourceMapper
    {
        public static HistoryResource ToResource(this NzbDrone.Core.History.History model)
        {
            if (model == null)
            {
                return null;
            }

            return new HistoryResource
            {
                Id = model.Id,
                IndexerId = model.IndexerId,
                Date = model.Date,
                DownloadId = model.DownloadId,
                Successful = model.Successful,
                EventType = model.EventType,
                Data = model.Data
            };
        }
    }
}
