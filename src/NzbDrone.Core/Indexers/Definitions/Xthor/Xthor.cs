using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FluentValidation;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions.Xthor
{
    public class Xthor : TorrentIndexerBase<XthorSettings>
    {
        public override string Name => "Xthor";
        public override string[] IndexerUrls => new string[] { "https://api.xthor.tk/" };
        public override string Language => "fr-fr";
        public override string Description => "Xthor is a general Private torrent site";
        public override Encoding Encoding => Encoding.GetEncoding("windows-1252");
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public override TimeSpan RateLimit => TimeSpan.FromSeconds(2.5);
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public Xthor(IHttpClient httpClient,
            IEventAggregator eventAggregator,
            IIndexerStatusService indexerStatusService,
            IConfigService configService,
            Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new XthorRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new XthorParser(Settings, Capabilities.Categories);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam> { TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep },
                MovieSearchParams = new List<MovieSearchParam> { MovieSearchParam.Q, MovieSearchParam.TmdbId },
                MusicSearchParams = new List<MusicSearchParam> { MusicSearchParam.Q },
                BookSearchParams = new List<BookSearchParam> { BookSearchParam.Q }
            };

            caps.Categories.AddCategoryMapping(118, NewznabStandardCategory.MoviesBluRay, "Films 2160p/Bluray");
            caps.Categories.AddCategoryMapping(119, NewznabStandardCategory.MoviesBluRay, "Films 2160p/Remux");
            caps.Categories.AddCategoryMapping(107, NewznabStandardCategory.MoviesUHD, "Films 2160p/x265");
            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.MoviesBluRay, "Films 1080p/BluRay");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.MoviesBluRay, "Films 1080p/Remux");
            caps.Categories.AddCategoryMapping(100, NewznabStandardCategory.MoviesHD, "Films 1080p/x265");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.MoviesHD, "Films 1080p/x264");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.MoviesHD, "Films 720p/x264");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.MoviesSD, "Films SD/x264");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.Movies3D, "Films 3D");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.MoviesSD, "Films XviD");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.MoviesDVD, "Films DVD");
            caps.Categories.AddCategoryMapping(122, NewznabStandardCategory.MoviesHD, "Films HDTV");
            caps.Categories.AddCategoryMapping(94, NewznabStandardCategory.MoviesWEBDL, "Films WEBDL");
            caps.Categories.AddCategoryMapping(95, NewznabStandardCategory.MoviesWEBDL, "Films WEBRiP");
            caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.TVDocumentary, "Films Documentaire");
            caps.Categories.AddCategoryMapping(31, NewznabStandardCategory.MoviesOther, "Films Animation");
            caps.Categories.AddCategoryMapping(33, NewznabStandardCategory.MoviesOther, "Films Spectacle");
            caps.Categories.AddCategoryMapping(125, NewznabStandardCategory.TVSport, "Films Sports");
            caps.Categories.AddCategoryMapping(20, NewznabStandardCategory.AudioVideo, "Films Concerts, Clips");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.MoviesOther, "Films VOSTFR");

            // TV / Series
            caps.Categories.AddCategoryMapping(104, NewznabStandardCategory.TVOther, "Series BluRay");
            caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.TVOther, "Series Pack VF");
            caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.TVHD, "Series HD VF");
            caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.TVSD, "Series SD VF");
            caps.Categories.AddCategoryMapping(98, NewznabStandardCategory.TVOther, "Series Pack VOSTFR");
            caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.TVHD, "Series HD VOSTFR");
            caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.TVSD, "Series SD VOSTFR");
            caps.Categories.AddCategoryMapping(101, NewznabStandardCategory.TVAnime, "Series Packs Anime");
            caps.Categories.AddCategoryMapping(32, NewznabStandardCategory.TVAnime, "Series Animes");
            caps.Categories.AddCategoryMapping(110, NewznabStandardCategory.TVAnime, "Series Anime VOSTFR");
            caps.Categories.AddCategoryMapping(123, NewznabStandardCategory.TVOther, "Series Animation");
            caps.Categories.AddCategoryMapping(109, NewznabStandardCategory.TVDocumentary, "Series DOC");
            caps.Categories.AddCategoryMapping(34, NewznabStandardCategory.TVOther, "Series Sport");
            caps.Categories.AddCategoryMapping(30, NewznabStandardCategory.TVOther, "Series Emission TV");

            // XxX / MISC
            caps.Categories.AddCategoryMapping(36, NewznabStandardCategory.XXX, "MISC XxX/Films");
            caps.Categories.AddCategoryMapping(105, NewznabStandardCategory.XXX, "MISC XxX/Séries");
            caps.Categories.AddCategoryMapping(114, NewznabStandardCategory.XXX, "MISC XxX/Lesbiennes");
            caps.Categories.AddCategoryMapping(115, NewznabStandardCategory.XXX, "MISC XxX/Gays");
            caps.Categories.AddCategoryMapping(113, NewznabStandardCategory.XXX, "MISC XxX/Hentai");
            caps.Categories.AddCategoryMapping(120, NewznabStandardCategory.XXX, "MISC XxX/Magazines");

            // Books / Livres
            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.BooksEBook, "Livres Romans");
            caps.Categories.AddCategoryMapping(124, NewznabStandardCategory.AudioAudiobook, "Livres Audio Books");
            caps.Categories.AddCategoryMapping(96, NewznabStandardCategory.BooksMags, "Livres  Magazines");
            caps.Categories.AddCategoryMapping(99, NewznabStandardCategory.BooksOther, "Livres Bandes dessinées");
            caps.Categories.AddCategoryMapping(116, NewznabStandardCategory.BooksEBook, "Livres Romans Jeunesse");
            caps.Categories.AddCategoryMapping(102, NewznabStandardCategory.BooksComics, "Livres Comics");
            caps.Categories.AddCategoryMapping(103, NewznabStandardCategory.BooksOther, "Livres Mangas");

            // SOFTWARE / Logiciels
            caps.Categories.AddCategoryMapping(25, NewznabStandardCategory.PCGames, "Logiciels Jeux PC");
            caps.Categories.AddCategoryMapping(27, NewznabStandardCategory.ConsolePS3, "Logiciels Playstation");
            caps.Categories.AddCategoryMapping(111, NewznabStandardCategory.PCMac, "Logiciels Jeux MAC");
            caps.Categories.AddCategoryMapping(26, NewznabStandardCategory.ConsoleXBox360, "Logiciels XboX");
            caps.Categories.AddCategoryMapping(112, NewznabStandardCategory.PC, "Logiciels Jeux Linux");
            caps.Categories.AddCategoryMapping(28, NewznabStandardCategory.ConsoleWii, "Logiciels Nintendo");
            caps.Categories.AddCategoryMapping(29, NewznabStandardCategory.ConsoleNDS, "Logiciels NDS");
            caps.Categories.AddCategoryMapping(117, NewznabStandardCategory.PC, "Logiciels ROM");
            caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.PC, "Logiciels Applis PC");
            caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.PCMac, "Logiciels Applis Mac");
            caps.Categories.AddCategoryMapping(23, NewznabStandardCategory.PCMobileAndroid, "Logiciels Smartphone");

            return caps;
        }
    }

    public class XthorRequestGenerator : IIndexerRequestGenerator
    {
        public XthorSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public XthorRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term,
            int[] categories,
            int pageNumber,
            string tmdbid = null)
        {
            var searchUrl = string.Format("{0}", Settings.BaseUrl.TrimEnd('/'));

            var searchString = term;

            var trackerCats = Capabilities.Categories.MapTorznabCapsToTrackers(categories) ?? new List<string>();

            var queryCollection = new NameValueCollection();

            queryCollection.Add("passkey", Settings.Passkey);

            if (tmdbid.IsNotNullOrWhiteSpace())
            {
                queryCollection.Add("tmdbid", tmdbid);
            }
            else if (!string.IsNullOrWhiteSpace(searchString))
            {
                searchString = searchString.Replace("'", ""); // ignore ' (e.g. search for america's Next Top Model)
                if (Settings.EnhancedAnime &&
                    (trackerCats.Contains("101") || trackerCats.Contains("32") || trackerCats.Contains("110")))
                {
                    var regex = new Regex(" ([0-9]+)");
                    searchString = regex.Replace(searchString, " E$1");
                }

                queryCollection.Add("search", searchString);
            }

            if (Settings.FreeleechOnly)
            {
                queryCollection.Add("freeleech", "1");
            }

            if (trackerCats.Count >= 1)
            {
                queryCollection.Add("category", string.Join("+", trackerCats));
            }

            if (Settings.Accent >= 1)
            {
                queryCollection.Add("accent", Settings.Accent.ToString());
            }

            queryCollection.Add("page", pageNumber.ToString());

            searchUrl = searchUrl + "?" + queryCollection.GetQueryString();

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequestsCommon(SearchCriteriaBase searchCriteria,
            string searchTerm,
            string tmdbid = null)
        {
            var pageableRequests = new IndexerPageableRequestChain();
            var actualPage = 0;

            while (actualPage < Settings.MaxPages)
            {
                pageableRequests.Add(GetPagedRequests(searchTerm, searchCriteria.Categories, actualPage, tmdbid));

                if (tmdbid.IsNotNullOrWhiteSpace() && Settings.ByPassPageForTmDbid)
                {
                    break;
                }

                ++actualPage;
            }

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            return GetSearchRequestsCommon(searchCriteria,
                string.Format("{0}", searchCriteria.SanitizedSearchTerm),
                searchCriteria.TmdbId.ToString());
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return GetSearchRequestsCommon(searchCriteria, string.Format("{0}", searchCriteria.SanitizedSearchTerm));
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            return GetSearchRequestsCommon(searchCriteria,
                string.Format("{0}", searchCriteria.SanitizedTvSearchString));
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return GetSearchRequestsCommon(searchCriteria, string.Format("{0}", searchCriteria.SanitizedSearchTerm));
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            return GetSearchRequestsCommon(searchCriteria, string.Format("{0}", searchCriteria.SanitizedSearchTerm));
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class XthorParser : IParseIndexerResponse
    {
        private readonly XthorSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;
        private string _torrentDetailsUrl;

        public XthorParser(XthorSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
            _torrentDetailsUrl = _settings.BaseUrl + "details.php?id={id}";
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();
            var contentString = indexerResponse.Content;
            var xthorResponse = JsonConvert.DeserializeObject<XthorResponse>(contentString);

            if (xthorResponse != null)
            {
                CheckApiState(xthorResponse.Error);

                // If contains torrents
                if (xthorResponse.Torrents != null)
                {
                    // Adding each torrent row to releases
                    // Exclude hidden torrents (category 106, example => search 'yoda' in the API) #10407
                    torrentInfos.AddRange(xthorResponse.Torrents
                        .Where(torrent => torrent.Category != 106).Select(torrent =>
                        {
                            if (_settings.NeedMultiReplacement)
                            {
                                var regex = new Regex("(?i)([\\.\\- ])MULTI([\\.\\- ])");
                                torrent.Name = regex.Replace(torrent.Name,
                                    "$1" + _settings.MultiReplacement + "$2");
                            }

                            // issue #8759 replace vostfr and subfrench with English
                            if (!string.IsNullOrEmpty(_settings.SubReplacement))
                            {
                                torrent.Name = torrent.Name.Replace("VOSTFR", _settings.SubReplacement)
                                    .Replace("SUBFRENCH", _settings.SubReplacement);
                            }

                            var publishDate = DateTimeUtil.UnixTimestampToDateTime(torrent.Added);

                            var guid = new string(_torrentDetailsUrl.Replace("{id}", torrent.Id.ToString()));
                            var details = new string(_torrentDetailsUrl.Replace("{id}", torrent.Id.ToString()));
                            var link = new string(torrent.Download_link);
                            var release = new TorrentInfo
                            {
                                // Mapping data
                                Categories = new List<IndexerCategory>(torrent.Category),
                                Title = torrent.Name,
                                Seeders = torrent.Seeders,
                                Peers = torrent.Seeders + torrent.Leechers,
                                MinimumRatio = 1,
                                MinimumSeedTime = 345600,
                                PublishDate = publishDate,
                                Size = torrent.Size,
                                Grabs = torrent.Times_completed,
                                Files = torrent.Numfiles,
                                UploadVolumeFactor = 1,
                                DownloadVolumeFactor = torrent.Freeleech == 1 ? 0 : 1,
                                Guid = guid,
                                InfoUrl = details,
                                DownloadUrl = link,
                                TmdbId = torrent.Tmdb_id
                            };

                            return release;
                        }));
                }
            }

            return torrentInfos.ToArray();
        }

        private void CheckApiState(XthorError state)
        {
            // Switch on state
            switch (state.Code)
            {
                case 0:
                    // Everything OK
                    break;
                case 1:
                    // Passkey not found
                    throw new Exception("Passkey not found in tracker's database");
                case 2:
                    // No results
                    break;
                case 3:
                    // Power Saver
                    break;
                case 4:
                    // DDOS Attack, API disabled
                    throw new Exception("Tracker is under DDOS attack, API disabled");
                case 8:
                    // AntiSpam Protection
                    throw new Exception("Triggered AntiSpam Protection, please delay your requests !");
                default:
                    // Unknown state
                    throw new Exception("Unknown state, aborting querying");
            }
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class XthorSettingsValidator : AbstractValidator<XthorSettings>
    {
    }

    public class XthorSettings : IIndexerSettings
    {
        private static readonly XthorSettingsValidator Validator = new XthorSettingsValidator();

        public XthorSettings()
        {
            BaseUrl = "https://api.xthor.tk/";
            Passkey = "";
            FreeleechOnly = false;
            Accent = 0;
            NeedMultiReplacement = false;
            MultiReplacement = "";
            SubReplacement = "";
            EnhancedAnime = true;
            ByPassPageForTmDbid = true;
            MaxPages = 1;
        }

        [FieldDefinition(1, Label = "Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Passkey", Privacy = PrivacyLevel.Password, Type = FieldType.Password, HelpText = "Site Passkey")]
        public string Passkey { get; set; }

        [FieldDefinition(3, Label = "Freeleech only", Privacy = PrivacyLevel.Normal, Type = FieldType.Checkbox, HelpText = "If you want to discover only freeleech torrents to not impact your ratio, check the related box.")]
        public bool FreeleechOnly { get; set; }

        [FieldDefinition(4, Label = "Specific language", Type = FieldType.Select, SelectOptions = typeof(XthorAccent), HelpText = "You can scope your searches with a specific language / accent.")]

        public int Accent { get; set; }

        [FieldDefinition(5, Label = "Replace MULTI keyword", Type = FieldType.Checkbox, HelpText = "Useful if you want MULTI release to be parsed as another language")]

        public bool NeedMultiReplacement { get; set; }

        [FieldDefinition(6, Label = "MULTI replacement", Type = FieldType.Textbox, HelpText = "Word used to replace \"MULTI\" keyword in release title")]

        public string MultiReplacement { get; set; }

        [FieldDefinition(7, Label = "SUB replacement", Type = FieldType.Textbox, HelpText = "Do you want to replace \"VOSTFR\" and \"SUBFRENCH\" with specific word ?")]

        public string SubReplacement { get; set; }

        [FieldDefinition(8, Label = "Do you want to use enhanced ANIME search ?", Type = FieldType.Checkbox, HelpText = "if you have \"Anime\", this will improve queries made to this tracker related to this type when making searches. (This will change the episode number to EXXX)")]

        public bool EnhancedAnime { get; set; }

        [FieldDefinition(9, Label = "Do you want to bypass max pages for TMDB searches ? (Radarr) - Hard limit of 4", Type = FieldType.Checkbox, HelpText = "(recommended) this indexer is compatible with TMDB queries (for movies only), so when requesting content with an TMDB ID, we will search directly ID on API. Results will be more accurate, so you can enable a max pages bypass for this query type.", Advanced = true)]

        public bool ByPassPageForTmDbid { get; set; }

        [FieldDefinition(10, Label = "How many pages do you want to follow ?", Type = FieldType.Select, SelectOptions = typeof(XthorPagesNumber), HelpText = "(not recommended) you can increase max pages to follow when making a request. But be aware that this API is very buggy on tracker side, most of time, results of next pages are same as the first page. Even if we deduplicate rows, you will loose performance for the same results.", Advanced = true)]

        public int MaxPages { get; set; }
        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
