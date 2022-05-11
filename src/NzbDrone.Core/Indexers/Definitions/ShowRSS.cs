using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;
using FluentValidation;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class ShowRSS : TorrentIndexerBase<NoAuthTorrentBaseSettings>
    {
        public override string Name => "ShowRSS";
        public override string[] IndexerUrls => new string[] { "https://showrss.info/" };
        public override string Language => "en-US";
        public override string Description => "showRSS is a service that allows you to keep track of your favorite TV shows";
        public override Encoding Encoding => Encoding.UTF8;
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public ShowRSS(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new ShowRSSRequestGenerator() { Settings = Settings, Capabilities = Capabilities };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new ShowRSSParser(Settings);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                                   {
                                       TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep
                                   }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.TV);
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVSD);
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.TVHD);

            return caps;
        }
    }

    public class ShowRSSRequestGenerator : IIndexerRequestGenerator
    {
        public NoAuthTorrentBaseSettings Settings { get; set; }
        public IndexerCapabilities Capabilities { get; set; }

        public ShowRSSRequestGenerator()
        {
        }

        private IEnumerable<IndexerRequest> GetPagedRequests(string term, int[] categories)
        {
            var searchUrl = string.Format("{0}/other/all.rss", Settings.BaseUrl.TrimEnd('/'));

            var request = new IndexerRequest(searchUrl, HttpAccept.Html);

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

            pageableRequests.Add(GetPagedRequests(string.Format("{0}", searchCriteria.SanitizedTvSearchString), searchCriteria.Categories));

            return pageableRequests;
        }

        public IndexerPageableRequestChain GetSearchRequests(BookSearchCriteria searchCriteria)
        {
            var pageableRequests = new IndexerPageableRequestChain();

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

    public class ShowRSSParser : IParseIndexerResponse
    {
        private readonly NoAuthTorrentBaseSettings _settings;
        private string BrowseUrl => _settings.BaseUrl + "browse/";

        public ShowRSSParser(NoAuthTorrentBaseSettings settings)
        {
            _settings = settings;
        }

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            var torrentInfos = new List<ReleaseInfo>();

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(indexerResponse.Content);
            foreach (XmlNode node in xmlDoc.GetElementsByTagName("item"))
            {
                var title = node.SelectSingleNode(".//*[local-name()='raw_title']").InnerText;

                // TODO: Make sure we don't return all sorts of trash
                //if (!query.MatchQueryStringAND(title))
                //{
                //    continue;
                //}
                var category = title.Contains("720p") || title.Contains("1080p") ?
                    NewznabStandardCategory.TVHD :
                    NewznabStandardCategory.TVSD;

                var magnetUri = node.SelectSingleNode("link")?.InnerText;
                var publishDate = DateTime.Parse(node.SelectSingleNode("pubDate").InnerText, CultureInfo.InvariantCulture);
                var infoHash = node.SelectSingleNode(".//*[local-name()='info_hash']").InnerText;
                var details = BrowseUrl + node.SelectSingleNode(".//*[local-name()='show_id']").InnerText;

                var release = new TorrentInfo
                {
                    Title = title,
                    InfoUrl = details,
                    Categories = new List<IndexerCategory> { category },
                    Guid = magnetUri,
                    PublishDate = publishDate,
                    InfoHash = infoHash,
                    MagnetUrl = magnetUri,
                    Size = 512,
                    Seeders = 1,
                    Peers = 2,
                    DownloadVolumeFactor = 0,
                    UploadVolumeFactor = 1
                };

                torrentInfos.Add(release);
            }

            return torrentInfos.ToArray();
        }

        public Action<IDictionary<string, string>, DateTime?> CookiesUpdater { get; set; }
    }
}
