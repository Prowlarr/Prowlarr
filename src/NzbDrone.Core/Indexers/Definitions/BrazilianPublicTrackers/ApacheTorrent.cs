using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class ApacheTorrent : BrazilianPublicTorrentBase
    {
        protected override BrazilianPublicTorrentSite Site => new()
        {
            Name = "ApacheTorrent",
            BaseUrl = "https://apachetorrents.com/",
            LegacyUrls = new[] { "https://apachetorrent.com/" },
            RequiresSearchToken = true
        };

        public ApacheTorrent(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        protected override IEnumerable<BrazilianPublicTorrentSearchItem> ParseSearchItems(IDocument document, BrazilianPublicTorrentSearchContext context)
        {
            var nodes = document.QuerySelectorAll(".custom-card, .capaname, .capa_lista, article, .card, .col").OfType<IElement>();

            foreach (var node in nodes)
            {
                var link = FindLink(node);
                var href = link?.GetAttribute("href");

                if (href.IsNullOrWhiteSpace() || href.StartsWith("magnet:", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var title = node.QuerySelector(".capa-titulo, h2[itemprop='headline'], h2, h3")?.TextContent ??
                            link.GetAttribute("title") ??
                            link.TextContent ??
                            node.TextContent;

                yield return new BrazilianPublicTorrentSearchItem
                {
                    Title = BrazilianPublicTorrentParser.CleanReleaseTitle(title),
                    Url = BrazilianPublicTorrentParser.AbsoluteUrl(Settings.BaseUrl, href),
                    Route = context.Route
                };
            }
        }

        private static IElement FindLink(IElement node)
        {
            if (node.LocalName == "a")
            {
                return node;
            }

            return node.QuerySelector("a[href]");
        }
    }
}
