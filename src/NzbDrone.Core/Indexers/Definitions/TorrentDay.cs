using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
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
    public class TorrentDay : TorrentIndexerBase<TorrentDaySettings>
    {
        public override string Name => "TorrentDay";

        public override string BaseUrl => "https://torrentday.cool/";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public TorrentDay(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new TorrentDayRequestGenerator() { Settings = Settings, Capabilities = Capabilities, BaseUrl = BaseUrl };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new TorrentDayParser(Settings, Capabilities.Categories, BaseUrl);
        }

        protected override IDictionary<string, string> GetCookies()
        {
            return CookieUtil.CookieHeaderToDictionary(Settings.Cookie);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                       },
                MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId
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

            caps.Categories.AddCategoryMapping(29, NewznabStandardCategory.TVAnime, "Anime");
            caps.Categories.AddCategoryMapping(28, NewznabStandardCategory.PC, "Appz/Packs");
            caps.Categories.AddCategoryMapping(42, NewznabStandardCategory.AudioAudiobook, "Audio Books");
            caps.Categories.AddCategoryMapping(20, NewznabStandardCategory.Books, "Books");
            caps.Categories.AddCategoryMapping(30, NewznabStandardCategory.TVDocumentary, "Documentary");
            caps.Categories.AddCategoryMapping(47, NewznabStandardCategory.Other, "Fonts");
            caps.Categories.AddCategoryMapping(43, NewznabStandardCategory.PCMac, "Mac");

            caps.Categories.AddCategoryMapping(96, NewznabStandardCategory.MoviesUHD, "Movie/4K");
            caps.Categories.AddCategoryMapping(25, NewznabStandardCategory.MoviesSD, "Movies/480p");
            caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.MoviesBluRay, "Movies/Bluray");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.MoviesBluRay, "Movies/Bluray-Full");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.MoviesDVD, "Movies/DVD-R");
            caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.MoviesSD, "Movies/MP4");
            caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.MoviesForeign, "Movies/Non-English");
            caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.Movies, "Movies/Packs");
            caps.Categories.AddCategoryMapping(44, NewznabStandardCategory.MoviesSD, "Movies/SD/x264");
            caps.Categories.AddCategoryMapping(48, NewznabStandardCategory.Movies, "Movies/x265");
            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.MoviesSD, "Movies/XviD");

            caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.AudioMP3, "Music/Audio");
            caps.Categories.AddCategoryMapping(23, NewznabStandardCategory.AudioForeign, "Music/Non-English");
            caps.Categories.AddCategoryMapping(41, NewznabStandardCategory.Audio, "Music/Packs");
            caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.AudioVideo, "Music/Video");
            caps.Categories.AddCategoryMapping(27, NewznabStandardCategory.Audio, "Music/Flac");

            caps.Categories.AddCategoryMapping(45, NewznabStandardCategory.AudioOther, "Podcast");

            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.PCGames, "PC/Games");
            caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.ConsolePS3, "PS3");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.ConsolePSP, "PSP");
            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.ConsoleWii, "Wii");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.ConsoleXBox360, "Xbox-360");

            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.TVSD, "TV/480p");
            caps.Categories.AddCategoryMapping(32, NewznabStandardCategory.TVHD, "TV/Bluray");
            caps.Categories.AddCategoryMapping(31, NewznabStandardCategory.TVSD, "TV/DVD-R");
            caps.Categories.AddCategoryMapping(33, NewznabStandardCategory.TVSD, "TV/DVD-Rip");
            caps.Categories.AddCategoryMapping(46, NewznabStandardCategory.TVSD, "TV/Mobile");
            caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.TV, "TV/Packs");
            caps.Categories.AddCategoryMapping(26, NewznabStandardCategory.TVSD, "TV/SD/x264");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.TVHD, "TV/x264");
            caps.Categories.AddCategoryMapping(34, NewznabStandardCategory.TVUHD, "TV/x265");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVSD, "TV/XviD");

            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.XXX, "XXX/Movies");
            caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.XXXPack, "XXX/Packs");

            return caps;
        }
    }

    public class TorrentDayRequestGenerator : IIndexerRequestGenerator
    {
        public TorrentDaySettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }
        public string BaseUrl { get; set; }

        public TorrentDayRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var searchUrl = BaseUrl + "t.json";

            var cats = Capabilities.Categories.MapTorznabCapsToTrackers(categories);
            if (cats.Count == 0)
            {
                cats = Capabilities.Categories.GetTrackerCategories();
            }

            var catStr = string.Join(";", cats);
            searchUrl = searchUrl + "?" + catStr;

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                searchUrl += ";q=" + imdbId;
            }
            else
            {
                searchUrl += ";q=" + term.UrlEncode(Encoding.UTF8);
            }

            var request = new IndexerRequest(searchUrl, HttpAccept.Rss);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SearchTerm), searchCriteria.Categories, searchCriteria.ImdbId));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories, searchCriteria.ImdbId));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class TorrentDayParser : IParseIndexerResponse
    {
        private readonly TorrentDaySettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        private readonly string _baseUrl;

        public TorrentDayParser(TorrentDaySettings settings, IndexerCapabilitiesCategories categories, string baseUrl)
        {
            _settings = settings;
            _categories = categories;
            _baseUrl = baseUrl;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();

            var rows = JsonConvert.DeserializeObject<dynamic>(indexerResponse.Content);

            foreach (var row in rows)
            {
                var title = (string)row.name;

                var torrentId = (long)row.t;
                var details = new Uri(_baseUrl + "details.php?id=" + torrentId);
                var seeders = (int)row.seeders;
                var imdbId = (string)row["imdb-id"];
                var downloadMultiplier = (double?)row["download-multiplier"] ?? 1;
                var link = new Uri(_baseUrl + "download.php/" + torrentId + "/" + torrentId + ".torrent");
                var publishDate = DateTimeUtil.UnixTimestampToDateTime((long)row.ctime).ToLocalTime();
                var imdb = ParseUtil.GetImdbID(imdbId) ?? 0;

                var release = new TorrentInfo
                {
                    Title = title,
                    Guid = details.AbsoluteUri,
                    DownloadUrl = link.AbsoluteUri,
                    InfoUrl = details.AbsoluteUri,
                    PublishDate = publishDate,
                    Category = _categories.MapTrackerCatToNewznab(row.c.ToString()),
                    Size = (long)row.size,
                    Files = (int)row.files,
                    Grabs = (int)row.completed,
                    Seeders = seeders,
                    Peers = seeders + (int)row.leechers,
                    ImdbId = imdb,
                    DownloadVolumeFactor = downloadMultiplier,
                    UploadVolumeFactor = 1,
                    MinimumRatio = 1,
                    MinimumSeedTime = 172800 // 48 hours
                };

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class TorrentDaySettingsValidator : AbstractValidator<TorrentDaySettings>
    {
        public TorrentDaySettingsValidator()
        {
            RuleFor(c => c.Cookie).NotEmpty();
        }
    }

    public class TorrentDaySettings : IProviderConfig
    {
        private static readonly TorrentDaySettingsValidator Validator = new TorrentDaySettingsValidator();

        public TorrentDaySettings()
        {
            Cookie = "";
        }

        [FieldDefinition(1, Label = "Cookie", HelpText = "Site Cookie")]
        public string Cookie { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
