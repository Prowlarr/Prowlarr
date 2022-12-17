using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Cardigann
{
    public class CardigannSettingsValidator : AbstractValidator<CardigannSettings>
    {
        public CardigannSettingsValidator()
        {
        }
    }

    public class CardigannSettings : NoAuthTorrentBaseSettings, IYmlIndexerSettings
    {
        private static readonly CardigannSettingsValidator Validator = new CardigannSettingsValidator();

        public CardigannSettings()
        {
            ExtraFieldData = new Dictionary<string, object>();
        }

        [FieldDefinition(0, Hidden = HiddenType.Hidden)]
        public string DefinitionFile { get; set; }

        public Dictionary<string, object> ExtraFieldData { get; set; }

        public override NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
