using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using FluentValidation;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common;
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
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class AnimeBytes : TorrentIndexerBase<AnimeBytesSettings>
    {
        public override string Name => "AnimeBytes";
        public override string[] IndexerUrls => new[] { "https://animebytes.tv/" };
        public override string Description => "AnimeBytes (AB) is the largest private torrent tracker that specialises in anime and anime-related content.";
        public override string Language => "en-US";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();
        public override TimeSpan RateLimit => TimeSpan.FromSeconds(4);

        public AnimeBytes(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AnimeBytesRequestGenerator(Settings, Capabilities);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new AnimeBytesParser(Settings);
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            return false;
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

            caps.Categories.AddCategoryMapping("anime[tv_series]", NewznabStandardCategory.TVAnime, "TV Series");
            caps.Categories.AddCategoryMapping("anime[tv_special]", NewznabStandardCategory.TVAnime, "TV Special");
            caps.Categories.AddCategoryMapping("anime[ova]", NewznabStandardCategory.TVAnime, "OVA");
            caps.Categories.AddCategoryMapping("anime[ona]", NewznabStandardCategory.TVAnime, "ONA");
            caps.Categories.AddCategoryMapping("anime[dvd_special]", NewznabStandardCategory.TVAnime, "DVD Special");
            caps.Categories.AddCategoryMapping("anime[bd_special]", NewznabStandardCategory.TVAnime, "BD Special");
            caps.Categories.AddCategoryMapping("anime[movie]", NewznabStandardCategory.Movies, "Movie");
            caps.Categories.AddCategoryMapping("audio", NewznabStandardCategory.Audio, "Music");
            caps.Categories.AddCategoryMapping("gamec[game]", NewznabStandardCategory.PCGames, "Game");
            caps.Categories.AddCategoryMapping("gamec[visual_novel]", NewznabStandardCategory.PCGames, "Game Visual Novel");
            caps.Categories.AddCategoryMapping("printedtype[manga]", NewznabStandardCategory.BooksComics, "Manga");
            caps.Categories.AddCategoryMapping("printedtype[oneshot]", NewznabStandardCategory.BooksComics, "Oneshot");
            caps.Categories.AddCategoryMapping("printedtype[anthology]", NewznabStandardCategory.BooksComics, "Anthology");
            caps.Categories.AddCategoryMapping("printedtype[manhwa]", NewznabStandardCategory.BooksComics, "Manhwa");
            caps.Categories.AddCategoryMapping("printedtype[light_novel]", NewznabStandardCategory.BooksComics, "Light Novel");
            caps.Categories.AddCategoryMapping("printedtype[artbook]", NewznabStandardCategory.BooksComics, "Artbook");

            return caps;
        }
    }

    public class AnimeBytesRequestGenerator : IIndexerRequestGenerator
    {
        private readonly AnimeBytesSettings _settings;
        private readonly IndexerCapabilities _capabilities;

        private static Regex YearRegex => new (@"\b((?:19|20)\d{2})$", RegexOptions.Compiled);

        public AnimeBytesRequestGenerator(AnimeBytesSettings settings, IndexerCapabilities capabilities)
        {
            _settings = settings;
            _capabilities = capabilities;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
            => GetRequestWithSearchType(searchCriteria, "anime");

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
            => GetRequestWithSearchType(searchCriteria, "music");

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
            => GetRequestWithSearchType(searchCriteria, "anime");

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
            => GetRequestWithSearchType(searchCriteria, "anime");

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
            => GetRequestWithSearchType(searchCriteria, "anime");

        private IndexerPageableRequestChain GetRequestWithSearchType(SearchCriteriaBase searchCriteria, string searchType)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest(searchCriteria, searchType));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetRequest(SearchCriteriaBase searchCriteria, string searchType)
        {
            var searchUrl = $"{_settings.BaseUrl.TrimEnd('/')}/scrape.php";

            var term = searchCriteria.SanitizedSearchTerm.Trim();
            var searchTerm = StripEpisodeNumber(term);

            var parameters = new NameValueCollection
            {
                { "username", _settings.Username },
                { "torrent_pass", _settings.Passkey },
                { "sort", "grouptime" },
                { "way", "desc" },
                { "type", searchType },
                { "searchstr", searchTerm },
                { "limit", searchTerm.IsNotNullOrWhiteSpace() ? "50" : "20" }
            };

            if (_settings.SearchByYear && searchType == "anime")
            {
                var searchYear = ParseYearFromSearchTerm(term);

                if (searchYear is > 0)
                {
                    parameters.Set("year", searchYear.ToString());
                }
            }

            var queryCats = _capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories);

            if (queryCats.Any())
            {
                queryCats.ForEach(cat => parameters.Set(cat, "1"));
            }

            if (_settings.FreeleechOnly)
            {
                parameters.Set("freeleech", "1");
            }

            if (_settings.ExcludeHentai && searchType == "anime")
            {
                parameters.Set("hentai", "0");
            }

            searchUrl += "?" + parameters.GetQueryString();

            var request = new IndexerRequest(searchUrl, HttpAccept.Json);

            yield return request;
        }

        private static string StripEpisodeNumber(string term)
        {
            // Tracer does not support searching with episode number so strip it if we have one
            term = Regex.Replace(term, @"\W(\dx)?\d?\d$", string.Empty);
            term = Regex.Replace(term, @"\W(S\d\d?E)?\d?\d$", string.Empty);
            term = Regex.Replace(term, @"\W\d+$", string.Empty);

            return term.Trim();
        }

        private static int? ParseYearFromSearchTerm(string term)
        {
            if (term.IsNullOrWhiteSpace())
            {
                return null;
            }

            var yearMatch = YearRegex.Match(term);

            if (!yearMatch.Success)
            {
                return null;
            }

            return ParseUtil.CoerceInt(yearMatch.Groups[1].Value);
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class AnimeBytesParser : IParseIndexerResponse
    {
        private readonly AnimeBytesSettings _settings;

        private readonly HashSet<string> _excludedProperties = new (StringComparer.OrdinalIgnoreCase)
        {
            "Freeleech"
        };

        public AnimeBytesParser(AnimeBytesSettings settings)
        {
            _settings = settings;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var releaseInfos = new List<ReleaseInfo>();

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from API request");
            }

            if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from API request, expected {HttpAccept.Json.Value}");
            }

            var response = JsonConvert.DeserializeObject<AnimeBytesResponse>(indexerResponse.Content);

            if (response.Matches == 0)
            {
                return releaseInfos.ToArray();
            }

            foreach (var group in response.Groups)
            {
                var categoryName = group.CategoryName;
                var description = group.Description;
                var year = group.Year;
                var groupName = group.GroupName;
                var seriesName = group.SeriesName;
                var mainTitle = WebUtility.HtmlDecode(group.FullName);

                if (seriesName.IsNotNullOrWhiteSpace())
                {
                    mainTitle = seriesName;
                }

                var synonyms = new HashSet<string>
                {
                    mainTitle
                };

                if (group.Synonymns != null && group.Synonymns.Any())
                {
                    if (_settings.AddJapaneseTitle && group.Synonymns.TryGetValue("Japanese", out var japaneseTitle) && japaneseTitle.IsNotNullOrWhiteSpace())
                    {
                        synonyms.Add(japaneseTitle.Trim());
                    }

                    if (_settings.AddRomajiTitle && group.Synonymns.TryGetValue("Romaji", out var romajiTitle) && romajiTitle.IsNotNullOrWhiteSpace())
                    {
                        synonyms.Add(romajiTitle.Trim());
                    }

                    if (_settings.AddAlternativeTitle && group.Synonymns.TryGetValue("Alternative", out var alternativeTitle) && alternativeTitle.IsNotNullOrWhiteSpace())
                    {
                        synonyms.UnionWith(alternativeTitle.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
                    }
                }

                List<IndexerCategory> categories = null;

                foreach (var torrent in group.Torrents)
                {
                    // Skip non-freeleech results when freeleech only is set
                    if (_settings.FreeleechOnly && torrent.RawDownMultiplier != 0)
                    {
                        continue;
                    }

                    var torrentId = torrent.Id;
                    var link = torrent.Link;
                    var publishDate = DateTime.ParseExact(torrent.UploadTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
                    var details = new Uri(_settings.BaseUrl + "torrent/" + torrentId + "/group");
                    var size = torrent.Size;
                    var snatched = torrent.Snatched;
                    var seeders = torrent.Seeders;
                    var leechers = torrent.Leechers;
                    var fileCount = torrent.FileCount;
                    var peers = seeders + leechers;
                    var rawDownMultiplier = torrent.RawDownMultiplier;
                    var rawUpMultiplier = torrent.RawUpMultiplier;

                    // MST with additional 5 hours per GB
                    var minimumSeedTime = 259200 + (int)(size / (int)Math.Pow(1024, 3) * 18000);

                    var properties = WebUtility.HtmlDecode(torrent.Property)
                        .Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                        .ToList();

                    properties.RemoveAll(p => _excludedProperties.Any(p.Contains));

                    if (_settings.ExcludeRaw && properties.ContainsIgnoreCase("RAW"))
                    {
                        continue;
                    }

                    var releaseInfo = _settings.EnableSonarrCompatibility && categoryName == "Anime" ? "S01" : "";
                    var editionTitle = torrent.EditionData.EditionTitle;

                    int? episode = null;
                    int? season = null;

                    if (editionTitle.IsNotNullOrWhiteSpace())
                    {
                        releaseInfo = WebUtility.HtmlDecode(editionTitle);

                        if (_settings.EnableSonarrCompatibility)
                        {
                            var simpleSeasonRegEx = new Regex(@"Season (\d+)", RegexOptions.Compiled);
                            var simpleSeasonRegExMatch = simpleSeasonRegEx.Match(releaseInfo);
                            if (simpleSeasonRegExMatch.Success)
                            {
                                season = ParseUtil.CoerceInt(simpleSeasonRegExMatch.Groups[1].Value);
                            }
                        }

                        var episodeRegEx = new Regex(@"Episode (\d+)", RegexOptions.Compiled);
                        var episodeRegExMatch = episodeRegEx.Match(releaseInfo);
                        if (episodeRegExMatch.Success)
                        {
                            episode = ParseUtil.CoerceInt(episodeRegExMatch.Groups[1].Value);
                        }
                    }

                    if (_settings.EnableSonarrCompatibility)
                    {
                        var advancedSeasonRegEx = new Regex(@"(\d+)(st|nd|rd|th) Season", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                        var advancedSeasonRegExMatch = advancedSeasonRegEx.Match(mainTitle);
                        if (advancedSeasonRegExMatch.Success)
                        {
                            season = ParseUtil.CoerceInt(advancedSeasonRegExMatch.Groups[1].Value);
                        }

                        var seasonCharactersRegEx = new Regex(@"(I{2,})$", RegexOptions.Compiled);
                        var seasonCharactersRegExMatch = seasonCharactersRegEx.Match(mainTitle);
                        if (seasonCharactersRegExMatch.Success)
                        {
                            season = seasonCharactersRegExMatch.Groups[1].Value.Length;
                        }

                        var seasonNumberRegEx = new Regex(@"([2-9])$", RegexOptions.Compiled);
                        var seasonNumberRegExMatch = seasonNumberRegEx.Match(mainTitle);
                        if (seasonNumberRegExMatch.Success)
                        {
                            season = ParseUtil.CoerceInt(seasonNumberRegExMatch.Groups[1].Value);
                        }
                    }

                    if (episode is > 0)
                    {
                        releaseInfo = $" - {episode:00}";
                    }
                    else if (_settings.EnableSonarrCompatibility && season is > 0)
                    {
                        releaseInfo = $"S{season:00}";
                    }

                    releaseInfo = releaseInfo.Trim();

                    // Ignore these categories as they'll cause hell with the matcher
                    // TV Special, DVD Special, BD Special
                    if (groupName is "TV Series" or "OVA" or "ONA")
                    {
                        categories = new List<IndexerCategory> { NewznabStandardCategory.TVAnime };
                    }

                    if (groupName is "Movie" or "Live Action Movie")
                    {
                        categories = new List<IndexerCategory> { NewznabStandardCategory.Movies };
                    }

                    if (categoryName is "Manga" or "Oneshot" or "Anthology" or "Manhwa" or "Manhua" or "Light Novel")
                    {
                        categories = new List<IndexerCategory> { NewznabStandardCategory.BooksComics };
                    }

                    if (categoryName is "Novel" or "Artbook")
                    {
                        categories = new List<IndexerCategory> { NewznabStandardCategory.BooksComics };
                    }

                    if (categoryName is "Game" or "Visual Novel")
                    {
                        if (properties.Contains("PSP"))
                        {
                            categories = new List<IndexerCategory> { NewznabStandardCategory.ConsolePSP };
                        }

                        if (properties.Contains("PS3"))
                        {
                            categories = new List<IndexerCategory> { NewznabStandardCategory.ConsolePS3 };
                        }

                        if (properties.Contains("PS Vita"))
                        {
                            categories = new List<IndexerCategory> { NewznabStandardCategory.ConsolePSVita };
                        }

                        if (properties.Contains("3DS"))
                        {
                            categories = new List<IndexerCategory> { NewznabStandardCategory.Console3DS };
                        }

                        if (properties.Contains("NDS"))
                        {
                            categories = new List<IndexerCategory> { NewznabStandardCategory.ConsoleNDS };
                        }

                        if (properties.Contains("PSX") || properties.Contains("PS2") || properties.Contains("SNES") || properties.Contains("NES") || properties.Contains("GBA") || properties.Contains("Switch"))
                        {
                            categories = new List<IndexerCategory> { NewznabStandardCategory.ConsoleOther };
                        }

                        if (properties.Contains("PC"))
                        {
                            categories = new List<IndexerCategory> { NewznabStandardCategory.PCGames };
                        }
                    }

                    if (categoryName is "Single" or "EP" or "Album" or "Compilation" or "Soundtrack" or "Remix CD" or "PV" or "Live Album" or "Image CD" or "Drama CD" or "Vocal CD")
                    {
                        if (properties.Any(p => p.Contains("Lossless")))
                        {
                            categories = new List<IndexerCategory> { NewznabStandardCategory.AudioLossless };
                        }
                        else if (properties.Any(p => p.Contains("MP3")))
                        {
                            categories = new List<IndexerCategory> { NewznabStandardCategory.AudioMP3 };
                        }
                        else
                        {
                            categories = new List<IndexerCategory> { NewznabStandardCategory.AudioOther };
                        }
                    }

                    // We don't actually have a release name >.> so try to create one
                    var releaseGroup = properties.LastOrDefault(p => !p.ContainsIgnoreCase("Hentai"));

                    if (releaseGroup.IsNotNullOrWhiteSpace() && releaseGroup.Contains('(') && releaseGroup.Contains(')'))
                    {
                        var start = releaseGroup.IndexOf("(", StringComparison.Ordinal);
                        releaseGroup = "[" + releaseGroup.Substring(start + 1, releaseGroup.IndexOf(")", StringComparison.Ordinal) - 1 - start) + "] ";
                    }
                    else
                    {
                        releaseGroup = string.Empty;
                    }

                    var infoString = properties.Select(p => "[" + p + "]").Join(string.Empty);

                    if (_settings.UseFilenameForSingleEpisodes && torrent.FileCount == 1)
                    {
                        var fileName = torrent.Files.First().FileName;

                        var guid = new Uri(details + "?nh=" + HashUtil.CalculateMd5(fileName));

                        var release = new TorrentInfo
                        {
                            MinimumRatio = 1,
                            MinimumSeedTime = minimumSeedTime,
                            Title = fileName,
                            InfoUrl = details.AbsoluteUri,
                            Guid = guid.AbsoluteUri,
                            DownloadUrl = link.AbsoluteUri,
                            PublishDate = publishDate,
                            Categories = categories,
                            Description = description,
                            Size = size,
                            Seeders = seeders,
                            Peers = peers,
                            Grabs = snatched,
                            Files = fileCount,
                            DownloadVolumeFactor = rawDownMultiplier,
                            UploadVolumeFactor = rawUpMultiplier,
                        };

                        releaseInfos.Add(release);

                        continue;
                    }

                    foreach (var title in synonyms)
                    {
                        var releaseTitle = groupName is "Movie" or "Live Action Movie" ?
                            $"{title} {year} {releaseGroup}{infoString}" :
                            $"{releaseGroup}{title} {releaseInfo} {infoString}";

                        var guid = new Uri(details + "?nh=" + HashUtil.CalculateMd5(title));

                        var release = new TorrentInfo
                        {
                            MinimumRatio = 1,
                            MinimumSeedTime = minimumSeedTime,
                            Title = releaseTitle.Trim(),
                            InfoUrl = details.AbsoluteUri,
                            Guid = guid.AbsoluteUri,
                            DownloadUrl = link.AbsoluteUri,
                            PublishDate = publishDate,
                            Categories = categories,
                            Description = description,
                            Size = size,
                            Seeders = seeders,
                            Peers = peers,
                            Grabs = snatched,
                            Files = fileCount,
                            DownloadVolumeFactor = rawDownMultiplier,
                            UploadVolumeFactor = rawUpMultiplier,
                        };

                        releaseInfos.Add(release);
                    }
                }
            }

            return releaseInfos
                .OrderByDescending(o => o.PublishDate)
                .ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class AnimeBytesSettingsValidator : NoAuthSettingsValidator<AnimeBytesSettings>
    {
        public AnimeBytesSettingsValidator()
        {
            RuleFor(c => c.Username).NotEmpty();

            RuleFor(c => c.Passkey).NotEmpty()
                .Must(x => x.Length is 32 or 48)
                .WithMessage("Passkey length must be 32 or 48");
        }
    }

    public class AnimeBytesSettings : NoAuthTorrentBaseSettings
    {
        private static readonly AnimeBytesSettingsValidator Validator = new ();

        public AnimeBytesSettings()
        {
            Username = "";
            Passkey = "";
            FreeleechOnly = false;
            ExcludeRaw = false;
            ExcludeHentai = false;
            SearchByYear = false;
            EnableSonarrCompatibility = true;
            UseFilenameForSingleEpisodes = false;
            AddJapaneseTitle = true;
            AddRomajiTitle = true;
            AddAlternativeTitle = true;
        }

        [FieldDefinition(2, Label = "Username", HelpText = "Site Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(3, Label = "Passkey", HelpText = "Site Passkey", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Passkey { get; set; }

        [FieldDefinition(4, Label = "Freeleech Only", Type = FieldType.Checkbox, HelpText = "Search freeleech torrents only")]
        public bool FreeleechOnly { get; set; }

        [FieldDefinition(5, Label = "Exclude RAW", Type = FieldType.Checkbox, HelpText = "Exclude RAW torrents from results")]
        public bool ExcludeRaw { get; set; }

        [FieldDefinition(6, Label = "Exclude Hentai", Type = FieldType.Checkbox, HelpText = "Exclude Hentai torrents from results")]
        public bool ExcludeHentai { get; set; }

        [FieldDefinition(7, Label = "Search By Year", Type = FieldType.Checkbox, HelpText = "Makes Prowlarr to search by year as a different argument in the request.")]
        public bool SearchByYear { get; set; }

        [FieldDefinition(8, Label = "Enable Sonarr Compatibility", Type = FieldType.Checkbox, HelpText = "Makes Prowlarr try to add Season information into Release names, without this Sonarr can't match any Seasons, but it has a lot of false positives as well")]
        public bool EnableSonarrCompatibility { get; set; }

        [FieldDefinition(9, Label = "Use Filenames for Single Episodes", Type = FieldType.Checkbox, HelpText = "Makes Prowlarr replace AnimeBytes release names with the actual filename, this currently only works for single episode releases")]
        public bool UseFilenameForSingleEpisodes { get; set; }

        [FieldDefinition(10, Label = "Add Japanese title as a synonym", Type = FieldType.Checkbox, HelpText = "Makes Prowlarr add Japanese titles as synonyms, i.e kanji/hiragana/katakana.")]
        public bool AddJapaneseTitle { get; set; }

        [FieldDefinition(11, Label = "Add Romaji title as a synonym", Type = FieldType.Checkbox, HelpText = "Makes Prowlarr add Romaji title as a synonym, i.e \"Shingeki no Kyojin\" with Attack on Titan")]
        public bool AddRomajiTitle { get; set; }

        [FieldDefinition(12, Label = "Add alternative title as a synonym", Type = FieldType.Checkbox, HelpText = "Makes Prowlarr add alternative title as a synonym, i.e \"AoT\" with Attack on Titan, but also \"Attack on Titan Season 4\" Instead of \"Attack on Titan: The Final Season\"")]
        public bool AddAlternativeTitle { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public class AnimeBytesResponse
    {
        [JsonProperty("Matches")]
        public int Matches { get; set; }

        [JsonProperty("Groups")]
        public AnimeBytesGroup[] Groups { get; set; }
    }

    public class AnimeBytesGroup
    {
        [JsonProperty("ID")]
        public long Id { get; set; }

        [JsonProperty("CategoryName")]
        public string CategoryName { get; set; }

        [JsonProperty("FullName")]
        public string FullName { get; set; }

        [JsonProperty("GroupName")]
        public string GroupName { get; set; }

        [JsonProperty("SeriesID")]
        public long? SeriesId { get; set; }

        [JsonProperty("SeriesName")]
        public string SeriesName { get; set; }

        [JsonProperty("Year")]
        public int? Year { get; set; }

        [JsonProperty("Image")]
        public string Image { get; set; }

        [JsonProperty("SynonymnsV2", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Synonymns { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("DescriptionHTML")]
        public string DescriptionHtml { get; set; }

        [JsonProperty("Tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("Torrents")]
        public List<AnimeBytesTorrent> Torrents { get; set; }
    }

    public class AnimeBytesTorrent
    {
        [JsonProperty("ID")]
        public long Id { get; set; }

        [JsonProperty("EditionData")]
        public AnimeBytesEditionData EditionData { get; set; }

        [JsonProperty("RawDownMultiplier")]
        public double RawDownMultiplier { get; set; }

        [JsonProperty("RawUpMultiplier")]
        public double RawUpMultiplier { get; set; }

        [JsonProperty("Link")]
        public Uri Link { get; set; }

        [JsonProperty("Property")]
        public string Property { get; set; }

        [JsonProperty("Snatched")]
        public int Snatched { get; set; }

        [JsonProperty("Seeders")]
        public int Seeders { get; set; }

        [JsonProperty("Leechers")]
        public int Leechers { get; set; }

        [JsonProperty("Size")]
        public long Size { get; set; }

        [JsonProperty("FileCount")]
        public int FileCount { get; set; }

        [JsonProperty("FileList")]
        public List<AnimeBytesFile> Files  { get; set; }

        [JsonProperty("UploadTime")]
        public string UploadTime { get; set; }
    }

    public class AnimeBytesFile
    {
        [JsonProperty("filename")]
        public string FileName { get; set; }

        [JsonProperty("size")]
        public string FileSize { get; set; }
    }

    public class AnimeBytesEditionData
    {
        [JsonProperty("EditionTitle")]
        public string EditionTitle { get; set; }
    }
}
