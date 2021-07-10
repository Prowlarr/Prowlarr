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
        public override string Description => "AnimeBytes (AB) is the largest private torrent tracker that specialises in anime and anime-related content.";
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

            //TODO: Create API Resource Type
            var json = JsonConvert.DeserializeObject<dynamic>(indexerResponse.Content);

            if (json["error"] != null)
            {
                throw new Exception(json["error"].ToString());
            }

            var matches = (long)json["Matches"];

            if (matches > 0)
            {
                var groups = (JArray)json.Groups;

                foreach (var group in groups)
                {
                    var synonyms = new List<string>();
                    var posterStr = (string)group["Image"];
                    var poster = string.IsNullOrWhiteSpace(posterStr) ? null : new Uri(posterStr);
                    var year = (int)group["Year"];
                    var groupName = (string)group["GroupName"];
                    var seriesName = (string)group["SeriesName"];
                    var mainTitle = WebUtility.HtmlDecode((string)group["FullName"]);
                    if (seriesName != null)
                    {
                        mainTitle = seriesName;
                    }

                    synonyms.Add(mainTitle);

                    // TODO: Do we need all these options?
                    //if (group["Synonymns"].HasValues)
                    //{
                    //    if (group["Synonymns"] is JArray)
                    //    {
                    //        var allSyonyms = group["Synonymns"].ToObject<List<string>>();

                    //        if (AddJapaneseTitle && allSyonyms.Count >= 1)
                    //            synonyms.Add(allSyonyms[0]);
                    //        if (AddRomajiTitle && allSyonyms.Count >= 2)
                    //            synonyms.Add(allSyonyms[1]);
                    //        if (AddAlternativeTitles && allSyonyms.Count >= 3)
                    //            synonyms.AddRange(allSyonyms[2].Split(',').Select(t => t.Trim()));
                    //    }
                    //    else
                    //    {
                    //        var allSynonyms = group["Synonymns"].ToObject<Dictionary<int, string>>();

                    //        if (AddJapaneseTitle && allSynonyms.ContainsKey(0))
                    //            synonyms.Add(allSynonyms[0]);
                    //        if (AddRomajiTitle && allSynonyms.ContainsKey(1))
                    //            synonyms.Add(allSynonyms[1]);
                    //        if (AddAlternativeTitles && allSynonyms.ContainsKey(2))
                    //        {
                    //            synonyms.AddRange(allSynonyms[2].Split(',').Select(t => t.Trim()));
                    //        }
                    //    }
                    //}
                    List<IndexerCategory> category = null;
                    var categoryName = (string)group["CategoryName"];

                    var description = (string)group["Description"];

                    foreach (var torrent in group["Torrents"])
                    {
                        var releaseInfo = "S01";
                        string episode = null;
                        int? season = null;
                        var editionTitle = (string)torrent["EditionData"]["EditionTitle"];
                        if (!string.IsNullOrWhiteSpace(editionTitle))
                        {
                            releaseInfo = WebUtility.HtmlDecode(editionTitle);
                        }

                        var seasonRegEx = new Regex(@"Season (\d+)", RegexOptions.Compiled);
                        var seasonRegExMatch = seasonRegEx.Match(releaseInfo);
                        if (seasonRegExMatch.Success)
                        {
                            season = ParseUtil.CoerceInt(seasonRegExMatch.Groups[1].Value);
                        }

                        var episodeRegEx = new Regex(@"Episode (\d+)", RegexOptions.Compiled);
                        var episodeRegExMatch = episodeRegEx.Match(releaseInfo);
                        if (episodeRegExMatch.Success)
                        {
                            episode = episodeRegExMatch.Groups[1].Value;
                        }

                        releaseInfo = releaseInfo.Replace("Episode ", "");
                        releaseInfo = releaseInfo.Replace("Season ", "S");
                        releaseInfo = releaseInfo.Trim();

                        //if (PadEpisode && int.TryParse(releaseInfo, out _) && releaseInfo.Length == 1)
                        //{
                        //    releaseInfo = "0" + releaseInfo;
                        //}

                        //if (FilterSeasonEpisode)
                        //{
                        //    if (query.Season != 0 && season != null && season != query.Season) // skip if season doesn't match
                        //        continue;
                        //    if (query.Episode != null && episode != null && episode != query.Episode) // skip if episode doesn't match
                        //        continue;
                        //}
                        var torrentId = (long)torrent["ID"];
                        var property = ((string)torrent["Property"]).Replace(" | Freeleech", "");
                        var link = (string)torrent["Link"];
                        var linkUri = new Uri(link);
                        var uploadTimeString = (string)torrent["UploadTime"];
                        var uploadTime = DateTime.ParseExact(uploadTimeString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        var publishDate = DateTime.SpecifyKind(uploadTime, DateTimeKind.Utc).ToLocalTime();
                        var details = new Uri(_settings.BaseUrl + "torrent/" + torrentId + "/group");
                        var size = (long)torrent["Size"];
                        var snatched = (int)torrent["Snatched"];
                        var seeders = (int)torrent["Seeders"];
                        var leechers = (int)torrent["Leechers"];
                        var fileCount = (int)torrent["FileCount"];
                        var peers = seeders + leechers;

                        var rawDownMultiplier = (int?)torrent["RawDownMultiplier"] ?? 0;
                        var rawUpMultiplier = (int?)torrent["RawUpMultiplier"] ?? 0;

                        if (groupName == "TV Series" || groupName == "OVA")
                        {
                            category = new List<IndexerCategory> { NewznabStandardCategory.TVAnime };
                        }

                        // Ignore these categories as they'll cause hell with the matcher
                        // TV Special, OVA, ONA, DVD Special, BD Special
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
                        var infoString = releaseTags.Aggregate("", (prev, cur) => prev + "[" + cur + "]");
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
                                DownloadUrl = linkUri.AbsoluteUri,
                                PublishDate = publishDate,
                                Categories = category,
                                Description = description,
                                Size = size,
                                Seeders = seeders,
                                Peers = peers,
                                Grabs = snatched,
                                Files = fileCount,
                                DownloadVolumeFactor = rawDownMultiplier,
                                UploadVolumeFactor = rawUpMultiplier
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

        [FieldDefinition(2, Label = "Passkey", HelpText = "Site Passkey", Privacy = PrivacyLevel.Password, Type = FieldType.Password)]
        public string Passkey { get; set; }

        [FieldDefinition(3, Label = "Username", HelpText = "Site Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(4)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
