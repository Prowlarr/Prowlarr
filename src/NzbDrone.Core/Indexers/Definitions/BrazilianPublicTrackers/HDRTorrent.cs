using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class HDRTorrent : BrazilianPublicTorrentBase
    {
        protected override BrazilianPublicTorrentSite Site => new()
        {
            Name = "HDRTorrent",
            BaseUrl = "https://hdrtorrents.net/",
            LegacyUrls = new[] { "https://hdrtorrent.com/" },
            UsesHdrRoutes = true
        };

        public HDRTorrent(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        protected override IEnumerable<BrazilianPublicTorrentSearchItem> ParseSearchItems(IDocument document, BrazilianPublicTorrentSearchContext context)
        {
            var nodes = document.QuerySelectorAll(".media-card, .capa-img, article, .card, .col").OfType<IElement>();

            foreach (var node in nodes)
            {
                var link = FindLink(node);
                var href = link?.GetAttribute("href");

                if (href.IsNullOrWhiteSpace() || href.StartsWith("magnet:", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var title = node.QuerySelector("h2, h3, .title, .media-title")?.TextContent ??
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

        protected override string GetMagnetOptionLabel(IElement anchor)
        {
            return Closest(anchor, ".download-row, .download-item, .lista-download, .link-download, li, tr")?.TextContent ??
                   anchor.ParentElement?.TextContent ??
                   anchor.TextContent;
        }

        private static IElement FindLink(IElement node)
        {
            if (node.LocalName == "a")
            {
                return node;
            }

            return node.QuerySelector("a.media-card-link[href], h2 a[href], h3 a[href], a[href]");
        }

        private static IElement Closest(IElement element, string selector)
        {
            var current = element;

            while (current != null)
            {
                if (current.Matches(selector))
                {
                    return current;
                }

                current = current.ParentElement;
            }

            return null;
        }
    }
}
