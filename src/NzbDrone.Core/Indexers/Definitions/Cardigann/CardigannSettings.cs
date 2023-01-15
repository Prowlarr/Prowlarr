using System.Collections.Generic;
using FluentValidation;
using FluentValidation.Results;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Cardigann
{
    public class CardigannSettings : NoAuthTorrentBaseSettings
    {
        public CardigannSettings()
        {
            ExtraFieldData = new Dictionary<string, object>();
        }

        [FieldDefinition(0, Hidden = HiddenType.Hidden)]
        public string DefinitionFile { get; set; }

        public Dictionary<string, object> ExtraFieldData { get; set; }
    }
}
