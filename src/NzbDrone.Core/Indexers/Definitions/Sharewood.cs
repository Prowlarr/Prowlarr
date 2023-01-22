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

namespace NzbDrone.Core.Indexers.Definitions;

public class Sharewood : TorrentIndexerBase<SharewoodSettings>
{
    public override string Name => "Sharewood API";
    public override string[] IndexerUrls => new[] { "https://www.sharewood.tv/" };
    public override string Description => "Sharewood is a Semi-Private FRENCH Torrent Tracker for GENERAL";
    public override string Language => "fr-FR";
    public override Encoding Encoding => Encoding.UTF8;
    public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
    public override IndexerPrivacy Privacy => IndexerPrivacy.SemiPrivate;
    public override IndexerCapabilities Capabilities => SetCapabilities();
    public override TimeSpan RateLimit => TimeSpan.FromSeconds(2.1);

    public Sharewood(IIndexerHttpClient httpClient,
                     IEventAggregator eventAggregator,
                     IIndexerStatusService indexerStatusService,
                     IConfigService configService,
                     Logger logger)
        : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
    {
    }

    public override IIndexerRequestGenerator GetRequestGenerator()
    {
        return new SharewoodRequestGenerator(Settings, Capabilities);
    }

    public override IParseIndexerResponse GetParser()
    {
        return new SharewoodParser(Settings, Capabilities.Categories);
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
            },
            SupportsRawSearch = true
        };

        caps.Categories.AddCategoryMapping("9", NewznabStandardCategory.Movies, "Films");
        caps.Categories.AddCategoryMapping("10", NewznabStandardCategory.TV, "Série");
        caps.Categories.AddCategoryMapping("11", NewznabStandardCategory.MoviesOther, "Film Animation");
        caps.Categories.AddCategoryMapping("12", NewznabStandardCategory.TVAnime, "Série Animation");
        caps.Categories.AddCategoryMapping("13", NewznabStandardCategory.TVDocumentary, "Documentaire");
        caps.Categories.AddCategoryMapping("14", NewznabStandardCategory.TVOther, "Emission TV");
        caps.Categories.AddCategoryMapping("15", NewznabStandardCategory.TVOther, "Spectacle/Concert");
        caps.Categories.AddCategoryMapping("16", NewznabStandardCategory.TVSport, "Sport");
        caps.Categories.AddCategoryMapping("17", NewznabStandardCategory.AudioOther, "Karaoké Vidéo");
        caps.Categories.AddCategoryMapping("18", NewznabStandardCategory.AudioOther, "Karaoké");
        caps.Categories.AddCategoryMapping("20", NewznabStandardCategory.Audio, "Musique");
        caps.Categories.AddCategoryMapping("21", NewznabStandardCategory.AudioOther, "Podcast");
        caps.Categories.AddCategoryMapping("22", NewznabStandardCategory.Audio, "Sample");
        caps.Categories.AddCategoryMapping("23", NewznabStandardCategory.AudioAudiobook, "Ebook Audio");
        caps.Categories.AddCategoryMapping("24", NewznabStandardCategory.Books, "BD");
        caps.Categories.AddCategoryMapping("25", NewznabStandardCategory.BooksComics, "Comic");
        caps.Categories.AddCategoryMapping("26", NewznabStandardCategory.BooksOther, "Manga");
        caps.Categories.AddCategoryMapping("27", NewznabStandardCategory.Books, "Livre");
        caps.Categories.AddCategoryMapping("28", NewznabStandardCategory.BooksMags, "Presse");
        caps.Categories.AddCategoryMapping("29", NewznabStandardCategory.Audio, "Application Linux");
        caps.Categories.AddCategoryMapping("30", NewznabStandardCategory.PC, "Application Window");
        caps.Categories.AddCategoryMapping("31", NewznabStandardCategory.PCMac, "Application Mac");
        caps.Categories.AddCategoryMapping("34", NewznabStandardCategory.PCMobileiOS, "Application Smartphone/Tablette");
        caps.Categories.AddCategoryMapping("34", NewznabStandardCategory.PCMobileAndroid, "Application Smartphone/Tablette");
        caps.Categories.AddCategoryMapping("35", NewznabStandardCategory.Other, "GPS");
        caps.Categories.AddCategoryMapping("36", NewznabStandardCategory.Audio, "Jeux Linux");
        caps.Categories.AddCategoryMapping("37", NewznabStandardCategory.PCGames, "Jeux Windows");
        caps.Categories.AddCategoryMapping("39", NewznabStandardCategory.ConsoleNDS, "Jeux Nintendo");
        caps.Categories.AddCategoryMapping("39", NewznabStandardCategory.ConsoleWii, "Jeux Nintendo");
        caps.Categories.AddCategoryMapping("39", NewznabStandardCategory.ConsoleWiiware, "Jeux Nintendo");
        caps.Categories.AddCategoryMapping("39", NewznabStandardCategory.Console3DS, "Jeux Nintendo");
        caps.Categories.AddCategoryMapping("39", NewznabStandardCategory.ConsoleWiiU, "Jeux Nintendo");
        caps.Categories.AddCategoryMapping("41", NewznabStandardCategory.PCMobileAndroid, "PC/Mobile-Android");
        caps.Categories.AddCategoryMapping("42", NewznabStandardCategory.PCGames, "Jeux Microsoft");
        caps.Categories.AddCategoryMapping("44", NewznabStandardCategory.XXX, "XXX Films");
        caps.Categories.AddCategoryMapping("45", NewznabStandardCategory.XXXOther, "XXX Hentai");
        caps.Categories.AddCategoryMapping("47", NewznabStandardCategory.XXXImageSet, "XXX Images");
        caps.Categories.AddCategoryMapping("48", NewznabStandardCategory.XXXOther, "XXX Jeu-Vidéo");
        caps.Categories.AddCategoryMapping("50", NewznabStandardCategory.OtherMisc, "Formation Logiciels");
        caps.Categories.AddCategoryMapping("49", NewznabStandardCategory.OtherMisc, "Formations Vidéos");
        caps.Categories.AddCategoryMapping("51", NewznabStandardCategory.XXXOther, "XXX Ebooks");
        caps.Categories.AddCategoryMapping("52", NewznabStandardCategory.AudioVideo, "Vidéos-Clips");
        caps.Categories.AddCategoryMapping("51", NewznabStandardCategory.XXXOther, "XXX Ebooks");
        caps.Categories.AddCategoryMapping("51", NewznabStandardCategory.XXXOther, "XXX Ebooks");

        return caps;
    }
}

public class SharewoodRequestGenerator : IIndexerRequestGenerator
{
    private readonly SharewoodSettings _settings;
    private readonly IndexerCapabilities _capabilities;

    public SharewoodRequestGenerator(SharewoodSettings settings, IndexerCapabilities capabilities)
    {
        _settings = settings;
        _capabilities = capabilities;
    }

    public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        var queryCats = _capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories);

        if (queryCats.Any())
        {
            queryCats.ForEach(cat => pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", cat)));
        }
        else
        {
            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", null, 100));
        }

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        var queryCats = _capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories);

        if (queryCats.Any())
        {
            queryCats.ForEach(cat => pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", cat)));
        }
        else
        {
            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", null, 100));
        }

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        var queryCats = _capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories);

        if (queryCats.Any())
        {
            queryCats.ForEach(cat => pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedTvSearchString}", cat)));
        }
        else
        {
            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedTvSearchString}", null, 100));
        }

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        var queryCats = _capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories);

        if (queryCats.Any())
        {
            queryCats.ForEach(cat => pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", cat)));
        }
        else
        {
            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", null, 100));
        }

        return pageableRequests;
    }

    public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
    {
        var pageableRequests = new IndexerPageableRequestChain();

        var queryCats = _capabilities.Categories.MapTorznabCapsToTrackers(searchCriteria.Categories);

        if (queryCats.Any())
        {
            queryCats.ForEach(cat => pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", cat)));
        }
        else
        {
            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", null, 100));
        }

        return pageableRequests;
    }

    private IEnumerable<IndexerRequest> GetPagedRequests(string term, string categoryId = null, int limit = 25)
    {
        var searchUrl = $"{_settings.BaseUrl.TrimEnd('/')}/api/{_settings.Passkey}";

        var parameters = new NameValueCollection
        {
            { "limit", limit.ToString() }
        };

        if (categoryId.IsNotNullOrWhiteSpace())
        {
            parameters.Add("subcategory", categoryId);
        }

        if (term.IsNotNullOrWhiteSpace())
        {
            searchUrl += "/search";
            parameters.Add("name", term);
        }
        else
        {
            searchUrl += "/last-torrents";
        }

        if (parameters.Count > 0)
        {
            searchUrl += $"?{parameters.GetQueryString()}";
        }

        yield return new IndexerRequest(searchUrl, HttpAccept.Json);
    }

    public Func<IDictionary<string, string>> GetCookies { get; set; }
    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class SharewoodParser : IParseIndexerResponse
{
    private readonly SharewoodSettings _settings;
    private readonly IndexerCapabilitiesCategories _categories;

    public SharewoodParser(SharewoodSettings settings, IndexerCapabilitiesCategories categories)
    {
        _settings = settings;
        _categories = categories;
    }

    public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
    {
        if (indexerResponse.HttpResponse.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new IndexerAuthException("Passkey invalid or not authorized.");
        }

        if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new IndexerException(indexerResponse, $"Search returned unexpected result. Expected 200 OK but got {indexerResponse.HttpResponse.StatusCode}.");
        }

        var releaseInfos = new List<ReleaseInfo>();

        var results = JsonConvert.DeserializeObject<List<SharewoodRelease>>(indexerResponse.Content);

        foreach (var row in results)
        {
            var infoUrl = $"{_settings.BaseUrl.TrimEnd('/')}/torrents/{row.Slug}.{row.Id}";
            var downloadUrl = $"{_settings.BaseUrl.TrimEnd('/')}/api/{_settings.Passkey}/{row.Id}/download";

            var release = new TorrentInfo
            {
                Guid = infoUrl,
                InfoUrl = infoUrl,
                DownloadUrl = downloadUrl,
                InfoHash = row.InfoHash,
                Title = GetTitle(row.Name, _settings),
                Categories = _categories.MapTrackerCatToNewznab(row.SubcategoryId),
                Seeders = row.Seeders,
                Peers = row.Seeders + row.Leechers,
                Size =  ParseUtil.GetBytes(row.Size),
                Grabs = row.Grabs,
                PublishDate = DateTime.Parse(row.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                DownloadVolumeFactor = row.FreeLeech ? 0 : 1,
                UploadVolumeFactor = row.DoubleUp ? 2 : 1
            };

            releaseInfos.Add(release);
        }

        return releaseInfos.ToArray();
    }

    private static string GetTitle(string title, SharewoodSettings settings)
    {
        if (settings.ReplaceMulti)
        {
            var replacementMulti = settings.ReplaceMultiLanguage switch
            {
                (int)SharewoodLanguage.French => "FRENCH",
                (int)SharewoodLanguage.MultiFrench => "MULTI.FRENCH",
                (int)SharewoodLanguage.English => "ENGLISH",
                (int)SharewoodLanguage.MultiEnglish => "MULTI.ENGLISH",
                (int)SharewoodLanguage.Vostfr => "VOSTFR",
                (int)SharewoodLanguage.MultiVostfr => "MULTI.VOSTFR",
                _ => "FRENCH"
            };

            title = Regex.Replace(title, @"\b(MULTI(?!.*(?:FRENCH|ENGLISH|VOSTFR)))\b", replacementMulti, RegexOptions.IgnoreCase);
        }

        if (settings.ReplaceVostfr)
        {
            title = Regex.Replace(title, @"\b(VOSTFR)\b", "ENGLISH", RegexOptions.IgnoreCase);
            title = Regex.Replace(title, @"\b(SUBFRENCH)\b", "ENGLISH", RegexOptions.IgnoreCase);
        }

        return title;
    }

    public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
}

public class SharewoodSettingsValidator : NoAuthSettingsValidator<SharewoodSettings>
{
    public SharewoodSettingsValidator()
    {
        RuleFor(c => c.Passkey).NotEmpty();
    }
}

public class SharewoodSettings : NoAuthTorrentBaseSettings
{
    private static readonly SharewoodSettingsValidator Validator = new ();

    public SharewoodSettings()
    {
        ReplaceMulti = false;
        ReplaceMultiLanguage = (int)SharewoodLanguage.French;
        ReplaceVostfr = false;
    }

    [FieldDefinition(2, Label = "Passkey", Type = FieldType.Password, Privacy = PrivacyLevel.Password, HelpText = "Site Passkey")]
    public string Passkey { get; set; }

    [FieldDefinition(3, Label = "Replace MULTI", Type = FieldType.Checkbox, HelpText = "Replace MULTI by another language in release name")]
    public bool ReplaceMulti { get; set; }

    [FieldDefinition(4, Label = "Replace MULTI by this language", Type = FieldType.Select, SelectOptions = typeof(SharewoodLanguage))]
    public int ReplaceMultiLanguage { get; set; }

    [FieldDefinition(5, Label = "Replace VOSTFR", Type = FieldType.Checkbox, HelpText = "Replace VOSTFR with ENGLISH")]
    public bool ReplaceVostfr { get; set; }

    public override NzbDroneValidationResult Validate()
    {
        return new NzbDroneValidationResult(Validator.Validate(this));
    }
}

public enum SharewoodLanguage
{
    [FieldOption(Label = "French", Hint = "FRENCH")]
    French = 1,

    [FieldOption(Label = "Multi.French", Hint = "MULTI.FRENCH")]
    MultiFrench = 2,

    [FieldOption(Label = "English", Hint = "ENGLISH")]
    English = 3,

    [FieldOption(Label = "Multi.English", Hint = "MULTI.ENGLISH")]
    MultiEnglish = 4,

    [FieldOption(Label = "VOSTFR", Hint = "VOSTFR")]
    Vostfr = 5,

    [FieldOption(Label = "Multi.VOSTFR", Hint = "MULTI.VOSTFR")]
    MultiVostfr = 6,
}

public class SharewoodRelease
{
    public int Id { get; set; }

    [JsonProperty(PropertyName = "info_hash")]
    public string InfoHash { get; set; }

    public string Type { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Size { get; set; }
    public int? Leechers { get; set; }
    public int? Seeders { get; set; }

    [JsonProperty(PropertyName = "times_completed")]
    public int Grabs { get; set; }

    [JsonProperty(PropertyName = "category_id")]
    public string CategoryId { get; set; }

    [JsonProperty(PropertyName = "subcategory_id")]
    public string SubcategoryId { get; set; }

    public string Language { get; set; }

    [JsonProperty(PropertyName = "free")]
    public bool FreeLeech { get; set; }

    [JsonProperty(PropertyName = "doubleup")]
    public bool DoubleUp { get; set; }

    [JsonProperty(PropertyName = "created_at")]
    public string CreatedAt { get; set; }
}
