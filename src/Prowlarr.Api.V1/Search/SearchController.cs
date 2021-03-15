using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Parser.Model;
using Prowlarr.Http;
using Prowlarr.Http.Extensions;

namespace Prowlarr.Api.V1.Search
{
    [V1ApiController]
    public class SearchController : Controller
    {
        private readonly ISearchForNzb _nzbSearhService;
        private readonly Logger _logger;

        public SearchController(ISearchForNzb nzbSearhService, Logger logger)
        {
            _nzbSearhService = nzbSearhService;
            _logger = logger;
        }

        [HttpGet]
        public List<SearchResource> GetAll(string query, [FromQuery] List<int> indexerIds, [FromQuery] List<int> categories)
        {
            if (query.IsNotNullOrWhiteSpace())
            {
                if (indexerIds.Any())
                {
                    return GetSearchReleases(query, indexerIds, categories);
                }
                else
                {
                    return GetSearchReleases(query, null, categories);
                }
            }

            return new List<SearchResource>();
        }

        private List<SearchResource> GetSearchReleases(string query, List<int> indexerIds, List<int> categories)
        {
            try
            {
                var request = new NewznabRequest { q = query, source = "Prowlarr", t = "search", cat = string.Join(",", categories), server = Request.GetServerUrl() };
                var decisions = _nzbSearhService.Search(request, indexerIds, true).Releases;

                return MapDecisions(decisions);
            }
            catch (SearchFailedException ex)
            {
                throw new NzbDroneClientException(HttpStatusCode.BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Search failed: " + ex.Message);
            }

            return new List<SearchResource>();
        }

        protected virtual List<SearchResource> MapDecisions(IEnumerable<ReleaseInfo> releases)
        {
            var result = new List<SearchResource>();

            foreach (var downloadDecision in releases)
            {
                var release = downloadDecision.ToResource();

                result.Add(release);
            }

            return result;
        }
    }
}
