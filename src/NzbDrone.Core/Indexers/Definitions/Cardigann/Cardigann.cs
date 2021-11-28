using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentValidation.Results;
using MonoTorrent;
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
    public class Cardigann : HttpIndexerBase<CardigannSettings>
    {
        private readonly IIndexerDefinitionUpdateService _definitionService;
        private readonly ICached<CardigannRequestGenerator> _generatorCache;

        public override string Name => "Cardigann";
        public override string[] IndexerUrls => new string[] { "" };
        public override string Description => "";
        public override bool SupportsRedirect => false;
        public override DownloadProtocol Protocol => DownloadProtocol.Unknown;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        // Page size is different per indexer, setting to 1 ensures we don't break out of paging logic
        // thinking its a partial page and instead all search_path requests are run for each indexer
        public override int PageSize => 1;

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            var generator = _generatorCache.Get(Settings.DefinitionFile, () =>
                new CardigannRequestGenerator(_configService,
                    _definitionService.GetCachedDefinition(Settings.DefinitionFile),
                    _logger)
                {
                    HttpClient = _httpClient,
                    Definition = Definition,
                    Settings = Settings
                });

            generator = (CardigannRequestGenerator)SetCookieFunctions(generator);

            generator.Settings = Settings;

            _generatorCache.ClearExpired();

            return generator;
        }

        public override IParseIndexerResponse GetParser()
        {
            return new CardigannParser(_configService,
                _definitionService.GetCachedDefinition(Settings.DefinitionFile),
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
                         IIndexerHttpClient httpClient,
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
                IndexerUrls = definition.Links.ToArray(),
                Settings = new CardigannSettings { DefinitionFile = definition.File },
                Protocol = definition.Protocol == "usenet" ? DownloadProtocol.Usenet : DownloadProtocol.Torrent,
                Privacy = definition.Type switch
                {
                    "private" => IndexerPrivacy.Private,
                    "public" => IndexerPrivacy.Public,
                    _ => IndexerPrivacy.SemiPrivate
                },
                SupportsRss = SupportsRss,
                SupportsSearch = SupportsSearch,
                SupportsRedirect = definition.Allowdownloadredirect,
                Capabilities = new IndexerCapabilities(),
                ExtraFields = settings
            };
        }

        protected override bool CheckIfLoginNeeded(HttpResponse httpResponse)
        {
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

        protected void ValidateMagnet(string link)
        {
            MagnetLink.Parse(link);
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
                var response = await _httpClient.ExecuteProxiedAsync(request, Definition);
                downloadBytes = response.ResponseData;
            }
            catch (HttpException ex)
            {
                if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.Error(ex, "Downloading torrent or nzb file for release failed since it no longer exists ({0})", request.Url.FullUri);
                    throw new ReleaseUnavailableException("Downloading torrent or nzb failed", ex);
                }

                if ((int)ex.Response.StatusCode == 429)
                {
                    _logger.Error("API Grab Limit reached for {0}", request.Url.FullUri);
                }
                else
                {
                    _logger.Error(ex, "Downloading torrent or nzb file for release failed ({0})", request.Url.FullUri);
                }

                throw new ReleaseDownloadException("Downloading torrent or nzb failed", ex);
            }
            catch (WebException ex)
            {
                _logger.Error(ex, "Downloading torrent or nzb file for release failed ({0})", request.Url.FullUri);

                throw new ReleaseDownloadException("Downloading torrent or nzb failed", ex);
            }
            catch (Exception)
            {
                _indexerStatusService.RecordFailure(Definition.Id);
                _logger.Error("Downloading torrent or nzb failed");
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

            if (action == "getUrls")
            {
                var devices = ((IndexerDefinition)Definition).IndexerUrls;

                return new
                {
                    options = devices.Select(d => new { Value = d, Name = d })
                };
            }

            return null;
        }
    }
}
