using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
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
    public class GazelleGames : TorrentIndexerBase<GazelleGamesSettings>
    {
        public override string Name => "GazelleGames";
        public override string[] IndexerUrls => new[] { "https://gazellegames.net/" };
        public override string Description => "GazelleGames (GGn) is a Private Torrent Tracker for GAMES";
        public override string Language => "en-US";
        public override Encoding Encoding => Encoding.UTF8;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public GazelleGames(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new GazelleGamesRequestGenerator(Settings, Capabilities);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new GazelleGamesParser(Settings, Capabilities.Categories);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities();

            // Apple
            caps.Categories.AddCategoryMapping("Mac", NewznabStandardCategory.ConsoleOther, "Mac");
            caps.Categories.AddCategoryMapping("iOS", NewznabStandardCategory.PCMobileiOS, "iOS");
            caps.Categories.AddCategoryMapping("Apple Bandai Pippin", NewznabStandardCategory.ConsoleOther, "Apple Bandai Pippin");
            caps.Categories.AddCategoryMapping("Apple II", NewznabStandardCategory.ConsoleOther, "Apple II");

            // Google
            caps.Categories.AddCategoryMapping("Android", NewznabStandardCategory.PCMobileAndroid, "Android");

            // Microsoft
            caps.Categories.AddCategoryMapping("DOS", NewznabStandardCategory.PCGames, "DOS");
            caps.Categories.AddCategoryMapping("Windows", NewznabStandardCategory.PCGames, "Windows");
            caps.Categories.AddCategoryMapping("Xbox", NewznabStandardCategory.ConsoleXBox, "Xbox");
            caps.Categories.AddCategoryMapping("Xbox 360", NewznabStandardCategory.ConsoleXBox360, "Xbox 360");

            // Nintendo
            caps.Categories.AddCategoryMapping("Game Boy", NewznabStandardCategory.ConsoleOther, "Game Boy");
            caps.Categories.AddCategoryMapping("Game Boy Advance", NewznabStandardCategory.ConsoleOther, "Game Boy Advance");
            caps.Categories.AddCategoryMapping("Game Boy Color", NewznabStandardCategory.ConsoleOther, "Game Boy Color");
            caps.Categories.AddCategoryMapping("NES", NewznabStandardCategory.ConsoleOther, "NES");
            caps.Categories.AddCategoryMapping("Nintendo 64", NewznabStandardCategory.ConsoleOther, "Nintendo 64");
            caps.Categories.AddCategoryMapping("Nintendo 3DS", NewznabStandardCategory.ConsoleOther, "Nintendo 3DS");
            caps.Categories.AddCategoryMapping("New Nintendo 3DS", NewznabStandardCategory.ConsoleOther, "New Nintendo 3DS");
            caps.Categories.AddCategoryMapping("Nintendo DS", NewznabStandardCategory.ConsoleNDS, "Nintendo DS");
            caps.Categories.AddCategoryMapping("Nintendo GameCube", NewznabStandardCategory.ConsoleOther, "Nintendo GameCube");
            caps.Categories.AddCategoryMapping("Pokemon Mini", NewznabStandardCategory.ConsoleOther, "Pokemon Mini");
            caps.Categories.AddCategoryMapping("SNES", NewznabStandardCategory.ConsoleOther, "SNES");
            caps.Categories.AddCategoryMapping("Switch", NewznabStandardCategory.ConsoleOther, "Switch");
            caps.Categories.AddCategoryMapping("Virtual Boy", NewznabStandardCategory.ConsoleOther, "Virtual Boy");
            caps.Categories.AddCategoryMapping("Wii", NewznabStandardCategory.ConsoleWii, "Wii");
            caps.Categories.AddCategoryMapping("Wii U", NewznabStandardCategory.ConsoleWiiU, "Wii U");

            // Sony
            caps.Categories.AddCategoryMapping("PlayStation 1", NewznabStandardCategory.ConsoleOther, "PlayStation 1");
            caps.Categories.AddCategoryMapping("PlayStation 2", NewznabStandardCategory.ConsoleOther, "PlayStation 2");
            caps.Categories.AddCategoryMapping("PlayStation 3", NewznabStandardCategory.ConsolePS3, "PlayStation 3");
            caps.Categories.AddCategoryMapping("PlayStation 4", NewznabStandardCategory.ConsolePS4, "PlayStation 4");
            caps.Categories.AddCategoryMapping("PlayStation Portable", NewznabStandardCategory.ConsolePSP, "PlayStation Portable");
            caps.Categories.AddCategoryMapping("PlayStation Vita", NewznabStandardCategory.ConsolePSVita, "PlayStation Vita");

            // Sega
            caps.Categories.AddCategoryMapping("Dreamcast", NewznabStandardCategory.ConsoleOther, "Dreamcast");
            caps.Categories.AddCategoryMapping("Game Gear", NewznabStandardCategory.ConsoleOther, "Game Gear");
            caps.Categories.AddCategoryMapping("Master System", NewznabStandardCategory.ConsoleOther, "Master System");
            caps.Categories.AddCategoryMapping("Mega Drive", NewznabStandardCategory.ConsoleOther, "Mega Drive");
            caps.Categories.AddCategoryMapping("Pico", NewznabStandardCategory.ConsoleOther, "Pico");
            caps.Categories.AddCategoryMapping("Saturn", NewznabStandardCategory.ConsoleOther, "Saturn");
            caps.Categories.AddCategoryMapping("SG-1000", NewznabStandardCategory.ConsoleOther, "SG-1000");

            // Atari
            caps.Categories.AddCategoryMapping("Atari 2600", NewznabStandardCategory.ConsoleOther, "Atari 2600");
            caps.Categories.AddCategoryMapping("Atari 5200", NewznabStandardCategory.ConsoleOther, "Atari 5200");
            caps.Categories.AddCategoryMapping("Atari 7800", NewznabStandardCategory.ConsoleOther, "Atari 7800");
            caps.Categories.AddCategoryMapping("Atari Jaguar", NewznabStandardCategory.ConsoleOther, "Atari Jaguar");
            caps.Categories.AddCategoryMapping("Atari Lynx", NewznabStandardCategory.ConsoleOther, "Atari Lynx");
            caps.Categories.AddCategoryMapping("Atari ST", NewznabStandardCategory.ConsoleOther, "Atari ST");

            // Amstrad
            caps.Categories.AddCategoryMapping("Amstrad CPC", NewznabStandardCategory.ConsoleOther, "Amstrad CPC");

            // Sinclair
            caps.Categories.AddCategoryMapping("ZX Spectrum", NewznabStandardCategory.ConsoleOther, "ZX Spectrum");

            // Spectravideo
            caps.Categories.AddCategoryMapping("MSX", NewznabStandardCategory.ConsoleOther, "MSX");
            caps.Categories.AddCategoryMapping("MSX 2", NewznabStandardCategory.ConsoleOther, "MSX 2");

            // Tiger
            caps.Categories.AddCategoryMapping("Game.com", NewznabStandardCategory.ConsoleOther, "Game.com");
            caps.Categories.AddCategoryMapping("Gizmondo", NewznabStandardCategory.ConsoleOther, "Gizmondo");

            // VTech
            caps.Categories.AddCategoryMapping("V.Smile", NewznabStandardCategory.ConsoleOther, "V.Smile");
            caps.Categories.AddCategoryMapping("CreatiVision", NewznabStandardCategory.ConsoleOther, "CreatiVision");

            // Tabletop Games
            caps.Categories.AddCategoryMapping("Board Game", NewznabStandardCategory.ConsoleOther, "Board Game");
            caps.Categories.AddCategoryMapping("Card Game", NewznabStandardCategory.ConsoleOther, "Card Game");
            caps.Categories.AddCategoryMapping("Miniature Wargames", NewznabStandardCategory.ConsoleOther, "Miniature Wargames");
            caps.Categories.AddCategoryMapping("Pen and Paper RPG", NewznabStandardCategory.ConsoleOther, "Pen and Paper RPG");

            // Other
            caps.Categories.AddCategoryMapping("3DO", NewznabStandardCategory.ConsoleOther, "3DO");
            caps.Categories.AddCategoryMapping("Bandai WonderSwan", NewznabStandardCategory.ConsoleOther, "Bandai WonderSwan");
            caps.Categories.AddCategoryMapping("Bandai WonderSwan Color", NewznabStandardCategory.ConsoleOther, "Bandai WonderSwan Color");
            caps.Categories.AddCategoryMapping("Casio Loopy", NewznabStandardCategory.ConsoleOther, "Casio Loopy");
            caps.Categories.AddCategoryMapping("Casio PV-1000", NewznabStandardCategory.ConsoleOther, "Casio PV-1000");
            caps.Categories.AddCategoryMapping("Colecovision", NewznabStandardCategory.ConsoleOther, "Colecovision");
            caps.Categories.AddCategoryMapping("Commodore 64", NewznabStandardCategory.ConsoleOther, "Commodore 64");
            caps.Categories.AddCategoryMapping("Commodore 128", NewznabStandardCategory.ConsoleOther, "Commodore 128");
            caps.Categories.AddCategoryMapping("Commodore Amiga", NewznabStandardCategory.ConsoleOther, "Commodore Amiga");
            caps.Categories.AddCategoryMapping("Commodore Plus-4", NewznabStandardCategory.ConsoleOther, "Commodore Plus-4");
            caps.Categories.AddCategoryMapping("Commodore VIC-20", NewznabStandardCategory.ConsoleOther, "Commodore VIC-20");
            caps.Categories.AddCategoryMapping("Emerson Arcadia 2001", NewznabStandardCategory.ConsoleOther, "Emerson Arcadia 2001");
            caps.Categories.AddCategoryMapping("Entex Adventure Vision", NewznabStandardCategory.ConsoleOther, "Entex Adventure Vision");
            caps.Categories.AddCategoryMapping("Epoch Super Casette Vision", NewznabStandardCategory.ConsoleOther, "Epoch Super Casette Vision");
            caps.Categories.AddCategoryMapping("Fairchild Channel F", NewznabStandardCategory.ConsoleOther, "Fairchild Channel F");
            caps.Categories.AddCategoryMapping("Funtech Super Acan", NewznabStandardCategory.ConsoleOther, "Funtech Super Acan");
            caps.Categories.AddCategoryMapping("GamePark GP32", NewznabStandardCategory.ConsoleOther, "GamePark GP32");
            caps.Categories.AddCategoryMapping("General Computer Vectrex", NewznabStandardCategory.ConsoleOther, "General Computer Vectrex");
            caps.Categories.AddCategoryMapping("Interactive DVD", NewznabStandardCategory.ConsoleOther, "Interactive DVD");
            caps.Categories.AddCategoryMapping("Linux", NewznabStandardCategory.ConsoleOther, "Linux");
            caps.Categories.AddCategoryMapping("Hartung Game Master", NewznabStandardCategory.ConsoleOther, "Hartung Game Master");
            caps.Categories.AddCategoryMapping("Magnavox-Phillips Odyssey", NewznabStandardCategory.ConsoleOther, "Magnavox-Phillips Odyssey");
            caps.Categories.AddCategoryMapping("Mattel Intellivision", NewznabStandardCategory.ConsoleOther, "Mattel Intellivision");
            caps.Categories.AddCategoryMapping("Memotech MTX", NewznabStandardCategory.ConsoleOther, "Memotech MTX");
            caps.Categories.AddCategoryMapping("Miles Gordon Sam Coupe", NewznabStandardCategory.ConsoleOther, "Miles Gordon Sam Coupe");
            caps.Categories.AddCategoryMapping("NEC PC-98", NewznabStandardCategory.ConsoleOther, "NEC PC-98");
            caps.Categories.AddCategoryMapping("NEC PC-FX", NewznabStandardCategory.ConsoleOther, "NEC PC-FX");
            caps.Categories.AddCategoryMapping("NEC SuperGrafx", NewznabStandardCategory.ConsoleOther, "NEC SuperGrafx");
            caps.Categories.AddCategoryMapping("NEC TurboGrafx-16", NewznabStandardCategory.ConsoleOther, "NEC TurboGrafx-16");
            caps.Categories.AddCategoryMapping("Nokia N-Gage", NewznabStandardCategory.ConsoleOther, "Nokia N-Gage");
            caps.Categories.AddCategoryMapping("Ouya", NewznabStandardCategory.ConsoleOther, "Ouya");
            caps.Categories.AddCategoryMapping("Philips Videopac+", NewznabStandardCategory.ConsoleOther, "Philips Videopac+");
            caps.Categories.AddCategoryMapping("Phone/PDA", NewznabStandardCategory.ConsoleOther, "Phone/PDA");
            caps.Categories.AddCategoryMapping("RCA Studio II", NewznabStandardCategory.ConsoleOther, "RCA Studio II");
            caps.Categories.AddCategoryMapping("Sharp X1", NewznabStandardCategory.ConsoleOther, "Sharp X1");
            caps.Categories.AddCategoryMapping("Sharp X68000", NewznabStandardCategory.ConsoleOther, "Sharp X68000");
            caps.Categories.AddCategoryMapping("SNK Neo Geo", NewznabStandardCategory.ConsoleOther, "SNK Neo Geo");
            caps.Categories.AddCategoryMapping("SNK Neo Geo Pocket", NewznabStandardCategory.ConsoleOther, "SNK Neo Geo Pocket");
            caps.Categories.AddCategoryMapping("Taito Type X", NewznabStandardCategory.ConsoleOther, "Taito Type X");
            caps.Categories.AddCategoryMapping("Tandy Color Computer", NewznabStandardCategory.ConsoleOther, "Tandy Color Computer");
            caps.Categories.AddCategoryMapping("Tangerine Oric", NewznabStandardCategory.ConsoleOther, "Tangerine Oric");
            caps.Categories.AddCategoryMapping("Thomson MO5", NewznabStandardCategory.ConsoleOther, "Thomson MO5");
            caps.Categories.AddCategoryMapping("Watara Supervision", NewznabStandardCategory.ConsoleOther, "Watara Supervision");
            caps.Categories.AddCategoryMapping("Retro - Other", NewznabStandardCategory.ConsoleOther, "Retro - Other");

            // special categories (real categories/not platforms)
            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.PCGames, "Games");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.PC0day, "Applications");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.BooksEBook, "E-Books");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.AudioOther, "OST");

            return caps;
        }

        protected override async Task Test(List<ValidationFailure> failures)
        {
            await FetchPasskey().ConfigureAwait(false);

            await base.Test(failures).ConfigureAwait(false);
        }

        private async Task FetchPasskey()
        {
            var request = new HttpRequestBuilder($"{Settings.BaseUrl.Trim().TrimEnd('/')}/api.php")
                .Accept(HttpAccept.Json)
                .SetHeader("X-API-Key", Settings.Apikey)
                .AddQueryParam("request", "quick_user")
                .Build();

            var indexResponse = await _httpClient.ExecuteAsync(request).ConfigureAwait(false);

            var index = Json.Deserialize<GazelleGamesUserResponse>(indexResponse.Content);

            if (index == null ||
                string.IsNullOrWhiteSpace(index.Status) ||
                index.Status != "success" ||
                string.IsNullOrWhiteSpace(index.Response.PassKey))
            {
                throw new IndexerAuthException("Failed to authenticate with GazelleGames.");
            }

            // Set passkey on settings so it can be used to generate the download URL
            Settings.Passkey = index.Response.PassKey;
        }
    }

    public class GazelleGamesRequestGenerator : IIndexerRequestGenerator
    {
        private readonly GazelleGamesSettings _settings;
        private readonly IndexerCapabilities _capabilities;

        public GazelleGamesRequestGenerator(GazelleGamesSettings settings, IndexerCapabilities capabilities)
        {
            _settings = settings;
            _capabilities = capabilities;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest(GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria)));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest(GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria)));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest(GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria)));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest(GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria)));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetRequest(GetBasicSearchParameters(searchCriteria.SanitizedSearchTerm, searchCriteria)));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetRequest(List<KeyValuePair<string, string>> parameters)
        {
            var request = RequestBuilder()
                .Resource($"/api.php?{parameters.GetQueryString()}")
                .Build();

            yield return new IndexerRequest(request);
        }

        private HttpRequestBuilder RequestBuilder()
        {
            return new HttpRequestBuilder($"{_settings.BaseUrl.Trim().TrimEnd('/')}")
                .Resource("/api.php")
                .Accept(HttpAccept.Json)
                .SetHeader("X-API-Key", _settings.Apikey);
        }

        private List<KeyValuePair<string, string>> GetBasicSearchParameters(string searchTerm, SearchCriteriaBase searchCriteria)
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                { "request", "search" },
                { "search_type", "torrents" },
                { "empty_groups", "filled" },
                { "order_by", "time" },
                { "order_way", "desc" }
            };

            if (searchTerm.IsNotNullOrWhiteSpace())
            {
                parameters.Add(
                    _settings.SearchGroupNames ? "groupname" : "searchstr",
                    searchTerm.Replace(".", " "));
            }

            if (searchCriteria.Categories != null)
            {
                var categoryMappings = _capabilities.Categories
                    .MapTorznabCapsToTrackers(searchCriteria.Categories)
                    .Distinct()
                    .Where(x => !x.IsAllDigits())
                    .ToList();

                categoryMappings.ForEach(category => parameters.Add("artistcheck[]", category));
            }

            if (searchCriteria.MinSize is > 0)
            {
                var minSize = searchCriteria.MinSize.Value / 1024L / 1024L;
                if (minSize > 0)
                {
                    parameters.Add("sizesmall", minSize.ToString());
                }
            }

            if (searchCriteria.MaxSize is > 0)
            {
                var maxSize = searchCriteria.MaxSize.Value / 1024L / 1024L;
                if (maxSize > 0)
                {
                    parameters.Add("sizeslarge", maxSize.ToString());
                }
            }

            return parameters;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class GazelleGamesParser : IParseIndexerResponse
    {
        private readonly GazelleGamesSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public GazelleGamesParser(GazelleGamesSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from indexer request");
            }

            if (!indexerResponse.HttpResponse.Headers.ContentType.Contains(HttpAccept.Json.Value))
            {
                throw new IndexerException(indexerResponse, $"Unexpected response header {indexerResponse.HttpResponse.Headers.ContentType} from indexer request, expected {HttpAccept.Json.Value}");
            }

            var jsonResponse = new HttpResponse<GazelleGamesResponse>(indexerResponse.HttpResponse);

            if (jsonResponse.Resource.Status != "success" ||
                string.IsNullOrWhiteSpace(jsonResponse.Resource.Status) ||
                jsonResponse.Resource.Response is not JObject response)
            {
                return torrentInfos;
            }

            var groups = response.ToObject<Dictionary<int, GazelleGamesGroup>>(JsonSerializer.Create(Json.GetSerializerSettings()));

            foreach (var group in groups)
            {
                if (group.Value.Torrents is not JObject groupTorrents)
                {
                    continue;
                }

                var torrents = groupTorrents
                    .ToObject<Dictionary<int, GazelleGamesTorrent>>(JsonSerializer.Create(Json.GetSerializerSettings()))
                    .Where(t => t.Value.TorrentType.ToUpperInvariant() == "TORRENT")
                    .ToList();

                var categories = group.Value.Artists
                    .SelectMany(a => _categories.MapTrackerCatDescToNewznab(a.Name))
                    .Distinct()
                    .ToArray();

                foreach (var torrent in torrents)
                {
                    var torrentId = torrent.Key;
                    var infoUrl = GetInfoUrl(group.Key, torrentId);

                    if (categories.Length == 0)
                    {
                        categories = _categories.MapTrackerCatToNewznab(torrent.Value.CategoryId.ToString()).ToArray();
                    }

                    var release = new TorrentInfo
                    {
                        Guid = infoUrl,
                        InfoUrl = infoUrl,
                        DownloadUrl = GetDownloadUrl(torrentId),
                        Title = GetTitle(group.Value, torrent.Value),
                        Categories = categories,
                        Files = torrent.Value.FileCount,
                        Size = long.Parse(torrent.Value.Size),
                        Grabs = torrent.Value.Snatched,
                        Seeders = torrent.Value.Seeders,
                        Peers = torrent.Value.Leechers + torrent.Value.Seeders,
                        PublishDate = torrent.Value.Time.ToUniversalTime(),
                        Scene = torrent.Value.Scene == 1,
                        DownloadVolumeFactor = torrent.Value.FreeTorrent is GazelleGamesFreeTorrent.FreeLeech or GazelleGamesFreeTorrent.Neutral || torrent.Value.LowSeedFL ? 0 : 1,
                        UploadVolumeFactor = torrent.Value.FreeTorrent == GazelleGamesFreeTorrent.Neutral ? 0 : 1,
                        MinimumSeedTime = 288000 // Minimum of 3 days and 8 hours (80 hours in total)
                    };

                    torrentInfos.Add(release);
                }
            }

            // order by date
            return
                torrentInfos
                    .OrderByDescending(o => o.PublishDate)
                    .ToArray();
        }

        private static string GetTitle(GazelleGamesGroup group, GazelleGamesTorrent torrent)
        {
            var title = WebUtility.HtmlDecode(torrent.ReleaseTitle);

            if (group.Year is > 0 && !title.Contains(group.Year.ToString()))
            {
                title += $" ({group.Year})";
            }

            if (torrent.RemasterTitle.IsNotNullOrWhiteSpace())
            {
                title += $" [{$"{torrent.RemasterTitle} {torrent.RemasterYear}".Trim()}]";
            }

            var flags = new List<string>
            {
                $"{torrent.Format} {torrent.Encoding}".Trim()
            };

            if (group.Artists is { Count: > 0 })
            {
                flags.AddIfNotNull(group.Artists.Select(a => a.Name).Join(", "));
            }

            flags.AddIfNotNull(torrent.Language);
            flags.AddIfNotNull(torrent.Region);
            flags.AddIfNotNull(torrent.Miscellaneous);

            if (torrent.Dupable == 1)
            {
                flags.Add("Trumpable");
            }

            flags = flags.Where(x => x.IsNotNullOrWhiteSpace()).ToList();

            if (flags.Any())
            {
                title += $" [{string.Join(" / ", flags)}]";
            }

            if (torrent.GameDoxType.IsNotNullOrWhiteSpace())
            {
                title += $" [{torrent.GameDoxType.Trim()}]";
            }

            return title;
        }

        private string GetDownloadUrl(int torrentId)
        {
            // AuthKey is required but not checked, just pass in a dummy variable
            // to avoid having to track authkey, which is randomly cycled
            var url = new HttpUri(_settings.BaseUrl)
                .CombinePath("/torrents.php")
                .AddQueryParam("action", "download")
                .AddQueryParam("id", torrentId)
                .AddQueryParam("authkey", "prowlarr")
                .AddQueryParam("torrent_pass", _settings.Passkey);

            return url.FullUri;
        }

        private string GetInfoUrl(int groupId, int torrentId)
        {
            var url = new HttpUri(_settings.BaseUrl)
                .CombinePath("/torrents.php")
                .AddQueryParam("id", groupId)
                .AddQueryParam("torrentid", torrentId);

            return url.FullUri;
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class GazelleGamesSettingsValidator : NoAuthSettingsValidator<GazelleGamesSettings>
    {
        public GazelleGamesSettingsValidator()
        {
            RuleFor(c => c.Apikey).NotEmpty();
        }
    }

    public class GazelleGamesSettings : NoAuthTorrentBaseSettings
    {
        private static readonly GazelleGamesSettingsValidator Validator = new ();

        public GazelleGamesSettings()
        {
            Apikey = "";
            Passkey = "";
        }

        [FieldDefinition(2, Label = "ApiKey", HelpText = "IndexerGazelleGamesSettingsApiKeyHelpText", HelpTextWarning = "IndexerGazelleGamesSettingsApiKeyHelpTextWarning", Privacy = PrivacyLevel.ApiKey)]
        public string Apikey { get; set; }

        [FieldDefinition(3, Label = "IndexerGazelleGamesSettingsSearchGroupNames", Type = FieldType.Checkbox, HelpText = "IndexerGazelleGamesSettingsSearchGroupNamesHelpText")]
        public bool SearchGroupNames { get; set; }

        public string Passkey { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public class GazelleGamesResponse
    {
        public string Status { get; set; }
        public object Response { get; set; }
    }

    public class GazelleGamesGroup
    {
        public ReadOnlyCollection<GazelleGamesArtist> Artists { get; set; }
        public object Torrents { get; set; }
        public int? Year { get; set; }
    }

    public class GazelleGamesArtist
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class GazelleGamesTorrent
    {
        public int CategoryId { get; set; }
        public string Format { get; set; }
        public string Encoding { get; set; }
        public string Language { get; set; }
        public string Region { get; set; }
        public string RemasterYear { get; set; }
        public string RemasterTitle { get; set; }
        public string ReleaseTitle { get; set; }
        public string Miscellaneous { get; set; }
        public int Scene { get; set; }
        public int Dupable { get; set; }
        public DateTime Time { get; set; }
        public string TorrentType { get; set; }
        public int FileCount { get; set; }
        public string Size { get; set; }
        public int? Snatched { get; set; }
        public int Seeders { get; set; }
        public int Leechers { get; set; }
        public GazelleGamesFreeTorrent FreeTorrent { get; set; }
        public bool PersonalFL { get; set; }
        public bool LowSeedFL { get; set; }

        [JsonProperty("GameDOXType")]
        public string GameDoxType { get; set; }
    }

    public class GazelleGamesUserResponse
    {
        public string Status { get; set; }
        public GazelleGamesUser Response { get; set; }
    }

    public class GazelleGamesUser
    {
        public string PassKey { get; set; }
    }

    public enum GazelleGamesFreeTorrent
    {
        Normal = 0,
        FreeLeech = 1,
        Neutral = 2,
        Either = 3
    }
}
