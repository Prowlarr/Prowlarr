using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class Nebulance : TorrentIndexerBase<NebulanceSettings>
    {
        public override string Name => "Nebulance";
        public override string[] IndexerUrls => new string[] { "https://nebulance.io/" };
        public override string Description => "Nebulance (NBL) is a ratioless Private Torrent Tracker for TV";
        public override string Language => "en-US";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public Nebulance(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new NebulanceRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new NebulanceParser(Settings, Capabilities.Categories);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                    {
                        TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId, TvSearchParam.TvMazeId
                    }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.TV);
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVSD);
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.TVHD);

            return caps;
        }
    }

    public class NebulanceRequestGenerator : IIndexerRequestGenerator
    {
        public NebulanceSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public NebulanceRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(NebulanceQuery parameters, int? results, int? offset)
        {
            var apiUrl = Settings.BaseUrl + "api.php";

            var builder = new JsonRpcRequestBuilder(apiUrl)
                .Call("getTorrents", Settings.ApiKey, parameters, results ?? 100, offset ?? 0);

            builder.SuppressHttpError = true;

            yield return new IndexerRequest(builder.Build());
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var queryParams = new NebulanceQuery
            {
                Age = ">0"
            };

            if (searchCriteria.SanitizedTvSearchString.IsNotNullOrWhiteSpace())
            {
                queryParams.Name = "%" + Regex.Replace(searchCriteria.SanitizedTvSearchString, @"[ -._]", "%").Trim() + "%";
            }

            if (searchCriteria.TvMazeId.HasValue)
            {
                queryParams.Tvmaze = searchCriteria.TvMazeId.Value;

                if (searchCriteria.EpisodeSearchString.IsNotNullOrWhiteSpace())
                {
                    queryParams.Name = "%" + Regex.Replace(searchCriteria.EpisodeSearchString, @"[ -._]", "%").Trim() + "%";
                }
            }
            else if (searchCriteria.ImdbId.IsNotNullOrWhiteSpace() && int.TryParse(searchCriteria.ImdbId, out var intImdb))
            {
                queryParams.Imdb = intImdb;

                if (searchCriteria.EpisodeSearchString.IsNotNullOrWhiteSpace())
                {
                    queryParams.Name = "%" + Regex.Replace(searchCriteria.EpisodeSearchString, @"[ -._]", "%").Trim() + "%";
                }
            }

            pageableRequests.Add(GetPagedRequests(queryParams, searchCriteria.Limit, searchCriteria.Offset));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            var queryParams = new NebulanceQuery
            {
                Age = ">0"
            };

            if (searchCriteria.SanitizedSearchTerm.IsNotNullOrWhiteSpace())
            {
                queryParams.Name = "%" + Regex.Replace(searchCriteria.SanitizedSearchTerm, @"[ -._]", "%").Trim() + "%";
            }

            pageableRequests.Add(GetPagedRequests(queryParams, searchCriteria.Limit, searchCriteria.Offset));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class NebulanceParser : IParseIndexerResponse
    {
        private readonly NebulanceSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public NebulanceParser(NebulanceSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            JsonRpcResponse<NebulanceTorrents> jsonResponse = new HttpResponse<JsonRpcResponse<NebulanceTorrents>>(indexerResponse.HttpResponse).Resource;

            if (jsonResponse.Error != null || jsonResponse.Result == null)
            {
                throw new IndexerException(indexerResponse, "Indexer API call returned an error [{0}]", jsonResponse.Error);
            }

            if (jsonResponse.Result.Items.Count == 0)
            {
                return torrentInfos;
            }

            var rows = jsonResponse.Result.Items;

            foreach (var row in rows)
            {
                var details = _settings.BaseUrl + "torrents.php?id=" + row.TorrentId;

                var release = new TorrentInfo
                {
                    Title = row.ReleaseTitle,
                    Guid = details,
                    InfoUrl = details,
                    PosterUrl = row.Banner,
                    DownloadUrl = row.Download,
                    Categories = new List<IndexerCategory> { TvCategoryFromQualityParser.ParseTvShowQuality(row.ReleaseTitle) },
                    Size = ParseUtil.CoerceLong(row.Size),
                    Files = row.FileList.Length,
                    PublishDate = DateTime.Parse(row.PublishDateUtc, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                    Grabs = ParseUtil.CoerceInt(row.Snatch),
                    Seeders = ParseUtil.CoerceInt(row.Seed),
                    Peers = ParseUtil.CoerceInt(row.Seed) + ParseUtil.CoerceInt(row.Leech),
                    MinimumRatio = 0, // ratioless
                    MinimumSeedTime = row.Category.ToLower() == "season" ? 432000 : 86400, // 120 hours for seasons and 24 hours for episodes
                    DownloadVolumeFactor = 0, // ratioless tracker
                    UploadVolumeFactor = 1
                };

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class NebulanceSettings : NoAuthTorrentBaseSettings
    {
        public NebulanceSettings()
        {
            ApiKey = "";
        }

        [FieldDefinition(4, Label = "API Key", HelpText = "API Key from User Settings > Api Keys. Key must have List and Download permissions")]
        public string ApiKey { get; set; }
    }

    public class NebulanceQuery
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Id { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Time { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Age { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Tvmaze { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Imdb { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Hash { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string[] Tags { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Name { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Category { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Series { get; set; }

        public NebulanceQuery Clone()
        {
            return MemberwiseClone() as NebulanceQuery;
        }
    }

    public class NebulanceTorrent
    {
        [JsonProperty(PropertyName = "rls_name")]
        public string ReleaseTitle { get; set; }
        public string Title { get; set; }
        [JsonProperty(PropertyName = "cat")]
        public string Category { get; set; }
        public string Size { get; set; }
        public string Seed { get; set; }
        public string Leech { get; set; }
        public string Snatch { get; set; }
        public string Download { get; set; }
        [JsonProperty(PropertyName = "file_list")]
        public string[] FileList { get; set; }
        [JsonProperty(PropertyName = "series_banner")]
        public string Banner { get; set; }
        [JsonProperty(PropertyName = "group_id")]
        public string TorrentId { get; set; }
        [JsonProperty(PropertyName = "rls_utc")]
        public string PublishDateUtc { get; set; }
    }

    public class NebulanceTorrents
    {
        public List<NebulanceTorrent> Items { get; set; }
        public int Results { get; set; }
    }
}
