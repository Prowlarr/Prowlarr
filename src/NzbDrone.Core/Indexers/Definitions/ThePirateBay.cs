using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using FluentValidation;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class ThePirateBay : HttpIndexerBase<ThePirateBaySettings>
    {
        public override string Name => "ThePirateBay";
        public override string BaseUrl => "https://thepiratebay.org/";
        public override string Language => "en-us";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public ThePirateBay(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new ThePirateBayRequestGenerator() { Capabilities = Capabilities, BaseUrl = BaseUrl };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new ThePirateBayParser(Capabilities.Categories, BaseUrl);
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

            caps.Categories.AddCategoryMapping(100, NewznabStandardCategory.Audio, "Audio");
            caps.Categories.AddCategoryMapping(101, NewznabStandardCategory.Audio, "Music");
            caps.Categories.AddCategoryMapping(102, NewznabStandardCategory.AudioAudiobook, "Audio Books");
            caps.Categories.AddCategoryMapping(103, NewznabStandardCategory.Audio, "Sound Clips");
            caps.Categories.AddCategoryMapping(104, NewznabStandardCategory.AudioLossless, "FLAC");
            caps.Categories.AddCategoryMapping(199, NewznabStandardCategory.AudioOther, "Audio Other");
            caps.Categories.AddCategoryMapping(200, NewznabStandardCategory.Movies, "Video");
            caps.Categories.AddCategoryMapping(201, NewznabStandardCategory.Movies, "Movies");
            caps.Categories.AddCategoryMapping(202, NewznabStandardCategory.Movies, "Movies");
            caps.Categories.AddCategoryMapping(203, NewznabStandardCategory.AudioVideo, "Music Videos");
            caps.Categories.AddCategoryMapping(204, NewznabStandardCategory.MoviesOther, "Movie Clips");
            caps.Categories.AddCategoryMapping(205, NewznabStandardCategory.TV, "TV");
            caps.Categories.AddCategoryMapping(206, NewznabStandardCategory.TVOther, "Handheld");
            caps.Categories.AddCategoryMapping(207, NewznabStandardCategory.MoviesHD, "HD - Movies");
            caps.Categories.AddCategoryMapping(208, NewznabStandardCategory.TVHD, "HD - TV shows");
            caps.Categories.AddCategoryMapping(209, NewznabStandardCategory.Movies3D, "3D");
            caps.Categories.AddCategoryMapping(299, NewznabStandardCategory.MoviesOther, "Video Other");
            caps.Categories.AddCategoryMapping(300, NewznabStandardCategory.PC, "Applications");
            caps.Categories.AddCategoryMapping(301, NewznabStandardCategory.PC, "Windows");
            caps.Categories.AddCategoryMapping(302, NewznabStandardCategory.PCMac, "Mac");
            caps.Categories.AddCategoryMapping(303, NewznabStandardCategory.PC, "UNIX");
            caps.Categories.AddCategoryMapping(304, NewznabStandardCategory.PCMobileOther, "Handheld");
            caps.Categories.AddCategoryMapping(305, NewznabStandardCategory.PCMobileiOS, "IOS (iPad/iPhone)");
            caps.Categories.AddCategoryMapping(306, NewznabStandardCategory.PCMobileAndroid, "Android");
            caps.Categories.AddCategoryMapping(399, NewznabStandardCategory.PC, "Other OS");
            caps.Categories.AddCategoryMapping(400, NewznabStandardCategory.Console, "Games");
            caps.Categories.AddCategoryMapping(401, NewznabStandardCategory.PCGames, "PC");
            caps.Categories.AddCategoryMapping(402, NewznabStandardCategory.PCMac, "Mac");
            caps.Categories.AddCategoryMapping(403, NewznabStandardCategory.ConsolePS4, "PSx");
            caps.Categories.AddCategoryMapping(404, NewznabStandardCategory.ConsoleXBox, "XBOX360");
            caps.Categories.AddCategoryMapping(405, NewznabStandardCategory.ConsoleWii, "Wii");
            caps.Categories.AddCategoryMapping(406, NewznabStandardCategory.ConsoleOther, "Handheld");
            caps.Categories.AddCategoryMapping(407, NewznabStandardCategory.ConsoleOther, "IOS (iPad/iPhone)");
            caps.Categories.AddCategoryMapping(408, NewznabStandardCategory.ConsoleOther, "Android");
            caps.Categories.AddCategoryMapping(499, NewznabStandardCategory.ConsoleOther, "Games Other");
            caps.Categories.AddCategoryMapping(500, NewznabStandardCategory.XXX, "Porn");
            caps.Categories.AddCategoryMapping(501, NewznabStandardCategory.XXX, "Movies");
            caps.Categories.AddCategoryMapping(502, NewznabStandardCategory.XXXDVD, "Movies DVDR");
            caps.Categories.AddCategoryMapping(503, NewznabStandardCategory.XXXImageSet, "Pictures");
            caps.Categories.AddCategoryMapping(504, NewznabStandardCategory.XXX, "Games");
            caps.Categories.AddCategoryMapping(505, NewznabStandardCategory.XXX, "HD - Movies");
            caps.Categories.AddCategoryMapping(506, NewznabStandardCategory.XXX, "Movie Clips");
            caps.Categories.AddCategoryMapping(599, NewznabStandardCategory.XXXOther, "Porn other");
            caps.Categories.AddCategoryMapping(600, NewznabStandardCategory.Other, "Other");
            caps.Categories.AddCategoryMapping(601, NewznabStandardCategory.Books, "E-books");
            caps.Categories.AddCategoryMapping(602, NewznabStandardCategory.BooksComics, "Comics");
            caps.Categories.AddCategoryMapping(603, NewznabStandardCategory.Books, "Pictures");
            caps.Categories.AddCategoryMapping(604, NewznabStandardCategory.Books, "Covers");
            caps.Categories.AddCategoryMapping(605, NewznabStandardCategory.Books, "Physibles");
            caps.Categories.AddCategoryMapping(699, NewznabStandardCategory.BooksOther, "Other Other");

            return caps;
        }
    }

    public class ThePirateBayRequestGenerator : IIndexerRequestGenerator
    {
        public IndexerCapabilities Capabilities { get; set; }
        public string BaseUrl { get; set; }
        private static string ApiUrl => "https://apibay.org/";

        public ThePirateBayRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, bool rssSearch)
        {
            if (rssSearch)
            {
                yield return new IndexerRequest($"{ApiUrl.TrimEnd('/')}/precompiled/data_top100_recent.json", HttpAccept.Html);
            }
            else
            {
                var cats = Capabilities.Categories.MapTorznabCapsToTrackers(categories);

                var queryStringCategories = string.Join(
                    ",",
                    cats.Count == 0
                        ? Capabilities.Categories.GetTrackerCategories()
                        : cats);

                var queryCollection = new NameValueCollection
                {
                    { "q", term },
                    { "cat", queryStringCategories }
                };

                var searchUrl = string.Format("{0}/q.php?{1}", ApiUrl.TrimEnd('/'), queryCollection.GetQueryString());

                var request = new IndexerRequest(searchUrl, HttpAccept.Json);

                yield return request;
            }
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories, searchCriteria.RssSearch));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories, searchCriteria.RssSearch));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories, searchCriteria.RssSearch));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories, searchCriteria.RssSearch));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories, searchCriteria.RssSearch));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class ThePirateBayParser : IParseIndexerResponse
    {
        private readonly IndexerCapabilitiesCategories _categories;
        private readonly string _baseUrl;

        public ThePirateBayParser(IndexerCapabilitiesCategories categories, string baseUrl)
        {
            _categories = categories;
            _baseUrl = baseUrl;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var queryResponseItems = JsonConvert.DeserializeObject<List<ThePirateBayTorrent>>(indexerResponse.Content);

            // The API returns a single item to represent a state of no results. Avoid returning this result.
            if (queryResponseItems.Count == 1 && queryResponseItems.First().Id == 0)
            {
                return (IList<ReleaseInfo>)Enumerable.Empty<ReleaseInfo>();
            }

            foreach (var item in queryResponseItems)
            {
                var details = item.Id == 0 ? null : $"{_baseUrl}description.php?id={item.Id}";
                var imdbId = string.IsNullOrEmpty(item.Imdb) ? null : ParseUtil.GetImdbID(item.Imdb);
                var torrentItem =  new TorrentInfo
                {
                    Title = item.Name,
                    Category = _categories.MapTrackerCatToNewznab(item.Category.ToString()),
                    Guid = details,
                    InfoUrl = details,
                    InfoHash = item.InfoHash, // magnet link is auto generated from infohash
                    PublishDate = DateTimeUtil.UnixTimestampToDateTime(item.Added),
                    Seeders = item.Seeders,
                    Peers = item.Seeders + item.Leechers,
                    Size = item.Size,
                    Files = item.NumFiles,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1,
                    ImdbId = imdbId.GetValueOrDefault()
                };

                torrentInfos.Add(torrentItem);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class ThePirateBaySettings : IProviderConfig
    {
        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult();
        }
    }

    public class ThePirateBayTorrent
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("info_hash")]
        public string InfoHash { get; set; }

        [JsonProperty("leechers")]
        public int Leechers { get; set; }

        [JsonProperty("seeders")]
        public int Seeders { get; set; }

        [JsonProperty("num_files")]
        public int NumFiles { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("added")]
        public long Added { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("category")]
        public long Category { get; set; }

        [JsonProperty("imdb")]
        public string Imdb { get; set; }
    }
}
