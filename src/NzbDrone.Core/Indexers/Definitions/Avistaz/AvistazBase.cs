using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions.Avistaz
{
    public abstract class AvistazBase : TorrentIndexerBase<AvistazSettings>
    {
        public override bool SupportsRss => true;
        public override bool SupportsSearch => true;
        public override bool SupportsPagination => false;
        public override int PageSize => 50;
        public override TimeSpan RateLimit => TimeSpan.FromSeconds(6);
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
                Capabilities = Capabilities,
                PageSize = PageSize,
                HttpClient = _httpClient,
                Logger = _logger
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new AvistazParserBase();
        }

        protected abstract IndexerCapabilities SetCapabilities();

        protected override async Task DoLogin()
        {
            try
            {
                Settings.Token = await GetToken();

                if (Definition.Id > 0)
                {
                    _indexerRepository.UpdateSettings((IndexerDefinition)Definition);
                }

                _logger.Debug("Avistaz authentication succeeded.");
            }
            catch (HttpException ex) when (ex.Response.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.Warn(ex, "Failed to authenticate with Avistaz");

                STJson.TryDeserialize<AvistazErrorResponse>(ex.Response.Content, out var jsonResponse);
                throw new IndexerAuthException(jsonResponse?.Message ?? "Unauthorized request to indexer");
            }
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            return httpResponse.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.PreconditionFailed;
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

                    STJson.TryDeserialize<AvistazErrorResponse>(ex.Response.Content, out var jsonResponse);
                    return new ValidationFailure(string.Empty, jsonResponse?.Message ?? "Unauthorized request to indexer");
                }

                _logger.Warn(ex, "Unable to connect to indexer");

                return new ValidationFailure(string.Empty, "Unable to connect to indexer, check the log above the ValidationFailure for more details. " + ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to connect to indexer");

                return new ValidationFailure(string.Empty, "Unable to connect to indexer, check the log above the ValidationFailure for more details");
            }

            return await base.TestConnection();
        }

        private async Task<string> GetToken()
        {
            var requestBuilder = new HttpRequestBuilder(LoginUrl)
            {
                LogResponseContent = true,
                Method = HttpMethod.Post
            };

            // TODO: Change to HttpAccept.Json after they fix the issue with missing headers
            var authLoginRequest = requestBuilder
                .AddFormParameter("username", Settings.Username)
                .AddFormParameter("password", Settings.Password)
                .AddFormParameter("pid", Settings.Pid.Trim())
                .Accept(HttpAccept.Html)
                .Build();

            var response = await ExecuteAuth(authLoginRequest);

            if (!STJson.TryDeserialize<AvistazAuthResponse>(response.Content, out var authResponse))
            {
                throw new Exception("Invalid response from AvistaZ, the response is not valid JSON");
            }

            return authResponse.Token;
        }
    }
}
