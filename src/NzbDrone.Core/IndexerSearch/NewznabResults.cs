using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.IndexerSearch
{
    public class NewznabResults
    {
        private static readonly XNamespace _AtomNs = "http://www.w3.org/2005/Atom";
        private static readonly XNamespace _TorznabNs = "http://torznab.com/schemas/2015/feed";

        // filters control characters but allows only properly-formed surrogate sequences
        // https://stackoverflow.com/a/961504
        private static readonly Regex _InvalidXmlChars = new Regex(
            @"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]",
            RegexOptions.Compiled);

        public List<ReleaseInfo> Releases;

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

        private static XElement GetTorznabElement(string name, object value)
        {
            if (value == null)
            {
                return null;
            }

            return new XElement(_TorznabNs + "attr", new XAttribute("name", name), new XAttribute("value", value));
        }

        public string ToXml()
        {
            // IMPORTANT: We can't use Uri.ToString(), because it generates URLs without URL encode (links with unicode
            // characters are broken). We must use Uri.AbsoluteUri instead that handles encoding correctly
            var xdoc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("rss",
                    new XAttribute("version", "1.0"),
                    new XAttribute(XNamespace.Xmlns + "atom", _AtomNs.NamespaceName),
                    new XAttribute(XNamespace.Xmlns + "torznab", _TorznabNs.NamespaceName),
                    new XElement("channel",
                        new XElement(_AtomNs + "link",
                            new XAttribute("rel", "self"),
                            new XAttribute("type", "application/rss+xml")),
                        new XElement("title", "Prowlarr"),
                        from r in Releases
                        let t = (r as TorrentInfo) ?? new TorrentInfo()
                        select new XElement("item",
                            new XElement("title", RemoveInvalidXMLChars(r.Title)),
                            new XElement("guid", r.Guid),  // GUID and (Link or Magnet) are mandatory
                            new XElement("prowlarrindexer", new XAttribute("id", r.IndexerId), r.Indexer),
                            r.PublishDate == DateTime.MinValue ? new XElement("pubDate", XmlDateFormat(DateTime.Now)) : new XElement("pubDate", XmlDateFormat(r.PublishDate)),
                            r.Category == null ? null : from c in r.Category select new XElement("category", c),
                            new XElement("size", r.Size),
                            new XElement(
                                "enclosure",
                                new XAttribute("length", r.Size),
                                new XAttribute("type", "application/x-bittorrent")),
                            GetTorznabElement("rageid", r.TvRageId),
                            GetTorznabElement("thetvdb", r.TvdbId),
                            GetTorznabElement("imdb", r.ImdbId.ToString("D7")),
                            GetTorznabElement("tmdb", r.TmdbId),
                            GetTorznabElement("seeders", t.Seeders),
                            GetTorznabElement("peers", t.Peers),
                            GetTorznabElement("infohash", RemoveInvalidXMLChars(r.Guid))))));

            return xdoc.Declaration + Environment.NewLine + xdoc;
        }
    }
}
