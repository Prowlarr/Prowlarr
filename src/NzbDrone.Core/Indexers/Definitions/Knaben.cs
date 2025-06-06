using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;
using static Newtonsoft.Json.Formatting;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class Knaben : TorrentIndexerBase<NoAuthTorrentBaseSettings>
    {
        public override string Name => "Knaben";
        public override string[] IndexerUrls => new[] { "https://knaben.org/" };
        public override string[] LegacyUrls => new[] { "https://knaben.eu/" };
        public override string Description => "Knaben is a Public torrent meta-search engine";
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public Knaben(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new KnabenRequestGenerator(Capabilities);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new KnabenParser(Capabilities.Categories);
        }

        private static IndexerCapabilities SetCapabilities()
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

            caps.Categories.AddCategoryMapping(1000000, NewznabStandardCategory.Audio, "Audio");
            caps.Categories.AddCategoryMapping(1001000, NewznabStandardCategory.AudioMP3, "MP3");
            caps.Categories.AddCategoryMapping(1002000, NewznabStandardCategory.AudioLossless, "Lossless");
            caps.Categories.AddCategoryMapping(1003000, NewznabStandardCategory.AudioAudiobook, "Audiobook");
            caps.Categories.AddCategoryMapping(1004000, NewznabStandardCategory.AudioVideo, "Audio Video");
            caps.Categories.AddCategoryMapping(1005000, NewznabStandardCategory.AudioOther, "Radio");
            caps.Categories.AddCategoryMapping(1006000, NewznabStandardCategory.AudioOther, "Audio Other");
            caps.Categories.AddCategoryMapping(2000000, NewznabStandardCategory.TV, "TV");
            caps.Categories.AddCategoryMapping(2001000, NewznabStandardCategory.TVHD, "TV HD");
            caps.Categories.AddCategoryMapping(2002000, NewznabStandardCategory.TVSD, "TV SD");
            caps.Categories.AddCategoryMapping(2003000, NewznabStandardCategory.TVUHD, "TV UHD");
            caps.Categories.AddCategoryMapping(2004000, NewznabStandardCategory.TVDocumentary, "Documentary");
            caps.Categories.AddCategoryMapping(2005000, NewznabStandardCategory.TVForeign, "TV Foreign");
            caps.Categories.AddCategoryMapping(2006000, NewznabStandardCategory.TVSport, "Sport");
            caps.Categories.AddCategoryMapping(2007000, NewznabStandardCategory.TVOther, "Cartoon");
            caps.Categories.AddCategoryMapping(2008000, NewznabStandardCategory.TVOther, "TV Other");
            caps.Categories.AddCategoryMapping(3000000, NewznabStandardCategory.Movies, "Movies");
            caps.Categories.AddCategoryMapping(3001000, NewznabStandardCategory.MoviesHD, "Movies HD");
            caps.Categories.AddCategoryMapping(3002000, NewznabStandardCategory.MoviesSD, "Movies SD");
            caps.Categories.AddCategoryMapping(3003000, NewznabStandardCategory.MoviesUHD, "Movies UHD");
            caps.Categories.AddCategoryMapping(3004000, NewznabStandardCategory.MoviesDVD, "Movies DVD");
            caps.Categories.AddCategoryMapping(3005000, NewznabStandardCategory.MoviesForeign, "Movies Foreign");
            caps.Categories.AddCategoryMapping(3006000, NewznabStandardCategory.MoviesForeign, "Movies Bollywood");
            caps.Categories.AddCategoryMapping(3007000, NewznabStandardCategory.Movies3D, "Movies 3D");
            caps.Categories.AddCategoryMapping(3008000, NewznabStandardCategory.MoviesOther, "Movies Other");
            caps.Categories.AddCategoryMapping(4000000, NewznabStandardCategory.PC, "PC");
            caps.Categories.AddCategoryMapping(4001000, NewznabStandardCategory.PCGames, "Games");
            caps.Categories.AddCategoryMapping(4002000, NewznabStandardCategory.PC0day, "Software");
            caps.Categories.AddCategoryMapping(4003000, NewznabStandardCategory.PCMac, "Mac");
            caps.Categories.AddCategoryMapping(4004000, NewznabStandardCategory.PCISO, "Unix");
            caps.Categories.AddCategoryMapping(5000000, NewznabStandardCategory.XXX, "XXX");
            caps.Categories.AddCategoryMapping(5001000, NewznabStandardCategory.XXXx264, "XXX Video");
            caps.Categories.AddCategoryMapping(5002000, NewznabStandardCategory.XXXImageSet, "XXX ImageSet");
            caps.Categories.AddCategoryMapping(5003000, NewznabStandardCategory.XXXOther, "XXX Games");
            caps.Categories.AddCategoryMapping(5004000, NewznabStandardCategory.XXXOther, "XXX Hentai");
            caps.Categories.AddCategoryMapping(5005000, NewznabStandardCategory.XXXOther, "XXX Other");
            caps.Categories.AddCategoryMapping(6000000, NewznabStandardCategory.TVAnime, "Anime");
            caps.Categories.AddCategoryMapping(6001000, NewznabStandardCategory.TVAnime, "Anime Subbed");
            caps.Categories.AddCategoryMapping(6002000, NewznabStandardCategory.TVAnime, "Anime Dubbed");
            caps.Categories.AddCategoryMapping(6003000, NewznabStandardCategory.TVAnime, "Anime Dual audio");
            caps.Categories.AddCategoryMapping(6004000, NewznabStandardCategory.TVAnime, "Anime Raw");
            caps.Categories.AddCategoryMapping(6005000, NewznabStandardCategory.AudioVideo, "Music Video");
            caps.Categories.AddCategoryMapping(6006000, NewznabStandardCategory.BooksOther, "Literature");
            caps.Categories.AddCategoryMapping(6007000, NewznabStandardCategory.AudioOther, "Music");
            caps.Categories.AddCategoryMapping(6008000, NewznabStandardCategory.TVAnime, "Anime non-english translated");
            caps.Categories.AddCategoryMapping(7000000, NewznabStandardCategory.Console, "Console");
            caps.Categories.AddCategoryMapping(7001000, NewznabStandardCategory.ConsolePS4, "PS4");
            caps.Categories.AddCategoryMapping(7002000, NewznabStandardCategory.ConsolePS3, "PS3");
            caps.Categories.AddCategoryMapping(7003000, NewznabStandardCategory.ConsolePS3, "PS2");
            caps.Categories.AddCategoryMapping(7004000, NewznabStandardCategory.ConsolePS3, "PS1");
            caps.Categories.AddCategoryMapping(7005000, NewznabStandardCategory.ConsolePSVita, "PS Vita");
            caps.Categories.AddCategoryMapping(7006000, NewznabStandardCategory.ConsolePSP, "PSP");
            caps.Categories.AddCategoryMapping(7007000, NewznabStandardCategory.ConsoleXBox360, "Xbox 360");
            caps.Categories.AddCategoryMapping(7008000, NewznabStandardCategory.ConsoleXBox, "Xbox");
            caps.Categories.AddCategoryMapping(7009000, NewznabStandardCategory.ConsoleNDS, "Switch");
            caps.Categories.AddCategoryMapping(7010000, NewznabStandardCategory.ConsoleNDS, "NDS");
            caps.Categories.AddCategoryMapping(7011000, NewznabStandardCategory.ConsoleWii, "Wii");
            caps.Categories.AddCategoryMapping(7012000, NewznabStandardCategory.ConsoleWiiU, "WiiU");
            caps.Categories.AddCategoryMapping(7013000, NewznabStandardCategory.Console3DS, "3DS");
            caps.Categories.AddCategoryMapping(7014000, NewznabStandardCategory.ConsoleWii, "GameCube");
            caps.Categories.AddCategoryMapping(7015000, NewznabStandardCategory.ConsoleOther, "Other");
            caps.Categories.AddCategoryMapping(8000000, NewznabStandardCategory.PCMobileOther, "Mobile");
            caps.Categories.AddCategoryMapping(8001000, NewznabStandardCategory.PCMobileAndroid, "Android");
            caps.Categories.AddCategoryMapping(8002000, NewznabStandardCategory.PCMobileiOS, "IOS");
            caps.Categories.AddCategoryMapping(8003000, NewznabStandardCategory.PCMobileOther, "PC Other");
            caps.Categories.AddCategoryMapping(9000000, NewznabStandardCategory.Books, "Books");
            caps.Categories.AddCategoryMapping(9001000, NewznabStandardCategory.BooksEBook, "EBooks");
            caps.Categories.AddCategoryMapping(9002000, NewznabStandardCategory.BooksComics, "Comics");
            caps.Categories.AddCategoryMapping(9003000, NewznabStandardCategory.BooksMags, "Magazines");
            caps.Categories.AddCategoryMapping(9004000, NewznabStandardCategory.BooksTechnical, "Technical");
            caps.Categories.AddCategoryMapping(9005000, NewznabStandardCategory.BooksOther, "Books Other");
            caps.Categories.AddCategoryMapping(10000000, NewznabStandardCategory.Other, "Other");
            caps.Categories.AddCategoryMapping(10001000, NewznabStandardCategory.OtherMisc, "Other Misc");

            return caps;
        }
    }

    public class KnabenRequestGenerator : IIndexerRequestGenerator
    {
        private const string ApiSearchEndpoint = "https://api.knaben.org/v1";

        private readonly IndexerCapabilities _capabilities;

        public KnabenRequestGenerator(IndexerCapabilities capabilities)
        {
            _capabilities = capabilities;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(CreateRequest(searchCriteria, searchCriteria.SanitizedSearchTerm));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(CreateRequest(searchCriteria, searchCriteria.SanitizedSearchTerm));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(CreateRequest(searchCriteria, searchCriteria.SanitizedTvSearchString));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(CreateRequest(searchCriteria, searchCriteria.SanitizedSearchTerm));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(CreateRequest(searchCriteria, searchCriteria.SanitizedSearchTerm));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> CreateRequest(SearchCriteriaBase searchCriteria, string searchTerm)
        {
            var body = new Dictionary<string, object>
            {
                { "order_by", "date" },
                { "order_direction", "desc" },
                { "from", 0 },
                { "size", 100 },
                { "hide_unsafe", true }
            };

            var searchQuery = searchTerm.Trim();

            if (searchQuery.IsNotNullOrWhiteSpace())
            {
                body.Add("search_type", "100%");
                body.Add("search_field", "title");
                body.Add("query", searchQuery);
            }

            var categories = _capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories);

            if (categories is { Count: > 0 })
            {
                body.Add("categories", categories.Select(int.Parse).Distinct().ToArray());
            }

            var request = new HttpRequest(ApiSearchEndpoint, HttpAccept.Json)
            {
                Headers =
                {
                    ContentType = "application/json"
                },
                Method = HttpMethod.Post
            };
            request.SetContent(body.ToJson());
            request.ContentSummary = body.ToJson(None);

            yield return new IndexerRequest(request);
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class KnabenParser : IParseIndexerResponse
    {
        private static readonly Regex DateTimezoneRegex = new(@"[+-]\d{2}:\d{2}$", RegexOptions.Compiled);

        private readonly IndexerCapabilitiesCategories _categories;

        public KnabenParser(IndexerCapabilitiesCategories categories)
        {
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var indexerHttpResponse = indexerResponse.HttpResponse;

            if (indexerHttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerHttpResponse.StatusCode} code from indexer request");
            }

            if (!indexerHttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerHttpResponse.Headers.ContentType} from indexer request, expected {HttpAccept.Json.Value}");
            }

            var releaseInfos = new List<ReleaseInfo>();

            var jsonResponse = STJson.Deserialize<KnabenResponse>(indexerResponse.Content);

            if (jsonResponse?.Hits == null)
            {
                return releaseInfos;
            }

            var rows = jsonResponse.Hits.Where(r => r.Seeders > 0).ToList();

            foreach (var row in rows)
            {
                // Not all entries have the TZ in the "date" field
                var publishDate = row.Date.IsNotNullOrWhiteSpace() && !DateTimezoneRegex.IsMatch(row.Date) ? $"{row.Date}+01:00" : row.Date;

                var releaseInfo = new TorrentInfo
                {
                    Guid = row.InfoUrl,
                    Title = row.Title,
                    InfoUrl = row.InfoUrl,
                    DownloadUrl = row.DownloadUrl.IsNotNullOrWhiteSpace() ? row.DownloadUrl : null,
                    MagnetUrl = row.MagnetUrl.IsNotNullOrWhiteSpace() ? row.MagnetUrl : null,
                    Categories = row.CategoryIds.SelectMany(cat => _categories.MapTrackerCatToNewznab(cat.ToString())).Distinct().ToList(),
                    InfoHash = row.InfoHash,
                    Size = row.Size,
                    Seeders = row.Seeders,
                    Peers = row.Leechers + row.Seeders,
                    PublishDate = DateTime.Parse(publishDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1
                };

                releaseInfos.Add(releaseInfo);
            }

            // order by date
            return releaseInfos;
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    internal sealed class KnabenResponse
    {
        public IReadOnlyCollection<KnabenRelease> Hits { get; init; } = Array.Empty<KnabenRelease>();
    }

    internal sealed class KnabenRelease
    {
        public string Title { get; init; }

        [JsonPropertyName("categoryId")]
        public IReadOnlyCollection<int> CategoryIds { get; init; } = Array.Empty<int>();

        [JsonPropertyName("hash")]
        public string InfoHash { get; init; }

        [JsonPropertyName("details")]
        public string InfoUrl { get; init; }

        [JsonPropertyName("link")]
        public string DownloadUrl { get; init; }

        public string MagnetUrl { get; init; }

        [JsonPropertyName("bytes")]
        public long Size { get; init; }

        public int Seeders { get; init; }

        [JsonPropertyName("peers")]
        public int Leechers { get; init; }

        public string Date { get; init; }
    }
}
