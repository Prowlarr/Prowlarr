using System;
using System.Collections.Generic;
using AngleSharp.Dom;

namespace NzbDrone.Core.Indexers.Definitions
{
    public enum BrazilianPublicTorrentSearchKind
    {
        Basic,
        Movie,
        Tv
    }

    public enum BrazilianPublicTorrentRoute
    {
        SearchPage,
        Movies,
        Series
    }

    public class BrazilianPublicTorrentSite
    {
        public string Name { get; init; }
        public string BaseUrl { get; init; }
        public string[] LegacyUrls { get; init; } = Array.Empty<string>();
        public bool RequiresSearchToken { get; init; }
        public bool UsesHdrRoutes { get; init; }
    }

    public class BrazilianPublicTorrentSearchContext
    {
        public BrazilianPublicTorrentSearchKind Kind { get; init; }
        public BrazilianPublicTorrentRoute Route { get; init; }
        public string Query { get; init; }
        public int? Season { get; init; }
        public string Episode { get; init; }
        public int? Year { get; init; }
    }

    public class BrazilianPublicTorrentSearchItem
    {
        public string Title { get; init; }
        public string Url { get; init; }
        public BrazilianPublicTorrentRoute Route { get; init; }
    }

    public class BrazilianPublicTorrentDetail
    {
        public string Title { get; init; }
        public string CategoryText { get; init; }
        public string Quality { get; init; }
        public string Audio { get; init; }
        public string Language { get; init; }
        public string Subtitle { get; init; }
        public string Genre { get; init; }
        public int? Year { get; init; }
        public DateTime PublishDate { get; init; } = DateTime.Today;
        public long? Size { get; init; }
        public IReadOnlyList<BrazilianPublicTorrentMagnetOption> MagnetOptions { get; init; } = Array.Empty<BrazilianPublicTorrentMagnetOption>();
    }

    public class BrazilianPublicTorrentMagnetOption
    {
        public string MagnetUrl { get; init; }
        public string Label { get; init; }
        public long? Size { get; init; }
        public IElement Element { get; init; }
    }
}
