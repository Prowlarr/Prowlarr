using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerStats;
using NzbDrone.Core.Tags;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Indexers
{
    [V1ApiController]
    public class IndexerStatsController : Controller
    {
        private readonly IIndexerStatisticsService _indexerStatisticsService;
        private readonly IIndexerFactory _indexerFactory;
        private readonly ITagService _tagService;

        public IndexerStatsController(IIndexerStatisticsService indexerStatisticsService, IIndexerFactory indexerFactory, ITagService tagService)
        {
            _indexerStatisticsService = indexerStatisticsService;
            _indexerFactory = indexerFactory;
            _tagService = tagService;
        }

        [HttpGet]
        [Produces("application/json")]
        public IndexerStatsResource GetAll(DateTime? startDate, DateTime? endDate, string indexers, string protocols, string tags)
        {
            var statsStartDate = startDate ?? DateTime.MinValue;
            var statsEndDate = endDate ?? DateTime.Now;
            var parsedIndexers = new List<int>();
            var parsedTags = new List<int>();

            var allIndexers = _indexerFactory.All().Select(_indexerFactory.GetInstance).ToList();

            if (protocols.IsNotNullOrWhiteSpace())
            {
                var parsedProtocols = protocols.Split(',')
                    .Select(protocol =>
                    {
                        Enum.TryParse(protocol, true, out DownloadProtocol downloadProtocol);

                        return downloadProtocol;
                    })
                    .ToHashSet();

                allIndexers = allIndexers.Where(i => parsedProtocols.Contains(i.Protocol)).ToList();
            }

            var indexerIds = allIndexers.Select(i => i.Definition.Id).ToList();

            if (tags.IsNotNullOrWhiteSpace())
            {
                parsedTags.AddRange(tags.Split(',').Select(_tagService.GetTag).Select(t => t.Id));

                indexerIds = indexerIds.Intersect(parsedTags.SelectMany(t => _indexerFactory.AllForTag(t).Select(i => i.Id))).ToList();
            }

            if (indexers.IsNotNullOrWhiteSpace())
            {
                parsedIndexers.AddRange(indexers.Split(',').Select(x => Convert.ToInt32(x)));

                indexerIds = indexerIds.Intersect(parsedIndexers).ToList();
            }

            var indexerStats = _indexerStatisticsService.IndexerStatistics(statsStartDate, statsEndDate, indexerIds);

            var indexerResource = new IndexerStatsResource
            {
                Indexers = indexerStats.IndexerStatistics,
                UserAgents = indexerStats.UserAgentStatistics,
                Hosts = indexerStats.HostStatistics
            };

            return indexerResource;
        }
    }
}
