using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class SubsPlease : TorrentIndexerBase<NoAuthTorrentBaseSettings>
    {
        public override string Name => "SubsPlease";
        public override string[] IndexerUrls => new[]
        {
            "https://subsplease.org/",
            "https://subsplease.mrunblock.bond/",
            "https://subsplease.nocensor.click/"
        };
        public override string[] LegacyUrls => new[]
        {
            "https://subsplease.nocensor.space/"
        };
        public override string Language => "en-US";
        public override string Description => "SubsPlease - A better HorribleSubs/Erai replacement";
        public override Encoding Encoding => Encoding.UTF8;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public SubsPlease(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new SubsPleaseRequestGenerator(Settings);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new SubsPleaseParser(Settings);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q
                }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.TVAnime);
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.MoviesOther);

            return caps;
        }
    }

    public class SubsPleaseRequestGenerator : IIndexerRequestGenerator
    {
        private readonly NoAuthTorrentBaseSettings _settings;

        public SubsPleaseRequestGenerator(NoAuthTorrentBaseSettings settings)
        {
            _settings = settings;
        }

        private IEnumerable<IndexerRequest> GetSearchRequests(string term)
        {
            var searchUrl = $"{_settings.BaseUrl.TrimEnd('/')}/api/?";

            var searchTerm = Regex.Replace(term, "\\[?SubsPlease\\]?\\s*", string.Empty, RegexOptions.IgnoreCase).Trim();

            // If the search terms contain a resolution, remove it from the query sent to the API
            var resMatch = Regex.Match(searchTerm, "\\d{3,4}[p|P]");
            if (resMatch.Success)
            {
                searchTerm = searchTerm.Replace(resMatch.Value, string.Empty);
            }

            var queryParameters = new NameValueCollection
            {
                { "f", "search" },
                { "tz", "UTC" },
                { "s", searchTerm }
            };

            var request = new IndexerRequest(searchUrl + queryParameters.GetQueryString(), HttpAccept.Json);

            yield return request;
        }

        private IEnumerable<IndexerRequest> GetRssRequest()
        {
            var searchUrl = $"{_settings.BaseUrl.TrimEnd('/')}/api/?";

            var queryParameters = new NameValueCollection
            {
                { "f", "latest" },
                { "tz", "UTC" }
            };

            var request = new IndexerRequest(searchUrl + queryParameters.GetQueryString(), HttpAccept.Json);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(searchCriteria.IsRssSearch
                ? GetRssRequest()
                : GetSearchRequests(searchCriteria.SanitizedTvSearchString));

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

            pageableRequests.Add(searchCriteria.IsRssSearch
                ? GetRssRequest()
                : GetSearchRequests(searchCriteria.SanitizedSearchTerm));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class SubsPleaseParser : IParseIndexerResponse
    {
        private static readonly Regex RegexSize = new (@"\&xl=(?<size>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly NoAuthTorrentBaseSettings _settings;

        public SubsPleaseParser(NoAuthTorrentBaseSettings settings)
        {
            _settings = settings;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from indexer request");
            }

            // When there are no results, the API returns an empty array or empty response instead of an object
            if (string.IsNullOrWhiteSpace(indexerResponse.Content) || indexerResponse.Content == "[]")
            {
                return torrentInfos;
            }

            var jsonResponse = new HttpResponse<Dictionary<string, SubPleaseRelease>>(indexerResponse.HttpResponse);

            foreach (var value in jsonResponse.Resource.Values)
            {
                foreach (var d in value.Downloads)
                {
                    var release = new TorrentInfo
                    {
                        InfoUrl = _settings.BaseUrl + $"shows/{value.Page}/",
                        PublishDate = value.ReleaseDate.LocalDateTime,
                        Files = 1,
                        Categories = new List<IndexerCategory> { NewznabStandardCategory.TVAnime },
                        Seeders = 1,
                        Peers = 2,
                        MinimumRatio = 1,
                        MinimumSeedTime = 172800, // 48 hours
                        DownloadVolumeFactor = 0,
                        UploadVolumeFactor = 1
                    };

                    if (value.Episode.ToLowerInvariant() == "movie")
                    {
                        release.Categories.Add(NewznabStandardCategory.MoviesOther);
                    }

                    // Ex: [SubsPlease] Shingeki no Kyojin (The Final Season) - 64 (1080p)
                    release.Title += $"[SubsPlease] {value.Show} - {value.Episode} ({d.Resolution}p)";
                    release.MagnetUrl = d.Magnet;
                    release.DownloadUrl = null;
                    release.Guid = d.Magnet;
                    release.Size = GetReleaseSize(d);

                    torrentInfos.Add(release);
                }
            }

            return torrentInfos.ToArray();
        }

        private static long GetReleaseSize(SubPleaseDownloadInfo info)
        {
            if (info.Magnet.IsNotNullOrWhiteSpace())
            {
                var sizeMatch = RegexSize.Match(info.Magnet);

                if (sizeMatch.Success &&
                    long.TryParse(sizeMatch.Groups["size"].Value, out var releaseSize)
                    && releaseSize > 0)
                {
                    return releaseSize;
                }
            }

            // The API doesn't tell us file size, so give an estimate based on resolution
            return info.Resolution switch
            {
                "1080" => 1.3.Gigabytes(),
                "720" => 700.Megabytes(),
                "480" => 350.Megabytes(),
                _ => 1.Gigabytes()
            };
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class SubPleaseRelease
    {
        public string Time { get; set; }

        [JsonProperty("release_date")]
        public DateTimeOffset ReleaseDate { get; set; }
        public string Show { get; set; }
        public string Episode { get; set; }
        public SubPleaseDownloadInfo[] Downloads { get; set; }
        public string Xdcc { get; set; }
        public string ImageUrl { get; set; }
        public string Page { get; set; }
    }

    public class SubPleaseDownloadInfo
    {
        [JsonProperty("res")]
        public string Resolution { get; set; }
        public string Magnet { get; set; }
    }
}
