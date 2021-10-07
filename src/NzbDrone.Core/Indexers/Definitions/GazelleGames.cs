using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using AngleSharp.Html.Parser;
using FluentValidation;
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
    public class GazelleGames : TorrentIndexerBase<GazelleGamesSettings>
    {
        public override string Name => "GazelleGames";
        public override string[] IndexerUrls => new string[] { "https://gazellegames.net/" };
        public override string Description => "GazelleGames (GGn) is a Private Torrent Tracker for GAMES";
        public override string Language => "en-US";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public GazelleGames(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new GazelleGamesRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new GazelleGamesParser(Settings, Capabilities.Categories);
        }

        protected override IDictionary<string, string> GetCookies()
        {
            return CookieUtil.CookieHeaderToDictionary(Settings.Cookie);
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            if (httpResponse.HasHttpRedirect && httpResponse.RedirectUrl.EndsWith("login.php"))
            {
                return true;
            }

            return false;
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
            };

            caps.Categories.AddCategoryMapping("Mac", NewznabStandardCategory.ConsoleOther, "Mac");
            caps.Categories.AddCategoryMapping("iOS", NewznabStandardCategory.PCMobileiOS, "iOS");
            caps.Categories.AddCategoryMapping("Apple Bandai Pippin", NewznabStandardCategory.ConsoleOther, "Apple Bandai Pippin");

            caps.Categories.AddCategoryMapping("Android", NewznabStandardCategory.PCMobileAndroid, "Android");

            caps.Categories.AddCategoryMapping("DOS", NewznabStandardCategory.PCGames, "DOS");
            caps.Categories.AddCategoryMapping("Windows", NewznabStandardCategory.PCGames, "Windows");
            caps.Categories.AddCategoryMapping("Xbox", NewznabStandardCategory.ConsoleXBox, "Xbox");
            caps.Categories.AddCategoryMapping("Xbox 360", NewznabStandardCategory.ConsoleXBox360, "Xbox 360");

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
            caps.Categories.AddCategoryMapping("Virtual Boy", NewznabStandardCategory.ConsoleOther, "Virtual Boy");
            caps.Categories.AddCategoryMapping("Wii", NewznabStandardCategory.ConsoleWii, "Wii");
            caps.Categories.AddCategoryMapping("Wii U", NewznabStandardCategory.ConsoleWiiU, "Wii U");

            caps.Categories.AddCategoryMapping("PlayStation 1", NewznabStandardCategory.ConsoleOther, "PlayStation 1");
            caps.Categories.AddCategoryMapping("PlayStation 2", NewznabStandardCategory.ConsoleOther, "PlayStation 2");
            caps.Categories.AddCategoryMapping("PlayStation 3", NewznabStandardCategory.ConsolePS3, "PlayStation 3");
            caps.Categories.AddCategoryMapping("PlayStation 4", NewznabStandardCategory.ConsolePS4, "PlayStation 4");
            caps.Categories.AddCategoryMapping("PlayStation Portable", NewznabStandardCategory.ConsolePSP, "PlayStation Portable");
            caps.Categories.AddCategoryMapping("PlayStation Vita", NewznabStandardCategory.ConsolePSVita, "PlayStation Vita");

            caps.Categories.AddCategoryMapping("Dreamcast", NewznabStandardCategory.ConsoleOther, "Dreamcast");
            caps.Categories.AddCategoryMapping("Game Gear", NewznabStandardCategory.ConsoleOther, "Game Gear");
            caps.Categories.AddCategoryMapping("Master System", NewznabStandardCategory.ConsoleOther, "Master System");
            caps.Categories.AddCategoryMapping("Mega Drive", NewznabStandardCategory.ConsoleOther, "Mega Drive");
            caps.Categories.AddCategoryMapping("Pico", NewznabStandardCategory.ConsoleOther, "Pico");
            caps.Categories.AddCategoryMapping("Saturn", NewznabStandardCategory.ConsoleOther, "Saturn");
            caps.Categories.AddCategoryMapping("SG-1000", NewznabStandardCategory.ConsoleOther, "SG-1000");

            caps.Categories.AddCategoryMapping("Atari 2600", NewznabStandardCategory.ConsoleOther, "Atari 2600");
            caps.Categories.AddCategoryMapping("Atari 5200", NewznabStandardCategory.ConsoleOther, "Atari 5200");
            caps.Categories.AddCategoryMapping("Atari 7800", NewznabStandardCategory.ConsoleOther, "Atari 7800");
            caps.Categories.AddCategoryMapping("Atari Jaguar", NewznabStandardCategory.ConsoleOther, "Atari Jaguar");
            caps.Categories.AddCategoryMapping("Atari Lynx", NewznabStandardCategory.ConsoleOther, "Atari Lynx");
            caps.Categories.AddCategoryMapping("Atari ST", NewznabStandardCategory.ConsoleOther, "Atari ST");

            caps.Categories.AddCategoryMapping("Amstrad CPC", NewznabStandardCategory.ConsoleOther, "Amstrad CPC");

            caps.Categories.AddCategoryMapping("ZX Spectrum", NewznabStandardCategory.ConsoleOther, "ZX Spectrum");

            caps.Categories.AddCategoryMapping("MSX", NewznabStandardCategory.ConsoleOther, "MSX");
            caps.Categories.AddCategoryMapping("MSX 2", NewznabStandardCategory.ConsoleOther, "MSX 2");

            caps.Categories.AddCategoryMapping("Game.com", NewznabStandardCategory.ConsoleOther, "Game.com");
            caps.Categories.AddCategoryMapping("Gizmondo", NewznabStandardCategory.ConsoleOther, "Gizmondo");

            caps.Categories.AddCategoryMapping("V.Smile", NewznabStandardCategory.ConsoleOther, "V.Smile");
            caps.Categories.AddCategoryMapping("CreatiVision", NewznabStandardCategory.ConsoleOther, "CreatiVision");

            caps.Categories.AddCategoryMapping("Board Game", NewznabStandardCategory.ConsoleOther, "Board Game");
            caps.Categories.AddCategoryMapping("Card Game", NewznabStandardCategory.ConsoleOther, "Card Game");
            caps.Categories.AddCategoryMapping("Miniature Wargames", NewznabStandardCategory.ConsoleOther, "Miniature Wargames");
            caps.Categories.AddCategoryMapping("Pen and Paper RPG", NewznabStandardCategory.ConsoleOther, "Pen and Paper RPG");

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

            caps.Categories.AddCategoryMapping("OST", NewznabStandardCategory.AudioOther, "OST");
            caps.Categories.AddCategoryMapping("Applications", NewznabStandardCategory.PC0day, "Applications");
            caps.Categories.AddCategoryMapping("E-Books", NewznabStandardCategory.BooksEBook, "E-Books");

            return caps;
        }
    }

    public class GazelleGamesRequestGenerator : IIndexerRequestGenerator
    {
        public GazelleGamesSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public GazelleGamesRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
        {
            var searchUrl = string.Format("{0}/torrents.php", Settings.BaseUrl.TrimEnd('/'));

            var searchString = term;

            var searchType = Settings.SearchGroupNames ? "groupname" : "searchstr";

            var queryCollection = new NameValueCollection
            {
                { searchType, searchString },
                { "order_by", "time" },
                { "order_way", "desc" },
                { "action", "basic" },
                { "searchsubmit", "1" }
            };

            var i = 0;

            foreach (var cat in Capabilities.Categories.MapTorznabCapsToTrackers(categories))
            {
                queryCollection.Add($"artistcheck[{i++}]", cat);
            }

            searchUrl += "?" + queryCollection.GetQueryString();

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
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

            var rowsSelector = ".torrent_table > tbody > tr";

            var searchResultParser = new HtmlParser();
            var searchResultDocument = searchResultParser.ParseDocument(indexerResponse.Content);
            var rows = searchResultDocument.QuerySelectorAll(rowsSelector);

            var stickyGroup = false;
            string categoryStr;
            ICollection<IndexerCategory> groupCategory = null;
            string groupTitle = null;

            foreach (var row in rows)
            {
                if (row.ClassList.Contains("torrent"))
                {
                    // garbage rows
                    continue;
                }
                else if (row.ClassList.Contains("group"))
                {
                    stickyGroup = row.ClassList.Contains("sticky");
                    var dispalyname = row.QuerySelector("#displayname");
                    var qCat = row.QuerySelector("td.cats_col > div");
                    categoryStr = qCat.GetAttribute("title");
                    var qArtistLink = dispalyname.QuerySelector("#groupplatform > a");
                    if (qArtistLink != null)
                    {
                        categoryStr = ParseUtil.GetArgumentFromQueryString(qArtistLink.GetAttribute("href"), "artistname");
                    }

                    groupCategory = _categories.MapTrackerCatToNewznab(categoryStr);

                    var qDetailsLink = dispalyname.QuerySelector("#groupname > a");
                    groupTitle = qDetailsLink.TextContent;
                }
                else if (row.ClassList.Contains("group_torrent"))
                {
                    if (row.QuerySelector("td.edition_info") != null)
                    {
                        continue;
                    }

                    var sizeString = row.QuerySelector("td:nth-child(4)").TextContent;
                    if (string.IsNullOrEmpty(sizeString))
                    {
                        continue;
                    }

                    var qDetailsLink = row.QuerySelector("a[href^=\"torrents.php?id=\"]");
                    var title = qDetailsLink.TextContent.Replace(", Freeleech!", "").Replace(", Neutral Leech!", "");

                    //if (stickyGroup && (query.ImdbID == null || !NewznabStandardCategory.MovieSearchImdbAvailable) && !query.MatchQueryStringAND(title)) // AND match for sticky releases
                    //{
                    //    continue;
                    //}
                    var qDescription = qDetailsLink.QuerySelector("span.torrent_info_tags");
                    var qDLLink = row.QuerySelector("a[href^=\"torrents.php?action=download\"]");
                    var qTime = row.QuerySelector("span.time");
                    var qGrabs = row.QuerySelector("td:nth-child(5)");
                    var qSeeders = row.QuerySelector("td:nth-child(6)");
                    var qLeechers = row.QuerySelector("td:nth-child(7)");
                    var qFreeLeech = row.QuerySelector("strong.freeleech_label");
                    var qNeutralLeech = row.QuerySelector("strong.neutralleech_label");
                    var time = qTime.GetAttribute("title");
                    var link = _settings.BaseUrl + qDLLink.GetAttribute("href");
                    var seeders = ParseUtil.CoerceInt(qSeeders.TextContent);
                    var publishDate = DateTime.SpecifyKind(
                        DateTime.ParseExact(time, "MMM dd yyyy, HH:mm", CultureInfo.InvariantCulture),
                        DateTimeKind.Unspecified).ToLocalTime();
                    var details = _settings.BaseUrl + qDetailsLink.GetAttribute("href");
                    var grabs = ParseUtil.CoerceInt(qGrabs.TextContent);
                    var leechers = ParseUtil.CoerceInt(qLeechers.TextContent);
                    var size = ParseUtil.GetBytes(sizeString);

                    var release = new TorrentInfo
                    {
                        MinimumRatio = 1,
                        MinimumSeedTime = 288000, //80 hours
                        Categories = groupCategory,
                        PublishDate = publishDate,
                        Size = size,
                        InfoUrl = details,
                        DownloadUrl = link,
                        Guid = link,
                        Grabs = grabs,
                        Seeders = seeders,
                        Peers = leechers + seeders,
                        Title = title,
                        Description = qDescription?.TextContent,
                        UploadVolumeFactor = qNeutralLeech is null ? 1 : 0,
                        DownloadVolumeFactor = qFreeLeech != null || qNeutralLeech != null ? 0 : 1
                    };

                    torrentInfos.Add(release);
                }
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class GazelleGamesSettingsValidator : AbstractValidator<GazelleGamesSettings>
    {
        public GazelleGamesSettingsValidator()
        {
            RuleFor(c => c.Cookie).NotEmpty();
        }
    }

    public class GazelleGamesSettings : IIndexerSettings
    {
        private static readonly GazelleGamesSettingsValidator Validator = new GazelleGamesSettingsValidator();

        public GazelleGamesSettings()
        {
            Cookie = "";
            SearchGroupNames = false;
        }

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Cookie", HelpText = "Login cookie from website")]
        public string Cookie { get; set; }

        [FieldDefinition(3, Label = "Search Group Names", Type = FieldType.Checkbox, HelpText = "Search Group Names Only")]
        public bool SearchGroupNames { get; set; }

        [FieldDefinition(4)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
