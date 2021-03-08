using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Newznab
{
    public class Newznab : HttpIndexerBase<NewznabSettings>
    {
        private readonly INewznabCapabilitiesProvider _capabilitiesProvider;

        public override string Name => "Newznab";
        public override string BaseUrl => GetBaseUrlFromSettings();
        public override bool FollowRedirect => true;
        public override bool SupportsRedirect => true;

        public override DownloadProtocol Protocol => DownloadProtocol.Usenet;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public override IndexerCapabilities Capabilities { get => GetCapabilitiesFromSettings(); protected set => base.Capabilities = value; }

        public override int PageSize => _capabilitiesProvider.GetCapabilities(Settings).LimitsDefault.Value;

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            return new NewznabRequestGenerator(_capabilitiesProvider)
            {
                PageSize = PageSize,
                Settings = Settings
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new NewznabRssParser(Settings);
        }

        public string GetBaseUrlFromSettings()
        {
            var baseUrl = "";

            if (Definition == null || Settings == null || Settings.Categories == null)
            {
                return baseUrl;
            }

            return Settings.BaseUrl;
        }

        public IndexerCapabilities GetCapabilitiesFromSettings()
        {
            var caps = new IndexerCapabilities();

            if (Definition == null || Settings == null || Settings.Categories == null)
            {
                return caps;
            }

            foreach (var category in Settings.Categories)
            {
                caps.Categories.AddCategoryMapping(category.Name, category);
            }

            return caps;
        }

        public override IndexerCapabilities GetCapabilities()
        {
            // Newznab uses different Caps per site, so we need to cache them to db on first indexer add to prevent issues with loading UI and pulling caps every time.
            return _capabilitiesProvider.GetCapabilities(Settings);
        }

        public override IEnumerable<ProviderDefinition> DefaultDefinitions
        {
            get
            {
                yield return GetDefinition("abNZB", GetSettings("https://abnzb.com/"));
                yield return GetDefinition("DOGnzb", GetSettings("https://api.dognzb.cr"));
                yield return GetDefinition("DrunkenSlug", GetSettings("https://api.drunkenslug.com"));
                yield return GetDefinition("Nzb.su", GetSettings("https://api.nzb.su"));
                yield return GetDefinition("NZBCat", GetSettings("https://nzb.cat"));
                yield return GetDefinition("NZBFinder.ws", GetSettings("https://nzbfinder.ws"));
                yield return GetDefinition("NZBgeek", GetSettings("https://api.nzbgeek.info"));
                yield return GetDefinition("nzbplanet.net", GetSettings("https://api.nzbplanet.net"));
                yield return GetDefinition("OZnzb.com", GetSettings("https://api.oznzb.com"));
                yield return GetDefinition("SimplyNZBs", GetSettings("https://simplynzbs.com"));
                yield return GetDefinition("Tabula Rasa", GetSettings("https://www.tabula-rasa.pw", apiPath: @"/api/v1/api"));
                yield return GetDefinition("Usenet Crawler", GetSettings("https://www.usenet-crawler.com"));
                yield return GetDefinition("Generic Newznab", GetSettings(""));
            }
        }

        public Newznab(INewznabCapabilitiesProvider capabilitiesProvider, IHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
            _capabilitiesProvider = capabilitiesProvider;
        }

        private IndexerDefinition GetDefinition(string name, NewznabSettings settings)
        {
            return new IndexerDefinition
            {
                Enable = true,
                Name = name,
                Implementation = GetType().Name,
                Settings = settings,
                Protocol = DownloadProtocol.Usenet,
                Privacy = IndexerPrivacy.Private,
                SupportsRss = SupportsRss,
                SupportsSearch = SupportsSearch,
                SupportsRedirect = SupportsRedirect,
                Capabilities = Capabilities
            };
        }

        private NewznabSettings GetSettings(string url, string apiPath = null)
        {
            var settings = new NewznabSettings { BaseUrl = url };

            if (apiPath.IsNotNullOrWhiteSpace())
            {
                settings.ApiPath = apiPath;
            }

            return settings;
        }

        protected override async Task Test(List<ValidationFailure> failures)
        {
            await base.Test(failures);
            if (failures.HasErrors())
            {
                return;
            }

            failures.AddIfNotNull(TestCapabilities());
        }

        protected static List<int> CategoryIds(IndexerCapabilitiesCategories categories)
        {
            var l = categories.GetTorznabCategoryTree().Select(c => c.Id).ToList();

            return l;
        }

        protected virtual ValidationFailure TestCapabilities()
        {
            try
            {
                var capabilities = _capabilitiesProvider.GetCapabilities(Settings);

                if (capabilities.SearchParams != null && capabilities.SearchParams.Contains(SearchParam.Q))
                {
                    return null;
                }

                if (capabilities.MovieSearchParams != null &&
                    new[] { MovieSearchParam.Q, MovieSearchParam.ImdbId }.Any(v => capabilities.MovieSearchParams.Contains(v)) &&
                    new[] { MovieSearchParam.ImdbTitle, MovieSearchParam.ImdbYear }.All(v => capabilities.MovieSearchParams.Contains(v)))
                {
                    return null;
                }

                return new ValidationFailure(string.Empty, "This indexer does not support searching for movies :(. Tell your indexer staff to enable this or force add the indexer by disabling search, adding the indexer and then enabling it again.");
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to connect to indexer: " + ex.Message);

                return new ValidationFailure(string.Empty, "Unable to connect to indexer, check the log for more details");
            }
        }
    }
}
