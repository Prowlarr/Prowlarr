using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Parser;
using NzbDrone.Http.Extensions;
using Prowlarr.Http.Extensions;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1.Indexers
{
    public class IndexerModule : ProviderModuleBase<IndexerResource, IIndexer, IndexerDefinition>
    {
        private IIndexerFactory _indexerFactory { get; set; }
        private ISearchForNzb _nzbSearchService { get; set; }
        private IDownloadMappingService _downloadMappingService { get; set; }
        private IDownloadService _downloadService { get; set; }

        public IndexerModule(IndexerFactory indexerFactory, ISearchForNzb nzbSearchService, IDownloadMappingService downloadMappingService, IDownloadService downloadService, IndexerResourceMapper resourceMapper)
            : base(indexerFactory, "indexer", resourceMapper)
        {
            _indexerFactory = indexerFactory;
            _nzbSearchService = nzbSearchService;
            _downloadMappingService = downloadMappingService;
            _downloadService = downloadService;

            Get("{id}/newznab", x =>
            {
                var request = this.Bind<NewznabRequest>();
                return GetNewznabResponse(request);
            });
            Get("{id}/download", x =>
            {
                return GetDownload(x.id);
            });
        }

        protected override void Validate(IndexerDefinition definition, bool includeWarnings)
        {
            if (!definition.Enable)
            {
                return;
            }

            base.Validate(definition, includeWarnings);
        }

        private object GetNewznabResponse(NewznabRequest request)
        {
            var requestType = request.t;
            request.source = UserAgentParser.ParseSource(Request.Headers.UserAgent);

            if (requestType.IsNullOrWhiteSpace())
            {
                throw new BadRequestException("Missing Function Parameter");
            }

            var indexer = _indexerFactory.Get(request.id);

            if (indexer == null)
            {
                throw new NotFoundException("Indexer Not Found");
            }

            var indexerInstance = _indexerFactory.GetInstance(indexer);
            var serverUrl = Request.GetServerUrl();

            switch (requestType)
            {
                case "caps":
                    Response response = indexerInstance.GetCapabilities().ToXml();
                    response.ContentType = "application/rss+xml";
                    return response;
                case "search":
                case "tvsearch":
                case "music":
                case "book":
                case "movie":
                    var results = _nzbSearchService.Search(request, new List<int> { indexer.Id }, false);

                    foreach (var result in results.Releases)
                    {
                        result.DownloadUrl = _downloadMappingService.ConvertToProxyLink(new Uri(result.DownloadUrl), serverUrl, indexer.Id, result.Title).ToString();
                    }

                    Response searchResponse = results.ToXml(indexerInstance.Protocol);
                    searchResponse.ContentType = "application/rss+xml";
                    return searchResponse;
                default:
                    throw new BadRequestException("Function Not Available");
            }
        }

        private object GetDownload(int id)
        {
            var indexer = _indexerFactory.Get(id);
            var link = Request.Query.Link;
            var file = Request.Query.File;

            if (!link.HasValue || !file.HasValue)
            {
                throw new BadRequestException("Invalid Prowlarr link");
            }

            file = WebUtility.UrlDecode(file);

            if (indexer == null)
            {
                throw new NotFoundException("Indexer Not Found");
            }

            var source = UserAgentParser.ParseSource(Request.Headers.UserAgent);

            var unprotectedlLink = _downloadMappingService.ConvertToNormalLink((string)link.Value);

            // If Indexer is set to download via Redirect then just redirect to the link
            if (indexer.SupportsRedirect && indexer.Redirect)
            {
                _downloadService.RecordRedirect(unprotectedlLink, id, source, file);
                return Response.AsRedirect(unprotectedlLink, RedirectResponse.RedirectType.Temporary);
            }

            var downloadBytes = Array.Empty<byte>();
            downloadBytes = _downloadService.DownloadReport(unprotectedlLink, id, source, file);

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
                return Response.AsRedirect(magnetUrl, RedirectResponse.RedirectType.Temporary);
            }

            var contentType = indexer.Protocol == DownloadProtocol.Torrent ? "application/x-bittorrent" : "application/x-nzb";
            var extension = indexer.Protocol == DownloadProtocol.Torrent ? "torrent" : "nzb";
            var filename = $"{file}.{extension}";

            return Response.FromByteArray(downloadBytes, contentType).AsAttachment(filename, contentType);
        }
    }
}
