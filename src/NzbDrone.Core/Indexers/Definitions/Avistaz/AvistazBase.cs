using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions.Avistaz
{
    public abstract class AvistazBase : TorrentIndexerBase<AvistazSettings>
    {
        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;
        public override int PageSize => 50;
        public override IndexerCapabilities Capabilities => SetCapabilities();
        protected virtual string LoginUrl => Settings.BaseUrl + "api/v1/jackett/auth";
        private IIndexerRepository _indexerRepository;

        public AvistazBase(IIndexerRepository indexerRepository,
                       IIndexerHttpClient httpClient,
                       IEventAggregator eventAggregator,
                       IIndexerStatusService indexerStatusService,
                       IConfigService configService,
                       Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
            _indexerRepository = indexerRepository;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new AvistazRequestGenerator
            {
                Settings = Settings,
                HttpClient = _httpClient,
                Logger = _logger,
                Capabilities = Capabilities
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new AvistazParserBase();
        }

        protected virtual IndexerCapabilities SetCapabilities()
        {
            return new IndexerCapabilities();
        }

        protected override async Task DoLogin()
        {
            Settings.Token = await GetToken();

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

        protected override void ModifyRequest(IndexerRequest request)
        {
            request.HttpRequest.Headers.Set("Authorization", $"Bearer {Settings.Token}");
            base.ModifyRequest(request);
        }

        protected override async Task<ValidationFailure> TestConnection()
        {
            try
            {
                await GetToken();
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.Warn(ex, "Unauthorized request to indexer");

                    var jsonResponse = new HttpResponse<AvistazErrorResponse>(ex.Response);
                    return new ValidationFailure(string.Empty, jsonResponse.Resource?.Message ?? "Unauthorized request to indexer");
                }
                else
                {
                    _logger.Warn(ex, "Unable to connect to indexer");

                    return new ValidationFailure(string.Empty, "Unable to connect to indexer, check the log for more details");
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to connect to indexer");

                return new ValidationFailure(string.Empty, "Unable to connect to indexer, check the log for more details");
            }

            return null;
        }

        private async Task<string> GetToken()
        {
            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true
            };

            requestBuilder.Method = HttpMethod.Post;
            requestBuilder.PostProcess += r => r.RequestTimeout = TimeSpan.FromSeconds(15);

            var authLoginRequest = requestBuilder
                .AddFormParameter("username", Settings.Username)
                .AddFormParameter("password", Settings.Password)
                .AddFormParameter("pid", Settings.Pid.Trim())
                .SetHeader("Content-Type", "application/json")
                .Accept(HttpAccept.Json)
                .Build();

            var response = await _httpClient.PostAsync<AvistazAuthResponse>(authLoginRequest);
            var token = response.Resource.Token;

            return token;
        }
    }
}
