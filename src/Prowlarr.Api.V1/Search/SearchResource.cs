using System.Collections.Generic;

namespace Prowlarr.Api.V1.Search
{
    public class SearchResource
    {
        public SearchResource()
        {
            Type = "search";
            Categories = new List<int>();
        }

        public string Query { get; set; }
        public string Type { get; set; }
        public List<int> IndexerIds { get; set; }
        public List<int> Categories { get; set; }
        public int Limit { get; set; }
        public int Offset { get; set; }
    }
}
