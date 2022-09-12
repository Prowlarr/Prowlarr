using System.Collections.Generic;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers.Gazelle;
using NzbDrone.Core.IndexerVersions;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class Orpheus : Gazelle.Gazelle
    {
        public override string Name => "Orpheus";

        public Orpheus(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IIndexerDefinitionUpdateService definitionService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, definitionService, configService, logger)
        {
        }

        public override IParseIndexerResponse GetParser()
        {
            return new OrpheusParser(Settings, Capabilities);
        }
    }

    public class OrpheusParser : GazelleParser
    {
        public OrpheusParser(GazelleSettings settings, IndexerCapabilities capabilities)
            : base(settings, capabilities)
        {
        }

        protected override string GetDownloadUrl(int torrentId)
        {
            var url = new HttpUri(_settings.BaseUrl)
                .CombinePath("/torrents.php")
                .AddQueryParam("action", "download")
                .AddQueryParam("id", torrentId);

            // Orpheus fails to download if usetoken=0 so we need to only add if we will use one
            if (_settings.UseFreeleechToken)
            {
                url = url.AddQueryParam("usetoken", "1");
            }

            return url.FullUri;
        }
    }
}
