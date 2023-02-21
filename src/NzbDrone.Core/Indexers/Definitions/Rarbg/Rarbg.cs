using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using NLog;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions.Rarbg
{
    public class Rarbg : TorrentIndexerBase<RarbgSettings>
    {
        public override string Name => "Rarbg";
        public override string[] IndexerUrls => new[] { "https://torrentapi.org/" };
        public override string[] LegacyUrls => new[] { "https://torrentapi.org" };
        public override string Description => "RARBG is a Public torrent site for MOVIES / TV / GENERAL";
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Public;
        public override IndexerCapabilities Capabilities => SetCapabilities();
        public override TimeSpan RateLimit => TimeSpan.FromSeconds(7);
        private readonly IRarbgTokenProvider _tokenProvider;

        public Rarbg(IRarbgTokenProvider tokenProvider, IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
            _tokenProvider = tokenProvider;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new RarbgRequestGenerator(_tokenProvider, RateLimit) { Settings = Settings, Categories = Capabilities.Categories };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new RarbgParser(Capabilities, _logger);
        }

        public static void CheckResponseByStatusCode(IndexerResponse response)
        {
            var responseCode = (int)response.HttpResponse.StatusCode;

            switch (responseCode)
            {
                case (int)HttpStatusCode.TooManyRequests:
                    throw new TooManyRequestsException(response.HttpRequest, response.HttpResponse, TimeSpan.FromMinutes(2));
                case 520:
                    throw new TooManyRequestsException(response.HttpRequest, response.HttpResponse, TimeSpan.FromMinutes(3));
                case (int)HttpStatusCode.OK:
                    break;
                default:
                    throw new IndexerException(response, "Indexer API call returned an unexpected StatusCode [{0}]", responseCode);
            }
        }

        private IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities
            {
                TvSearchParams = new List<TvSearchParam>
                {
                    TvSearchParam.Q, TvSearchParam.Season, TvSearchParam.Ep, TvSearchParam.ImdbId, TvSearchParam.TvdbId
                },
                MovieSearchParams = new List<MovieSearchParam>
                {
                    MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId
                },
                MusicSearchParams = new List<MusicSearchParam>
                {
                    MusicSearchParam.Q
                }
            };

            // caps.Categories.AddCategoryMapping(4, NewznabStandardCategory.XXX, "XXX (18+)"); // 3x is not supported by API #11848
            caps.Categories.AddCategoryMapping(14, NewznabStandardCategory.MoviesSD, "Movies/XVID");
            caps.Categories.AddCategoryMapping(17, NewznabStandardCategory.MoviesSD, "Movies/x264");
            caps.Categories.AddCategoryMapping(18, NewznabStandardCategory.TVSD, "TV Episodes");
            caps.Categories.AddCategoryMapping(23, NewznabStandardCategory.AudioMP3, "Music/MP3");
            caps.Categories.AddCategoryMapping(25, NewznabStandardCategory.AudioLossless, "Music/FLAC");
            caps.Categories.AddCategoryMapping(27, NewznabStandardCategory.PCGames, "Games/PC ISO");
            caps.Categories.AddCategoryMapping(28, NewznabStandardCategory.PCGames, "Games/PC RIP");
            caps.Categories.AddCategoryMapping(32, NewznabStandardCategory.ConsoleXBox360, "Games/XBOX-360");
            caps.Categories.AddCategoryMapping(33, NewznabStandardCategory.PCISO, "Software/PC ISO");
            caps.Categories.AddCategoryMapping(40, NewznabStandardCategory.ConsolePS3, "Games/PS3");
            caps.Categories.AddCategoryMapping(41, NewznabStandardCategory.TVHD, "TV HD Episodes");
            caps.Categories.AddCategoryMapping(42, NewznabStandardCategory.MoviesBluRay, "Movies/Full BD");
            caps.Categories.AddCategoryMapping(44, NewznabStandardCategory.MoviesHD, "Movies/x264/1080");
            caps.Categories.AddCategoryMapping(45, NewznabStandardCategory.MoviesHD, "Movies/x264/720");
            caps.Categories.AddCategoryMapping(46, NewznabStandardCategory.MoviesBluRay, "Movies/BD Remux");
            caps.Categories.AddCategoryMapping(47, NewznabStandardCategory.Movies3D, "Movies/x264/3D");
            caps.Categories.AddCategoryMapping(48, NewznabStandardCategory.MoviesHD, "Movies/XVID/720");
            caps.Categories.AddCategoryMapping(49, NewznabStandardCategory.TVUHD, "TV UHD Episodes");

            // torrentapi.org returns "Movies/TV-UHD-episodes" for some reason
            // possibly because thats what the category is called on the /top100.php page
            caps.Categories.AddCategoryMapping(49, NewznabStandardCategory.TVUHD, "Movies/TV-UHD-episodes");
            caps.Categories.AddCategoryMapping(50, NewznabStandardCategory.MoviesUHD, "Movies/x264/4k");
            caps.Categories.AddCategoryMapping(51, NewznabStandardCategory.MoviesUHD, "Movies/x265/4k");
            caps.Categories.AddCategoryMapping(52, NewznabStandardCategory.MoviesUHD, "Movs/x265/4k/HDR");
            caps.Categories.AddCategoryMapping(53, NewznabStandardCategory.ConsolePS4, "Games/PS4");
            caps.Categories.AddCategoryMapping(54, NewznabStandardCategory.MoviesHD, "Movies/x265/1080");

            return caps;
        }

        protected override async Task<IndexerQueryResult> FetchPage(IndexerRequest request, IParseIndexerResponse parser)
        {
            var response = await FetchIndexerResponse(request);

            CheckResponseByStatusCode(response);

            // try and recover from token errors
            var jsonResponse = new HttpResponse<RarbgResponse>(response.HttpResponse);

            if (jsonResponse.Resource.error_code.HasValue)
            {
                if (jsonResponse.Resource.error_code is 4 or 2)
                {
                    _logger.Debug("Invalid or expired token, refreshing token from Rarbg");
                    _tokenProvider.ExpireToken(Settings);
                    var newToken = _tokenProvider.GetToken(Settings, RateLimit);

                    var qs = HttpUtility.ParseQueryString(request.HttpRequest.Url.Query);
                    qs.Set("token", newToken);

                    request.HttpRequest.Url = request.Url.SetQuery(qs.GetQueryString());
                    response = await FetchIndexerResponse(request);
                }
                else if (jsonResponse.Resource.error_code is 5)
                {
                    _logger.Debug("Rarbg temp rate limit hit, retrying request");
                    response = await FetchIndexerResponse(request);
                }
            }

            try
            {
                var releases = parser.ParseResponse(response).ToList();

                if (releases.Count == 0)
                {
                    _logger.Trace(response.Content);
                }

                return new IndexerQueryResult
                {
                    Releases = releases,
                    Response = response.HttpResponse
                };
            }
            catch (Exception ex)
            {
                ex.WithData(response.HttpResponse, 128 * 1024);
                _logger.Trace("Unexpected Response content ({0} bytes): {1}", response.HttpResponse.ResponseData.Length, response.HttpResponse.Content);
                throw;
            }
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            if (action == "checkCaptcha")
            {
                Settings.Validate().Filter("BaseUrl").ThrowOnError();

                var request = new HttpRequestBuilder(Settings.BaseUrl.Trim('/'))
                    .Resource($"/pubapi_v2.php?get_token=get_token&app_id=rralworP_{BuildInfo.Version}")
                    .Accept(HttpAccept.Json)
                    .Build();

                _httpClient.Get(request);

                return new
                {
                    captchaToken = ""
                };
            }

            if (action == "getCaptchaCookie")
            {
                if (query["responseUrl"].IsNullOrWhiteSpace())
                {
                    throw new BadRequestException("QueryParam responseUrl invalid.");
                }

                if (query["ray"].IsNullOrWhiteSpace())
                {
                    throw new BadRequestException("QueryParam ray invalid.");
                }

                if (query["captchaResponse"].IsNullOrWhiteSpace())
                {
                    throw new BadRequestException("QueryParam captchaResponse invalid.");
                }

                var request = new HttpRequestBuilder(query["responseUrl"])
                    .AddQueryParam("id", query["ray"])
                    .AddQueryParam("g-recaptcha-response", query["captchaResponse"])
                    .Build();

                request.UseSimplifiedUserAgent = true;
                request.AllowAutoRedirect = false;

                var response = _httpClient.Get(request);

                var cfClearanceCookie = response.GetCookies()["cf_clearance"];

                return new
                {
                    captchaToken = cfClearanceCookie
                };
            }

            if (action == "getUrls")
            {
                var links = IndexerUrls;

                return new
                {
                    options = links.Select(d => new { Value = d, Name = d })
                };
            }

            return new { };
        }
    }
}
