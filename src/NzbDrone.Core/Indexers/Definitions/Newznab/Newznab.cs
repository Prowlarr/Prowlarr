using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Newznab
{
    public class Newznab : UsenetIndexerBase<NewznabSettings>
    {
        private readonly INewznabCapabilitiesProvider _capabilitiesProvider;

        public override string Name => "Newznab";
        public override string[] IndexerUrls => GetBaseUrlFromSettings();
        public override string Description => "Newznab is an API search specification for Usenet";
        public override bool FollowRedirect => true;
        public override bool SupportsRedirect => true;
        public override bool SupportsPagination => true;

        public override DownloadProtocol Protocol => DownloadProtocol.Usenet;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public override IndexerCapabilities Capabilities { get => GetCapabilitiesFromSettings(); protected set => base.Capabilities = value; }

        public override int PageSize => _capabilitiesProvider.GetCapabilities(Settings, Definition).LimitsDefault.Value;

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
            return new NewznabRssParser(Settings, Definition, _capabilitiesProvider);
        }

        public string[] GetBaseUrlFromSettings()
        {
            var baseUrl = "";

            if (Definition == null || Settings == null || Settings.Categories == null)
            {
                return new string[] { baseUrl };
            }

            return new string[] { Settings.BaseUrl };
        }

        protected override NewznabSettings GetDefaultBaseUrl(NewznabSettings settings)
        {
            return settings;
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
            return _capabilitiesProvider.GetCapabilities(Settings, Definition);
        }

        public override IEnumerable<ProviderDefinition> DefaultDefinitions
        {
            get
            {
                yield return GetDefinition("abNZB", GetSettings("https://abnzb.com"));
                yield return GetDefinition("altHUB", GetSettings("https://api.althub.co.za"));
                yield return GetDefinition("AnimeTosho (Usenet)", GetSettings("https://feed.animetosho.org"));
                yield return GetDefinition("DOGnzb", GetSettings("https://api.dognzb.cr"));
                yield return GetDefinition("DrunkenSlug", GetSettings("https://drunkenslug.com"));
                yield return GetDefinition("GingaDADDY", GetSettings("https://www.gingadaddy.com"));
                yield return GetDefinition("Miatrix", GetSettings("https://www.miatrix.com"));
                yield return GetDefinition("Newz-Complex", GetSettings("https://newz-complex.org/www"));
                yield return GetDefinition("Newz69", GetSettings("https://newz69.keagaming.com"));
                yield return GetDefinition("NinjaCentral", GetSettings("https://ninjacentral.co.za"));
                yield return GetDefinition("Nzb.su", GetSettings("https://api.nzb.su"));
                yield return GetDefinition("NZBCat", GetSettings("https://nzb.cat"));
                yield return GetDefinition("NZBFinder", GetSettings("https://nzbfinder.ws"));
                yield return GetDefinition("NZBgeek", GetSettings("https://api.nzbgeek.info"));
                yield return GetDefinition("NzbNoob", GetSettings("https://www.nzbnoob.com"));
                yield return GetDefinition("NZBNDX", GetSettings("https://www.nzbndx.com"));
                yield return GetDefinition("NzbPlanet", GetSettings("https://api.nzbplanet.net"));
                yield return GetDefinition("NZBStars", GetSettings("https://nzbstars.com"));
                yield return GetDefinition("OZnzb", GetSettings("https://api.oznzb.com"));
                yield return GetDefinition("SimplyNZBs", GetSettings("https://simplynzbs.com"));
                yield return GetDefinition("SpotNZB", GetSettings("https://spotnzb.xyz"));
                yield return GetDefinition("Tabula Rasa", GetSettings("https://www.tabula-rasa.pw", apiPath: @"/api/v1/api"));
                yield return GetDefinition("VeryCouch LazyMuch", GetSettings("https://api.verycouch.com"));
                yield return GetDefinition("Generic Newznab", GetSettings(""));
            }
        }

        public Newznab(INewznabCapabilitiesProvider capabilitiesProvider, IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, IValidateNzbs nzbValidationService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, nzbValidationService, logger)
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
                SupportsPagination = SupportsPagination,
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
                var capabilities = _capabilitiesProvider.GetCapabilities(Settings, Definition);

                if (capabilities.SearchParams != null && capabilities.SearchParams.Contains(SearchParam.Q))
                {
                    return null;
                }

                if (capabilities.MovieSearchParams != null &&
                    new[] { MovieSearchParam.Q, MovieSearchParam.ImdbId, MovieSearchParam.TmdbId, MovieSearchParam.TraktId }.Any(v => capabilities.MovieSearchParams.Contains(v)))
                {
                    return null;
                }

                if (capabilities.TvSearchParams != null &&
                    new[] { TvSearchParam.Q, TvSearchParam.TvdbId, TvSearchParam.ImdbId, TvSearchParam.TmdbId, TvSearchParam.RId }.Any(v => capabilities.TvSearchParams.Contains(v)) &&
                    new[] { TvSearchParam.Season, TvSearchParam.Ep }.All(v => capabilities.TvSearchParams.Contains(v)))
                {
                    return null;
                }

                if (capabilities.MusicSearchParams != null &&
                    new[] { MusicSearchParam.Q, MusicSearchParam.Artist, MusicSearchParam.Album }.Any(v => capabilities.MusicSearchParams.Contains(v)))
                {
                    return null;
                }

                if (capabilities.BookSearchParams != null &&
                    new[] { BookSearchParam.Q, BookSearchParam.Author, BookSearchParam.Title }.Any(v => capabilities.BookSearchParams.Contains(v)))
                {
                    return null;
                }

                return new ValidationFailure(string.Empty, "This indexer does not support searching for tv, music, or movies :(. Tell your indexer staff to enable this or force add the indexer by disabling search, adding the indexer and then enabling it again.");
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to connect to indexer: " + ex.Message);

                return new ValidationFailure(string.Empty, "Unable to connect to indexer, check the log above the ValidationFailure for more details");
            }
        }
    }
}
