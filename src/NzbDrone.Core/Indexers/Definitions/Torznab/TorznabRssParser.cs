using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers.Torznab
{
    public class TorznabRssParser : TorrentRssParser
    {
        public const string ns = "{http://torznab.com/schemas/2015/feed}";

        private readonly TorznabSettings _settings;
        private readonly ProviderDefinition _definition;
        private readonly INewznabCapabilitiesProvider _capabilitiesProvider;

        public TorznabRssParser(TorznabSettings settings, ProviderDefinition definition, INewznabCapabilitiesProvider capabilitiesProvider)
        {
            UseEnclosureUrl = true;
            _settings = settings;
            _definition = definition;
            _capabilitiesProvider = capabilitiesProvider;
        }

        protected override bool PreProcess(IndexerResponse indexerResponse)
        {
            if (indexerResponse.HttpResponse.HasHttpError)
            {
                base.PreProcess(indexerResponse);
            }

            var xdoc = LoadXmlDocument(indexerResponse);
            var error = xdoc.Descendants("error").FirstOrDefault();

            if (error == null)
            {
                return true;
            }

            var code = Convert.ToInt32(error.Attribute("code").Value);
            var errorMessage = error.Attribute("description").Value;

            if (code >= 100 && code <= 199)
            {
                throw new IndexerAuthException("Invalid API key");
            }

            if (!indexerResponse.Request.Url.FullUri.Contains("apikey=") && errorMessage == "Missing parameter")
            {
                throw new IndexerAuthException("Indexer requires an API key");
            }

            if (errorMessage == "Request limit reached")
            {
                throw new RequestLimitReachedException(indexerResponse, "API limit reached");
            }

            throw new TorznabException("Torznab error detected: {0}", errorMessage);
        }

        protected override ReleaseInfo ProcessItem(XElement item, ReleaseInfo releaseInfo)
        {
            var torrentInfo = base.ProcessItem(item, releaseInfo) as TorrentInfo;

            if (torrentInfo != null)
            {
                if (GetImdbId(item) != null)
                {
                    torrentInfo.ImdbId = int.Parse(GetImdbId(item).Substring(2));
                }

                var downloadFactor = TryGetFloatTorznabAttribute(item, "downloadvolumefactor", 1);
                var uploadFactor = TryGetFloatTorznabAttribute(item, "uploadvolumefactor", 1);

                torrentInfo.DownloadVolumeFactor = downloadFactor;
                torrentInfo.UploadVolumeFactor = uploadFactor;

                torrentInfo.ImdbId = GetIntAttribute(item, new[] { "imdb", "imdbid" });
                torrentInfo.TmdbId = GetIntAttribute(item, new[] { "tmdbid", "tmdb" });
                torrentInfo.TvdbId = GetIntAttribute(item, new[] { "tvdbid", "tvdb" });
                torrentInfo.TvMazeId = GetIntAttribute(item, new[] { "tvmazeid", "tvmaze" });
                torrentInfo.TraktId = GetIntAttribute(item, new[] { "traktid", "trakt" });
                torrentInfo.TvRageId = GetIntAttribute(item, new[] { "rageid" });
                torrentInfo.Grabs = GetIntAttribute(item, new[] { "grabs" });
                torrentInfo.Files = GetIntAttribute(item, new[] { "files" });

                torrentInfo.IndexerFlags = GetFlags(item);
                torrentInfo.PosterUrl = GetPosterUrl(item);
                torrentInfo.InfoHash = GetInfoHash(item);
            }

            return torrentInfo;
        }

        protected override bool PostProcess(IndexerResponse indexerResponse, List<XElement> items, List<ReleaseInfo> releases)
        {
            var enclosureTypes = items.SelectMany(GetEnclosures).Select(v => v.Type).Distinct().ToArray();
            if (enclosureTypes.Any() && enclosureTypes.Intersect(PreferredEnclosureMimeTypes).Empty())
            {
                if (enclosureTypes.Intersect(UsenetEnclosureMimeTypes).Any())
                {
                    _logger.Warn("Feed does not contain {0}, found {1}, did you intend to add a Newznab indexer?", TorrentEnclosureMimeType, enclosureTypes[0]);
                }
                else
                {
                    _logger.Warn("Feed does not contain {0}, found {1}.", TorrentEnclosureMimeType, enclosureTypes[0]);
                }
            }

            return true;
        }

        protected override string GetInfoUrl(XElement item)
        {
            return ParseUrl(item.TryGetValue("comments").TrimEnd("#comments"));
        }

        protected override string GetCommentUrl(XElement item)
        {
            return ParseUrl(item.TryGetValue("comments"));
        }

        protected override long GetSize(XElement item)
        {
            long size;

            var sizeString = TryGetTorznabAttribute(item, "size");
            if (!sizeString.IsNullOrWhiteSpace() && long.TryParse(sizeString, out size))
            {
                return size;
            }

            size = GetEnclosureLength(item);

            return size;
        }

        protected override DateTime GetPublishDate(XElement item)
        {
            return base.GetPublishDate(item);
        }

        protected override string GetDownloadUrl(XElement item)
        {
            var url = base.GetDownloadUrl(item);

            if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                url = ParseUrl((string)item.Element("enclosure").Attribute("url"));
            }

            return url;
        }

        protected virtual string GetImdbId(XElement item)
        {
            var imdbIdString = TryGetTorznabAttribute(item, "imdbid");
            return !imdbIdString.IsNullOrWhiteSpace() ? imdbIdString.Substring(2) : null;
        }

        protected virtual string GetPosterUrl(XElement item)
        {
            return ParseUrl(TryGetTorznabAttribute(item, "coverurl"));
        }

        protected override string GetInfoHash(XElement item)
        {
            return TryGetTorznabAttribute(item, "infohash");
        }

        protected override string GetMagnetUrl(XElement item)
        {
            return TryGetTorznabAttribute(item, "magneturl");
        }

        protected override ICollection<IndexerCategory> GetCategory(XElement item)
        {
            var capabilities = _capabilitiesProvider.GetCapabilities(_settings, _definition);
            var cats = TryGetMultipleTorznabAttributes(item, "category");
            var results = new List<IndexerCategory>();

            // Try to find <category> elements for some indexers that suck at following the rules.
            if (cats.Count == 0)
            {
                cats = item.Elements("category").Select(e => e.Value).ToList();
            }

            foreach (var cat in cats)
            {
                var indexerCat = capabilities.Categories.MapTrackerCatToNewznab(cat);

                if (indexerCat != null)
                {
                    results.AddRange(indexerCat);
                }
            }

            return results;
        }

        protected override int? GetSeeders(XElement item)
        {
            var seeders = TryGetTorznabAttribute(item, "seeders");

            if (seeders.IsNotNullOrWhiteSpace())
            {
                return int.Parse(seeders);
            }

            return base.GetSeeders(item);
        }

        protected override int? GetPeers(XElement item)
        {
            var peers = TryGetTorznabAttribute(item, "peers");

            if (peers.IsNotNullOrWhiteSpace())
            {
                return int.Parse(peers);
            }

            var seeders = TryGetTorznabAttribute(item, "seeders");
            var leechers = TryGetTorznabAttribute(item, "leechers");

            if (seeders.IsNotNullOrWhiteSpace() && leechers.IsNotNullOrWhiteSpace())
            {
                return int.Parse(seeders) + int.Parse(leechers);
            }

            return base.GetPeers(item);
        }

        protected HashSet<IndexerFlag> GetFlags(XElement item)
        {
            var flags = new HashSet<IndexerFlag>();

            var downloadFactor = TryGetFloatTorznabAttribute(item, "downloadvolumefactor", 1);
            var uploadFactor = TryGetFloatTorznabAttribute(item, "uploadvolumefactor", 1);

            if (uploadFactor == 2.0)
            {
                flags.Add(IndexerFlag.DoubleUpload);
            }

            if (downloadFactor == 0.5)
            {
                flags.Add(IndexerFlag.HalfLeech);
            }

            if (downloadFactor == 0.0)
            {
                flags.Add(IndexerFlag.FreeLeech);
            }

            return flags;
        }

        protected string TryGetTorznabAttribute(XElement item, string key, string defaultValue = "")
        {
            var attr = item.Elements(ns + "attr").FirstOrDefault(e => e.Attribute("name").Value.Equals(key, StringComparison.CurrentCultureIgnoreCase));

            if (attr != null)
            {
                return attr.Attribute("value").Value;
            }

            return defaultValue;
        }

        protected float TryGetFloatTorznabAttribute(XElement item, string key, float defaultValue = 0)
        {
            var attr = TryGetTorznabAttribute(item, key, defaultValue.ToString());

            float result = 0;

            if (float.TryParse(attr, out result))
            {
                return result;
            }

            return defaultValue;
        }

        protected List<string> TryGetMultipleTorznabAttributes(XElement item, string key)
        {
            var attrElements = item.Elements(ns + "attr").Where(e => e.Attribute("name").Value.Equals(key, StringComparison.OrdinalIgnoreCase));
            var results = new List<string>();

            foreach (var element in attrElements)
            {
                var attrValue = element.Attribute("value");
                if (attrValue != null)
                {
                    results.Add(attrValue.Value);
                }
            }

            return results;
        }

        protected virtual int GetIntAttribute(XElement item, string[] attributes)
        {
            foreach (var attr in attributes)
            {
                var idString = TryGetTorznabAttribute(item, attr);
                int idInt;

                if (!idString.IsNullOrWhiteSpace() && int.TryParse(idString, out idInt))
                {
                    return idInt;
                }
            }

            return 0;
        }
    }
}
