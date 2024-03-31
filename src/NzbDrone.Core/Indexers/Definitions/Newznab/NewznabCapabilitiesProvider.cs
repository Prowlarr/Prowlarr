using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Xml;
using System.Xml.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Indexers.Newznab
{
    public interface INewznabCapabilitiesProvider
    {
        IndexerCapabilities GetCapabilities(NewznabSettings settings, ProviderDefinition definition);
    }

    public class NewznabCapabilitiesProvider : INewznabCapabilitiesProvider
    {
        private readonly ICached<IndexerCapabilities> _capabilitiesCache;
        private readonly IIndexerHttpClient _httpClient;
        private readonly Logger _logger;

        public NewznabCapabilitiesProvider(ICacheManager cacheManager, IIndexerHttpClient httpClient, Logger logger)
        {
            _capabilitiesCache = cacheManager.GetCache<IndexerCapabilities>(GetType());
            _httpClient = httpClient;
            _logger = logger;
        }

        public IndexerCapabilities GetCapabilities(NewznabSettings indexerSettings, ProviderDefinition definition)
        {
            var key = indexerSettings.ToJson();

            return _capabilitiesCache.Get(key, () => FetchCapabilities(indexerSettings, definition), TimeSpan.FromDays(7));
        }

        private IndexerCapabilities FetchCapabilities(NewznabSettings indexerSettings, ProviderDefinition definition)
        {
            var capabilities = new IndexerCapabilities();

            var url = string.Format("{0}{1}?t=caps", indexerSettings.BaseUrl.TrimEnd('/'), indexerSettings.ApiPath.TrimEnd('/'));

            if (indexerSettings.ApiKey.IsNotNullOrWhiteSpace())
            {
                url += "&apikey=" + indexerSettings.ApiKey;
            }

            var request = new HttpRequest(url, HttpAccept.Rss);
            request.AllowAutoRedirect = true;
            request.Method = HttpMethod.Get;

            HttpResponse response;

            try
            {
                response = _httpClient.ExecuteProxied(request, definition);
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
                        if (Enum.TryParse(param, true, out SearchParam searchParam) && !capabilities.SearchParams.Contains(searchParam))
                        {
                            capabilities.SearchParams.AddIfNotNull(searchParam);
                        }
                    }

                    capabilities.SupportsRawSearch = xmlBasicSearch.Attribute("searchEngine")?.Value == "raw";
                }
                else
                {
                    capabilities.SearchParams = new List<SearchParam> { SearchParam.Q };
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
                        if (Enum.TryParse(param, true, out MovieSearchParam searchParam) && !capabilities.MovieSearchParams.Contains(searchParam))
                        {
                            capabilities.MovieSearchParams.AddIfNotNull(searchParam);
                        }
                    }
                }
                else
                {
                    capabilities.MovieSearchParams = new List<MovieSearchParam> { MovieSearchParam.Q };
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
                        if (Enum.TryParse(param, true, out TvSearchParam searchParam) && !capabilities.TvSearchParams.Contains(searchParam))
                        {
                            capabilities.TvSearchParams.AddIfNotNull(searchParam);
                        }
                    }
                }
                else
                {
                    capabilities.TvSearchParams = new List<TvSearchParam> { TvSearchParam.Q };
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
                        if (Enum.TryParse(param, true, out MusicSearchParam searchParam) && !capabilities.MusicSearchParams.Contains(searchParam))
                        {
                            capabilities.MusicSearchParams.AddIfNotNull(searchParam);
                        }
                    }
                }
                else
                {
                    capabilities.MusicSearchParams = new List<MusicSearchParam> { MusicSearchParam.Q };
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
                        if (Enum.TryParse(param, true, out BookSearchParam searchParam) && !capabilities.BookSearchParams.Contains(searchParam))
                        {
                            capabilities.BookSearchParams.AddIfNotNull(searchParam);
                        }
                    }
                }
                else
                {
                    capabilities.BookSearchParams = new List<BookSearchParam> { BookSearchParam.Q };
                }
            }

            var xmlCategories = xmlRoot.Element("categories");
            if (xmlCategories != null)
            {
                foreach (var xmlCategory in xmlCategories.Elements("category"))
                {
                    var parentName = xmlCategory.Attribute("name").Value;
                    var parentNameLower = parentName?.ToLowerInvariant();
                    var parentId = int.Parse(xmlCategory.Attribute("id").Value);

                    var mappedCat = NewznabStandardCategory.ParentCats.FirstOrDefault(x => parentNameLower.Contains(x.Name.ToLower()));

                    if (mappedCat == null)
                    {
                        // Try to find name and Id in AllCats for sub cats that are mapped as parents
                        mappedCat = NewznabStandardCategory.AllCats.FirstOrDefault(x => x.Id == parentId && x.Name.ToLower().Contains(parentNameLower));
                    }

                    if (mappedCat == null)
                    {
                        // Try by parent id if name fails
                        mappedCat = NewznabStandardCategory.ParentCats.FirstOrDefault(x => x.Id == parentId);
                    }

                    if (mappedCat == null)
                    {
                        // Fallback to Other
                        mappedCat = NewznabStandardCategory.Other;
                    }

                    foreach (var xmlSubcat in xmlCategory.Elements("subcat"))
                    {
                        var subName = xmlSubcat.Attribute("name").Value;
                        var subId = int.Parse(xmlSubcat.Attribute("id").Value);

                        var mappingName = $"{mappedCat.Name}/{subName}";
                        var mappedSubCat = NewznabStandardCategory.AllCats.FirstOrDefault(x => x.Name.ToLower() == mappingName.ToLower());

                        if (mappedSubCat == null)
                        {
                            // Try by child id if name fails
                            mappedSubCat = NewznabStandardCategory.AllCats.FirstOrDefault(x => x.Id == subId);
                        }

                        if (mappedSubCat == null && mappedCat.Id != NewznabStandardCategory.Other.Id)
                        {
                            // Try by Parent/Other if parent is not other
                            mappedSubCat = NewznabStandardCategory.AllCats.FirstOrDefault(x => x.Name.ToLower() == $"{mappedCat.Name.ToLower()}/other");
                        }

                        if (mappedSubCat == null)
                        {
                            // Fallback to Misc Other
                            mappedSubCat = NewznabStandardCategory.OtherMisc;
                        }

                        if (mappedSubCat != null)
                        {
                            capabilities.Categories.AddCategoryMapping(subId, mappedSubCat, $"{parentName}/{subName}");
                        }
                    }

                    if (mappedCat != null)
                    {
                        capabilities.Categories.AddCategoryMapping(parentId, mappedCat, parentName);
                    }
                }
            }

            return capabilities;
        }
    }
}
