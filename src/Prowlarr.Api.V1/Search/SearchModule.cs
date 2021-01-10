using System;
using System.Collections.Generic;
using System.Net;
using Nancy.ModelBinding;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Parser.Model;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Search
{
    public class SearchModule : ProwlarrRestModule<SearchResource>
    {
        private readonly ISearchForNzb _nzbSearhService;
        private readonly Logger _logger;

        public SearchModule(ISearchForNzb nzbSearhService, Logger logger)
        {
            _nzbSearhService = nzbSearhService;
            _logger = logger;

            GetResourceAll = GetAll;
        }

        private List<SearchResource> GetAll()
        {
            var request = this.Bind<SearchRequest>();

            if (request.Query.IsNotNullOrWhiteSpace())
            {
                var indexerIds = request.IndexerIds ?? new List<int>();
                var categories = request.Categories ?? new List<int>();

                if (indexerIds.Count > 0)
                {
                    return GetSearchReleases(request.Query, indexerIds, categories);
                }
                else
                {
                    return GetSearchReleases(request.Query, null, categories);
                }
            }

            return new List<SearchResource>();
        }

        private List<SearchResource> GetSearchReleases(string query, List<int> indexerIds, List<int> categories)
        {
            try
            {
                var decisions = _nzbSearhService.Search(new NewznabRequest { q = query, source = "Prowlarr", t = "search", cat = string.Join(",", categories) }, indexerIds, true).Releases;

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
