using System;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Gazelle
{
    public abstract class Gazelle : HttpIndexerBase<GazelleSettings>
    {
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override string BaseUrl => "";
        protected virtual string LoginUrl => BaseUrl + "login.php";
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;
        public override int PageSize => 50;
        public override IndexerCapabilities Capabilities => SetCapabilities();

        public Gazelle(IHttpClient httpClient,
                       IEventAggregator eventAggregator,
                       IIndexerStatusService indexerStatusService,
                       IConfigService configService,
                       Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new GazelleRequestGenerator()
            {
                Settings = Settings,
                HttpClient = _httpClient,
                Logger = _logger,
                Capabilities = Capabilities,
                BaseUrl = BaseUrl
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new GazelleParser(Settings, Capabilities, BaseUrl);
        }

        protected virtual IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities();

            return caps;
        }

        protected override async Task DoLogin()
        {
            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true
            };

            requestBuilder.Method = HttpMethod.POST;
            requestBuilder.PostProcess += r => r.RequestTimeout = TimeSpan.FromSeconds(15);

            var cookies = Cookies;

            Cookies = null;
            var authLoginRequest = requestBuilder
                .AddFormParameter("username", Settings.Username)
                .AddFormParameter("password", Settings.Password)
                .AddFormParameter("keeplogged", "1")
                .SetHeader("Content-Type", "multipart/form-data")
                .Accept(HttpAccept.Json)
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            cookies = response.GetCookies();

            UpdateCookies(cookies, DateTime.Now + TimeSpan.FromDays(30));

            _logger.Debug("Gazelle authentication succeeded.");
        }

        protected override bool CheckIfLoginNeeded(HttpResponse response)
        {
            if (response.HasHttpRedirect || (response.Content != null && response.Content.Contains("\"bad credentials\"")))
            {
                return true;
            }

            return false;
        }
    }
}
