using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.FileList
{
    public class FileList : HttpIndexerBase<FileListSettings>
    {
        public override string Name => "FileList.io";
        public override string BaseUrl => "https://filelist.io";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;
        public override bool SupportsRedirect => true;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public FileList(IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new FileListRequestGenerator() { Settings = Settings, BaseUrl = BaseUrl };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new FileListParser(Settings, BaseUrl);
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
                       },
                Flags = new List<IndexerFlag>
                {
                    IndexerFlag.FreeLeech
                }
            };

            caps.Categories.AddCategoryMapping(1, NewznabStandardCategory.MoviesSD, "Filme SD");
            caps.Categories.AddCategoryMapping(2, NewznabStandardCategory.MoviesDVD, "Filme DVD");
            caps.Categories.AddCategoryMapping(3, NewznabStandardCategory.MoviesForeign, "Filme DVD-RO");
            caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.MoviesHD, "Filme HD");
            caps.Categories.AddCategoryMapping(5, NewznabStandardCategory.AudioLossless, "FLAC");
            caps.Categories.AddCategoryMapping(6, NewznabStandardCategory.MoviesUHD, "Filme 4K");
            caps.Categories.AddCategoryMapping(7, NewznabStandardCategory.XXX, "XXX");
            caps.Categories.AddCategoryMapping(8, NewznabStandardCategory.PC, "Programe");
            caps.Categories.AddCategoryMapping(9, NewznabStandardCategory.PCGames, "Jocuri PC");
            caps.Categories.AddCategoryMapping(10, NewznabStandardCategory.Console, "Jocuri Console");
            caps.Categories.AddCategoryMapping(11, NewznabStandardCategory.Audio, "Audio");
            caps.Categories.AddCategoryMapping(12, NewznabStandardCategory.AudioVideo, "Videoclip");
            caps.Categories.AddCategoryMapping(13, NewznabStandardCategory.TVSport, "Sport");
            caps.Categories.AddCategoryMapping(15, NewznabStandardCategory.TV, "Desene");
            caps.Categories.AddCategoryMapping(16, NewznabStandardCategory.Books, "Docs");
            caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.PC, "Linux");
            caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.Other, "Diverse");
            caps.Categories.AddCategoryMapping(19, NewznabStandardCategory.MoviesForeign, "Filme HD-RO");
            caps.Categories.AddCategoryMapping(20, NewznabStandardCategory.MoviesBluRay, "Filme Blu-Ray");
            caps.Categories.AddCategoryMapping(21, NewznabStandardCategory.TVHD, "Seriale HD");
            caps.Categories.AddCategoryMapping(22, NewznabStandardCategory.PCMobileOther, "Mobile");
            caps.Categories.AddCategoryMapping(23, NewznabStandardCategory.TVSD, "Seriale SD");
            caps.Categories.AddCategoryMapping(24, NewznabStandardCategory.TVAnime, "Anime");
            caps.Categories.AddCategoryMapping(25, NewznabStandardCategory.Movies3D, "Filme 3D");
            caps.Categories.AddCategoryMapping(26, NewznabStandardCategory.MoviesBluRay, "Filme 4K Blu-Ray");
            caps.Categories.AddCategoryMapping(27, NewznabStandardCategory.TVUHD, "Seriale 4K");

            return caps;
        }
    }
}
