using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Indexers.Definitions
{
    public class RedeTorrent : BrazilianPublicTorrentBase
    {
        protected override BrazilianPublicTorrentSite Site => new()
        {
            Name = "RedeTorrent",
            BaseUrl = "https://redestorrents.com/",
            LegacyUrls = new[] { "https://redetorrent.com/" },
            RequiresSearchToken = true
        };

        public RedeTorrent(IIndexerHttpClient httpClient, IEventAggregator eventAggregator, IIndexerStatusService indexerStatusService, IConfigService configService, Logger logger)
            : base(httpClient, eventAggregator, indexerStatusService, configService, logger)
        {
        }

        protected override IEnumerable<BrazilianPublicTorrentSearchItem> ParseSearchItems(IDocument document, BrazilianPublicTorrentSearchContext context)
        {
            var nodes = document.QuerySelectorAll(".custom-card, .capa_lista, article, .card, .col").OfType<IElement>();

            foreach (var node in nodes)
            {
                var link = FindLinkIncludingAncestors(node);
                var href = link?.GetAttribute("href");

                if (href.IsNullOrWhiteSpace() || href.StartsWith("magnet:", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var title = node.GetAttribute("data-title") ??
                            node.QuerySelector(".movie-tooltip, h2[itemprop='headline'], .capa-titulo, h2, h3")?.TextContent ??
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
            return Closest(anchor, ".download-row, .download-item, .lista-download, .link-download")?.TextContent ??
                   anchor.ParentElement?.TextContent ??
                   anchor.TextContent;
        }

        private static IElement FindLinkIncludingAncestors(IElement node)
        {
            if (node.LocalName == "a")
            {
                return node;
            }

            var link = node.QuerySelector("a[href]");
            if (link != null)
            {
                return link;
            }

            var parent = node.ParentElement;
            while (parent != null)
            {
                if (parent.LocalName == "a" && parent.HasAttribute("href"))
                {
                    return parent;
                }

                parent = parent.ParentElement;
            }

            return null;
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
