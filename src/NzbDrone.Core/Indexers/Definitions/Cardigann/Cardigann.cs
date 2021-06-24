using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Cardigann
{
    public class Cardigann : TorrentIndexerBase<CardigannSettings>
    {
        private readonly IIndexerDefinitionUpdateService _definitionService;
        private readonly ICached<CardigannRequestGenerator> _generatorCache;

        public override string Name => "Cardigann";
        public override string BaseUrl => "";

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        // Page size is different per indexer, setting to 1 ensures we don't break out of paging logic
        // thinking its a partial page and insteaad all search_path requests are run for each indexer
        public override int PageSize => 1;

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            var generator = _generatorCache.Get(Settings.DefinitionFile, () =>
                new CardigannRequestGenerator(_configService,
                    _definitionService.GetDefinition(Settings.DefinitionFile),
                    _logger)
                {
                    HttpClient = _httpClient,
                    Settings = Settings
                });

            generator = (CardigannRequestGenerator)SetCookieFunctions(generator);

            _generatorCache.ClearExpired();

            return generator;
        }

        public override IParseIndexerResponse GetParser()
        {
            return new CardigannParser(_configService,
                _definitionService.GetDefinition(Settings.DefinitionFile),
                _logger)
            {
                Settings = Settings
            };
        }

        protected override IDictionary<string, string> GetCookies()
        {
            if (Settings.ExtraFieldData.TryGetValue("cookie", out var cookies))
            {
                return CookieUtil.CookieHeaderToDictionary((string)cookies);
            }

            return base.GetCookies();
        }

        public override IEnumerable<ProviderDefinition> DefaultDefinitions
        {
            get
            {
                foreach (var def in _definitionService.All())
                {
                    yield return GetDefinition(def);
                }
            }
        }

        public Cardigann(IIndexerDefinitionUpdateService definitionService,
                         IHttpClient httpClient,
                         IEventAggregator eventAggregator,
                         IIndexerStatusService indexerStatusService,
                         IConfigService configService,
                         ICacheManager cacheManager,
                         Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
            _definitionService = definitionService;
            _generatorCache = cacheManager.GetRollingCache<CardigannRequestGenerator>(GetType(), "CardigannGeneratorCache", TimeSpan.FromMinutes(5));
        }

        private IndexerDefinition GetDefinition(CardigannMetaDefinition definition)
        {
            var defaultSettings = new List<SettingsField>
            {
                new SettingsField { Name = "username", Label = "Username", Type = "text" },
                new SettingsField { Name = "password", Label = "Password", Type = "password" }
            };

            var settings = definition.Settings ?? defaultSettings;

            if (definition.Login?.Captcha != null)
            {
                settings.Add(new SettingsField
                {
                    Name = "cardigannCaptcha",
                    Type = "cardigannCaptcha",
                    Label = "CAPTCHA"
                });
            }

            return new IndexerDefinition
            {
                Enable = true,
                Name = definition.Name,
                Language = definition.Language,
                Description = definition.Description,
                Implementation = GetType().Name,
                Settings = new CardigannSettings { DefinitionFile = definition.File },
                Protocol = DownloadProtocol.Torrent,
                Privacy = definition.Type == "private" ? IndexerPrivacy.Private : IndexerPrivacy.Public,
                SupportsRss = SupportsRss,
                SupportsSearch = SupportsSearch,
                SupportsRedirect = SupportsRedirect,
                Capabilities = new IndexerCapabilities(),
                ExtraFields = settings
            };
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
            if (httpResponse.HasHttpError)
            {
                return true;
            }

            var generator = (CardigannRequestGenerator)GetRequestGenerator();

            SetCookieFunctions(generator);

            generator.Settings = Settings;

            return generator.CheckIfLoginIsNeeded(httpResponse);
        }

        protected override async Task DoLogin()
        {
            var generator = (CardigannRequestGenerator)GetRequestGenerator();

            SetCookieFunctions(generator);

            generator.Settings = Settings;

            await generator.DoLogin();
        }

        public override async Task<byte[]> Download(Uri link)
        {
            var generator = (CardigannRequestGenerator)GetRequestGenerator();

            var request = await generator.DownloadRequest(link);

            if (request.Url.Scheme == "magnet")
            {
                ValidateMagnet(request.Url.FullUri);
                return Encoding.UTF8.GetBytes(request.Url.FullUri);
            }

            request.AllowAutoRedirect = true;

            var downloadBytes = Array.Empty<byte>();

            try
            {
                var response = await _httpClient.ExecuteAsync(request);
                downloadBytes = response.ResponseData;
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.Error(ex, "Downloading torrent file for release failed since it no longer exists ({0})", request.Url.FullUri);
                    throw new ReleaseUnavailableException("Downloading torrent failed", ex);
                }

                if ((int)ex.Response.StatusCode == 429)
                {
                    _logger.Error("API Grab Limit reached for {0}", request.Url.FullUri);
                }
                else
                {
                    _logger.Error(ex, "Downloading torrent file for release failed ({0})", request.Url.FullUri);
                }

                throw new ReleaseDownloadException("Downloading torrent failed", ex);
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Downloading torrent file for release failed ({0})", request.Url.FullUri);

                throw new ReleaseDownloadException("Downloading torrent failed", ex);
            }
            catch (Exception)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.Error("Downloading torrent failed");
                throw;
            }

            return downloadBytes;
        }

        protected override async Task Test(List<ValidationFailure> failures)
        {
            await base.Test(failures);
            if (failures.HasErrors())
            {
                return;
            }
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            if (action == "checkCaptcha")
            {
                var generator = (CardigannRequestGenerator)GetRequestGenerator();

                var result = generator.GetConfigurationForSetup(false).GetAwaiter().GetResult();
                return new
                {
                    captchaRequest = result
                };
            }

            return null;
        }
    }
}
