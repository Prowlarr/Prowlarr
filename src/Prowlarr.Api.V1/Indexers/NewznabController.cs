using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Parser;
using Prowlarr.Http.Extensions;
using Prowlarr.Http.REST;

namespace NzbDrone.Api.V1.Indexers
{
    [Route("")]
    [EnableCors("ApiCorsPolicy")]
    [ApiController]
    public class NewznabController : Controller
    {
        private IIndexerFactory _indexerFactory { get; set; }
        private ISearchForNzb _nzbSearchService { get; set; }
        private IDownloadMappingService _downloadMappingService { get; set; }
        private IDownloadService _downloadService { get; set; }

        public NewznabController(IndexerFactory indexerFactory,
            ISearchForNzb nzbSearchService,
            IDownloadMappingService downloadMappingService,
            IDownloadService downloadService)
        {
            _indexerFactory = indexerFactory;
            _nzbSearchService = nzbSearchService;
            _downloadMappingService = downloadMappingService;
            _downloadService = downloadService;
        }

        [HttpGet("{id:int}/api")]
        public async Task<IActionResult> GetNewznabResponse(int id, [FromQuery] NewznabRequest request)
        {
            var requestType = request.t;
            request.source = UserAgentParser.ParseSource(Request.Headers["User-Agent"]);
            request.server = Request.GetServerUrl();

            if (requestType.IsNullOrWhiteSpace())
            {
                throw new BadRequestException("Missing Function Parameter");
            }

            var indexer = _indexerFactory.Get(id);

            if (indexer == null)
            {
                throw new NotFoundException("Indexer Not Found");
            }

            var indexerInstance = _indexerFactory.GetInstance(indexer);

            switch (requestType)
            {
                case "caps":
                    var caps = indexerInstance.GetCapabilities();
                    return Content(caps.ToXml(), "application/rss+xml");
                case "search":
                case "tvsearch":
                case "music":
                case "book":
                case "movie":
                    var results = await _nzbSearchService.Search(request, new List<int> { indexer.Id }, false);

                    foreach (var result in results.Releases)
                    {
                        result.DownloadUrl = _downloadMappingService.ConvertToProxyLink(new Uri(result.DownloadUrl), request.server, indexer.Id, result.Title).ToString();
                    }

                    return Content(results.ToXml(indexerInstance.Protocol), "application/rss+xml");
                default:
                    throw new BadRequestException("Function Not Available");
            }
        }

        [HttpGet("{id:int}/download")]
        public async Task<object> GetDownload(int id, string link, string file)
        {
            var indexerDef = _indexerFactory.Get(id);
            var indexer = _indexerFactory.GetInstance(indexerDef);

            if (link.IsNullOrWhiteSpace() || file.IsNullOrWhiteSpace())
            {
                throw new BadRequestException("Invalid Prowlarr link");
            }

            file = WebUtility.UrlDecode(file);

            if (indexer == null)
            {
                throw new NotFoundException("Indexer Not Found");
            }

            var source = UserAgentParser.ParseSource(Request.Headers["User-Agent"]);

            var unprotectedlLink = _downloadMappingService.ConvertToNormalLink(link);

            // If Indexer is set to download via Redirect then just redirect to the link
            if (indexer.SupportsRedirect && indexerDef.Redirect)
            {
                _downloadService.RecordRedirect(unprotectedlLink, id, source, file);
                return RedirectPermanent(unprotectedlLink);
            }

            var downloadBytes = Array.Empty<byte>();
            downloadBytes = await _downloadService.DownloadReport(unprotectedlLink, id, source, file);

            // handle magnet URLs
            if (downloadBytes.Length >= 7
                && downloadBytes[0] == 0x6d
                && downloadBytes[1] == 0x61
                && downloadBytes[2] == 0x67
                && downloadBytes[3] == 0x6e
                && downloadBytes[4] == 0x65
                && downloadBytes[5] == 0x74
                && downloadBytes[6] == 0x3a)
            {
                var magnetUrl = Encoding.UTF8.GetString(downloadBytes);
                return RedirectPermanent(magnetUrl);
            }

            var contentType = indexer.Protocol == DownloadProtocol.Torrent ? "application/x-bittorrent" : "application/x-nzb";
            var extension = indexer.Protocol == DownloadProtocol.Torrent ? "torrent" : "nzb";
            var filename = $"{file}.{extension}";

            return File(downloadBytes, contentType, filename);
        }
    }
}
