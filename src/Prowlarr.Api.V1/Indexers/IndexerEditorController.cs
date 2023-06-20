using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Commands;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Indexers
{
    [V1ApiController("indexer/editor")]
    public class IndexerEditorController : Controller
    {
        private readonly IIndexerFactory _indexerFactory;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IndexerResourceMapper _resourceMapper;

        public IndexerEditorController(IIndexerFactory indexerFactory, IManageCommandQueue commandQueueManager, IndexerResourceMapper resourceMapper)
        {
            _indexerFactory = indexerFactory;
            _commandQueueManager = commandQueueManager;
            _resourceMapper = resourceMapper;
        }

        [HttpPut]
        [Consumes("application/json")]
        public IActionResult SaveAll(IndexerEditorResource resource)
        {
            var indexersToUpdate = _indexerFactory.AllProviders(false).Select(x => (IndexerDefinition)x.Definition).Where(d => resource.IndexerIds.Contains(d.Id));

            foreach (var indexer in indexersToUpdate)
            {
                if (resource.Enable.HasValue)
                {
                    indexer.Enable = resource.Enable.Value;
                }

                if (resource.AppProfileId.HasValue)
                {
                    indexer.AppProfileId = resource.AppProfileId.Value;
                }

                if (resource.Tags != null)
                {
                    var newTags = resource.Tags;
                    var applyTags = resource.ApplyTags;

                    switch (applyTags)
                    {
                        case ApplyTags.Add:
                            newTags.ForEach(t => indexer.Tags.Add(t));
                            break;
                        case ApplyTags.Remove:
                            newTags.ForEach(t => indexer.Tags.Remove(t));
                            break;
                        case ApplyTags.Replace:
                            indexer.Tags = new HashSet<int>(newTags);
                            break;
                    }
                }
            }

            _indexerFactory.Update(indexersToUpdate);

            var indexers = _indexerFactory.All();

            foreach (var definition in indexers)
            {
                _indexerFactory.SetProviderCharacteristics(definition);
            }

            return Accepted(_resourceMapper.ToResource(indexers));
        }

        [HttpDelete]
        [Consumes("application/json")]
        public object DeleteIndexers([FromBody] IndexerEditorResource resource)
        {
            _indexerFactory.Delete(resource.IndexerIds);

            return new { };
        }
    }
}
