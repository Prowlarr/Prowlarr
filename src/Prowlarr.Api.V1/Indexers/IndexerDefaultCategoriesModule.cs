using NzbDrone.Core.Indexers;
using Prowlarr.Api.V1;

namespace NzbDrone.Api.V1.Indexers
{
    public class IndexerDefaultCategoriesModule : ProwlarrV1Module
    {
        public IndexerDefaultCategoriesModule()
            : base("/indexer/categories")
        {
            Get("/", movie => GetAll());
        }

        private IndexerCategory[] GetAll()
        {
            return NewznabStandardCategory.ParentCats;
        }
    }
}
