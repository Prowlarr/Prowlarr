using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Indexers;
using Prowlarr.Http.REST;

namespace Prowlarr.Api.V1.Indexers
{
    public class IndexerCapabilityResource : RestResource
    {
        public int? LimitsMax { get; set; }
        public int? LimitsDefault { get; set; }
        public List<IndexerCategory> Categories { get; set; }
        public bool SupportsRawSearch { get; set; }
        public List<SearchParam> SearchParams { get; set; }
        public List<TvSearchParam> TvSearchParams { get; set; }
        public List<MovieSearchParam> MovieSearchParams { get; set; }
        public List<MusicSearchParam> MusicSearchParams { get; set; }
        public List<BookSearchParam> BookSearchParams { get; set; }
    }

    public static class IndexerCapabilitiesResourceMapper
    {
        public static IndexerCapabilityResource ToResource(this IndexerCapabilities model)
        {
            if (model == null)
            {
                return null;
            }

            return new IndexerCapabilityResource
            {
                LimitsMax = model.LimitsMax,
                LimitsDefault = model.LimitsDefault,
                Categories = model.Categories.GetTorznabCategoryTree(),
                SupportsRawSearch = model.SupportsRawSearch,
                SearchParams = model.SearchParams,
                TvSearchParams = model.TvSearchParams,
                MovieSearchParams = model.MovieSearchParams,
                MusicSearchParams = model.MusicSearchParams,
                BookSearchParams = model.BookSearchParams
            };
        }

        public static List<IndexerCapabilityResource> ToResource(this IEnumerable<IndexerCapabilities> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
