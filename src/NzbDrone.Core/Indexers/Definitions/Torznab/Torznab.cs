using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Torznab
{
    public class Torznab : TorrentIndexerBase<TorznabSettings>
    {
        private readonly INewznabCapabilitiesProvider _capabilitiesProvider;

        public override string Name => "Torznab";
        public override string[] IndexerUrls => GetBaseUrlFromSettings();
        public override string Description => "";
        public override bool FollowRedirect => true;
        public override bool SupportsRedirect => true;

        public override DownloadProtocol Protocol => DownloadProtocol.Torrent;
        public override IndexerPrivacy Privacy => IndexerPrivacy.Private;

        public override int PageSize => _capabilitiesProvider.GetCapabilities(Settings, Definition).LimitsDefault.Value;
        public override IndexerCapabilities Capabilities { get => GetCapabilitiesFromSettings(); protected set => base.Capabilities = value; }

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
            return new TorznabRssParser(Settings);
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
                yield return GetDefinition("AnimeTosho", GetSettings("https://feed.animetosho.org"));
                yield return GetDefinition("HD4Free.xyz", GetSettings("http://hd4free.xyz"));
                yield return GetDefinition("Generic Torznab", GetSettings(""), true);
            }
        }

        public Torznab(INewznabCapabilitiesProvider capabilitiesProvider, IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
            _capabilitiesProvider = capabilitiesProvider;
        }

        private IndexerDefinition GetDefinition(string name, TorznabSettings settings, bool pinned = false)
        {
            return new IndexerDefinition
            {
                Enable = true,
                Name = name,
                Implementation = GetType().Name,
                Settings = settings,
                Protocol = DownloadProtocol.Usenet,
                SupportsRss = SupportsRss,
                SupportsSearch = SupportsSearch,
                SupportsRedirect = SupportsRedirect,
                Capabilities = Capabilities,
                Pinned = pinned
            };
        }

        private TorznabSettings GetSettings(string url, string apiPath = null)
        {
            var settings = new TorznabSettings { BaseUrl = url };

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
                    new[] { MovieSearchParam.Q, MovieSearchParam.ImdbId }.Any(v => capabilities.MovieSearchParams.Contains(v)))
                {
                    return null;
                }

                if (capabilities.TvSearchParams != null &&
                    new[] { TvSearchParam.Q, TvSearchParam.TvdbId, TvSearchParam.RId }.Any(v => capabilities.TvSearchParams.Contains(v)) &&
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

                return new ValidationFailure(string.Empty, "Unable to connect to indexer, check the log for more details");
            }
        }
    }
}
