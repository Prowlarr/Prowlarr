using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class InternetArchive : TorrentIndexerBase<InternetArchiveSettings>
    {
        public override string Name => "Internet Archive";

        public override string[] IndexerUrls => new string[] { "https://archive.org/" };

        public override string Description => "Internet Archive is a non-profit library of millions of free books, movies, software, music, websites, and more.";

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;

        public override IndexerCapabilities Capabilities => SetCapabilities();

        public InternetArchive(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IParseIndexerResponse GetParser()
        {
            return new InternetArchiveParser(Settings, Capabilities.Categories);
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new InternetArchiveRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                }
            };

            // c.f. https://archive.org/services/docs/api/metadata-schema/index.html?highlight=mediatype#mediatype
            caps.Categories.AddCategoryMapping("texts", NewznabStandardCategory.Books);
            caps.Categories.AddCategoryMapping("etree", NewznabStandardCategory.Audio);
            caps.Categories.AddCategoryMapping("audio", NewznabStandardCategory.Audio);
            caps.Categories.AddCategoryMapping("movies", NewznabStandardCategory.Movies);
            caps.Categories.AddCategoryMapping("software", NewznabStandardCategory.PC);
            caps.Categories.AddCategoryMapping("image", NewznabStandardCategory.OtherMisc);
            caps.Categories.AddCategoryMapping("data", NewznabStandardCategory.Other);
            caps.Categories.AddCategoryMapping("web", NewznabStandardCategory.Other);
            caps.Categories.AddCategoryMapping("collection", NewznabStandardCategory.Other);
            caps.Categories.AddCategoryMapping("account", NewznabStandardCategory.Other);

            caps.Categories.AddCategoryMapping("other", NewznabStandardCategory.Other);
            return caps;
        }
    }

    public class InternetArchiveRequestGenerator : IIndexerRequestGenerator
    {
        public InternetArchiveSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public InternetArchiveRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string searchTerm, SearchCriteriaBase searchCriteria)
        {
            var query = "format:(\"Archive BitTorrent\")";

            if (searchTerm.IsNotNullOrWhiteSpace())
            {
                if (Settings.TitleOnly)
                {
                    query = string.Format("title:({0}) AND {1}", searchTerm, query);
                }
                else
                {
                    query = string.Format("{0} AND {1}", searchTerm, query);
                }
            }

            var categories = Capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories);
            if (categories.Count > 0)
            {
                query = string.Format("{0} AND mediatype:({1})", query, string.Join(" OR ", categories));
            }

            string sortBy = (InternetArchiveSort)Settings.SortBy switch
            {
                InternetArchiveSort.PublicDate => "publicdate",
                InternetArchiveSort.Downloads => "downloads",
                InternetArchiveSort.Size => "item_size",
                _ => "publicdate",
            };

            string sortOrder = (InternetArchiveSortOrder)Settings.SortOrder switch
            {
                InternetArchiveSortOrder.Descending => "desc",
                InternetArchiveSortOrder.Ascending => "asc",
                _ => "desc",
            };

            var parameters = new NameValueCollection
            {
                { "q", query },
                { "fields", "btih,downloads,identifier,item_size,mediatype,publicdate,title" },
                { "count", searchCriteria.Limit.GetValueOrDefault(100).ToString() }, // API default is 5000, don't think thats viable at all.
                { "sorts", string.Format("{0} {1}", sortBy, sortOrder) }
            };

            var searchUrl = string.Format("{0}/services/search/v1/scrape?{1}", Settings.BaseUrl.TrimEnd('/'), parameters.GetQueryString());
            var request = new IndexerRequest(searchUrl, HttpAccept.Json);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria));
            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria));
            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class InternetArchiveParser : IParseIndexerResponse
    {
        private readonly InternetArchiveSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }

        public InternetArchiveParser(InternetArchiveSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from API request");
            }

            if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from API request, expected {HttpAccept.Json.Value}");
            }

            var jsonResponse = new HttpResponse<InternetArchiveResponse>(indexerResponse.HttpResponse);

            foreach (var searchResult in jsonResponse.Resource.SearchResults)
            {
                var title = searchResult.Title ?? searchResult.Identifier;

                var downloadUrl = string.Format("{0}/download/{1}/{1}_archive.torrent", _settings.BaseUrl.TrimEnd('/'), searchResult.Identifier);
                var detailsUrl = string.Format("{0}/details/{1}", _settings.BaseUrl.TrimEnd('/'), searchResult.Identifier);

                var category = _categories.MapTrackerCatToNewznab(searchResult.MediaType);

                var release = new TorrentInfo
                {
                    Categories = category,
                    CommentUrl = detailsUrl,
                    DownloadUrl = downloadUrl,
                    DownloadVolumeFactor = 0,
                    Guid = detailsUrl,
                    Grabs = searchResult.Downloads,
                    InfoHash = searchResult.InfoHash,
                    InfoUrl = detailsUrl,
                    MagnetUrl = MagnetLinkBuilder.BuildPublicMagnetLink(searchResult.InfoHash, title),
                    Peers = 2,
                    PublishDate = searchResult.PublicDate,
                    Seeders = 1,
                    Size = searchResult.Size,
                    Title = title,
                    UploadVolumeFactor = 1
                };

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }
    }

    public class InternetArchiveResponse
    {
        [JsonProperty(PropertyName = "items")]
        public List<InternetArchiveTorrent> SearchResults { get; set; }

        public string Cursor { get; set; }
        public int Count { get; set; }
        public int Total { get; set; }
    }

    public class InternetArchiveTorrent
    {
        public int Downloads { get; set; }
        public string Identifier { get; set; }

        [JsonProperty(PropertyName = "btih")]
        public string InfoHash { get; set; }
        public string MediaType { get; set; }
        public DateTime PublicDate { get; set; }

        [JsonProperty(PropertyName = "item_size")]
        public long Size { get; set; }
        public string Title { get; set; }
    }

    public class InternetArchiveSettings : IIndexerSettings
    {
        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Sort By", Type = FieldType.Select, Advanced = true, SelectOptions = typeof(InternetArchiveSort), HelpText = "Field used to sort the search results.")]
        public int SortBy { get; set; }

        [FieldDefinition(3, Label = "Sort Order", Type = FieldType.Select, Advanced = true, SelectOptions = typeof(InternetArchiveSortOrder), HelpText = "Order to use when sorting results.")]
        public int SortOrder { get; set; }

        [FieldDefinition(4, Label = "Title Only", Type = FieldType.Checkbox, Advanced = true, HelpText = "Whether to search in title only.")]
        public bool TitleOnly { get; set; }

        [FieldDefinition(5)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public InternetArchiveSettings()
        {
            SortBy = (int)InternetArchiveSort.PublicDate;
            SortOrder = (int)InternetArchiveSortOrder.Descending;
            TitleOnly = false;
        }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult();
        }
    }

    public enum InternetArchiveSort
    {
        PublicDate,
        Downloads,
        Size
    }

    public enum InternetArchiveSortOrder
    {
        Descending,
        Ascending
    }
}
