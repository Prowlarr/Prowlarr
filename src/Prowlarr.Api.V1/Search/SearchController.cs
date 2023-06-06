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
    public class SearchController : RestController<ReleaseResource>
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

        public override ReleaseResource GetResourceById(int id)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public ActionResult<ReleaseResource> GrabRelease(ReleaseResource release)
        {
            ValidateResource(release);

            var releaseInfo = _remoteReleaseCache.Find(GetCacheKey(release));

            if (releaseInfo == null)
            {
                _logger.Debug("Couldn't find requested release in cache, cache timeout probably expired.");

                throw new NzbDroneClientException(HttpStatusCode.NotFound, "Couldn't find requested release in cache, try searching again");
            }

            var indexerDef = _indexerFactory.Get(release.IndexerId);
            var source = Request.GetSource();
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

        [HttpPost("bulk")]
        public ActionResult<ReleaseResource> GrabReleases(List<ReleaseResource> releases)
        {
            var source = Request.GetSource();
            var host = Request.GetHostName();

            var groupedReleases = releases.GroupBy(r => r.IndexerId);

            foreach (var indexerReleases in groupedReleases)
            {
                var indexerDef = _indexerFactory.Get(indexerReleases.Key);

                foreach (var release in indexerReleases)
                {
                    ValidateResource(release);

                    var releaseInfo = _remoteReleaseCache.Find(GetCacheKey(release));

                    try
                    {
                        _downloadService.SendReportToClient(releaseInfo, source, host, indexerDef.Redirect);
                    }
                    catch (ReleaseDownloadException ex)
                    {
                        _logger.Error(ex, "Getting release from indexer failed");
                    }
                }
            }

            return Ok(releases);
        }

        [HttpGet]
        public Task<List<ReleaseResource>> GetAll([FromQuery] SearchResource payload)
        {
            return GetSearchReleases(payload);
        }

        private async Task<List<ReleaseResource>> GetSearchReleases([FromQuery] SearchResource payload)
        {
            try
            {
                var request = new NewznabRequest
                {
                    q = payload.Query,
                    t = payload.Type,
                    source = "Prowlarr",
                    cat = string.Join(",", payload.Categories),
                    server = Request.GetServerUrl(),
                    host = Request.GetHostName(),
                    limit = payload.Limit,
                    offset = payload.Offset
                };

                request.QueryToParams();

                var result = await _nzbSearhService.Search(request, payload.IndexerIds, true);
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

            return new List<ReleaseResource>();
        }

        protected virtual List<ReleaseResource> MapDecisions(IEnumerable<ReleaseInfo> releases, string serverUrl)
        {
            var result = new List<ReleaseResource>();

            foreach (var downloadDecision in releases)
            {
                var release = downloadDecision.ToResource();

                _remoteReleaseCache.Set(GetCacheKey(release), downloadDecision, TimeSpan.FromMinutes(30));
                release.DownloadUrl = release.DownloadUrl.IsNotNullOrWhiteSpace() ? _downloadMappingService.ConvertToProxyLink(new Uri(release.DownloadUrl), serverUrl, release.IndexerId, release.Title).AbsoluteUri : null;
                release.MagnetUrl = release.MagnetUrl.IsNotNullOrWhiteSpace() ? _downloadMappingService.ConvertToProxyLink(new Uri(release.MagnetUrl), serverUrl, release.IndexerId, release.Title).AbsoluteUri : null;

                result.Add(release);
            }

            _remoteReleaseCache.ClearExpired();

            return result;
        }

        private string GetCacheKey(ReleaseResource resource)
        {
            return string.Concat(resource.IndexerId, "_", resource.Guid);
        }
    }
}
