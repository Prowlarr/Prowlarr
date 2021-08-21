using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
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
        private IIndexerLimitService _indexerLimitService { get; set; }
        private IDownloadMappingService _downloadMappingService { get; set; }
        private IDownloadService _downloadService { get; set; }

        public NewznabController(IndexerFactory indexerFactory,
            ISearchForNzb nzbSearchService,
            IIndexerLimitService indexerLimitService,
            IDownloadMappingService downloadMappingService,
            IDownloadService downloadService)
        {
            _indexerFactory = indexerFactory;
            _nzbSearchService = nzbSearchService;
            _indexerLimitService = indexerLimitService;
            _downloadMappingService = downloadMappingService;
            _downloadService = downloadService;
        }

        [HttpGet("/api/v1/indexer/{id:int}/newznab")]
        [HttpGet("{id:int}/api")]
        public async Task<IActionResult> GetNewznabResponse(int id, [FromQuery] NewznabRequest request)
        {
            var requestType = request.t;
            request.source = UserAgentParser.ParseSource(Request.Headers["User-Agent"]);
            request.server = Request.GetServerUrl();
            request.host = Request.GetHostName();

            if (requestType.IsNullOrWhiteSpace())
            {
                return Content(CreateErrorXML(200, "Missing parameter (t)"), "application/rss+xml");
            }

            request.imdbid = request.imdbid?.TrimStart('t') ?? null;

            if (request.imdbid.IsNotNullOrWhiteSpace())
            {
                if (!int.TryParse(request.imdbid, out var imdb) || imdb == 0)
                {
                    return Content(CreateErrorXML(201, "Incorrect parameter (imdbid)"), "application/rss+xml");
                }
            }

            if (id == 0)
            {
                switch (requestType)
                {
                    case "caps":
                        var caps = new IndexerCapabilities
                        {
                            TvSearchParams = new List<TvSearchParam>
                                   {
                                       TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                                   },
                            MovieSearchParams = new List<MovieSearchParam>
                                   {
                                       MovieSearchParam.Q
                                   },
                            MusicSearchParams = new List<MusicSearchParam>
                                   {
                                       MusicSearchParam.Q
                                   },
                            BookSearchParams = new List<BookSearchParam>
                                   {
                                       BookSearchParam.Q
                                   }
                        };

                        foreach (var cat in NewznabStandardCategory.AllCats)
                        {
                            caps.Categories.AddCategoryMapping(1, cat);
                        }

                        return Content(caps.ToXml(), "application/rss+xml");
                    case "search":
                    case "tvsearch":
                    case "music":
                    case "book":
                    case "movie":
                        var results = new NewznabResults();
                        results.Releases = new List<ReleaseInfo>
                        {
                            new ReleaseInfo
                            {
                                Title = "Test Release",
                                Guid = "https://prowlarr.com",
                                DownloadUrl = "https://prowlarr.com",
                                PublishDate = DateTime.Now
                            }
                        };

                        return Content(results.ToXml(DownloadProtocol.Usenet), "application/rss+xml");
                }
            }

            var indexerDef = _indexerFactory.Get(id);

            if (indexerDef == null)
            {
                throw new NotFoundException("Indexer Not Found");
            }

            var indexer = _indexerFactory.GetInstance(indexerDef);

            //TODO Optimize this so it's not called here and in NzbSearchService (for manual search)
            if (_indexerLimitService.AtQueryLimit(indexerDef))
            {
                return Content(CreateErrorXML(500, $"Request limit reached ({((IIndexerSettings)indexer.Definition.Settings).BaseSettings.QueryLimit})"), "application/rss+xml");
            }

            switch (requestType)
            {
                case "caps":
                    var caps = indexer.GetCapabilities();
                    return Content(caps.ToXml(), "application/rss+xml");
                case "search":
                case "tvsearch":
                case "music":
                case "book":
                case "movie":
                    var results = await _nzbSearchService.Search(request, new List<int> { indexerDef.Id }, false);

                    foreach (var result in results.Releases)
                    {
                        result.DownloadUrl = result.DownloadUrl.IsNotNullOrWhiteSpace() ? _downloadMappingService.ConvertToProxyLink(new Uri(result.DownloadUrl), request.server, indexerDef.Id, result.Title).ToString() : null;

                        if (result.DownloadProtocol == DownloadProtocol.Torrent)
                        {
                            ((TorrentInfo)result).MagnetUrl = ((TorrentInfo)result).MagnetUrl.IsNotNullOrWhiteSpace() ? _downloadMappingService.ConvertToProxyLink(new Uri(((TorrentInfo)result).MagnetUrl), request.server, indexerDef.Id, result.Title).ToString() : null;
                        }
                    }

                    return Content(results.ToXml(indexer.Protocol), "application/rss+xml");
                default:
                    return Content(CreateErrorXML(202, $"No such function ({requestType})"), "application/rss+xml");
            }
        }

        [HttpGet("/api/v1/indexer/{id:int}/download")]
        [HttpGet("{id:int}/download")]
        public async Task<object> GetDownload(int id, string link, string file)
        {
            var indexerDef = _indexerFactory.Get(id);
            var indexer = _indexerFactory.GetInstance(indexerDef);

            if (_indexerLimitService.AtDownloadLimit(indexerDef))
            {
                throw new BadRequestException("Grab limit reached");
            }

            if (link.IsNullOrWhiteSpace() || file.IsNullOrWhiteSpace())
            {
                throw new BadRequestException("Invalid Prowlarr link");
            }

            file = WebUtility.UrlDecode(file);

            if (indexerDef == null)
            {
                throw new NotFoundException("Indexer Not Found");
            }

            var source = UserAgentParser.ParseSource(Request.Headers["User-Agent"]);
            var host = Request.GetHostName();

            var unprotectedlLink = _downloadMappingService.ConvertToNormalLink(link);

            // If Indexer is set to download via Redirect then just redirect to the link
            if (indexer.SupportsRedirect && indexerDef.Redirect)
            {
                _downloadService.RecordRedirect(unprotectedlLink, id, source, host, file);
                return RedirectPermanent(unprotectedlLink);
            }

            var downloadBytes = Array.Empty<byte>();
            downloadBytes = await _downloadService.DownloadReport(unprotectedlLink, id, source, host, file);

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

        public static string CreateErrorXML(int code, string description)
        {
            var xdoc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("error",
                    new XAttribute("code", code.ToString()),
                    new XAttribute("description", description)));

            return xdoc.Declaration + Environment.NewLine + xdoc;
        }
    }
}
