using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Indexers.Exceptions;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Indexers.Headphones
{
    public class HeadphonesRssParser : RssParser
    {
        public const string ns = "{http://www.newznab.com/DTD/2010/feeds/attributes/}";

        private readonly IndexerCapabilitiesCategories _categories;

        public HeadphonesRssParser(IndexerCapabilitiesCategories categories)
        {
            PreferredEnclosureMimeTypes = UsenetEnclosureMimeTypes;
            UseEnclosureUrl = true;
            _categories = categories;
        }

        public static void CheckError(XDocument xdoc, IndexerResponse indexerResponse)
        {
            var error = xdoc.Descendants("error").FirstOrDefault();

            if (error == null)
            {
                return;
            }

            var code = Convert.ToInt32(error.Attribute("code").Value);
            var errorMessage = error.Attribute("description").Value;

            if (code >= 100 && code <= 199)
            {
                throw new IndexerAuthException(errorMessage);
            }

            if (!indexerResponse.Request.Url.FullUri.Contains("apikey=") && (errorMessage == "Missing parameter" || errorMessage.Contains("apikey")))
            {
                throw new IndexerAuthException("Indexer requires an API key");
            }

            if (errorMessage == "Request limit reached")
            {
                throw new RequestLimitReachedException(indexerResponse, "API limit reached");
            }

            throw new NewznabException(indexerResponse, errorMessage);
        }

        protected override bool PreProcess(IndexerResponse indexerResponse)
        {
            if (indexerResponse.HttpResponse.HasHttpError &&
                (indexerResponse.HttpResponse.Headers.ContentType == null || !indexerResponse.HttpResponse.Headers.ContentType.Contains("xml")))
            {
                base.PreProcess(indexerResponse);
            }

            var xdoc = LoadXmlDocument(indexerResponse);

            CheckError(xdoc, indexerResponse);

            return true;
        }

        protected override bool PostProcess(IndexerResponse indexerResponse, List<XElement> items, List<ReleaseInfo> releases)
        {
            var enclosureTypes = items.SelectMany(GetEnclosures).Select(v => v.Type).Distinct().ToArray();
            if (enclosureTypes.Any() && enclosureTypes.Intersect(PreferredEnclosureMimeTypes).Empty())
            {
                if (enclosureTypes.Intersect(TorrentEnclosureMimeTypes).Any())
                {
                    _logger.Warn("Feed does not contain {0}, found {1}, did you intend to add a Torznab indexer?", NzbEnclosureMimeType, enclosureTypes[0]);
                }
                else
                {
                    _logger.Warn("Feed does not contain {0}, found {1}.", NzbEnclosureMimeType, enclosureTypes[0]);
                }
            }

            return true;
        }

        protected override ReleaseInfo ProcessItem(XElement item, ReleaseInfo releaseInfo)
        {
            releaseInfo = base.ProcessItem(item, releaseInfo);
            releaseInfo.ImdbId = GetImdbId(item);
            releaseInfo.Grabs = GetGrabs(item);
            releaseInfo.Files = GetFiles(item);

            return releaseInfo;
        }

        protected override string GetInfoUrl(XElement item)
        {
            return ParseUrl(item.TryGetValue("comments").TrimEnd("#comments"));
        }

        protected override string GetCommentUrl(XElement item)
        {
            return ParseUrl(item.TryGetValue("comments"));
        }

        protected override ICollection<IndexerCategory> GetCategory(XElement item)
        {
            var cats = TryGetMultipleNewznabAttributes(item, "category");
            var results = new List<IndexerCategory>();

            foreach (var cat in cats)
            {
                if (int.TryParse(cat, out var intCategory))
                {
                    var indexerCat = _categories.MapTrackerCatToNewznab(intCategory.ToString()) ?? null;

                    if (indexerCat != null)
                    {
                        results.AddRange(indexerCat);
                    }
                }
            }

            return results;
        }

        protected override List<string> GetLanguages(XElement item)
        {
            var languages = TryGetMultipleNewznabAttributes(item, "language");
            var results = new List<string>();

            // Try to find <language> elements for some indexers that suck at following the rules.
            if (languages.Count == 0)
            {
                languages = item.Elements("language").Select(e => e.Value).ToList();
            }

            foreach (var language in languages)
            {
                if (language.IsNotNullOrWhiteSpace())
                {
                    results.Add(language);
                }
            }

            return results;
        }

        protected override long GetSize(XElement item)
        {
            var sizeString = TryGetNewznabAttribute(item, "size");
            if (!sizeString.IsNullOrWhiteSpace() && long.TryParse(sizeString, out var size))
            {
                return size;
            }

            size = GetEnclosureLength(item);

            return size;
        }

        protected override DateTime GetPublishDate(XElement item)
        {
            var dateString = TryGetNewznabAttribute(item, "usenetdate");
            if (!dateString.IsNullOrWhiteSpace())
            {
                return XElementExtensions.ParseDate(dateString);
            }

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

        protected virtual int GetImdbId(XElement item)
        {
            var imdbIdString = TryGetNewznabAttribute(item, "imdb");

            if (!imdbIdString.IsNullOrWhiteSpace() && int.TryParse(imdbIdString, out var imdbId))
            {
                return imdbId;
            }

            return 0;
        }

        protected virtual int GetGrabs(XElement item)
        {
            var grabsString = TryGetNewznabAttribute(item, "grabs");

            if (!grabsString.IsNullOrWhiteSpace() && int.TryParse(grabsString, out var grabs))
            {
                return grabs;
            }

            return 0;
        }

        protected virtual int GetFiles(XElement item)
        {
            var filesString = TryGetNewznabAttribute(item, "files");

            if (!filesString.IsNullOrWhiteSpace() && int.TryParse(filesString, out var files))
            {
                return files;
            }

            return 0;
        }

        protected virtual int GetImdbYear(XElement item)
        {
            var imdbYearString = TryGetNewznabAttribute(item, "imdbyear");

            if (!imdbYearString.IsNullOrWhiteSpace() && int.TryParse(imdbYearString, out var imdbYear))
            {
                return imdbYear;
            }

            return 1900;
        }

        protected string TryGetNewznabAttribute(XElement item, string key, string defaultValue = "")
        {
            var attrElement = item.Elements(ns + "attr").FirstOrDefault(e => e.Attribute("name").Value.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (attrElement != null)
            {
                var attrValue = attrElement.Attribute("value");
                if (attrValue != null)
                {
                    return attrValue.Value;
                }
            }

            return defaultValue;
        }

        protected List<string> TryGetMultipleNewznabAttributes(XElement item, string key)
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
    }
}
