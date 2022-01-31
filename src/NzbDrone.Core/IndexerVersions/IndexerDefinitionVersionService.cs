using System.Collections.Generic;
using System.Linq;

namespace NzbDrone.Core.IndexerVersions
{
    public interface IIndexerDefinitionVersionService
    {
        IndexerDefinitionVersion Get(int indexerVersionId);
        IndexerDefinitionVersion GetByDefId(string defId);
        List<IndexerDefinitionVersion> All();
        IndexerDefinitionVersion Add(IndexerDefinitionVersion defVersion);
        IndexerDefinitionVersion Upsert(IndexerDefinitionVersion defVersion);
        void Delete(int indexerVersionId);
    }

    public class IndexerDefinitionVersionService : IIndexerDefinitionVersionService
    {
        private readonly IIndexerDefinitionVersionRepository _repo;

        public IndexerDefinitionVersionService(IIndexerDefinitionVersionRepository repo)
        {
            _repo = repo;
        }

        public IndexerDefinitionVersion Get(int indexerVersionId)
        {
            return _repo.Get(indexerVersionId);
        }

        public IndexerDefinitionVersion GetByDefId(string defId)
        {
            return _repo.GetByDefId(defId);
        }

        public List<IndexerDefinitionVersion> All()
        {
            return _repo.All().ToList();
        }

        public IndexerDefinitionVersion Add(IndexerDefinitionVersion defVersion)
        {
            _repo.Insert(defVersion);

            return defVersion;
        }

        public IndexerDefinitionVersion Upsert(IndexerDefinitionVersion defVersion)
        {
            var existing = _repo.GetByDefId(defVersion.DefinitionId);

            if (existing != null)
            {
                defVersion.Id = existing.Id;
            }

            defVersion = _repo.Upsert(defVersion);

            return defVersion;
        }

        public void Delete(int indexerVersionId)
        {
            _repo.Delete(indexerVersionId);
        }
    }
}
