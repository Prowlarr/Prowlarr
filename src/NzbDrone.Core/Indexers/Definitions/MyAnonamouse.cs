using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
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
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class MyAnonamouse : TorrentIndexerBase<MyAnonamouseSettings>
    {
        public override string Name => "MyAnonamouse";

        public override string[] IndexerUrls => new string[] { "https://www.myanonamouse.net/" };
        public override string Description => "MyAnonaMouse (MAM) is a large ebook and audiobook tracker.";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public MyAnonamouse(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new MyAnonamouseRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new MyAnonamouseParser(Settings, Capabilities.Categories);
        }

        protected override IDictionary<string, string> GetCookies()
        {
            return CookieUtil.CookieHeaderToDictionary("mam_id=" + Settings.MamId);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                BookSearchParams = new List<BookSearchParam>
                       {
                           BookSearchParam.Q
                       }
            };

            caps.Categories.AddCategoryMapping("13", NewznabStandardCategory.AudioAudiobook, "AudioBooks");
            caps.Categories.AddCategoryMapping("14", NewznabStandardCategory.BooksEBook, "E-Books");
            caps.Categories.AddCategoryMapping("15", NewznabStandardCategory.AudioAudiobook, "Musicology");
            caps.Categories.AddCategoryMapping("16", NewznabStandardCategory.AudioAudiobook, "Radio");
            caps.Categories.AddCategoryMapping("39", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Action/Adventure");
            caps.Categories.AddCategoryMapping("49", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Art");
            caps.Categories.AddCategoryMapping("50", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Biographical");
            caps.Categories.AddCategoryMapping("83", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Business");
            caps.Categories.AddCategoryMapping("51", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Computer/Internet");
            caps.Categories.AddCategoryMapping("97", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Crafts");
            caps.Categories.AddCategoryMapping("40", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Crime/Thriller");
            caps.Categories.AddCategoryMapping("41", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Fantasy");
            caps.Categories.AddCategoryMapping("106", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Food");
            caps.Categories.AddCategoryMapping("42", NewznabStandardCategory.AudioAudiobook, "Audiobooks - General Fiction");
            caps.Categories.AddCategoryMapping("52", NewznabStandardCategory.AudioAudiobook, "Audiobooks - General Non-Fic");
            caps.Categories.AddCategoryMapping("98", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Historical Fiction");
            caps.Categories.AddCategoryMapping("54", NewznabStandardCategory.AudioAudiobook, "Audiobooks - History");
            caps.Categories.AddCategoryMapping("55", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Home/Garden");
            caps.Categories.AddCategoryMapping("43", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Horror");
            caps.Categories.AddCategoryMapping("99", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Humor");
            caps.Categories.AddCategoryMapping("84", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Instructional");
            caps.Categories.AddCategoryMapping("44", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Juvenile");
            caps.Categories.AddCategoryMapping("56", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Language");
            caps.Categories.AddCategoryMapping("45", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Literary Classics");
            caps.Categories.AddCategoryMapping("57", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Math/Science/Tech");
            caps.Categories.AddCategoryMapping("85", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Medical");
            caps.Categories.AddCategoryMapping("87", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Mystery");
            caps.Categories.AddCategoryMapping("119", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Nature");
            caps.Categories.AddCategoryMapping("88", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Philosophy");
            caps.Categories.AddCategoryMapping("58", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Pol/Soc/Relig");
            caps.Categories.AddCategoryMapping("59", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Recreation");
            caps.Categories.AddCategoryMapping("46", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Romance");
            caps.Categories.AddCategoryMapping("47", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Science Fiction");
            caps.Categories.AddCategoryMapping("53", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Self-Help");
            caps.Categories.AddCategoryMapping("89", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Travel/Adventure");
            caps.Categories.AddCategoryMapping("100", NewznabStandardCategory.AudioAudiobook, "Audiobooks - True Crime");
            caps.Categories.AddCategoryMapping("108", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Urban Fantasy");
            caps.Categories.AddCategoryMapping("48", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Western");
            caps.Categories.AddCategoryMapping("111", NewznabStandardCategory.AudioAudiobook, "Audiobooks - Young Adult");
            caps.Categories.AddCategoryMapping("60", NewznabStandardCategory.BooksEBook, "Ebooks - Action/Adventure");
            caps.Categories.AddCategoryMapping("71", NewznabStandardCategory.BooksEBook, "Ebooks - Art");
            caps.Categories.AddCategoryMapping("72", NewznabStandardCategory.BooksEBook, "Ebooks - Biographical");
            caps.Categories.AddCategoryMapping("90", NewznabStandardCategory.BooksEBook, "Ebooks - Business");
            caps.Categories.AddCategoryMapping("61", NewznabStandardCategory.BooksComics, "Ebooks - Comics/Graphic novels");
            caps.Categories.AddCategoryMapping("73", NewznabStandardCategory.BooksEBook, "Ebooks - Computer/Internet");
            caps.Categories.AddCategoryMapping("101", NewznabStandardCategory.BooksEBook, "Ebooks - Crafts");
            caps.Categories.AddCategoryMapping("62", NewznabStandardCategory.BooksEBook, "Ebooks - Crime/Thriller");
            caps.Categories.AddCategoryMapping("63", NewznabStandardCategory.BooksEBook, "Ebooks - Fantasy");
            caps.Categories.AddCategoryMapping("107", NewznabStandardCategory.BooksEBook, "Ebooks - Food");
            caps.Categories.AddCategoryMapping("64", NewznabStandardCategory.BooksEBook, "Ebooks - General Fiction");
            caps.Categories.AddCategoryMapping("74", NewznabStandardCategory.BooksEBook, "Ebooks - General Non-Fiction");
            caps.Categories.AddCategoryMapping("102", NewznabStandardCategory.BooksEBook, "Ebooks - Historical Fiction");
            caps.Categories.AddCategoryMapping("76", NewznabStandardCategory.BooksEBook, "Ebooks - History");
            caps.Categories.AddCategoryMapping("77", NewznabStandardCategory.BooksEBook, "Ebooks - Home/Garden");
            caps.Categories.AddCategoryMapping("65", NewznabStandardCategory.BooksEBook, "Ebooks - Horror");
            caps.Categories.AddCategoryMapping("103", NewznabStandardCategory.BooksEBook, "Ebooks - Humor");
            caps.Categories.AddCategoryMapping("115", NewznabStandardCategory.BooksEBook, "Ebooks - Illusion/Magic");
            caps.Categories.AddCategoryMapping("91", NewznabStandardCategory.BooksEBook, "Ebooks - Instructional");
            caps.Categories.AddCategoryMapping("66", NewznabStandardCategory.BooksEBook, "Ebooks - Juvenile");
            caps.Categories.AddCategoryMapping("78", NewznabStandardCategory.BooksEBook, "Ebooks - Language");
            caps.Categories.AddCategoryMapping("67", NewznabStandardCategory.BooksEBook, "Ebooks - Literary Classics");
            caps.Categories.AddCategoryMapping("79", NewznabStandardCategory.BooksMags, "Ebooks - Magazines/Newspapers");
            caps.Categories.AddCategoryMapping("80", NewznabStandardCategory.BooksTechnical, "Ebooks - Math/Science/Tech");
            caps.Categories.AddCategoryMapping("92", NewznabStandardCategory.BooksEBook, "Ebooks - Medical");
            caps.Categories.AddCategoryMapping("118", NewznabStandardCategory.BooksEBook, "Ebooks - Mixed Collections");
            caps.Categories.AddCategoryMapping("94", NewznabStandardCategory.BooksEBook, "Ebooks - Mystery");
            caps.Categories.AddCategoryMapping("120", NewznabStandardCategory.BooksEBook, "Ebooks - Nature");
            caps.Categories.AddCategoryMapping("95", NewznabStandardCategory.BooksEBook, "Ebooks - Philosophy");
            caps.Categories.AddCategoryMapping("81", NewznabStandardCategory.BooksEBook, "Ebooks - Pol/Soc/Relig");
            caps.Categories.AddCategoryMapping("82", NewznabStandardCategory.BooksEBook, "Ebooks - Recreation");
            caps.Categories.AddCategoryMapping("68", NewznabStandardCategory.BooksEBook, "Ebooks - Romance");
            caps.Categories.AddCategoryMapping("69", NewznabStandardCategory.BooksEBook, "Ebooks - Science Fiction");
            caps.Categories.AddCategoryMapping("75", NewznabStandardCategory.BooksEBook, "Ebooks - Self-Help");
            caps.Categories.AddCategoryMapping("96", NewznabStandardCategory.BooksEBook, "Ebooks - Travel/Adventure");
            caps.Categories.AddCategoryMapping("104", NewznabStandardCategory.BooksEBook, "Ebooks - True Crime");
            caps.Categories.AddCategoryMapping("109", NewznabStandardCategory.BooksEBook, "Ebooks - Urban Fantasy");
            caps.Categories.AddCategoryMapping("70", NewznabStandardCategory.BooksEBook, "Ebooks - Western");
            caps.Categories.AddCategoryMapping("112", NewznabStandardCategory.BooksEBook, "Ebooks - Young Adult");
            caps.Categories.AddCategoryMapping("19", NewznabStandardCategory.AudioAudiobook, "Guitar/Bass Tabs");
            caps.Categories.AddCategoryMapping("20", NewznabStandardCategory.AudioAudiobook, "Individual Sheet");
            caps.Categories.AddCategoryMapping("24", NewznabStandardCategory.AudioAudiobook, "Individual Sheet MP3");
            caps.Categories.AddCategoryMapping("126", NewznabStandardCategory.AudioAudiobook, "Instructional Book with Video");
            caps.Categories.AddCategoryMapping("22", NewznabStandardCategory.AudioAudiobook, "Instructional Media - Music");
            caps.Categories.AddCategoryMapping("113", NewznabStandardCategory.AudioAudiobook, "Lick Library - LTP/Jam With");
            caps.Categories.AddCategoryMapping("114", NewznabStandardCategory.AudioAudiobook, "Lick Library - Techniques/QL");
            caps.Categories.AddCategoryMapping("17", NewznabStandardCategory.AudioAudiobook, "Music - Complete Editions");
            caps.Categories.AddCategoryMapping("26", NewznabStandardCategory.AudioAudiobook, "Music Book");
            caps.Categories.AddCategoryMapping("27", NewznabStandardCategory.AudioAudiobook, "Music Book MP3");
            caps.Categories.AddCategoryMapping("30", NewznabStandardCategory.AudioAudiobook, "Sheet Collection");
            caps.Categories.AddCategoryMapping("31", NewznabStandardCategory.AudioAudiobook, "Sheet Collection MP3");
            caps.Categories.AddCategoryMapping("127", NewznabStandardCategory.AudioAudiobook, "Radio -  Comedy");
            caps.Categories.AddCategoryMapping("130", NewznabStandardCategory.AudioAudiobook, "Radio - Drama");
            caps.Categories.AddCategoryMapping("128", NewznabStandardCategory.AudioAudiobook, "Radio - Factual/Documentary");
            caps.Categories.AddCategoryMapping("132", NewznabStandardCategory.AudioAudiobook, "Radio - Reading");

            return caps;
        }
    }

    public class MyAnonamouseRequestGenerator : IIndexerRequestGenerator
    {
        public MyAnonamouseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public MyAnonamouseRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
        {
            var qParams = new NameValueCollection
            {
                { "tor[text]", term },
                { "tor[srchIn][title]", "true" },
                { "tor[srchIn][author]", "true" },
                { "tor[searchType]", Settings.ExcludeVip ? "nVIP" : "all" }, // exclude VIP torrents
                { "tor[searchIn]", "torrents" },
                { "tor[hash]", "" },
                { "tor[sortType]", "default" },
                { "tor[startNumber]", "0" },
                { "thumbnails", "1" }, // gives links for thumbnail sized versions of their posters

                //{ "posterLink", "1"}, // gives links for a full sized poster
                //{ "dlLink", "1"}, // include the url to download the torrent
                { "description", "1" } // include the description

                //{"bookmarks", "0"} // include if the item is bookmarked or not
            };

            var catList = Capabilities.Categories.MapTorznabCapsToTrackers(categories);
            if (catList.Any())
            {
                var index = 0;
                foreach (var cat in catList)
                {
                    qParams.Add("tor[cat][" + index + "]", cat);
                    index++;
                }
            }
            else
            {
                qParams.Add("tor[cat][]", "0");
            }

            var urlSearch = Settings.BaseUrl + "tor/js/loadSearchJSONbasic.php";

            if (qParams.Count > 0)
            {
                urlSearch += $"?{qParams.GetQueryString()}";
            }

            var request = new IndexerRequest(urlSearch, HttpAccept.Json);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

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

    public class MyAnonamouseParser : IParseIndexerResponse
    {
        private readonly MyAnonamouseSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public MyAnonamouseParser(MyAnonamouseSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();

            var jsonResponse = JsonConvert.DeserializeObject<MyAnonamouseResponse>(indexerResponse.Content);

            var error = jsonResponse.Error;
            if (error != null && error == "Nothing returned, out of 0")
            {
                return torrentInfos.ToArray();
            }

            foreach (var item in jsonResponse.Data)
            {
                //TODO shift to ReleaseInfo object initializer for consistency
                var release = new TorrentInfo();

                var id = item.Id;
                release.Title = item.Title;

                // release.Description = item.Value<string>("description");
                var author = string.Empty;

                if (item.AuthorInfo != null)
                {
                    var authorInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(item.AuthorInfo);
                    author = authorInfo?.First().Value;
                }

                if (author != null)
                {
                    release.Title += " by " + author;
                }

                var flags = new List<string>();

                var langCode = item.LangCode;
                if (!string.IsNullOrEmpty(langCode))
                {
                    flags.Add(langCode);
                }

                var filetype = item.Filetype;
                if (!string.IsNullOrEmpty(filetype))
                {
                    flags.Add(filetype);
                }

                if (flags.Count > 0)
                {
                    release.Title += " [" + string.Join(" / ", flags) + "]";
                }

                if (item.Vip)
                {
                    release.Title += " [VIP]";
                }

                var category = item.Category;
                release.Categories = _categories.MapTrackerCatToNewznab(category);

                release.DownloadUrl = _settings.BaseUrl + "/tor/download.php?tid=" + id;
                release.InfoUrl = _settings.BaseUrl + "/t/" + id;
                release.Guid = release.InfoUrl;

                var dateStr = item.Added;
                var dateTime = DateTime.ParseExact(dateStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                release.PublishDate = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToLocalTime();

                release.Grabs = item.Grabs;
                release.Files = item.NumFiles;
                release.Seeders = item.Seeders;
                release.Peers = item.Leechers + release.Seeders;
                var size = item.Size;
                release.Size = ReleaseInfo.GetBytes(size);

                release.DownloadVolumeFactor = item.Free ? 0 : 1;
                release.UploadVolumeFactor = 1;

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class MyAnonamouseSettingsValidator : AbstractValidator<MyAnonamouseSettings>
    {
        public MyAnonamouseSettingsValidator()
        {
            RuleFor(c => c.MamId).NotEmpty();
        }
    }

    public class MyAnonamouseSettings : IIndexerSettings
    {
        private static readonly MyAnonamouseSettingsValidator Validator = new MyAnonamouseSettingsValidator();

        public MyAnonamouseSettings()
        {
            MamId = "";
        }

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Mam Id", HelpText = "Mam Session Id (Created Under Profile -> Security)")]
        public string MamId { get; set; }

        [FieldDefinition(3, Type = FieldType.Checkbox, Label = "Exclude VIP", HelpText = "Exclude VIP Torrents from search results")]
        public bool ExcludeVip { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public class MyAnonamouseTorrent
    {
        public int Id { get; set; }
        public string Title { get; set; }

        [JsonProperty(PropertyName = "author_info")]
        public string AuthorInfo { get; set; }

        [JsonProperty(PropertyName = "lang_code")]
        public string LangCode { get; set; }
        public string Filetype { get; set; }
        public bool Vip { get; set; }
        public bool Free { get; set; }
        public string Category { get; set; }
        public string Added { get; set; }

        [JsonProperty(PropertyName = "times_completed")]
        public int Grabs { get; set; }
        public int Seeders { get; set; }
        public int Leechers { get; set; }
        public int NumFiles { get; set; }
        public string Size { get; set; }
    }

    public class MyAnonamouseResponse
    {
        public string Error { get; set; }
        public List<MyAnonamouseTorrent> Data { get; set; }
    }
}
