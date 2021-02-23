using System.Collections.Generic;
using System.Linq;
using Nancy;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Messaging.Commands;
using Prowlarr.Http.Extensions;

namespace Prowlarr.Api.V1.Indexers
{
    public class IndexerEditorModule : ProwlarrV1Module
    {
        private readonly IIndexerFactory _indexerService;
        private readonly IManageCommandQueue _commandQueueManager;
        public static readonly IndexerResourceMapper ResourceMapper = new IndexerResourceMapper();

        public IndexerEditorModule(IIndexerFactory indexerService, IManageCommandQueue commandQueueManager)
            : base("/indexer/editor")
        {
            _indexerService = indexerService;
            _commandQueueManager = commandQueueManager;
            Put("/", movie => SaveAll());
            Delete("/", movie => DeleteIndexers());
        }

        private object SaveAll()
        {
            var resource = Request.Body.FromJson<IndexerEditorResource>();
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

            return ResponseWithCode(ResourceMapper.ToResource(indexers), HttpStatusCode.Accepted);
        }

        private object DeleteIndexers()
        {
            var resource = Request.Body.FromJson<IndexerEditorResource>();

            _indexerService.DeleteIndexers(resource.IndexerIds);

            return new object();
        }
    }
}
