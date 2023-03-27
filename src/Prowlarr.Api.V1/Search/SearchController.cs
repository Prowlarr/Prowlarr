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
using NzbDrone.Core.Download.Clients;
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
        private readonly IReleaseSearchService _releaseSearchService;
        private readonly IDownloadService _downloadService;
        private readonly IIndexerFactory _indexerFactory;
        private readonly IDownloadMappingService _downloadMappingService;
        private readonly Logger _logger;

        private readonly ICached<ReleaseInfo> _remoteReleaseCache;

        public SearchController(IReleaseSearchService releaseSearchService, IDownloadService downloadService, IIndexerFactory indexerFactory, IDownloadMappingService downloadMappingService, ICacheManager cacheManager, Logger logger)
        {
            _releaseSearchService = releaseSearchService;
            _downloadService = downloadService;
            _indexerFactory = indexerFactory;
            _downloadMappingService = downloadMappingService;
            _logger = logger;

            PostValidator.RuleFor(s => s.IndexerId).ValidId();
            PostValidator.RuleFor(s => s.Guid).NotEmpty();

            _remoteReleaseCache = cacheManager.GetCache<ReleaseInfo>(GetType(), "remoteReleases");
        }

        [NonAction]
        public override ReleaseResource GetResourceById(int id)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<ActionResult<ReleaseResource>> GrabRelease([FromBody] ReleaseResource release)
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
                await _downloadService.SendReportToClient(releaseInfo, source, host, indexerDef.Redirect, release.DownloadClientId);
            }
            catch (ReleaseDownloadException ex)
            {
                _logger.Error(ex, "Getting release from indexer failed");

                throw new NzbDroneClientException(HttpStatusCode.Conflict, "Getting release from indexer failed");
            }

            return Ok(release);
        }

        [HttpPost("bulk")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<ActionResult<ReleaseResource>> GrabReleases([FromBody] List<ReleaseResource> releases)
        {
            releases.ForEach(release => ValidateResource(release));

            var source = Request.GetSource();
            var host = Request.GetHostName();

            var grabbedReleases = new List<ReleaseResource>();

            var groupedReleases = releases.GroupBy(r => r.IndexerId).ToList();

            foreach (var indexerReleases in groupedReleases)
            {
                var indexerDef = _indexerFactory.Get(indexerReleases.Key);

                foreach (var release in indexerReleases)
                {
                    var releaseInfo = _remoteReleaseCache.Find(GetCacheKey(release));

                    if (releaseInfo == null)
                    {
                        _logger.Error("Couldn't find requested release in cache, cache timeout probably expired.");

                        continue;
                    }

                    try
                    {
                        await _downloadService.SendReportToClient(releaseInfo, source, host, indexerDef.Redirect, null);
                    }
                    catch (ReleaseDownloadException ex)
                    {
                        _logger.Error(ex, "Getting release from indexer failed");

                        continue;
                    }
                    catch (DownloadClientException ex)
                    {
                        _logger.Error(ex, "Failed to send grabbed release to download client");

                        continue;
                    }

                    grabbedReleases.Add(release);
                }
            }

            if (!grabbedReleases.Any())
            {
                throw new NzbDroneClientException(HttpStatusCode.BadRequest, "Failed to grab any release");
            }

            return Ok(grabbedReleases);
        }

        [HttpGet]
        [Produces("application/json")]
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

                var result = await _releaseSearchService.Search(request, payload.IndexerIds, true);
                var releases = result.Releases;

                return MapReleases(releases, Request.GetServerUrl());
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

        protected virtual List<ReleaseResource> MapReleases(IEnumerable<ReleaseInfo> releases, string serverUrl)
        {
            var result = new List<ReleaseResource>();

            foreach (var releaseInfo in releases)
            {
                var release = releaseInfo.ToResource();

                _remoteReleaseCache.Set(GetCacheKey(release), releaseInfo, TimeSpan.FromMinutes(30));
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
