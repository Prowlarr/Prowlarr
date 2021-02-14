using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.Indexers.AwesomeHD
{
    public class AwesomeHD : HttpIndexerBase<AwesomeHDSettings>
    {
        public override string Name => "AwesomeHD";
        public override string BaseUrl => "https://awesome-hd.club";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;

        public override IndexerCapabilities Capabilities => SetCapabilities();

        public override int PageSize => 50;

        public AwesomeHD(IHttpClient httpClient, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AwesomeHDRequestGenerator() { Settings = Settings, BaseUrl = BaseUrl };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new AwesomeHDRssParser(Settings, BaseUrl);
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                       {
                           TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId
                       },
                MovieSearchParams = new List<MovieSearchParam>
                       {
                           MovieSearchParam.Q, MovieSearchParam.ImdbId
                       },
                MusicSearchParams = new List<MusicSearchParam>
                       {
                           MusicSearchParam.Q
                       }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.MoviesHD);
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.TVHD);
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.Audio);

            return caps;
        }
    }
}
