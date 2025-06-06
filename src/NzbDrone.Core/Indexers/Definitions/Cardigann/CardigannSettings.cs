using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers.Settings;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Definitions.Cardigann
{
    public class CardigannSettingsValidator : NoAuthSettingsValidator<CardigannSettings>
    {
        public CardigannSettingsValidator()
        {
            RuleFor(c => c.DefinitionFile).NotEmpty();
        }
    }

    public class CardigannSettings : NoAuthTorrentBaseSettings
    {
        private static readonly CardigannSettingsValidator Validator = new();

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
