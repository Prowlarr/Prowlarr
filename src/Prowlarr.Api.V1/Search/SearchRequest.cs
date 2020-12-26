using System.Collections.Generic;

namespace Prowlarr.Api.V1.Search
{
    public class SearchRequest
    {
        public List<int> IndexerIds { get; set; }
        public string Query { get; set; }
        public int[] Categories { get; set; }
    }
}
