using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
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
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class SuperBits : TorrentIndexerBase<SuperBitsSettings>
    {
        public override string Name => "SuperBits";

        public override string[] IndexerUrls => new string[] { "https://superbits.org/" };
        public override string Description => "Superbits is a SWEDISH Private Torrent Tracker for MOVIES / TV / GENERAL";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public SuperBits(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new SuperBitsRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new SuperBitsParser(Settings, Capabilities.Categories);
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
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
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

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.MoviesDVD, "DVD-R Swesub");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TV, "DVD-R TV");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.BooksEBook, "eBok");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.MoviesHD, "Film 1080");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.Movies3D, "Film 3D");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.MoviesHD, "Film 720");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.MoviesBluRay, "Film Bluray");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.TV, "Svensk TV");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.AudioAudiobook, "Ljudböcker");
            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.AudioVideo, "Musikvideos");
            caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.BooksMags, "E-tidningar");
            caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.Audio, "Musik");
            caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.Other, "Omslag");
            caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.Other, "Övrigt");
            caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.PCGames, "PC-Spel");
            caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.PC0day, "Program");
            caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.ConsolePS3, "PS3");
            caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.TV, "TV");
            caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.ConsoleWii, "Wii");
            caps.Categories.AddCategoryMapping(20, NewznabStandardCategory.ConsoleXBox, "Xbox");
            caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.MoviesOther, "Xvid");
            caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.XXX, "XXX");
            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.MoviesUHD, "Film 4K");
            caps.Categories.AddCategoryMapping(26, NewznabStandardCategory.TV, "TV DK");
            caps.Categories.AddCategoryMapping(27, NewznabStandardCategory.TV, "TV NO");
            caps.Categories.AddCategoryMapping(28, NewznabStandardCategory.TV, "TV FI");
            return caps;
        }
    }

    public class SuperBitsRequestGenerator : IIndexerRequestGenerator
    {
        public SuperBitsSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public SuperBitsRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories, string imdbId = null)
        {
            var searchUrl = Settings.BaseUrl + "api/v1/torrents";

            // And this was option one from
            // https://github.com/Jackett/Jackett/pull/7166#discussion_r376817517
            var queryCollection = new NameValueCollection();
            var searchString = term;

            queryCollection.Add("extendedSearch", "false");
            queryCollection.Add("freeleech", "false");
            queryCollection.Add("index", "0");
            queryCollection.Add("limit", "100");
            queryCollection.Add("order", "desc");
            queryCollection.Add("page", "search");

            if (imdbId.IsNotNullOrWhiteSpace())
            {
                queryCollection.Add("searchText", imdbId);
            }
            else
            {
                queryCollection.Add("searchText", searchString);
            }

            queryCollection.Add("sort", "d");
            queryCollection.Add("section", "all");
            queryCollection.Add("stereoscopic", "false");
            queryCollection.Add("sweaudio", "false");
            queryCollection.Add("swesub", "false");
            queryCollection.Add("watchview", "false");

            searchUrl += "?" + queryCollection.GetQueryString();

            foreach (var cat in Capabilities.Categories.MapTorznabCapsToTrackers(categories))
            {
                searchUrl += "&categories[]=" + cat;
            }

            var request = new IndexerRequest(searchUrl, HttpAccept.Json);

            request.HttpRequest.Headers.Add("Referer", Settings.BaseUrl);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories, searchCriteria.FullImdbId));

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

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories, searchCriteria.FullImdbId));

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

    public class SuperBitsParser : IParseIndexerResponse
    {
        private readonly SuperBitsSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public SuperBitsParser(SuperBitsSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<TorrentInfo>();

            var json = JsonConvert.DeserializeObject<dynamic>(indexerResponse.Content);
            foreach (var row in json ?? Enumerable.Empty<dynamic>())
            {
                var release = new TorrentInfo();
                var descriptions = new List<string>();
                var tags = new List<string>();

                release.MinimumRatio = 1.1;
                release.MinimumSeedTime = 172800; // 48 hours
                release.Title = row.name;
                release.Categories = _categories.MapTrackerCatToNewznab(row.category.ToString());
                release.Size = row.size;
                release.Seeders = row.seeders;
                release.Peers = row.leechers + release.Seeders;
                release.PublishDate = DateTime.ParseExact(row.added.ToString() + " +01:00", "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
                release.Files = row.numfiles;
                release.Grabs = row.times_completed;

                release.InfoUrl = _settings.BaseUrl + "torrent/" + row.id.ToString() + "/";
                release.Guid = release.InfoUrl;
                release.DownloadUrl = _settings.BaseUrl + "api/v1/torrents/download/" + row.id.ToString();

                if (row.frileech == 1)
                {
                    release.DownloadVolumeFactor = 0;
                }
                else
                {
                    release.DownloadVolumeFactor = 1;
                }

                release.UploadVolumeFactor = 1;

                //if (!string.IsNullOrWhiteSpace(row.customcover.ToString()))
                //{
                //    release.Poster = new Uri(SiteLink + row.customcover);
                //}
                if (row.imdbid2 != null && row.imdbid2.ToString().StartsWith("tt"))
                {
                    release.ImdbId = ParseUtil.CoerceInt(row.imdbid2.ToString().Substring(2));
                    descriptions.Add("Title: " + row.title);
                    descriptions.Add("Year: " + row.year);
                    descriptions.Add("Genres: " + row.genres);
                    descriptions.Add("Tagline: " + row.tagline);
                    descriptions.Add("Cast: " + row.cast);
                    descriptions.Add("Rating: " + row.rating);
                    descriptions.Add("Plot: " + row.plot);

                    //release.Poster = new Uri(SiteLink + "img/imdb/" + row.imdbid2 + ".jpg");
                }

                if ((int)row.p2p == 1)
                {
                    tags.Add("P2P");
                }

                if ((int)row.pack == 1)
                {
                    tags.Add("Pack");
                }

                if ((int)row.reqid != 0)
                {
                    tags.Add("Request");
                }

                if ((int)row.sweaudio != 0)
                {
                    tags.Add("Swedish audio");
                }

                if ((int)row.swesub != 0)
                {
                    tags.Add("Swedish subtitles");
                }

                if (tags.Count > 0)
                {
                    descriptions.Add("Tags: " + string.Join(", ", tags));
                }

                var preDate = row.preDate.ToString();
                if (!string.IsNullOrWhiteSpace(preDate) && preDate != "1970-01-01 01:00:00")
                {
                    descriptions.Add("PRE: " + preDate);
                }

                descriptions.Add("Section: " + row.section);

                //release.Description = string.Join("<br>\n", descriptions);
                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class SuperBitsSettingsValidator : AbstractValidator<SuperBitsSettings>
    {
        public SuperBitsSettingsValidator()
        {
            RuleFor(c => c.Cookie).NotEmpty();
        }
    }

    public class SuperBitsSettings : IIndexerSettings
    {
        private static readonly SuperBitsSettingsValidator Validator = new SuperBitsSettingsValidator();

        public SuperBitsSettings()
        {
            Cookie = "";
        }

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2, Label = "Cookie", HelpText = "Site Cookie")]
        public string Cookie { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
