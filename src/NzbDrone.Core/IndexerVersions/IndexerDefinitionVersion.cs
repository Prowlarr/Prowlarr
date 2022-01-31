using System;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.IndexerVersions
{
    public class IndexerDefinitionVersion : ModelBase
    {
        public string File { get; set; }
        public string Sha { get; set; }
        public DateTime LastUpdated { get; set; }
        public string DefinitionId { get; set; }
    }
}
