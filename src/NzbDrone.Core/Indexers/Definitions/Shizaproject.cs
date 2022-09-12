using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
using FluentValidation;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class Shizaproject : TorrentIndexerBase<NoAuthTorrentBaseSettings>
    {
        public override string Name => "ShizaProject";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public Shizaproject(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new ShizaprojectRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new ShizaprojectParser(Settings, Capabilities.Categories);
        }
    }

    public class ShizaprojectRequestGenerator : IIndexerRequestGenerator
    {
        public NoAuthTorrentBaseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public ShizaprojectRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
        {
            var variables = new
            {
                // Number of fetched releases (required parameter) TODO: consider adding pagination
                first = 50,

                // Remove season and episode info from search term cause it breaks search
                query = Regex.Replace(term, @"(?:[SsEe]?\d{1,4}){1,2}$", "").TrimEnd()
            };

            var query = @"
            query fetchReleases($first: Int, $query: String) {
                releases(first: $first, query: $query) {
                    edges {
                        node {
                            name
                            type
                            originalName
                            alternativeNames
                            publishedAt
                            slug
                            torrents {
                                downloaded
                                seeders
                                leechers
                                size
                                magnetUri
                                updatedAt
                                file {
                                    url
                                }
                                videoQualities
                            }
                        }
                    }
                }
            }";

            var queryCollection = new NameValueCollection
            {
                { "query", query.Replace('\n', ' ').Trim() },
                { "variables", Newtonsoft.Json.JsonConvert.SerializeObject(variables) }
            };

            var requestUrl = string.Format("{0}/graphql?", Settings.BaseUrl.TrimEnd('/')) + queryCollection.GetQueryString();

            var request = new IndexerRequest(requestUrl, HttpAccept.Json);
            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        // Shizaproject doesn't support music, but this function required by interface
        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        // Shizaproject doesn't support books, but this function required by interface
        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class ShizaprojectParser : IParseIndexerResponse
    {
        private readonly NoAuthTorrentBaseSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public ShizaprojectParser(NoAuthTorrentBaseSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        private string composeTitle(ShizaprojectNode n, ShizaprojectTorrent tr)
        {
            var title = string.Format("{0} / {1}", n.Name, n.OriginalName);
            foreach (var tl in n.AlternativeNames)
            {
                title += " / " + tl;
            }

            title += " [";
            foreach (var q in tr.VideoQualities)
            {
                title += " " + q;
            }

            title += " ]";
            return title;
        }

        private DateTime getActualPublishDate(ShizaprojectNode n, ShizaprojectTorrent t)
        {
            if (n.PublishedAt == null)
            {
                return t.UpdatedAt;
            }
            else
            {
                return (t.UpdatedAt > n.PublishedAt) ? t.UpdatedAt : n.PublishedAt.Value;
            }
        }

        private string getResolution(string[] qualities)
        {
            var resPrefix = "RESOLUTION_";
            var res = Array.Find(qualities, s => s.StartsWith(resPrefix));
            return res != null ? res.Replace(resPrefix, "") : "Unknown";
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();
            var response = JsonConvert.DeserializeObject<ShizaprojectReleasesResponse>(indexerResponse.Content);

            if (response.Errors != null && response.Errors.Length > 0)
            {
                var message = "Errors:\n";
                foreach (var e in response.Errors)
                {
                    message += string.Format("{0} - {1}\n", e.Extensions.Code, e.Message);
                }

                throw new IndexerException(indexerResponse, message);
            }

            foreach (var e in response.Data.Releases.Edges)
            {
                foreach (var tr in e.Node.Torrents)
                {
                    var torrentInfo = new TorrentInfo
                    {
                        Title = composeTitle(e.Node, tr),
                        InfoUrl = string.Format("{0}/releases/{1}/", _settings.BaseUrl.TrimEnd('/'), e.Node.Slug),
                        DownloadVolumeFactor = 0,
                        UploadVolumeFactor = 1,
                        Seeders = tr.Seeders,
                        Peers = tr.Leechers + tr.Seeders,
                        Grabs = tr.Downloaded,
                        Categories = _categories.MapTrackerCatDescToNewznab(e.Node.Type),
                        PublishDate = getActualPublishDate(e.Node, tr),
                        Guid = tr.File.Url,
                        DownloadUrl = tr.File.Url,
                        MagnetUrl = tr.MagnetUri,
                        Size = tr.Size,
                        Resolution = getResolution(tr.VideoQualities)
                    };

                    torrentInfos.Add(torrentInfo);
                }
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class ShizaprojectReleasesResponse
    {
        public ShizaprojectData Data { get; set; }
        public ShizaprojectError[] Errors { get; set; }
    }

    public class ShizaprojectError
    {
        public ShizaprojectErrorExtensions Extensions { get; set; }
        public string Message { get; set; }
    }

    public class ShizaprojectErrorExtensions
    {
        public string Code { get; set; }
    }

    public class ShizaprojectData
    {
        public ShizaprojectReleases Releases { get; set; }
    }

    public class ShizaprojectReleases
    {
        public ShizaprojectEdge[] Edges { get; set; }
    }

    public class ShizaprojectEdge
    {
        public ShizaprojectNode Node { get; set; }
    }

    public class ShizaprojectNode
    {
        public string Name { get; set; }
        public string OriginalName { get; set; }
        public string[] AlternativeNames { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string Slug { get; set; }
        public ShizaprojectTorrent[] Torrents { get; set; }
        public string Type { get; set; }
    }

    public class ShizaprojectFile
    {
        public string Url { get; set; }
    }

    public class ShizaprojectTorrent
    {
        public int Downloaded { get; set; }
        public int Seeders { get; set; }
        public int Leechers { get; set; }
        public long Size { get; set; }
        public string MagnetUri { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string[] VideoQualities { get; set; }
        public ShizaprojectFile File { get; set; }
    }
}
