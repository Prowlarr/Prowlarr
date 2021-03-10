using System;
using System.Net;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;

namespace NzbDrone.Core.Indexers.Definitions.Avistaz
{
    public abstract class Avistaz : HttpIndexerBase<AvistazSettings>
    {
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override string BaseUrl => "";
        protected virtual string LoginUrl => BaseUrl + "api/v1/jackett/auth";
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;
        public override int PageSize => 50;
        public override IndexerCapabilities Capabilities => SetCapabilities();
        private IIndexerRepository _indexerRepository;

        public Avistaz(IIndexerRepository indexerRepository,
                       IHttpClient httpClient,
                       IIndexerStatusService indexerStatusService,
                       IConfigService configService,
                       Logger logger)
            : base(httpClient, indexerStatusService, configService, logger)
        {
            _indexerRepository = indexerRepository;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AvistazRequestGenerator()
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
            return new AvistazParser(Settings, Capabilities, BaseUrl);
        }

        protected virtual IndexerCapabilities SetCapabilities()
        {
            var caps = new IndexerCapabilities();

            return caps;
        }

        protected override void DoLogin()
        {
            Settings.Token = GetToken();

            if (Definition.Id > 0)
            {
                _indexerRepository.UpdateSettings((IndexerDefinition)Definition);
            }

            _logger.Debug("Avistaz authentication succeeded.");
        }

        protected override bool CheckIfLoginNeeded(HttpResponse response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                return true;
            }

            return false;
        }

        protected override ValidationFailure TestConnection()
        {
            try
            {
                GetToken();
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to connect to indexer");

                return new ValidationFailure(string.Empty, "Unable to connect to indexer, check the log for more details");
            }

            return null;
        }

        private string GetToken()
        {
            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true
            };

            requestBuilder.Method = HttpMethod.POST;
            requestBuilder.PostProcess += r => r.RequestTimeout = TimeSpan.FromSeconds(15);

            var authLoginRequest = requestBuilder
                .AddFormParameter("username", Settings.Username)
                .AddFormParameter("password", Settings.Password)
                .AddFormParameter("pid", Settings.Pid.Trim())
                .SetHeader("Content-Type", "application/json")
                .Accept(HttpAccept.Json)
                .Build();

            var response = _httpClient.Post<AvistazAuthResponse>(authLoginRequest);
            var token = response.Resource.Token;

            return token;
        }
    }
}
