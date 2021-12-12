using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using FluentValidation;
using Newtonsoft.Json;
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
    [Obsolete("Moved to YML for Cardigann v3")]
    public class TorrentParadiseMl : TorrentIndexerBase<TorrentParadiseMlSettings>
    {
        public override string Name => "TorrentParadiseMl";
        public override string[] IndexerUrls => new[] { "https://torrent-paradise.ml/" };
        public override string Language => "en-US";
        public override string Description => "The most innovative torrent site";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public TorrentParadiseMl(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new TorrentParadiseMlRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new TorrentParadiseMlParser(Settings);
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

            caps.Categories.AddCategoryMapping(8000, NewznabStandardCategory.Other);

            return caps;
        }
    }

    public class TorrentParadiseMlRequestGenerator : IIndexerRequestGenerator
    {
        public TorrentParadiseMlSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public TorrentParadiseMlRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term)
        {
            var searchTerm = term;

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = DateTime.Now.Year.ToString();
            }

            var qc = new NameValueCollection
            {
                { "q", searchTerm }
            };

            var searchUrl = string.Format("{0}/api/search?{1}", Settings.BaseUrl.TrimEnd('/'), qc.GetQueryString());

            var request = new IndexerRequest(searchUrl, HttpAccept.Json);

            yield return request;
        }

        public IndexerPageableRequestChain GetSearchRequests(MovieSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm)));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(MusicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm)));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(TvSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString)));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm)));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BasicSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedSearchTerm)));

            return pageableRequests;
        }

        public Func<IDictionary<string, string>> GetCookies { get; set; }
        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class TorrentParadiseMlParser : IParseIndexerResponse
    {
        private readonly TorrentParadiseMlSettings _settings;

        public TorrentParadiseMlParser(TorrentParadiseMlSettings settings)
        {
            _settings = settings;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            if (indexerResponse.HttpResponse.StatusCode != HttpStatusCode.OK)
            {
                // Remove cookie cache
                CookiesUpdater(null, null);

                throw new IndexerException(indexerResponse, $"Unexpected response status {indexerResponse.HttpResponse.StatusCode} code from API request");
            }

            var results = JsonConvert.DeserializeObject<List<TorrentParadiseResult>>(indexerResponse.Content);

            if (results == null)
            {
                return torrentInfos;
            }

            foreach (var result in results)
            {
                var magnetUrl = MagnetLinkBuilder.BuildPublicMagnetLink(result.Id, result.Text);

                var release = new TorrentInfo
                {
                    Title = result.Text,
                    Size = result.Size,
                    Seeders = result.Seeders,
                    Peers = result.Seeders + result.Leechers,
                    InfoHash = result.Id,
                    Guid = magnetUrl,
                    MagnetUrl = magnetUrl,
                    PublishDate = DateTime.UtcNow,
                    InfoUrl = _settings.BaseUrl,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1,
                    Categories = new List<IndexerCategory> { NewznabStandardCategory.Other }
                };

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        private class TorrentParadiseResult
        {
            public string Id { get; set; }
            public string Text { get; set; }

            [JsonProperty(PropertyName = "len")]
            public long? Size { get; set; }

            [JsonProperty(PropertyName = "s")]
            public int? Seeders { get; set; }

            [JsonProperty(PropertyName = "l")]
            public int? Leechers { get; set; }
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }

    public class TorrentParadiseMlSettingsValidator : AbstractValidator<TorrentParadiseMlSettings>
    {
    }

    public class TorrentParadiseMlSettings : IIndexerSettings
    {
        private static readonly TorrentParadiseMlSettingsValidator Validator = new TorrentParadiseMlSettingsValidator();

        [FieldDefinition(1, Label = "Base Url", Type = FieldType.Select, SelectOptionsProviderAction = "getUrls", HelpText = "Select which baseurl Prowlarr will use for requests to the site")]
        public string BaseUrl { get; set; }

        [FieldDefinition(2)]
        public IndexerBaseSettings BaseSettings { get; set; } = new IndexerBaseSettings();

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
