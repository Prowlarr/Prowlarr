using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Http.CloudFlare;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Rarbg
{
    public class Rarbg : TorrentIndexerBase<RarbgSettings>
    {
        private readonly IRarbgTokenProvider _tokenProvider;

        public override string Name => "Rarbg";

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;

        public override TimeSpan RateLimit => TimeSpan.FromSeconds(2);

        public Rarbg(IRarbgTokenProvider tokenProvider, IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
        {
            _tokenProvider = tokenProvider;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new RarbgRequestGenerator(_tokenProvider) { Settings = Settings, Categories = Capabilities.Categories };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new RarbgParser(Capabilities);
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            if (action == "checkCaptcha")
            {
                Settings.Validate().Filter("BaseUrl").ThrowOnError();

                var request = new HttpRequestBuilder(Settings.BaseUrl.Trim('/'))
                           .Resource($"/pubapi_v2.php?get_token=get_token&app_id={BuildInfo.AppName}")
                           .Accept(HttpAccept.Json)
                           .Build();

                _httpClient.Get(request);

                return new
                {
                    captchaToken = ""
                };
            }
            else if (action == "getCaptchaCookie")
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
            else if (action == "getUrls")
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
