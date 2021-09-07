using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.RegularExpressions;
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
    public class Anilibria : TorrentIndexerBase<AnilibriaSettings>
    {
        public override string Name => "Anilibria";
        public override string[] IndexerUrls => new string[] { "https://anilibria.tv/" };
        public override string Description => "Anilibria is russian anime voiceover group and eponymous anime tracker.";
        public override string Language => "ru-RU";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public Anilibria(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AnilibriaRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new AnilibriaParser(Settings, Capabilities.Categories);
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
                                   }
            };
            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.TVAnime, "ТВ");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.TVAnime, "ONA");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.TVAnime, "OVA");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.Movies, "Фильм");
            return caps;
        }
    }

    public class AnilibriaRequestGenerator : IIndexerRequestGenerator
    {
        public AnilibriaSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public AnilibriaRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
        {
            var apiUrl = Regex.Replace(Settings.BaseUrl, @"(https?:\/\/)(.*)", "$1api.$2v2");
            var queryCollection = new NameValueCollection
            {
                { "limit", "100" },
                { "filter", "names,code,torrents.list,season.year,type.string" }
            };

            if (string.IsNullOrWhiteSpace(term))
            {
                apiUrl += "/getUpdates?" + queryCollection.GetQueryString();
            }
            else
            {
                apiUrl += "/searchTitles?" + queryCollection.GetQueryString() + "&search=" + Uri.EscapeDataString(term);
            }

            var request = new IndexerRequest(apiUrl, HttpAccept.Json);
            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
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

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm), searchCriteria.Categories));

            return pageableRequests;
        }

        // Anilibria doesn't support music, but this function required by interface
        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        // Anilibria doesn't support books, but this function required by interface
        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            return new IndexerPageableRequestChain();
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class AnilibriaParser : IParseIndexerResponse
    {
        private readonly AnilibriaSettings _settings;
        private readonly IndexerCapabilitiesCategories _categories;

        public AnilibriaParser(AnilibriaSettings settings, IndexerCapabilitiesCategories categories)
        {
            _settings = settings;
            _categories = categories;
        }

        private string composeTitle(AnilibriaTitle tl, AnilibriaTorrent tr)
        {
            var title = tl.Names.Ru;
            title += " / " + tl.Names.En;
            if (tl.Names.Alternative is string)
            {
                title += " / " + tl.Names.Alternative;
            }

            title += " " + tl.Season.Year;
            title += " [" + tr.Quality.String + "]";
            if (!string.IsNullOrWhiteSpace(tr.Series.String))
            {
                title += " - E" + tr.Series.String;
            }

            return title;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();
            var queryResponseItems = JsonConvert.DeserializeObject<List<AnilibriaTitle>>(indexerResponse.Content);

            foreach (var tl in queryResponseItems)
            {
                foreach (var tr in tl.Torrents.List)
                {
                    var torrentInfo = new TorrentInfo
                    {
                        Title = composeTitle(tl, tr),
                        InfoUrl = string.Format("{0}/release/{1}.html", _settings.BaseUrl.TrimEnd('/'), tl.Code),
                        DownloadVolumeFactor = 0,
                        UploadVolumeFactor = 1,
                        Seeders = tr.Seeders,
                        Peers = tr.Leechers + tr.Seeders,
                        Grabs = tr.Downloads,
                        Categories = _categories.MapTrackerCatDescToNewznab(tl.Type.String),

                        // API provides timestamp in UTC+3 timezone, so we need to substract 3 hours
                        PublishDate = DateTimeUtil.UnixTimestampToDateTime(tr.UploadedTimestamp).AddHours(-3),
                        Guid = _settings.BaseUrl + tr.Url,
                        DownloadUrl = _settings.BaseUrl + tr.Url,
                        Size = tr.TotalSize,
                        Resolution = tr.Quality.Resolution,
                        Codec = tr.Quality.Encoder
                    };

                    torrentInfos.Add(torrentInfo);
                }
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class AnilibriaSettingsValidator : AbstractValidator<AnilibriaSettings>
    {
        public AnilibriaSettingsValidator()
        {
        }
    }

    public class AnilibriaSettings : IIndexerSettings
    {
        private static readonly AnilibriaSettingsValidator Validator = new AnilibriaSettingsValidator();

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public class AnilibriaTitle
    {
        public AnilibriaNames Names { get; set; }
        public string Code { get; set; }
        public AnilibriaTorrents Torrents { get; set; }
        public AnilibriaSeason Season { get; set; }
        public AnilibriaTitleType Type { get; set; }
    }

    public class AnilibriaTitleType
    {
        public string String { get; set; }
    }

    public class AnilibriaNames
    {
        public string Ru { get; set; }
        public string En { get; set; }
        public object Alternative { get; set; }
    }

    public class AnilibriaSeason
    {
        public long Year { get; set; }
    }

    public class AnilibriaTorrents
    {
        public AnilibriaTorrent[] List { get; set; }
    }

    public class AnilibriaTorrent
    {
        public AnilibriaSeries Series { get; set; }
        public AnilibriaQuality Quality { get; set; }
        public int Leechers { get; set; }
        public int Seeders { get; set; }
        public int Downloads { get; set; }

        [JsonProperty("total_size")]
        public long TotalSize { get; set; }
        public string Url { get; set; }

        [JsonProperty("uploaded_timestamp")]
        public long UploadedTimestamp { get; set; }
    }

    public class AnilibriaQuality
    {
        public string String { get; set; }
        public string Type { get; set; }
        public string Resolution { get; set; }
        public string Encoder { get; set; }
    }

    public class AnilibriaSeries
    {
        public long First { get; set; }
        public long Last { get; set; }
        public string String { get; set; }
    }
}
