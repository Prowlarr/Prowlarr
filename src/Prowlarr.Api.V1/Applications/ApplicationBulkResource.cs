using System.Collections.Generic;
using NzbDrone.Core.Applications;

namespace Prowlarr.Api.V1.Applications
{
    public class ApplicationBulkResource : ProviderBulkResource<ApplicationBulkResource>
    {
        public ApplicationSyncLevel? SyncLevel { get; set; }
    }

    public class ApplicationBulkResourceMapper : ProviderBulkResourceMapper<ApplicationBulkResource, ApplicationDefinition>
    {
        public override List<ApplicationDefinition> UpdateModel(ApplicationBulkResource resource, List<ApplicationDefinition> existingDefinitions)
        {
            if (resource == null)
            {
                return new List<ApplicationDefinition>();
            }

            existingDefinitions.ForEach(existing =>
            {
                existing.SyncLevel = resource.SyncLevel ?? existing.SyncLevel;
            });

            return existingDefinitions;
        }
    }
}
