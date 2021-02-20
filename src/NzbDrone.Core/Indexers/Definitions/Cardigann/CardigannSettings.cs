using System.Collections.Generic;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.Cardigann
{
    public class CardigannSettingsValidator : AbstractValidator<CardigannSettings>
    {
        public CardigannSettingsValidator()
        {
        }
    }

    public class CardigannSettings : IProviderConfig
    {
        private static readonly CardigannSettingsValidator Validator = new CardigannSettingsValidator();

        public CardigannSettings()
        {
            ExtraFieldData = new Dictionary<string, object>();
        }

        [FieldDefinition(0, Hidden = HiddenType.Hidden)]
        public string DefinitionFile { get; set; }

        public Dictionary<string, object> ExtraFieldData { get; set; }

        // Field 8 is used by TorznabSettings MinimumSeeders
        // If you need to add another field here, update TorznabSettings as well and this comment
        public virtual NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
