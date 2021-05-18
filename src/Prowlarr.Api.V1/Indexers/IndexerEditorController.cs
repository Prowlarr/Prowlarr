using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Commands;
using Prowlarr.Http;

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
            var indexersToUpdate = _indexerService.AllProviders(false).Select(x => (IndexerDefinition)x.Definition).Where(d => resource.IndexerIds.Contains(d.Id));

            foreach (var indexer in indexersToUpdate)
            {
                if (resource.Enable.IsNotNullOrWhiteSpace())
                {
                    indexer.Enable = bool.Parse(resource.Enable);
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
