using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Commands;
using Prowlarr.Http;
using Prowlarr.Http.Extensions;

namespace Prowlarr.Api.V1.Indexers
{
    [V1ApiController("indexer/editor")]
    public class IndexerEditorController : Controller
    {
        private readonly IIndexerFactory _indexerService;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IndexerResourceMapper _resourceMapper;

        public IndexerEditorController(IIndexerFactory indexerService, IManageCommandQueue commandQueueManager, IndexerResourceMapper resourceMapper)
        {
            _indexerService = indexerService;
            _commandQueueManager = commandQueueManager;
            _resourceMapper = resourceMapper;
        }

        [HttpPut]
        public IActionResult SaveAll(IndexerEditorResource resource)
        {
            var indexersToUpdate = _indexerService.All().Where(x => resource.IndexerIds.Contains(x.Id));

            foreach (var indexer in indexersToUpdate)
            {
                if (resource.Enable.HasValue)
                {
                    indexer.Enable = resource.Enable.Value;
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

            _indexerService.Update(indexersToUpdate);

            var indexers = _indexerService.All();

            foreach (var definition in indexers)
            {
                _indexerService.SetProviderCharacteristics(definition);
            }

            return Accepted(_resourceMapper.ToResource(indexers));
        }

        [HttpDelete]
        public object DeleteIndexers(IndexerEditorResource resource)
        {
            _indexerService.DeleteIndexers(resource.IndexerIds);

            return new object();
        }
    }
}
