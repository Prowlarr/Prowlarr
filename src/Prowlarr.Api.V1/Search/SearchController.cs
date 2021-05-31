using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Download;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;
using Prowlarr.Http;
using Prowlarr.Http.Extensions;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1.Search
{
    [V1ApiController]
    public class SearchController : RestController<SearchResource>
    {
        private readonly ISearchForNzb _nzbSearhService;
        private readonly IDownloadService _downloadService;
        private readonly IIndexerFactory _indexerFactory;
        private readonly IDownloadMappingService _downloadMappingService;
        private readonly Logger _logger;

        private readonly ICached<ReleaseInfo> _remoteReleaseCache;

        public SearchController(ISearchForNzb nzbSearhService, IDownloadService downloadService, IIndexerFactory indexerFactory, IDownloadMappingService downloadMappingService, ICacheManager cacheManager, Logger logger)
        {
            _nzbSearhService = nzbSearhService;
            _downloadService = downloadService;
            _indexerFactory = indexerFactory;
            _downloadMappingService = downloadMappingService;
            _logger = logger;

            PostValidator.RuleFor(s => s.IndexerId).ValidId();
            PostValidator.RuleFor(s => s.Guid).NotEmpty();

            _remoteReleaseCache = cacheManager.GetCache<ReleaseInfo>(GetType(), "remoteReleases");
        }

        public override SearchResource GetResourceById(int id)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public ActionResult<SearchResource> Create(SearchResource release)
        {
            ValidateResource(release);

            var releaseInfo = _remoteReleaseCache.Find(GetCacheKey(release));

            var indexerDef = _indexerFactory.Get(release.IndexerId);
            var source = UserAgentParser.ParseSource(Request.Headers["User-Agent"]);
            var host = Request.GetHostName();

            try
            {
                _downloadService.SendReportToClient(releaseInfo, source, host, indexerDef.Redirect);
            }
            catch (ReleaseDownloadException ex)
            {
                _logger.Error(ex, "Getting release from indexer failed");
                throw new NzbDroneClientException(HttpStatusCode.Conflict, "Getting release from indexer failed");
            }

            return Ok(release);
        }

        [HttpGet]
        public Task<List<SearchResource>> GetAll(string query, [FromQuery] List<int> indexerIds, [FromQuery] List<int> categories)
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

            return Task.FromResult(new List<SearchResource>());
        }

        private async Task<List<SearchResource>> GetSearchReleases(string query, List<int> indexerIds, List<int> categories)
        {
            try
            {
                var request = new NewznabRequest { q = query, source = "Prowlarr", t = "search", cat = string.Join(",", categories), server = Request.GetServerUrl() };
                var result = await _nzbSearhService.Search(request, indexerIds, true);
                var decisions = result.Releases;

                return MapDecisions(decisions, Request.GetServerUrl());
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

        protected virtual List<SearchResource> MapDecisions(IEnumerable<ReleaseInfo> releases, string serverUrl)
        {
            var result = new List<SearchResource>();

            foreach (var downloadDecision in releases)
            {
                var release = downloadDecision.ToResource();

                _remoteReleaseCache.Set(GetCacheKey(release), downloadDecision, TimeSpan.FromMinutes(30));
                release.DownloadUrl = release.DownloadUrl != null ? _downloadMappingService.ConvertToProxyLink(new Uri(release.DownloadUrl), serverUrl, release.IndexerId, release.Title).ToString() : null;
                release.MagnetUrl = release.MagnetUrl != null ? _downloadMappingService.ConvertToProxyLink(new Uri(release.MagnetUrl), serverUrl, release.IndexerId, release.Title).ToString() : null;

                result.Add(release);
            }

            _remoteReleaseCache.ClearExpired();

            return result;
        }

        private string GetCacheKey(SearchResource resource)
        {
            return string.Concat(resource.IndexerId, "_", resource.Guid);
        }
    }
}
