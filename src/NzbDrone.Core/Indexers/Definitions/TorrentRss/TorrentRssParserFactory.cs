using System;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers.Definitions.TorrentRss
{
    public interface ITorrentRssParserFactory
    {
        TorrentRssParser GetParser(TorrentRssIndexerSettings settings, ProviderDefinition definition);
    }

    public class TorrentRssParserFactory : ITorrentRssParserFactory
    {
        protected readonly Logger _logger;

        private readonly ICached<TorrentRssIndexerParserSettings> _settingsCache;

        private readonly ITorrentRssSettingsDetector _torrentRssSettingsDetector;

        public TorrentRssParserFactory(ICacheManager cacheManager, ITorrentRssSettingsDetector torrentRssSettingsDetector, Logger logger)
        {
            _settingsCache = cacheManager.GetCache<TorrentRssIndexerParserSettings>(GetType());
            _torrentRssSettingsDetector = torrentRssSettingsDetector;
            _logger = logger;
        }

        public TorrentRssParser GetParser(TorrentRssIndexerSettings indexerSettings, ProviderDefinition definition)
        {
            var key = indexerSettings.ToJson();
            var parserSettings = _settingsCache.Get(key, () => DetectParserSettings(indexerSettings, definition), TimeSpan.FromDays(7));

            if (parserSettings.UseEZTVFormat)
            {
                return new EzrssTorrentRssParser();
            }

            return new TorrentRssParser
            {
                UseGuidInfoUrl = false,
                ParseSeedersInDescription = parserSettings.ParseSeedersInDescription,

                UseEnclosureUrl = parserSettings.UseEnclosureUrl,
                UseEnclosureLength = parserSettings.UseEnclosureLength,
                ParseSizeInDescription = parserSettings.ParseSizeInDescription,
                SizeElementName = parserSettings.SizeElementName,

                DefaultReleaseSize = indexerSettings.DefaultReleaseSize,
                DefaultReleaseSeeders = 1
            };
        }

        private TorrentRssIndexerParserSettings DetectParserSettings(TorrentRssIndexerSettings indexerSettings, ProviderDefinition definition)
        {
            var settings = _torrentRssSettingsDetector.Detect(indexerSettings, definition);

            if (settings == null)
            {
                throw new UnsupportedFeedException("Could not parse feed from {0}", indexerSettings.BaseUrl);
            }

            return settings;
        }
    }
}
