using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using AngleSharp.Html.Parser;
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

namespace NzbDrone.Core.Indexers.Definitions
{
    public class Anidex : TorrentIndexerBase<AnidexSettings>
    {
        public override string Name => "Anidex";
        public override string[] IndexerUrls => new[] { "https://anidex.info/" };
        public override string Description => "Anidex is a Public torrent tracker and indexer, primarily for English fansub groups of anime";
        public override string Language => "en-US";
        public override Encoding Encoding => Encoding.UTF8;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public Anidex(IIndexerHttpClient httpClient,
            IEventAggregator eventAggregator,
            IIndexerStatusService indexerStatusService,
            IConfigService configService,
            Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AnidexRequestGenerator(Settings, Capabilities);
        }

        public override IParseIndexerResponse GetParser()
        {
            return new AnidexParser(Settings, Capabilities.Categories);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q,
                },
                BookSearchParams = new List<BookSearchParam>
                {
                    BookSearchParam.Q,
                }
            };

            caps.Categories.AddCategoryMapping("1", NewznabStandardCategory.TVAnime, "Anime - Sub");
            caps.Categories.AddCategoryMapping("2", NewznabStandardCategory.TVAnime, "Anime - Raw");
            caps.Categories.AddCategoryMapping("3", NewznabStandardCategory.TVAnime, "Anime - Dub");
            caps.Categories.AddCategoryMapping("4", NewznabStandardCategory.TVAnime, "LA - Sub");
            caps.Categories.AddCategoryMapping("5", NewznabStandardCategory.TVAnime, "LA - Raw");
            caps.Categories.AddCategoryMapping("6", NewznabStandardCategory.BooksEBook, "Light Novel");
            caps.Categories.AddCategoryMapping("7", NewznabStandardCategory.BooksComics, "Manga - TLed");
            caps.Categories.AddCategoryMapping("8", NewznabStandardCategory.BooksComics, "Manga - Raw");
            caps.Categories.AddCategoryMapping("9", NewznabStandardCategory.AudioMP3, "♫ - Lossy");
            caps.Categories.AddCategoryMapping("10", NewznabStandardCategory.AudioLossless, "♫ - Lossless");
            caps.Categories.AddCategoryMapping("11", NewznabStandardCategory.AudioVideo, "♫ - Video");
            caps.Categories.AddCategoryMapping("12", NewznabStandardCategory.PCGames, "Games");
            caps.Categories.AddCategoryMapping("13", NewznabStandardCategory.PC0day, "Applications");
            caps.Categories.AddCategoryMapping("14", NewznabStandardCategory.XXXImageSet, "Pictures");
            caps.Categories.AddCategoryMapping("15", NewznabStandardCategory.XXX, "Adult Video");
            caps.Categories.AddCategoryMapping("16", NewznabStandardCategory.Other, "Other");

            return caps;
        }
    }

    public class AnidexRequestGenerator : IIndexerRequestGenerator
    {
        private readonly AnidexSettings _settings;
        private readonly IndexerCapabilities _capabilities;

        public AnidexRequestGenerator(AnidexSettings settings, IndexerCapabilities capabilities)
        {
            _settings = settings;
            _capabilities = capabilities;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests($"{searchCriteria.SanitizedSearchTerm}", searchCriteria.Categories));

            return pageableRequests;
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
        {
            var parameters = new NameValueCollection
            {
                { "page", "search" },
                { "s", "upload_timestamp" },
                { "o", "desc" },
                { "group_id", "0" }, // All groups
                { "q", term ?? string.Empty }
            };

            if (_settings.AuthorisedOnly)
            {
                parameters.Add("a", "1");
            }

            var searchUrl = $"{_settings.BaseUrl}?{parameters.GetQueryString()}";

            var queryCats = _capabilities.Categories.MapTorznabCapsToTrackers(categories);

            if (queryCats.Any() && _capabilities.Categories.GetTrackerCategories().Except(queryCats).Any())
            {
                searchUrl += "&id=" + string.Join(",", queryCats);
            }

            if (_settings.LanguagesOnly.Any())
            {
                searchUrl += "&lang_id=" + string.Join(",", _settings.LanguagesOnly);
            }

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

            yield return request;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class AnidexParser : IParseIndexerResponse
    {
        private readonly AnidexSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public AnidexParser(AnidexSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new IndexerException(indexerResponse, $"Anidex search returned unexpected result. Expected 200 OK but got {indexerResponse.HttpResponse.StatusCode}.");
            }

            var releaseInfos = new List<ReleaseInfo>();

            var parser = new HtmlParser();
            using var dom = parser.ParseDocument(indexerResponse.Content);

            var rows = dom.QuerySelectorAll("div#content table > tbody > tr");
            foreach (var row in rows)
            {
                var downloadUrl = _settings.BaseUrl + row.QuerySelector("a[href^=\"/dl/\"]")?.GetAttribute("href");
                var infoUrl = _settings.BaseUrl + row.QuerySelector("td:nth-child(3) a")?.GetAttribute("href");

                var title = row.QuerySelector("td:nth-child(3) span")?.GetAttribute("title")?.Trim();
                var language = row.QuerySelector("td:nth-child(1) img")?.GetAttribute("title")?.Trim();

                if (language.IsNotNullOrWhiteSpace())
                {
                    title += $" [{language}]";
                }

                var categoryLink = row.QuerySelector("td:nth-child(1) a").GetAttribute("href");
                var cat = ParseUtil.GetArgumentFromQueryString(categoryLink, "id");
                var categories = _categories.MapTrackerCatToNewznab(cat);

                var seeders = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(9)")?.TextContent);
                var peers = seeders + ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(10)")?.TextContent.Trim());

                var added = row.QuerySelector("td:nth-child(8)").GetAttribute("title").Trim();

                var release = new TorrentInfo
                {
                    Guid = infoUrl,
                    InfoUrl = infoUrl,
                    DownloadUrl = downloadUrl,
                    MagnetUrl = row.QuerySelector("a[href^=\"magnet:?\"]")?.GetAttribute("href"),
                    Title = title,
                    Categories = categories,
                    Seeders = seeders,
                    Peers = peers,
                    Size =  ParseUtil.GetBytes(row.QuerySelector("td:nth-child(7)")?.TextContent.Trim()),
                    Grabs = ParseUtil.CoerceInt(row.QuerySelector("td:nth-child(11)")?.TextContent),
                    PublishDate = DateTime.ParseExact(added, "yyyy-MM-dd HH:mm:ss UTC", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1
                };

                releaseInfos.Add(release);
            }

            return releaseInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class AnidexSettings : NoAuthTorrentBaseSettings
    {
        public AnidexSettings()
        {
            AuthorisedOnly = false;
            LanguagesOnly = Array.Empty<int>();
        }

        [FieldDefinition(2, Label = "Authorised Only", Type = FieldType.Checkbox, HelpText = "Search authorised torrents only")]
        public bool AuthorisedOnly { get; set; }

        [FieldDefinition(3, Label = "Languages Only", Type = FieldType.Select, SelectOptions = typeof(AnidexLanguage), HelpText = "Search selected languages only. None ticked = ALL.")]
        public IEnumerable<int> LanguagesOnly { get; set; }
    }

    public enum AnidexLanguage
    {
        [FieldOption(Hint = "English")]
        GB = 1,

        [FieldOption(Hint = "Japanese")]
        JP = 2,

        [FieldOption(Hint = "Polish")]
        PL = 3,

        [FieldOption(Hint = "Serbo-Croatian")]
        RS = 4,

        [FieldOption(Hint = "Dutch")]
        NL = 5,

        [FieldOption(Hint = "Italian")]
        IT = 6,

        [FieldOption(Hint = "Russian")]
        RU = 7,

        [FieldOption(Hint = "German")]
        DE = 8,

        [FieldOption(Hint = "Hungarian")]
        HU = 9,

        [FieldOption(Hint = "French")]
        FR = 10,

        [FieldOption(Hint = "Finnish")]
        FI = 11,

        [FieldOption(Hint = "Vietnamese")]
        VN = 12,

        [FieldOption(Hint = "Greek")]
        GR = 13,

        [FieldOption(Hint = "Bulgarian")]
        BG = 14,

        [FieldOption(Hint = "Spanish (Spain)")]
        ES = 15,

        [FieldOption(Hint = "Portuguese (Brazil)")]
        BR = 16,

        [FieldOption(Hint = "Portuguese (Portugal)")]
        PT = 17,

        [FieldOption(Hint = "Swedish")]
        SE = 18,

        [FieldOption(Hint = "Arabic")]
        SA = 19,

        [FieldOption(Hint = "Danish")]
        DK = 20,

        [FieldOption(Hint = "Chinese (Simplified)")]
        CN = 21,

        [FieldOption(Hint = "Bengali")]
        BD = 22,

        [FieldOption(Hint = "Romanian")]
        RO = 23,

        [FieldOption(Hint = "Czech")]
        CZ = 24,

        [FieldOption(Hint = "Mongolian")]
        MN = 25,

        [FieldOption(Hint = "Turkish")]
        TR = 26,

        [FieldOption(Hint = "Indonesian")]
        ID = 27,

        [FieldOption(Hint = "Korean")]
        KR = 28,

        [FieldOption(Hint = "Spanish (LATAM)")]
        MX = 29,

        [FieldOption(Hint = "Persian")]
        IR = 30,

        [FieldOption(Hint = "Malaysian")]
        MY = 31,
    }
}
