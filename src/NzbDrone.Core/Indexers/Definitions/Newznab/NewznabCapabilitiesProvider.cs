using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Indexers.Newznab
{
    public interface INewznabCapabilitiesProvider
    {
        IndexerCapabilities GetCapabilities(NewznabSettings settings);
    }

    public class NewznabCapabilitiesProvider : INewznabCapabilitiesProvider
    {
        private readonly ICached<IndexerCapabilities> _capabilitiesCache;
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public NewznabCapabilitiesProvider(ICacheManager cacheManager, IHttpClient httpClient, Logger logger)
        {
            _capabilitiesCache = cacheManager.GetCache<IndexerCapabilities>(GetType());
            _httpClient = httpClient;
            _logger = logger;
        }

        public IndexerCapabilities GetCapabilities(NewznabSettings indexerSettings)
        {
            var key = indexerSettings.ToJson();
            var capabilities = _capabilitiesCache.Get(key, () => FetchCapabilities(indexerSettings), TimeSpan.FromDays(7));

            return capabilities;
        }

        private IndexerCapabilities FetchCapabilities(NewznabSettings indexerSettings)
        {
            var capabilities = new IndexerCapabilities();

            var url = string.Format("{0}{1}?t=caps", indexerSettings.BaseUrl.TrimEnd('/'), indexerSettings.ApiPath.TrimEnd('/'));

            if (indexerSettings.ApiKey.IsNotNullOrWhiteSpace())
            {
                url += "&apikey=" + indexerSettings.ApiKey;
            }

            var request = new HttpRequest(url, HttpAccept.Rss);

            HttpResponse response;

            try
            {
                response = _httpClient.Get(request);
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Failed to get Newznab API capabilities from {0}", indexerSettings.BaseUrl);
                throw;
            }

            try
            {
                capabilities = ParseCapabilities(response);
            }
            catch (XmlException ex)
            {
                ex.WithData(response, 128 * 1024);
                _logger.Trace("Unexpected Response content ({0} bytes): {1}", response.ResponseData.Length, response.Content);
                _logger.Debug(ex, "Failed to parse newznab api capabilities for {0}", indexerSettings.BaseUrl);
                throw;
            }
            catch (Exception ex)
            {
                ex.WithData(response, 128 * 1024);
                _logger.Trace("Unexpected Response content ({0} bytes): {1}", response.ResponseData.Length, response.Content);
            }

            return capabilities;
        }

        private IndexerCapabilities ParseCapabilities(HttpResponse response)
        {
            var capabilities = new IndexerCapabilities();

            var xDoc = XDocument.Parse(response.Content);

            if (xDoc == null)
            {
                throw new XmlException("Invalid XML").WithData(response);
            }

            NewznabRssParser.CheckError(xDoc, new IndexerResponse(new IndexerRequest(response.Request), response));

            var xmlRoot = xDoc.Element("caps");

            if (xmlRoot == null)
            {
                throw new XmlException("Unexpected XML").WithData(response);
            }

            var xmlLimits = xmlRoot.Element("limits");
            if (xmlLimits != null)
            {
                capabilities.LimitsDefault = int.Parse(xmlLimits.Attribute("default").Value);
                capabilities.LimitsMax = int.Parse(xmlLimits.Attribute("max").Value);
            }

            var xmlSearching = xmlRoot.Element("searching");
            if (xmlSearching != null)
            {
                var xmlBasicSearch = xmlSearching.Element("search");
                if (xmlBasicSearch == null || xmlBasicSearch.Attribute("available").Value != "yes")
                {
                    capabilities.SearchParams = new List<SearchParam>();
                }
                else if (xmlBasicSearch.Attribute("supportedParams") != null)
                {
                    foreach (var param in xmlBasicSearch.Attribute("supportedParams").Value.Split(','))
                    {
                        if (Enum.TryParse(param, true, out SearchParam searchParam))
                        {
                            capabilities.SearchParams.AddIfNotNull(searchParam);
                        }
                    }
                }

                var xmlMovieSearch = xmlSearching.Element("movie-search");
                if (xmlMovieSearch == null || xmlMovieSearch.Attribute("available").Value != "yes")
                {
                    capabilities.MovieSearchParams = new List<MovieSearchParam>();
                }
                else if (xmlMovieSearch.Attribute("supportedParams") != null)
                {
                    foreach (var param in xmlMovieSearch.Attribute("supportedParams").Value.Split(','))
                    {
                        if (Enum.TryParse(param, true, out MovieSearchParam searchParam))
                        {
                            capabilities.MovieSearchParams.AddIfNotNull(searchParam);
                        }
                    }
                }

                var xmlTvSearch = xmlSearching.Element("tv-search");
                if (xmlTvSearch == null || xmlTvSearch.Attribute("available").Value != "yes")
                {
                    capabilities.TvSearchParams = new List<TvSearchParam>();
                }
                else if (xmlTvSearch.Attribute("supportedParams") != null)
                {
                    foreach (var param in xmlTvSearch.Attribute("supportedParams").Value.Split(','))
                    {
                        if (Enum.TryParse(param, true, out TvSearchParam searchParam))
                        {
                            capabilities.TvSearchParams.AddIfNotNull(searchParam);
                        }
                    }
                }

                var xmlAudioSearch = xmlSearching.Element("audio-search");
                if (xmlAudioSearch == null || xmlAudioSearch.Attribute("available").Value != "yes")
                {
                    capabilities.MusicSearchParams = new List<MusicSearchParam>();
                }
                else if (xmlAudioSearch.Attribute("supportedParams") != null)
                {
                    foreach (var param in xmlAudioSearch.Attribute("supportedParams").Value.Split(','))
                    {
                        if (Enum.TryParse(param, true, out MusicSearchParam searchParam))
                        {
                            capabilities.MusicSearchParams.AddIfNotNull(searchParam);
                        }
                    }
                }

                var xmlBookSearch = xmlSearching.Element("book-search");
                if (xmlBookSearch == null || xmlBookSearch.Attribute("available").Value != "yes")
                {
                    capabilities.BookSearchParams = new List<BookSearchParam>();
                }
                else if (xmlBookSearch.Attribute("supportedParams") != null)
                {
                    foreach (var param in xmlBookSearch.Attribute("supportedParams").Value.Split(','))
                    {
                        if (Enum.TryParse(param, true, out BookSearchParam searchParam))
                        {
                            capabilities.BookSearchParams.AddIfNotNull(searchParam);
                        }
                    }
                }
            }

            var xmlCategories = xmlRoot.Element("categories");
            if (xmlCategories != null)
            {
                foreach (var xmlCategory in xmlCategories.Elements("category"))
                {
                    var cat = new IndexerCategory
                    {
                        Id = int.Parse(xmlCategory.Attribute("id").Value),
                        Name = xmlCategory.Attribute("name").Value,
                        Description = xmlCategory.Attribute("description") != null ? xmlCategory.Attribute("description").Value : string.Empty
                    };

                    foreach (var xmlSubcat in xmlCategory.Elements("subcat"))
                    {
                        var subCat = new IndexerCategory
                        {
                            Id = int.Parse(xmlSubcat.Attribute("id").Value),
                            Name = xmlSubcat.Attribute("name").Value,
                            Description = xmlSubcat.Attribute("description") != null ? xmlSubcat.Attribute("description").Value : string.Empty
                        };

                        cat.SubCategories.Add(subCat);
                        capabilities.Categories.AddCategoryMapping(subCat.Name, subCat);
                    }

                    capabilities.Categories.AddCategoryMapping(cat.Name, cat);
                }
            }

            return capabilities;
        }
    }
}
