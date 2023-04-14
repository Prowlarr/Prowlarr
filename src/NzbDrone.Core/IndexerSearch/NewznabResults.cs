using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.IndexerSearch
{
    public class NewznabResults
    {
        private static readonly XNamespace _AtomNs = "http://www.w3.org/2005/Atom";
        private static readonly XNamespace _TorznabNs = "http://torznab.com/schemas/2015/feed";
        private static readonly XNamespace _NewznabNs = "http://www.newznab.com/DTD/2010/feeds/attributes/";

        // filters control characters but allows only properly-formed surrogate sequences
        // https://stackoverflow.com/a/961504
        private static readonly Regex _InvalidXmlChars = new Regex(
            @"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]",
            RegexOptions.Compiled);

        public IList<ReleaseInfo> Releases { get; set; }

        private static string RemoveInvalidXMLChars(string text)
        {
            if (text == null)
            {
                return null;
            }

            return _InvalidXmlChars.Replace(text, "");
        }

        private static string XmlDateFormat(DateTime dt)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            //Sat, 14 Mar 2015 17:10:42 -0400
            return $"{dt:ddd, dd MMM yyyy HH:mm:ss} " + $"{dt:zzz}".Replace(":", "");
        }

        private static XElement GetNabElement(string name, object value, DownloadProtocol protocol)
        {
            if (value == null)
            {
                return null;
            }

            var feedNamespace = protocol == DownloadProtocol.Torrent ? _TorznabNs : _NewznabNs;

            return new XElement(feedNamespace + "attr", new XAttribute("name", name), new XAttribute("value", value));
        }

        public string ToXml(DownloadProtocol protocol)
        {
            // IMPORTANT: We can't use Uri.ToString(), because it generates URLs without URL encode (links with unicode
            // characters are broken). We must use Uri.AbsoluteUri instead that handles encoding correctly
            var xdoc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("rss",
                    new XAttribute("version", "1.0"),
                    new XAttribute(XNamespace.Xmlns + "atom", _AtomNs.NamespaceName),
                    protocol == DownloadProtocol.Torrent ?
                        new XAttribute(XNamespace.Xmlns + "torznab", _TorznabNs.NamespaceName) :
                        new XAttribute(XNamespace.Xmlns + "newznab", _NewznabNs.NamespaceName),
                    new XElement("channel",
                        new XElement(_AtomNs + "link",
                            new XAttribute("rel", "self"),
                            new XAttribute("type", "application/rss+xml")),
                        new XElement("title", "Prowlarr"),
                        from r in Releases
                        let t = (r as TorrentInfo) ?? new TorrentInfo()
                        select new XElement("item",
                            new XElement("title", RemoveInvalidXMLChars(r.Title)),
                            new XElement("description", RemoveInvalidXMLChars(r.Description)),
                            new XElement("guid", r.Guid),  // GUID and (Link or Magnet) are mandatory
                            new XElement("prowlarrindexer", new XAttribute("id", r.IndexerId), r.Indexer),
                            r.InfoUrl == null ? null : new XElement("comments", r.InfoUrl),
                            r.PublishDate == DateTime.MinValue ? new XElement("pubDate", XmlDateFormat(DateTime.Now)) : new XElement("pubDate", XmlDateFormat(r.PublishDate)),
                            new XElement("size", r.Size),
                            new XElement("link", r.DownloadUrl ?? t.MagnetUrl ?? string.Empty),
                            r.Categories == null ? null : from c in r.Categories select new XElement("category", c.Id),
                            new XElement(
                                "enclosure",
                                new XAttribute("url", r.DownloadUrl ?? t.MagnetUrl ?? string.Empty),
                                r.Size == null ? null : new XAttribute("length", r.Size),
                                new XAttribute("type", protocol == DownloadProtocol.Torrent ? "application/x-bittorrent" : "application/x-nzb")),
                            r.Categories == null ? null : from c in r.Categories select GetNabElement("category", c.Id, protocol),
                            r.IndexerFlags == null ? null : from f in r.IndexerFlags select GetNabElement("tag", f.Name, protocol),
                            r.Languages == null ? null : from c in r.Languages select GetNabElement("language", c, protocol),
                            r.Subs == null ? null : from c in r.Subs select GetNabElement("subs", c, protocol),
                            r.Genres == null ? null : GetNabElement("genre", string.Join(", ", r.Genres), protocol),
                            r.TvRageId == 0 ? null : GetNabElement("rageid", r.TvRageId, protocol),
                            r.TvdbId == 0 ? null : GetNabElement("tvdbid", r.TvdbId, protocol),
                            r.ImdbId == 0 ? null : GetNabElement("imdb", r.ImdbId.ToString("D7"), protocol),
                            r.TmdbId == 0 ? null : GetNabElement("tmdbid", r.TmdbId, protocol),
                            r.TraktId == 0 ? null : GetNabElement("traktid", r.TraktId, protocol),
                            r.TvMazeId == 0 ? null : GetNabElement("tvmazeid", r.TvMazeId, protocol),
                            r.DoubanId == 0 ? null : GetNabElement("doubanid", r.DoubanId, protocol),
                            GetNabElement("seeders", t.Seeders, protocol),
                            GetNabElement("files", r.Files, protocol),
                            GetNabElement("grabs", r.Grabs, protocol),
                            GetNabElement("peers", t.Peers, protocol),
                            GetNabElement("author", RemoveInvalidXMLChars(r.Author), protocol),
                            GetNabElement("booktitle", RemoveInvalidXMLChars(r.BookTitle), protocol),
                            GetNabElement("artist", RemoveInvalidXMLChars(r.Artist), protocol),
                            GetNabElement("album", RemoveInvalidXMLChars(r.Album), protocol),
                            GetNabElement("label", RemoveInvalidXMLChars(r.Label), protocol),
                            GetNabElement("track", RemoveInvalidXMLChars(r.Track), protocol),
                            GetNabElement("infohash", RemoveInvalidXMLChars(t.InfoHash), protocol),
                            GetNabElement("minimumratio", t.MinimumRatio, protocol),
                            GetNabElement("minimumseedtime", t.MinimumSeedTime, protocol),
                            GetNabElement("downloadvolumefactor", t.DownloadVolumeFactor, protocol),
                            GetNabElement("uploadvolumefactor", t.UploadVolumeFactor, protocol),
                            GetNabElement("coverurl", r.PosterUrl, protocol)))));

            return xdoc.Declaration + Environment.NewLine + xdoc;
        }
    }
}
