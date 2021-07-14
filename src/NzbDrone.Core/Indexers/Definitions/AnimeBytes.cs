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
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NLog;
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
    public class AnimeBytes : TorrentIndexerBase<AnimeBytesSettings>
    {
        public override string Name => "AnimeBytes";
        public override string[] IndexerUrls => new string[] { "https://animebytes.tv/" };
        public override string Description => "Powered by Tentacles";
        public override string Language => "en-us";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public AnimeBytes(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AnimeBytesRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new AnimeBytesParser(Settings, Capabilities.Categories);
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
        public AnimeBytesSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public AnimeBytesRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string searchType, string term, int[] categories)
        {
            var searchUrl = string.Format("{0}/scrape.php", Settings.BaseUrl.TrimEnd('/'));

            var queryCollection = new NameValueCollection
            {
                { "username", Settings.Username },
                { "torrent_pass", Settings.Passkey },
                { "type", searchType },
                { "searchstr", term }
            };

            var queryCats = Capabilities.Categories.MapTorznabCapsToTrackers(categories);

            if (queryCats.Count > 0)
            {
                foreach (var cat in queryCats)
                {
                    queryCollection.Add(cat, "1");
                }
            }

            var queryUrl = searchUrl + "?" + queryCollection.GetQueryString();

            var request = new IndexerRequest(queryUrl, HttpAccept.Json);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests("anime", searchCriteria.SanitizedSearchTerm, searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests("music", searchCriteria.SanitizedSearchTerm, searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests("anime", searchCriteria.SanitizedSearchTerm, searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests("anime", searchCriteria.SanitizedSearchTerm, searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests("anime", searchCriteria.SanitizedSearchTerm, searchCriteria.Categories));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class AnimeBytesParser : IParseIndexerResponse
    {
        private readonly AnimeBytesSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public AnimeBytesParser(AnimeBytesSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from API request");
            }

            if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from API request, expected {HttpAccept.Json.Value}");
            }

            var response = JsonConvert.DeserializeObject<AnimeBytesResponse>(indexerResponse.Content);

            if (response.Matches > 0)
            {
                foreach (var group in response.Groups)
                {
                    var synonyms = new List<string>();
                    var year = group.Year;
                    var groupName = group.GroupName;
                    var seriesName = group.SeriesName;
                    var mainTitle = WebUtility.HtmlDecode(group.FullName);
                    if (seriesName != null)
                    {
                        mainTitle = seriesName;
                    }

                    synonyms.Add(mainTitle);

                    if (group.Synonymns.StringArray != null)
                    {
                        synonyms.AddRange(group.Synonymns.StringArray);
                    }
                    else
                    {
                        synonyms.AddRange(group.Synonymns.StringMap.Values);
                    }

                    List<IndexerCategory> category = null;
                    var categoryName = group.CategoryName;

                    var description = group.Description;

                    foreach (var torrent in group.Torrents)
                    {
                        const string defaultReleaseInfo = "S01";
                        var releaseInfo = defaultReleaseInfo;
                        string episode = null;
                        int? season = null;
                        var editionTitle = torrent.EditionData.EditionTitle;
                        if (!string.IsNullOrWhiteSpace(editionTitle))
                        {
                            releaseInfo = WebUtility.HtmlDecode(editionTitle);

                            var simpleSeasonRegEx = new Regex(@"Season (\d+)", RegexOptions.Compiled);
                            var simpleSeasonRegExMatch = simpleSeasonRegEx.Match(releaseInfo);
                            if (simpleSeasonRegExMatch.Success)
                            {
                                season = ParseUtil.CoerceInt(simpleSeasonRegExMatch.Groups[1].Value);
                            }

                            var episodeRegEx = new Regex(@"Episode (\d+)", RegexOptions.Compiled);
                            var episodeRegExMatch = episodeRegEx.Match(releaseInfo);
                            if (episodeRegExMatch.Success)
                            {
                                episode = episodeRegExMatch.Groups[1].Value;
                            }
                        }

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

                        var foundSeason = false;

                        if (season != null)
                        {
                            releaseInfo = $"Season {season}";

                            foundSeason = true;
                        }

                        if (episode != null)
                        {
                            var epString = $"Episode {episode}";

                            if (foundSeason)
                            {
                                releaseInfo += $" {epString}";
                            }
                            else
                            {
                                releaseInfo = epString;
                            }
                        }

                        releaseInfo = releaseInfo.Replace("Episode ", string.Empty);
                        releaseInfo = releaseInfo.Replace("Season ", "S");
                        releaseInfo = releaseInfo.Trim();

                        var torrentId = torrent.Id;
                        var property = torrent.Property.Replace(" | Freeleech", string.Empty);
                        var link = torrent.Link;
                        var uploadTime = torrent.UploadTime;
                        var publishDate = DateTime.SpecifyKind(uploadTime.DateTime, DateTimeKind.Utc).ToLocalTime();
                        var details = new Uri(_settings.BaseUrl + "torrent/" + torrentId + "/group");
                        var size = torrent.Size;
                        var snatched = torrent.Snatched;
                        var seeders = torrent.Seeders;
                        var leechers = torrent.Leechers;
                        var fileCount = torrent.FileCount;
                        var peers = seeders + leechers;

                        var rawDownMultiplier = torrent.RawDownMultiplier;
                        var rawUpMultiplier = torrent.RawUpMultiplier;

                        // Ignore these categories as they'll cause hell with the matcher
                        // TV Special, ONA, DVD Special, BD Special
                        if (groupName == "TV Series" || groupName == "OVA")
                        {
                            category = new List<IndexerCategory> { NewznabStandardCategory.TVAnime };
                        }

                        if (groupName == "Movie" || groupName == "Live Action Movie")
                        {
                            category = new List<IndexerCategory> { NewznabStandardCategory.Movies };
                        }

                        if (categoryName == "Manga" || categoryName == "Oneshot" || categoryName == "Anthology" || categoryName == "Manhwa" || categoryName == "Manhua" || categoryName == "Light Novel")
                        {
                            category = new List<IndexerCategory> { NewznabStandardCategory.BooksComics };
                        }

                        if (categoryName == "Novel" || categoryName == "Artbook")
                        {
                            category = new List<IndexerCategory> { NewznabStandardCategory.BooksComics };
                        }

                        if (categoryName == "Game" || categoryName == "Visual Novel")
                        {
                            if (property.Contains(" PSP "))
                            {
                                category = new List<IndexerCategory> { NewznabStandardCategory.ConsolePSP };
                            }

                            if (property.Contains("PSX"))
                            {
                                category = new List<IndexerCategory> { NewznabStandardCategory.ConsoleOther };
                            }

                            if (property.Contains(" NES "))
                            {
                                category = new List<IndexerCategory> { NewznabStandardCategory.ConsoleOther };
                            }

                            if (property.Contains(" PC "))
                            {
                                category = new List<IndexerCategory> { NewznabStandardCategory.PCGames };
                            }
                        }

                        if (categoryName == "Single" || categoryName == "EP" || categoryName == "Album" || categoryName == "Compilation" || categoryName == "Soundtrack" || categoryName == "Remix CD" || categoryName == "PV" || categoryName == "Live Album" || categoryName == "Image CD" || categoryName == "Drama CD" || categoryName == "Vocal CD")
                        {
                            if (property.Contains(" Lossless "))
                            {
                                category = new List<IndexerCategory> { NewznabStandardCategory.AudioLossless };
                            }
                            else if (property.Contains("MP3"))
                            {
                                category = new List<IndexerCategory> { NewznabStandardCategory.AudioMP3 };
                            }
                            else
                            {
                                category = new List<IndexerCategory> { NewznabStandardCategory.AudioOther };
                            }
                        }

                        // We don't actually have a release name >.> so try to create one
                        var releaseTags = property.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                        for (var i = releaseTags.Count - 1; i >= 0; i--)
                        {
                            releaseTags[i] = releaseTags[i].Trim();
                            if (string.IsNullOrWhiteSpace(releaseTags[i]))
                            {
                                releaseTags.RemoveAt(i);
                            }
                        }

                        var releaseGroup = releaseTags.LastOrDefault();
                        if (releaseGroup != null && releaseGroup.Contains("(") && releaseGroup.Contains(")"))
                        {
                            //// Skip raws if set
                            //if (releaseGroup.ToLowerInvariant().StartsWith("raw") && !AllowRaws)
                            //{
                            //    continue;
                            //}
                            var start = releaseGroup.IndexOf("(", StringComparison.Ordinal);
                            releaseGroup = "[" + releaseGroup.Substring(start + 1, (releaseGroup.IndexOf(")", StringComparison.Ordinal) - 1) - start) + "] ";
                        }
                        else
                        {
                            releaseGroup = string.Empty;
                        }

                        //if (!AllowRaws && releaseTags.Contains("raw", StringComparer.InvariantCultureIgnoreCase))
                        //{
                        //    continue;
                        //}
                        var infoString = releaseTags.Aggregate(string.Empty, (prev, cur) => prev + "[" + cur + "]");
                        var minimumSeedTime = 259200;

                        //  Additional 5 hours per GB
                        minimumSeedTime += (int)((size / 1000000000) * 18000);

                        foreach (var title in synonyms)
                        {
                            var releaseTitle = groupName == "Movie" ?
                                $"{title} {year} {releaseGroup}{infoString}" :
                                $"{releaseGroup}{title} {releaseInfo} {infoString}";

                            var guid = new Uri(details + "&nh=" + StringUtil.Hash(title));

                            var release = new TorrentInfo
                            {
                                MinimumRatio = 1,
                                MinimumSeedTime = minimumSeedTime,
                                Title = releaseTitle,
                                InfoUrl = details.AbsoluteUri,
                                Guid = guid.AbsoluteUri,
                                DownloadUrl = link.AbsoluteUri,
                                PublishDate = publishDate,
                                Categories = category,
                                Description = description,
                                Size = size,
                                Seeders = seeders,
                                Peers = peers,
                                Grabs = snatched,
                                Files = fileCount,
                                DownloadVolumeFactor = rawDownMultiplier,
                                UploadVolumeFactor = rawUpMultiplier,
                            };

                            torrentInfos.Add(release);
                        }
                    }
                }
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class AnimeBytesSettingsValidator : AbstractValidator<AnimeBytesSettings>
    {
        public AnimeBytesSettingsValidator()
        {
            RuleFor(c => c.Passkey).NotEmpty()
                                   .Must(x => x.Length == 32 || x.Length == 48)
                                   .WithMessage("Passkey length must be 32 or 48");

            RuleFor(c => c.Username).NotEmpty();
        }
    }

    public class AnimeBytesSettings : IIndexerSettings
    {
        private static readonly AnimeBytesSettingsValidator Validator = new AnimeBytesSettingsValidator();

        public AnimeBytesSettings()
        {
            Passkey = "";
            Username = "";
        }

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Passkey", Privacy = PrivacyLevel.Password, Type = FieldType.Password, HelpText = "Site Passkey")]
        public string Passkey { get; set; }

        [FieldDefinition(3, Label = "Username", HelpText = "Site Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public class AnimeBytesResponse
    {
        [JsonProperty("Matches")]
        public long Matches { get; set; }

        [JsonProperty("Limit")]
        public long Limit { get; set; }

        [JsonProperty("Results")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Results { get; set; }

        [JsonProperty("Groups")]
        public Group[] Groups { get; set; }
    }

    public class Group
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
        [JsonConverter(typeof(ParseStringConverter))]
        public long SeriesId { get; set; }

        [JsonProperty("SeriesName")]
        public string SeriesName { get; set; }

        [JsonProperty("Artists")]
        public object Artists { get; set; }

        [JsonProperty("Year")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Year { get; set; }

        [JsonProperty("Image")]
        public Uri Image { get; set; }

        [JsonProperty("Synonymns")]
        [JsonConverter(typeof(SynonymnsConverter))]
        public Synonymns Synonymns { get; set; }

        [JsonProperty("Snatched")]
        public long Snatched { get; set; }

        [JsonProperty("Comments")]
        public long Comments { get; set; }

        [JsonProperty("Links")]
        public LinksUnion Links { get; set; }

        [JsonProperty("Votes")]
        public long Votes { get; set; }

        [JsonProperty("AvgVote")]
        public double AvgVote { get; set; }

        [JsonProperty("Associations")]
        public object Associations { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("DescriptionHTML")]
        public string DescriptionHtml { get; set; }

        [JsonProperty("EpCount")]
        public long EpCount { get; set; }

        [JsonProperty("StudioList")]
        public string StudioList { get; set; }

        [JsonProperty("PastWeek")]
        public long PastWeek { get; set; }

        [JsonProperty("Incomplete")]
        public bool Incomplete { get; set; }

        [JsonProperty("Ongoing")]
        public bool Ongoing { get; set; }

        [JsonProperty("Tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("Torrents")]
        public List<Torrent> Torrents { get; set; }
    }

    public class LinksClass
    {
        [JsonProperty("ANN", NullValueHandling = NullValueHandling.Ignore)]
        public Uri Ann { get; set; }

        [JsonProperty("Manga-Updates", NullValueHandling = NullValueHandling.Ignore)]
        public Uri MangaUpdates { get; set; }

        [JsonProperty("Wikipedia", NullValueHandling = NullValueHandling.Ignore)]
        public Uri Wikipedia { get; set; }

        [JsonProperty("MAL", NullValueHandling = NullValueHandling.Ignore)]
        public Uri Mal { get; set; }

        [JsonProperty("AniDB", NullValueHandling = NullValueHandling.Ignore)]
        public Uri AniDb { get; set; }
    }

    public class Torrent
    {
        [JsonProperty("ID")]
        public long Id { get; set; }

        [JsonProperty("EditionData")]
        public EditionData EditionData { get; set; }

        [JsonProperty("RawDownMultiplier")]
        public double? RawDownMultiplier { get; set; }

        [JsonProperty("RawUpMultiplier")]
        public double? RawUpMultiplier { get; set; }

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

        [JsonProperty("UploadTime")]
        public DateTimeOffset UploadTime { get; set; }
    }

    public class EditionData
    {
        [JsonProperty("EditionTitle")]
        public string EditionTitle { get; set; }
    }

    public struct LinksUnion
    {
        public List<object> AnythingArray;
        public LinksClass LinksClass;

        public static implicit operator LinksUnion(List<object> anythingArray) => new LinksUnion { AnythingArray = anythingArray };

        public static implicit operator LinksUnion(LinksClass linksClass) => new LinksUnion { LinksClass = linksClass };
    }

    public struct Synonymns
    {
        public List<string> StringArray;
        public Dictionary<string, string> StringMap;

        public static implicit operator Synonymns(List<string> stringArray) => new Synonymns { StringArray = stringArray };

        public static implicit operator Synonymns(Dictionary<string, string> stringMap) => new Synonymns { StringMap = stringMap };
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                LinksUnionConverter.Singleton,
                SynonymnsConverter.Singleton,
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }

    internal class LinksUnionConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(LinksUnion) || t == typeof(LinksUnion?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.StartObject:
                    var objectValue = serializer.Deserialize<LinksClass>(reader);
                    return new LinksUnion { LinksClass = objectValue };
                case JsonToken.StartArray:
                    var arrayValue = serializer.Deserialize<List<object>>(reader);
                    return new LinksUnion { AnythingArray = arrayValue };
            }

            throw new Exception("Cannot unmarshal type LinksUnion");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            var value = (LinksUnion)untypedValue;
            if (value.AnythingArray != null)
            {
                serializer.Serialize(writer, value.AnythingArray);
                return;
            }

            if (value.LinksClass == null)
            {
                throw new Exception("Cannot marshal type LinksUnion");
            }

            serializer.Serialize(writer, value.LinksClass);
            return;
        }

        public static readonly LinksUnionConverter Singleton = new LinksUnionConverter();
    }

    internal class ParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var value = serializer.Deserialize<string>(reader);
            if (long.TryParse(value, out var l))
            {
                return l;
            }

            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }

            var value = (long)untypedValue;
            serializer.Serialize(writer, value.ToString());
            return;
        }

        public static readonly ParseStringConverter Singleton = new ParseStringConverter();
    }

    internal class SynonymnsConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(Synonymns) || t == typeof(Synonymns?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.StartObject:
                    var objectValue = serializer.Deserialize<Dictionary<string, string>>(reader);
                    return new Synonymns { StringMap = objectValue };
                case JsonToken.StartArray:
                    var arrayValue = serializer.Deserialize<List<string>>(reader);
                    return new Synonymns { StringArray = arrayValue };
            }

            throw new Exception("Cannot unmarshal type Synonymns");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            var value = (Synonymns)untypedValue;
            if (value.StringArray != null)
            {
                serializer.Serialize(writer, value.StringArray);
                return;
            }

            if (value.StringMap == null)
            {
                throw new Exception("Cannot marshal type Synonymns");
            }

            serializer.Serialize(writer, value.StringMap);
        }

        public static readonly SynonymnsConverter Singleton = new SynonymnsConverter();
    }
}
